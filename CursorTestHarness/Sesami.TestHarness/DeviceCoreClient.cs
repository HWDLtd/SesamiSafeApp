using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Sesami.TestHarness;

public sealed class DeviceCoreClient : IDisposable
{
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly HttpClient _http;
    private readonly HttpClientHandler _handler;
    private readonly string _baseUrl;
    private List<int> _permissionIds = [];

    public DeviceCoreClient(string baseUrl, string? certificatePath)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _handler = new HttpClientHandler();
        var handler = _handler;
        var root = LoadRoot(certificatePath);
        if (root is not null)
        {
            handler.ServerCertificateCustomValidationCallback = (_, cert, chain, errors) =>
            {
                if (errors == SslPolicyErrors.None)
                    return true;
                if (cert is null || chain is null)
                    return false;
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(root);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                return chain.Build(cert);
            };
        }

        _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
    }

    public string BaseUrl => _baseUrl;
    public JsonElement LoginResponse { get; private set; }
    public string? Token { get; private set; }
    public string? PersonId { get; private set; }
    public string? SessionId { get; private set; }
    public bool IsSystemUser { get; private set; }
    public JsonElement Session { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    public HttpMessageHandler CreateHandler()
    {
        var handler = new HttpClientHandler();
        if (_handler.ServerCertificateCustomValidationCallback is { } callback)
            handler.ServerCertificateCustomValidationCallback = callback;
        return handler;
    }

    public async Task<JsonDocument> GetAsync(string path, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _baseUrl + Normalize(path));
        ApplyHeaders(request, hasBody: false);
        return await SendAsync(request, cancellationToken);
    }

    public async Task<JsonDocument> PostAsync(string path, object? body, CancellationToken cancellationToken = default, params int[] acceptStatuses)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + Normalize(path));
        var json = body is null ? "{}" : JsonSerializer.Serialize(body, Json);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        ApplyHeaders(request, hasBody: true);
        return await SendAsync(request, cancellationToken, acceptStatuses);
    }

    public async Task LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var types = await GetAsync("/api/v1/type/userSessionTypeCodes", cancellationToken);
        var local = types.RootElement.GetProperty("typeGenerics").EnumerateArray()
            .First(x => x.GetProperty("name").GetString() == "Local");

        using var response = await PostAsync("/api/v1/authentication/login", new
        {
            Username = username,
            Password = password,
            UserSessionType = new
            {
                Id = local.GetProperty("id").GetInt32(),
                Name = local.GetProperty("name").GetString(),
                DisplayNameKey = local.GetProperty("displayNameKey").GetString()
            }
        }, cancellationToken);

        var root = response.RootElement;
        LoginResponse = root.Clone();
        IsSystemUser = root.GetProperty("isSystemUser").GetBoolean();
        Session = root.GetProperty("userSession").Clone();
        Token = Session.GetProperty("apiToken").GetString();
        PersonId = Session.GetProperty("personId").GetString();
        SessionId = Session.GetProperty("id").GetString();

        if (IsSystemUser)
            _permissionIds = await LoadAllPermissionIdsAsync(cancellationToken);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        if (!IsLoggedIn)
            return;
        await PostAsync("/api/v1/authentication/logout", new { }, cancellationToken);
        Token = null;
        PersonId = null;
        SessionId = null;
        Session = default;
        LoginResponse = default;
        _permissionIds = [];
    }

    public void Dispose() => _http.Dispose();

    private async Task<List<int>> LoadAllPermissionIdsAsync(CancellationToken cancellationToken)
    {
        using var doc = await GetAsync("/api/v1/type/permissionCodes", cancellationToken);
        return doc.RootElement.GetProperty("typePermissionCodes").EnumerateArray()
            .Select(x => x.GetProperty("id").GetInt32())
            .ToList();
    }

    private void ApplyHeaders(HttpRequestMessage request, bool hasBody)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!IsLoggedIn)
            return;

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        request.Headers.TryAddWithoutValidation("UserSession", Session.GetRawText());
        request.Headers.TryAddWithoutValidation("UserPermissions", JsonSerializer.Serialize(_permissionIds));
        if (!hasBody)
            request.Content = null;
    }

    private async Task<JsonDocument> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken, params int[] acceptStatuses)
    {
        using var response = await _http.SendAsync(request, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        var status = (int)response.StatusCode;
        if (!response.IsSuccessStatusCode && !acceptStatuses.Contains(status))
        {
            throw new InvalidOperationException(
                $"{(int)response.StatusCode} {request.Method} {request.RequestUri}{Environment.NewLine}{text}");
        }

        if (string.IsNullOrWhiteSpace(text))
            return JsonDocument.Parse("{}");
        return JsonDocument.Parse(text);
    }

    private static string Normalize(string path) => path.StartsWith('/') ? path : "/" + path;

    private static X509Certificate2? LoadRoot(string? certificatePath)
    {
        var path = certificatePath;
        if (string.IsNullOrWhiteSpace(path))
            path = FindCertificate();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Console.WriteLine("No Sesami CA certificate found. TLS validation will use the machine trust store.");
            return null;
        }

        Console.WriteLine($"Trusting Sesami CA from {path}");
        var pem = File.ReadAllText(path);
        return X509Certificate2.CreateFromPem(pem);
    }

    private static string? FindCertificate()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var pem = Path.Combine(dir.FullName, "ca.cert.pem");
            if (File.Exists(pem))
                return pem;
            var zip = Path.Combine(dir.FullName, "ca.cert.zip");
            if (File.Exists(zip))
            {
                var folder = Path.Combine(Path.GetTempPath(), "sesami-ca");
                Directory.CreateDirectory(folder);
                var nested = Path.Combine(folder, "ca.cert.pem");
                if (!File.Exists(nested))
                    System.IO.Compression.ZipFile.ExtractToDirectory(zip, folder, overwriteFiles: true);
                if (File.Exists(nested))
                    return nested;
            }
            dir = dir.Parent;
        }
        return null;
    }
}
