using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.SignalR.Client;

namespace Sesami.TestHarness;

public static class Codes
{
    public const int Login = 3001;
    public const int Logout = 3002;
    public const int AddCash = 1004;
    public const int AcceptCash = 1000;
    public const int PrintReceipt = 1007;
    public const int Aborted = 3;
    public const int TimedOut = 4;
}

public static class Workflows
{
    public static async Task StartupAsync(DeviceCoreClient api, CancellationToken cancellationToken)
    {
        Console.WriteLine("Aborting orphaned workflows");
        await api.PostAsync("/api/v1/workflow/workFlowAbortOrphans", new { }, cancellationToken);

        using var config = await api.GetAsync("/api/v1/configuration/getApplicationConfiguration", cancellationToken);
        var appConfig = config.RootElement.GetProperty("appConfig");
        PrintAuthenticationMethods(appConfig);
        var general = appConfig.GetProperty("generalConfiguration");
        Console.WriteLine($"Store: {general.GetProperty("storeName").GetString()} @ {general.GetProperty("storeLocation").GetString()}");
        Console.WriteLine($"Workflow template name: {general.GetProperty("workFlow").GetString()}");
        Console.WriteLine($"First time setup complete: {general.GetProperty("isFirstTimeSetupComplete").GetBoolean()}");
        Console.WriteLine($"Theme: {general.GetProperty("applicationTheme").GetString()}  Language: {general.GetProperty("language").GetString()}");

        using var templates = await api.GetAsync("/api/v1/workflow/getWorkFlowTemplates", cancellationToken);
        var template = templates.RootElement.GetProperty("workFlowTemplates").EnumerateArray().First();
        var flows = template.GetProperty("workFlows").EnumerateArray()
            .Select(x => x.GetProperty("workFlowCodeId").GetInt32())
            .Order()
            .ToArray();
        Console.WriteLine($"Active template: {template.GetProperty("name").GetString()} v{template.GetProperty("version").GetString()}");
        Console.WriteLine($"Configured workflow codes: {string.Join(", ", flows)}");

        await api.PostAsync("/api/v1/logging/logInfo", new
        {
            timestamp = DateTime.UtcNow.ToString("o"),
            type = "Info",
            message = "Sesami test harness application start"
        }, cancellationToken);
        Console.WriteLine("Logged application start");
    }

    public static async Task ShowDeviceAuthenticationAsync(DeviceCoreClient api, CancellationToken cancellationToken)
    {
        using var config = await api.GetAsync("/api/v1/configuration/getApplicationConfiguration", cancellationToken);
        PrintAuthenticationMethods(config.RootElement.GetProperty("appConfig"));
    }

    private static void PrintAuthenticationMethods(JsonElement appConfig)
    {
        Console.WriteLine("Authentication methods on this safe:");
        var authentication = appConfig.GetProperty("authenticationConfiguration");
        PrintProperty(authentication, "authenticationConfiguration.allowNFCLogin", "allowNFCLogin");
        PrintProperty(authentication, "authenticationConfiguration.allowBarcodeLogin", "allowBarcodeLogin");
        PrintProperty(authentication, "authenticationConfiguration.allowRFIDLogin", "allowRFIDLogin");
        PrintNested(authentication, "authenticationConfiguration.pacnConfiguration.isEnabled", "pacnConfiguration", "isEnabled");
        var courier = appConfig.GetProperty("courierConfiguration");
        PrintNested(courier, "courierConfiguration.cracnConfiguration.isEnabled", "cracnConfiguration", "isEnabled");
        Console.WriteLine("Device configuration:");
        var node = JsonNode.Parse(appConfig.GetRawText());
        RedactSecrets(node);
        Console.WriteLine(node?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static readonly HashSet<string> SecretNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "key", "secret", "password", "apiToken"
    };

    private static void RedactSecrets(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToList())
            {
                if (SecretNames.Contains(property.Key) && property.Value is JsonValue)
                    obj[property.Key] = "***";
                else
                    RedactSecrets(property.Value);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
                RedactSecrets(item);
        }
    }

