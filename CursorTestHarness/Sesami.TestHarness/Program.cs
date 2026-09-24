using System.Text.Json;
using Sesami.TestHarness;

var baseUrl = Environment.GetEnvironmentVariable("SESAMI_BASE_URL") ?? "https://devicecore.io";
var username = Environment.GetEnvironmentVariable("SESAMI_USERNAME") ?? "1234";
var password = Environment.GetEnvironmentVariable("SESAMI_PASSWORD") ?? "1234";
var certificate = Environment.GetEnvironmentVariable("SESAMI_CA_CERT");

if (args.Length > 0 && args[0] is "-h" or "--help" or "help")
{
    PrintHelp();
    return;
}

using var api = new DeviceCoreClient(baseUrl, certificate);
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

if (args.Length > 0)
{
    await RunCommandAsync(args[0], args.Skip(1).ToArray());
    return;
}

await RunMenuAsync();

async Task RunMenuAsync()
{
    Console.WriteLine("Sesami Device Core");
    Console.WriteLine(baseUrl);
    Console.WriteLine();
    try
    {
        await Workflows.ShowDeviceAuthenticationAsync(api, cts.Token);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not read device authentication methods: {ex.Message}");
    }
    Console.WriteLine();

    while (!cts.IsCancellationRequested)
    {
        PrintStatus();
        Console.WriteLine("1  Login                 workflow 3001, stage 6001");
        Console.WriteLine("2  Startup");
        Console.WriteLine("3  Incomplete workflow   show workflow and stage numbers");
        Console.WriteLine("4  Abort incomplete workflow");
        Console.WriteLine("5  Add Cash              workflow 1004, stage 1000 then stage 1007");
        Console.WriteLine("6  Logout                workflow 3002, stage 6002");
        Console.WriteLine("7  Full session          workflow 3001, then workflow 3002");
        Console.WriteLine("Q  Quit");
        Console.WriteLine();
        var choice = Prompt("Choose").Trim().ToLowerInvariant();
        if (choice is "q" or "quit" or "0")
            break;

        try
        {
            switch (choice)
            {
                case "1":
                    var user = Prompt($"Username [{username}]");
                    if (string.IsNullOrWhiteSpace(user))
                        user = username;
                    var pin = PromptSecret($"Password [{new string('*', password.Length)}]");
                    if (string.IsNullOrEmpty(pin))
                        pin = password;
                    username = user;
                    password = pin;
                    await Workflows.LoginAsync(api, username, password, cts.Token);
                    break;
                case "2":
                    await RequireLogin();
                    await Workflows.StartupAsync(api, cts.Token);
                    break;
                case "3":
                    await RequireLogin();
                    await Workflows.ReportIncompleteAsync(api, cts.Token);
                    break;
                case "4":
                    if (!Confirm("Abort every incomplete workflow?"))
                        break;
                    await RequireLogin();
                    await Workflows.AbortIncompleteAsync(api, cts.Token);
                    break;
                case "5":
                    if (!Confirm("Start workflow 1004 Add Cash? The backend returns each stage number."))
                        break;
                    await RequireLogin();
                    await Workflows.AcceptCashAsync(api, cts.Token);
                    break;
                case "6":
                    if (!api.IsLoggedIn)
                    {
                        Console.WriteLine("Not signed in.");
                        break;
                    }
                    if (!Confirm("Start workflow 3002 Logout?"))
                        break;
                    await Workflows.LogoutAsync(api, cts.Token);
                    break;
                case "7":
                    if (!Confirm("Run workflow 3001 Login, startup, then workflow 3002 Logout?"))
                        break;
                    await Workflows.LoginAsync(api, username, password, cts.Token);
                    await Workflows.StartupAsync(api, cts.Token);
                    await Workflows.LogoutAsync(api, cts.Token);
                    break;
                default:
                    Console.WriteLine("Enter a number from the list, or Q to quit.");
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Cancelled.");
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine(ex.Message);
        }

        if (!cts.IsCancellationRequested)
            Prompt("Press Enter to return to the menu");
        Console.WriteLine();
    }
}

async Task RunCommandAsync(string command, string[] _)
{
    try
    {
        switch (command.ToLowerInvariant())
        {
            case "startup":
                await RequireLogin();
                await Workflows.StartupAsync(api, cts.Token);
                break;
            case "login":
                await Workflows.LoginAsync(api, username, password, cts.Token);
                break;
            case "incomplete":
                await RequireLogin();
                await Workflows.ReportIncompleteAsync(api, cts.Token);
                break;
            case "abort":
                await RequireLogin();
                await Workflows.AbortIncompleteAsync(api, cts.Token);
                break;
            case "accept-cash":
                await RequireLogin();
                await Workflows.AcceptCashAsync(api, cts.Token);
                break;
            case "logout":
                await RequireLogin();
                await Workflows.LogoutAsync(api, cts.Token);
                break;
            case "session":
                await Workflows.LoginAsync(api, username, password, cts.Token);
                await Workflows.StartupAsync(api, cts.Token);
                await Workflows.LogoutAsync(api, cts.Token);
                break;
            default:
                Console.Error.WriteLine($"Unknown command '{command}'.");
                PrintHelp();
                Environment.ExitCode = 2;
                break;
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        Environment.ExitCode = 1;
    }
}

async Task RequireLogin()
{
    if (!api.IsLoggedIn)
    {
        Console.WriteLine($"Signing in as {username}");
        await api.LoginAsync(username, password, cts.Token);
        Workflows.PrintLoggedInUser(api);
    }
}

void PrintStatus()
{
    if (!api.IsLoggedIn)
    {
        Console.WriteLine("Not signed in");
        return;
    }

    var name = username;
    string? loginName = null;
    if (api.Session.TryGetProperty("person", out var person))
    {
        if (person.TryGetProperty("displayName", out var display) &&
            display.GetString() is { Length: > 0 } shown)
            name = shown;
        if (person.TryGetProperty("username", out var userName))
            loginName = userName.GetString();
    }

    if (loginName is { Length: > 0 } && !string.Equals(loginName, name, StringComparison.Ordinal))
        Console.WriteLine($"Signed in as {name} ({loginName})");
    else
        Console.WriteLine($"Signed in as {name}");

    if (api.LoginResponse.ValueKind == JsonValueKind.Object &&
        api.LoginResponse.TryGetProperty("requiresPinChange", out var pin))
        Console.WriteLine($"System user: {api.IsSystemUser}    PIN change required: {pin.GetBoolean()}");
}

static string Prompt(string label)
{
    Console.Write($"{label}: ");
    return Console.ReadLine() ?? "";
}

static string PromptSecret(string label)
{
    if (Console.IsInputRedirected)
        return Prompt(label);

    Console.Write($"{label}: ");
    var buffer = new System.Text.StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return buffer.ToString();
        }
        if (key.Key == ConsoleKey.Backspace)
        {
            if (buffer.Length > 0)
            {
                buffer.Length--;
                Console.Write("\b \b");
            }
            continue;
        }
        if (!char.IsControl(key.KeyChar))
        {
            buffer.Append(key.KeyChar);
            Console.Write('*');
        }
    }
}

static bool Confirm(string question)
{
    var answer = Prompt($"{question} [y/N]").Trim();
    return answer.Equals("y", StringComparison.OrdinalIgnoreCase) ||
           answer.Equals("yes", StringComparison.OrdinalIgnoreCase);
}

static void PrintHelp()
{
    Console.WriteLine("""
        Sesami Device Core test harness

        With no arguments, the app opens an interactive menu.

        Usage:
          dotnet run --project Sesami.TestHarness
          dotnet run --project Sesami.TestHarness -- <command>

        Commands:
          startup       Load configuration, templates, abort orphans, log start
          login         Workflow 3001 Login, then check for a workflow to resume
          incomplete    Show the workflow and stage numbers still open
          abort         Abort incomplete workflows until none remain
          accept-cash   Workflow 1004. Follow each stage number the backend returns
          logout        Workflow 3002 Logout, then end the API session
          session       Workflow 3001, startup, workflow 3002

        Environment:
          SESAMI_BASE_URL   default https://devicecore.io
          SESAMI_USERNAME   default 1234
          SESAMI_PASSWORD   default 1234
          SESAMI_CA_CERT    path to ca.cert.pem (otherwise searched upward from the app)
        """);
}