    private static void PrintProperty(JsonElement element, string label, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            Console.WriteLine($"  {label}: (not in config)");
            return;
        }
        Console.WriteLine($"  {label}: {value.GetRawText()}");
    }

    private static void PrintNested(JsonElement element, string label, string objectName, string propertyName)
    {
        if (!element.TryGetProperty(objectName, out var child) || child.ValueKind != JsonValueKind.Object)
        {
            Console.WriteLine($"  {label}: (not in config)");
            return;
        }
        PrintProperty(child, label, propertyName);
    }

    public static async Task<bool> LoginAsync(DeviceCoreClient api, string username, string password, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Logging in as {username}");
        await api.LoginAsync(username, password, cancellationToken);
        PrintLoggedInUser(api);

        Console.WriteLine("Starting workflow 3001 Login");
        using var started = await api.PostAsync("/api/v1/workflow/workFlowStart", new
        {
            WorkFlowCodeId = Codes.Login,
            UserSessionId = api.SessionId,
            PersonId = api.PersonId
        }, cancellationToken);
        var stageId = started.RootElement.GetProperty("workFlowExecutionContext")
            .GetProperty("currentWorkFlowStageInstanceId").GetString();
        using var completed = await api.PostAsync("/api/v1/workflow/workFlowStageComplete", new
        {
            WorkFlowStageInstanceId = stageId,
            WorkFlowStageData = (string?)null
        }, cancellationToken);
        var ctx = completed.RootElement.GetProperty("workFlowExecutionContext");
        PrintContext(ctx);

        using var config = await api.GetAsync("/api/v1/configuration/getApplicationConfiguration", cancellationToken);
        var fts = config.RootElement.GetProperty("appConfig").GetProperty("generalConfiguration")
            .GetProperty("isFirstTimeSetupComplete").GetBoolean();
        Console.WriteLine($"First time setup complete: {fts}");
        if (!fts)
        {
            Console.WriteLine("First time setup is not complete. The emulator UI would start workflow 1 (FirstTimeSetup) for a system user.");
            return false;
        }

        return await ReportIncompleteAsync(api, cancellationToken);
    }

    public static void PrintLoggedInUser(DeviceCoreClient api)
    {
        var login = api.LoginResponse;
        Console.WriteLine($"Session {api.SessionId} person {api.PersonId} systemUser={api.IsSystemUser}");
        if (login.ValueKind != JsonValueKind.Object)
            return;

        if (login.TryGetProperty("requiresPinChange", out var pin))
            Console.WriteLine($"Requires PIN change: {pin.GetBoolean()}");
        if (login.TryGetProperty("typeUserSessionTypeCodeId", out var sessionType))
            Console.WriteLine($"Session type: {sessionType.GetInt32()}");

        if (!login.TryGetProperty("userSession", out var session) || session.ValueKind != JsonValueKind.Object)
        {
            PrintLoginTimeouts(login);
            return;
        }

        if (session.TryGetProperty("sessionStart", out var start) && start.ValueKind == JsonValueKind.String)
            Console.WriteLine($"Session start: {start.GetString()}");
        if (session.TryGetProperty("person", out var person) && person.ValueKind == JsonValueKind.Object)
            PrintPerson(person);
        PrintLoginTimeouts(login);
    }

    private static void PrintPerson(JsonElement person)
    {
        var node = JsonNode.Parse(person.GetRawText());
        RedactSecrets(node);
        Console.WriteLine("User:");
        Console.WriteLine(node?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    public static async Task<bool> ReportIncompleteAsync(DeviceCoreClient api, CancellationToken cancellationToken)
    {
        using var doc = await api.GetAsync($"/api/v1/workflow/getIncompleteWorkFlow/{api.PersonId}", cancellationToken);
        if (!doc.RootElement.TryGetProperty("workFlowResume", out var resume) || resume.ValueKind is JsonValueKind.Null)
        {
            Console.WriteLine("No incomplete workflow.");
            return false;
        }

        var context = resume.GetProperty("workFlowExecutionContext");
        Console.WriteLine(
            $"Incomplete workflow code {context.GetProperty("currentWorkFlowCodeId").GetInt32()} " +
            $"stage {context.GetProperty("currentWorkFlowStageCodeId").GetInt32()} " +
            $"instance {context.GetProperty("currentWorkFlowInstanceId").GetString()}");
        return true;
    }

    public static async Task AbortIncompleteAsync(DeviceCoreClient api, CancellationToken cancellationToken)
    {
        var guard = 0;
        while (await ReportIncompleteAsync(api, cancellationToken))
        {
            if (++guard > 10)
                throw new InvalidOperationException("Abort chain did not clear incomplete workflows.");

            using var doc = await api.GetAsync($"/api/v1/workflow/getIncompleteWorkFlow/{api.PersonId}", cancellationToken);
            var context = doc.RootElement.GetProperty("workFlowResume").GetProperty("workFlowExecutionContext");
            var stageId = context.GetProperty("currentWorkFlowStageInstanceId").GetString();
            Console.WriteLine(
                $"Aborting workflow {context.GetProperty("currentWorkFlowCodeId").GetInt32()} " +
                $"stage {context.GetProperty("currentWorkFlowStageCodeId").GetInt32()}");
            await api.PostAsync("/api/v1/workflow/workFlowAbort", new
            {
                WorkFlowStageInstanceId = stageId,
                WorkFlowExecutionStatusCodeId = Codes.Aborted,
                WorkFlowStageAbortReason = "Test harness abort"
            }, cancellationToken);
        }
    }

    public static async Task LogoutAsync(DeviceCoreClient api, CancellationToken cancellationToken)
    {
        if (api.IsLoggedIn)
        {
            Console.WriteLine("Starting workflow 3002 Logout");
            using var started = await api.PostAsync("/api/v1/workflow/workFlowStart", new
            {
                WorkFlowCodeId = Codes.Logout,
                UserSessionId = api.SessionId,
                PersonId = api.PersonId
            }, cancellationToken);
            var context = started.RootElement.GetProperty("workFlowExecutionContext");
            var stageCode = context.GetProperty("currentWorkFlowStageCodeId").GetInt32();
            var stageId = context.GetProperty("currentWorkFlowStageInstanceId").GetString();
            Console.WriteLine($"Workflow 3002 stage {stageCode}, completing {stageId}");
            await api.PostAsync("/api/v1/workflow/workFlowStageComplete", new
            {
                WorkFlowStageInstanceId = stageId,
                WorkFlowStageData = (string?)null
            }, cancellationToken);
        }

        Console.WriteLine("Calling authentication logout");
        await api.LogoutAsync(cancellationToken);
        Console.WriteLine("Logged out");
    }

    public static async Task AcceptCashAsync(DeviceCoreClient api, CancellationToken cancellationToken)
    {
        if (await ReportIncompleteAsync(api, cancellationToken))
        {
            Console.WriteLine("Clearing the incomplete workflow before workflow 1004.");
            await AbortIncompleteAsync(api, cancellationToken);
        }

        var userTimeout = ReadUserTimeout(api.LoginResponse);
        var stageTimeout = await ReadStageTimeoutAsync(api, Codes.AcceptCash, cancellationToken);
        Console.WriteLine("Timeouts from the login response:");
        PrintLoginTimeouts(api.LoginResponse);
        Console.WriteLine($"User timeout: {FormatTimeout(userTimeout)}");
        Console.WriteLine($"Stage timeout: {FormatTimeout(stageTimeout)}");
        Console.WriteLine("Hardware timeout: the device publishes FinancialTransactionCompletedEvent");

        Console.WriteLine("Starting workflow 1004 Add Cash");
        using var started = await api.PostAsync("/api/v1/workflow/workFlowStart", new
        {
            WorkFlowCodeId = Codes.AddCash,
            UserSessionId = api.SessionId,
            PersonId = api.PersonId
        }, cancellationToken);
        var context = started.RootElement.GetProperty("workFlowExecutionContext").Clone();
        var deposits = new List<JsonElement>();

        while (!context.GetProperty("isWorkFlowComplete").GetBoolean())
        {
            PrintContext(context);
            var stageCode = context.GetProperty("currentWorkFlowStageCodeId").GetInt32();
            var stageId = context.GetProperty("currentWorkFlowStageInstanceId").GetString()
                ?? throw new InvalidOperationException("The backend did not return a stage instance.");
            var workflowInstanceId = context.GetProperty("currentWorkFlowInstanceId").GetString();

            string? stageData;
            if (stageCode == Codes.AcceptCash)
            {
                stageData = await RunAcceptCashStageAsync(api, stageId, userTimeout, stageTimeout, deposits, cancellationToken);
            }
            else if (stageCode == Codes.PrintReceipt)
            {
                stageData = await RunPrintReceiptStageAsync(api, workflowInstanceId, deposits, cancellationToken);
            }
            else
            {
                Console.WriteLine($"Workflow {context.GetProperty("currentWorkFlowCodeId").GetInt32()} stage {stageCode} is not implemented. Leaving the workflow on this stage.");
                return;
            }

            if (stageData is null)
                return;

            using var completed = await api.PostAsync("/api/v1/workflow/workFlowStageComplete", new
            {
                WorkFlowStageInstanceId = stageId,
                WorkFlowStageData = stageData
            }, cancellationToken);
            context = completed.RootElement.GetProperty("workFlowExecutionContext").Clone();
        }

        PrintContext(context);
        Console.WriteLine("Workflow 1004 finished.");
    }

    private static async Task<string?> RunAcceptCashStageAsync(
        DeviceCoreClient api,
        string stageId,
        TimeSpan? userTimeout,
        TimeSpan? stageTimeout,
        List<JsonElement> deposits,
        CancellationToken cancellationToken)
    {
        using var periods = await api.GetAsync("/api/v1/accountingPeriod/getOpenAccountingPeriods", cancellationToken);
        string? accountingPeriodId = null;
        if (periods.RootElement.TryGetProperty("accountingPeriods", out var periodList))
        {
            var first = periodList.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Object && first.TryGetProperty("id", out var id))
                accountingPeriodId = id.GetString();
        }
        Console.WriteLine($"Open accounting period: {accountingPeriodId ?? "(none)"}");

        using var types = await api.GetAsync("/api/v1/type/transactionTypeCodes", cancellationToken);
        var depositId = types.RootElement.GetProperty("typeGenerics").EnumerateArray()
            .First(x => x.GetProperty("name").GetString() == "Deposit")
            .GetProperty("id").GetInt32();

        Console.WriteLine("Creating accept transactions for BNR, BCR, SS");
        using var created = await api.PostAsync("/api/v1/transaction/createAcceptTransactions", new
        {
            WorkFlowStageInstanceId = stageId,
            AccountingPeriodId = accountingPeriodId,
            DepartmentId = (string?)null,
            RegisterId = (string?)null,
            ComponentCodes = new[] { "BNR", "BCR", "SS" },
            TransactionTypeCodeId = depositId,
            TransactionSubTypeCodeId = (int?)null
        }, cancellationToken);

        var open = created.RootElement.TryGetProperty("deviceTransactions", out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray().Select(x => x.Clone()).ToList()
            : [];
        Console.WriteLine($"Devices ready to accept: {open.Count}");
        foreach (var tx in open)
            Console.WriteLine(tx.GetRawText());

        if (open.Count == 0)
        {
            Console.WriteLine("No acceptance devices. Aborting workflow 1004 stage 1000.");
            await AbortStageAsync(api, stageId, Codes.Aborted, "No devices available to accept cash", cancellationToken);
            return null;
        }

        var gate = new object();
        var stopRequested = false;
        await using var hub = await NotificationListener.ConnectAsync(api, cancellationToken);
        hub.On("FinancialTransactionCompletedEvent", payload =>
        {
            var financial = payload.TryGetProperty("message", out var message) &&
                            message.TryGetProperty("transactionFinancial", out var transaction)
                ? transaction
                : payload;
            var copy = financial.Clone();
            lock (gate)
            {
                var index = open.FindIndex(device => SameDevice(device, copy));
                if (index < 0)
                {
                    Console.WriteLine("FinancialTransactionCompletedEvent for a device that was not started.");
                    return;
                }

                open.RemoveAt(index);
                deposits.Add(copy);
                var hasError = copy.TryGetProperty("transactionError", out var transactionError) &&
                               transactionError.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined;
                Console.WriteLine(
                    $"FinancialTransactionCompletedEvent transaction {copy.GetProperty("id").GetInt32()} " +
                    $"asset {copy.GetProperty("assetId").GetString()}. {open.Count} device(s) still open.");
                if (hasError)
                {
                    Console.WriteLine($"Transaction error: {transactionError}");
                    if (copy.TryGetProperty("transactionStatus", out var transactionStatus) &&
                        transactionStatus.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
                        Console.WriteLine($"Transaction status: {transactionStatus.GetRawText()}");
                }
            }
        });
        hub.On("NoteRecyclerNoteProcessedEvent", payload =>
        {
            Console.WriteLine("NoteRecyclerNoteProcessedEvent");
            Console.WriteLine(payload.GetRawText());
        });

        Console.WriteLine("Starting acceptance");
        await api.PostAsync("/api/v1/recycler/startAcceptCash", new { DeviceTransactions = open.ToArray() }, cancellationToken);
        Console.WriteLine("S stops acceptance. Q aborts the stage. A keypress resets the user timeout.");

        var userDeadline = userTimeout is null ? (DateTime?)null : DateTime.UtcNow + userTimeout.Value;
        var stageDeadline = stageTimeout is null ? (DateTime?)null : DateTime.UtcNow + stageTimeout.Value;
        var reader = Console.IsInputRedirected ? Console.In.ReadLineAsync() : null;

        while (true)
        {
            lock (gate)
            {
                if (open.Count == 0)
                    break;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (userDeadline is not null && DateTime.UtcNow >= userDeadline)
            {
                Console.WriteLine("User timeout. Aborting the stage and logging out.");
                await AbortStageAsync(api, stageId, Codes.TimedOut, "User timeout", CancellationToken.None);
                await LogoutAsync(api, CancellationToken.None);
                return null;
            }

            if (!stopRequested && stageDeadline is not null && DateTime.UtcNow >= stageDeadline)
            {
                Console.WriteLine("Stage timeout. Stopping devices that are still open.");
                await StopOpenDevicesAsync(api, open, gate);
                stopRequested = true;
            }

            var key = await ReadControlKeyAsync(reader);
            if (key is not null)
            {
                if (userTimeout is not null)
                    userDeadline = DateTime.UtcNow + userTimeout.Value;
                if (Console.IsInputRedirected)
                    reader = Console.In.ReadLineAsync();

                if (key is "s" && !stopRequested)
                {
                    Console.WriteLine("Stop requested. Waiting for each open device to finish.");
                    await StopOpenDevicesAsync(api, open, gate);
                    stopRequested = true;
                }
                else if (key is "q")
                {
                    Console.WriteLine("Aborting workflow 1004 stage 1000.");
                    await AbortStageAsync(api, stageId, Codes.Aborted, "User aborted acceptance", cancellationToken);
                    return null;
                }
            }

            await Task.Delay(200, cancellationToken);
        }

        Console.WriteLine("Every started device has completed.");
        return JsonSerializer.Serialize(new { completedTransactions = deposits });
    }

    private static async Task<string?> RunPrintReceiptStageAsync(
        DeviceCoreClient api,
        string? workflowInstanceId,
        List<JsonElement> deposits,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("Workflow 1004 stage 1007 Print Receipt");
        using var codes = await api.GetAsync("/api/v1/type/workFlowCodes", cancellationToken);
        var addCashCode = codes.RootElement.GetProperty("typeWorkFlowCodes").EnumerateArray()
            .First(x => x.GetProperty("id").GetInt32() == Codes.AddCash)
            .Clone();
        using var receipt = await api.PostAsync("/api/v1/receipt/addCashReceipt", new
        {
            WorkFlowInstanceId = workflowInstanceId,
            WorkFlowCode = addCashCode,
            DeviceTransactions = deposits,
            IsPrinted = true,
            IsStored = true,
            NumberOfPrints = 1
        }, cancellationToken, 503);
        if (receipt.RootElement.TryGetProperty("type", out var receiptType) && receiptType.ValueKind == JsonValueKind.Object)
            Console.WriteLine($"Receipt not printed: {receiptType.GetProperty("name").GetString()}. Completing the stage anyway.");

        return JsonSerializer.Serialize(new { completedTransactions = deposits });
    }

    private static async Task StopOpenDevicesAsync(DeviceCoreClient api, List<JsonElement> open, object gate)
    {
        JsonElement[] pending;
        lock (gate)
            pending = open.ToArray();
        if (pending.Length == 0)
            return;
        await api.PostAsync("/api/v1/recycler/stopAcceptCash", new { DeviceTransactions = pending }, CancellationToken.None);
    }

    private static async Task AbortStageAsync(
        DeviceCoreClient api,
        string stageId,
        int statusCode,
        string reason,
        CancellationToken cancellationToken)
    {
        await api.PostAsync("/api/v1/workflow/workFlowAbort", new
        {
            WorkFlowStageInstanceId = stageId,
            WorkFlowExecutionStatusCodeId = statusCode,
            WorkFlowStageAbortReason = reason
        }, cancellationToken);
    }

    private static async Task<string?> ReadControlKeyAsync(Task<string?>? redirectedLine)
    {
        if (redirectedLine is not null)
        {
            if (!redirectedLine.IsCompleted)
                return null;
            return (await redirectedLine)?.Trim().ToLowerInvariant();
        }

        if (!Console.KeyAvailable)
            return null;
        var key = Console.ReadKey(intercept: true);
        return char.ToLowerInvariant(key.KeyChar).ToString();
    }

    private static void PrintLoginTimeouts(JsonElement login)
    {
        var found = new List<string>();
        CollectTimeouts(login, "login", found);
        if (found.Count == 0)
        {
            Console.WriteLine("Login response timeout fields: none");
            return;
        }

        foreach (var line in found)
            Console.WriteLine(line);
    }

    private static void CollectTimeouts(JsonElement element, string path, List<string> found)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;
        foreach (var property in element.EnumerateObject())
        {
            var child = path + "." + property.Name;
            if (property.Name.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                found.Add($"  {child}: {property.Value}");
            if (property.Value.ValueKind == JsonValueKind.Object)
                CollectTimeouts(property.Value, child, found);
            else if (property.Value.ValueKind == JsonValueKind.Array)
            {
                var index = 0;
                foreach (var item in property.Value.EnumerateArray())
                {
                    CollectTimeouts(item, $"{child}[{index}]", found);
                    index++;
                }
            }
        }
    }

    private static TimeSpan? ReadUserTimeout(JsonElement login)
    {
        if (login.ValueKind != JsonValueKind.Object ||
            !login.TryGetProperty("userSession", out var session) ||
            !session.TryGetProperty("person", out var person) ||
            !person.TryGetProperty("userTimeout", out var personTimeout) ||
            !TryParseTimeout(personTimeout, out var timeout))
            return null;
        return timeout;
    }

    private static async Task<TimeSpan?> ReadStageTimeoutAsync(DeviceCoreClient api, int stageCode, CancellationToken cancellationToken)
    {
        using var templates = await api.GetAsync("/api/v1/workflow/getWorkFlowTemplates", cancellationToken);
        var template = templates.RootElement.GetProperty("workFlowTemplates").EnumerateArray().First();
        if (!template.TryGetProperty("workFlowStageConfigurations", out var configurations))
            return null;
        var match = configurations.EnumerateArray()
            .FirstOrDefault(x => x.GetProperty("workFlowStageCodeId").GetInt32() == stageCode);
        if (match.ValueKind != JsonValueKind.Object || !match.TryGetProperty("workFlowStageParameters", out var parameters))
            return null;
        var duration = parameters.EnumerateArray()
            .FirstOrDefault(x => string.Equals(x.GetProperty("key").GetString(), "timeoutDuration", StringComparison.OrdinalIgnoreCase));
        if (duration.ValueKind != JsonValueKind.Object)
            return null;
        return TryParseTimeout(duration.GetProperty("value"), out var timeout) ? timeout : null;
    }

    private static bool TryParseTimeout(JsonElement value, out TimeSpan timeout)
    {
        timeout = default;
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return false;
        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        if (string.IsNullOrWhiteSpace(text) || text == "0")
            return false;
        if (int.TryParse(text, out var seconds))
        {
            if (seconds <= 0)
                return false;
            timeout = TimeSpan.FromSeconds(seconds);
            return true;
        }
        if (TimeSpan.TryParse(text, out timeout))
            return timeout > TimeSpan.Zero;
        return false;
    }

    private static string FormatTimeout(TimeSpan? timeout) =>
        timeout is null ? "off" : timeout.Value.ToString();

    private static bool SameDevice(JsonElement started, JsonElement financial)
    {
        var startedComponent = ComponentKey(started);
        var financialComponent = ComponentKey(financial);
        if (startedComponent is not null && startedComponent == financialComponent)
            return true;
        var startedAsset = AssetKey(started);
        var financialAsset = AssetKey(financial);
        return startedAsset is not null && startedAsset == financialAsset;
    }

    private static string? ComponentKey(JsonElement element)
    {
        if (element.TryGetProperty("componentId", out var componentId) && componentId.ValueKind == JsonValueKind.String)
            return componentId.GetString();
        if (element.TryGetProperty("componentCode", out var code) && code.ValueKind == JsonValueKind.Object &&
            code.TryGetProperty("id", out var id))
            return id.ToString();
        return null;
    }

    private static string? AssetKey(JsonElement element) =>
        element.TryGetProperty("assetId", out var assetId) && assetId.ValueKind == JsonValueKind.String
            ? assetId.GetString()
            : null;

    private static void PrintContext(JsonElement context)
    {
        Console.WriteLine(
            $"Workflow {context.GetProperty("currentWorkFlowCodeId").GetInt32()} " +
            $"stage {context.GetProperty("currentWorkFlowStageCodeId").GetInt32()} " +
            $"position {context.GetProperty("currentPosition").GetInt32()} " +
            $"complete={context.GetProperty("isWorkFlowComplete").GetBoolean()}");
    }
}

public sealed class NotificationListener : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly List<IDisposable> _subscriptions = [];

    private NotificationListener(HubConnection connection) => _connection = connection;

    public static async Task<NotificationListener> ConnectAsync(DeviceCoreClient api, CancellationToken cancellationToken)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(api.BaseUrl + "/signalr/notificationHub", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(api.Token);
                options.HttpMessageHandlerFactory = _ => api.CreateHandler();
                options.Headers["UserSession"] = api.Session.GetRawText();
                options.Headers["UserPermissions"] = "[]";
            })
            .WithAutomaticReconnect()
            .Build();

        await connection.StartAsync(cancellationToken);
        Console.WriteLine($"SignalR connected: {connection.State}");
        return new NotificationListener(connection);
    }

    public void On(string eventName, Action<JsonElement> handler)
    {
        _subscriptions.Add(_connection.On<JsonElement>(eventName, handler));
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var subscription in _subscriptions)
            subscription.Dispose();
        await _connection.DisposeAsync();
    }
}
