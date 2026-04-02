using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SplatterVault;

/// <summary>
/// Integration test harness for the SplatterVault Unity SDK.
///
/// Modes:
///   dotnet run -- --api-key KEY                       Run all safe tests
///   dotnet run -- --api-key KEY --start-session       Start a session, save ID to disk
///   dotnet run -- --api-key KEY --session-status      Check saved session status
///   dotnet run -- --api-key KEY --stop-session        Stop the saved session
///
/// The API key can be personal (sv_...) or org (sv_org_...).
/// Org keys auto-detect and route to org endpoints.
/// </summary>
class Program
{
    static int passed = 0;
    static int failed = 0;
    static int skipped = 0;
    static readonly List<string> failures = new();

    static readonly string SessionFilePath = Path.Combine(
        AppContext.BaseDirectory, ".active-session.json");

    static async Task Main(string[] args)
    {
        var config = ParseArgs(args);

        if (string.IsNullOrEmpty(config.ApiKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: --api-key is required.");
            Console.ResetColor();
            PrintUsage();
            Environment.Exit(1);
        }

        // ── Session management modes ────────────────────────────────
        if (config.StartSession || config.StopSession || config.SessionStatus)
        {
            using var client = new SplatterVaultClient(config.ApiKey, config.ApiUrl);

            if (config.StartSession)
                await HandleStartSession(client, config);
            else if (config.SessionStatus)
                await HandleSessionStatus(client);
            else if (config.StopSession)
                await HandleStopSession(client, config);
            return;
        }

        // ── Test suite mode ─────────────────────────────────────────
        Console.WriteLine("╔══════════════════════════════════════════════════╗");
        Console.WriteLine("║   SplatterVault SDK Integration Test Harness    ║");
        Console.WriteLine("╚══════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"  API URL:          {config.ApiUrl}");
        Console.WriteLine($"  API Key:          {Mask(config.ApiKey)}");
        if (config.OrgId.HasValue)
            Console.WriteLine($"  Org ID:           {config.OrgId}");
        Console.WriteLine($"  Skip destructive: {config.SkipDestructive}");
        Console.WriteLine();

        var sw = Stopwatch.StartNew();

        using var testClient = new SplatterVaultClient(config.ApiKey, config.ApiUrl);

        await RunTest("GET /credits — fetch credit balance", async () =>
        {
            var balance = await testClient.GetCreditBalanceAsync();
            Assert(balance != null, "balance should not be null");
            Console.WriteLine($"    balance={balance!.balance}, available={balance.GetAvailableBalance()}");
        });

        await RunTest("GET /credits/stats — fetch credit stats", async () =>
        {
            var stats = await testClient.GetCreditStatsAsync();
            Assert(stats != null, "stats should not be null");
            Console.WriteLine($"    balance={stats!.balance}, canStart={stats.canStartSession}, monthlyUsage={stats.monthlyUsage}");
        });

        await RunTest("GET /subscriptions — fetch subscription details", async () =>
        {
            var details = await testClient.GetSubscriptionAsync();
            Assert(details != null, "details should not be null");
            Console.WriteLine($"    current={details!.current?.tier ?? "(none)"}, total={details.all?.Count ?? 0}");
        });

        await RunTest("GET /subscriptions/usage — fetch usage stats", async () =>
        {
            var usage = await testClient.GetUsageStatsAsync();
            Assert(usage != null, "usage should not be null");
            Console.WriteLine($"    instances={usage!.currentInstances}/{usage.maxInstances}, totalSessions={usage.totalSessions}");
        });

        await RunTest("GET /game-sessions/my-sessions — list sessions", async () =>
        {
            var sessions = await testClient.GetMySessionsAsync();
            Assert(sessions != null, "sessions should not be null");
            Console.WriteLine($"    count={sessions!.Count}");
            foreach (var s in sessions)
                Console.WriteLine($"      [{s.id}] {s.gameType} {s.region} status={s.status} type={s.serverType}");
        });

        // ── Session lifecycle (destructive) ─────────────────────────
        if (config.SkipDestructive)
        {
            Skip("Credit session lifecycle (create -> get -> rename -> stop)");
            Skip("Session with custom variables");
        }
        else
        {
            await RunTest("Credit session lifecycle (create -> get -> rename -> stop)", async () =>
            {
                var req = new CreateSessionRequest
                {
                    region = Region.NYC3.ToApiString(),
                    gameKey = "sys_1774636058786_30e0fc4d",
                    friendlyName = "SDK-Test-Session"
                };

                Console.WriteLine("    Creating credit session...");
                var session = await testClient.CreateCreditSessionAsync(req);
                Assert(session != null, "created session should not be null");
                Assert(session!.id > 0, $"session id should be > 0, got {session.id}");
                Console.WriteLine($"    Created session {session.id}, status={session.status}");

                Console.WriteLine("    Fetching session by ID...");
                var fetched = await testClient.GetSessionAsync(session.id);
                Assert(fetched != null, "fetched session should not be null");
                Assert(fetched!.id == session.id, $"id mismatch: {fetched.id} != {session.id}");

                Console.WriteLine("    Updating friendly name...");
                var renamed = await testClient.UpdateCreditSessionFriendlyNameAsync(session.id, "SDK-Renamed");
                Assert(renamed != null, "renamed session should not be null");
                Assert(renamed!.friendlyName == "SDK-Renamed",
                    $"name mismatch: '{renamed.friendlyName}' != 'SDK-Renamed'");

                Console.WriteLine("    Stopping session...");
                var result = await testClient.StopCreditSessionAsync(session.id);
                Assert(result != null, "stop result should not be null");
                Console.WriteLine($"    Stopped. hours={result!.totalHours}, cost={result.totalCost}");
            });

            await RunTest("Session with custom variables", async () =>
            {
                var req = new CreateSessionRequest
                {
                    region = Region.NYC3.ToApiString(),
                    gameKey = "sys_1774636058786_30e0fc4d",
                    friendlyName = "SDK-CustomVars-Test"
                };
                req.AddCustomVariable("MAP_NAME", "speedball");
                req.AddCustomVariable("MAX_PLAYERS", 10);

                Console.WriteLine("    Creating session with custom variables...");
                var session = await testClient.CreateCreditSessionAsync(req);
                Assert(session != null, "session should not be null");
                Assert(session!.id > 0, $"session id should be > 0, got {session.id}");
                Console.WriteLine($"    Created session {session.id}");

                Console.WriteLine("    Stopping session...");
                await testClient.StopCreditSessionAsync(session.id);
                Console.WriteLine("    Stopped.");
            });
        }

        // ── Organization tests ──────────────────────────────────────
        if (config.OrgId.HasValue)
        {
            await RunTest($"GET /organizations/{config.OrgId}/credits — org credit balance", async () =>
            {
                var orgCredits = await testClient.GetOrgCreditBalanceAsync(config.OrgId);
                Assert(orgCredits != null, "org credits should not be null");
                Console.WriteLine($"    balance={orgCredits!.balance}, available={orgCredits.GetAvailableBalance()}");
            });

            await RunTest($"GET /organizations/{config.OrgId}/subscription — org subscription", async () =>
            {
                var orgSub = await testClient.GetOrgSubscriptionAsync(config.OrgId);
                Assert(orgSub != null, "org subscription should not be null");
                Console.WriteLine($"    current={orgSub!.current?.tier ?? "(none)"}");
            });
        }
        else
        {
            Skip("Org credit balance (no --org-id provided)");
            Skip("Org subscription (no --org-id provided)");
        }

        // ── Error handling tests ────────────────────────────────────
        await RunTest("Error handling — invalid session ID returns error", async () =>
        {
            try
            {
                await testClient.GetSessionAsync(999999999);
                Assert(false, "Expected exception for invalid session ID");
            }
            catch (Exception ex) when (ex is not AssertionException)
            {
                Assert(ex.Message.Contains("API Error"), $"Expected API Error, got: {ex.Message}");
                Console.WriteLine($"    Correctly threw: {Truncate(ex.Message, 80)}");
            }
        });

        await RunTest("Error handling — empty API key throws ArgumentException", async () =>
        {
            try
            {
                _ = new SplatterVaultClient("", config.ApiUrl);
                Assert(false, "Expected ArgumentException");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("    Correctly threw ArgumentException");
            }
            await Task.CompletedTask;
        });

        await RunTest("Error handling — invalid API key returns auth error", async () =>
        {
            using var badClient = new SplatterVaultClient("sv_invalid_key_12345", config.ApiUrl);
            try
            {
                await badClient.GetCreditBalanceAsync();
                Assert(false, "Expected exception for invalid API key");
            }
            catch (Exception ex) when (ex is not AssertionException)
            {
                Assert(ex.Message.Contains("401") || ex.Message.Contains("Unauthorized") || ex.Message.Contains("API Error"),
                    $"Expected auth error, got: {ex.Message}");
                Console.WriteLine($"    Correctly threw: {Truncate(ex.Message, 80)}");
            }
        });

        // ── Model helper tests (no API calls) ──────────────────────
        await RunTest("Model — CreateSessionRequest helpers + JSON serialization", async () =>
        {
            var req = new CreateSessionRequest();
            req.SetRegion(Region.LON1);
            Assert(req.region == "LON1", $"region should be LON1, got {req.region}");

            req.gameKey = "sys_test_key_123";
            Assert(req.gameKey == "sys_test_key_123", $"gameKey mismatch");

            // Verify [JsonProperty] serialization: gameKey -> gameTypeConfigKey
            var json = JsonConvert.SerializeObject(req, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            Assert(json.Contains("gameTypeConfigKey"), $"gameKey should serialize as gameTypeConfigKey, got: {Truncate(json, 100)}");
            Assert(!json.Contains("\"gameKey\""), $"gameKey field name should NOT appear in JSON");

            req.SetScheduledStartTime(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));
            Assert(req.scheduledStartTime != null, "scheduledStartTime should be set");

            req.AddCustomVariable("-mstRoomMode", "XBall");
            req.AddCustomVariable("-maxPlayers", 10);
            Assert(req.customVariables!.Count == 2, $"expected 2 custom vars, got {req.customVariables.Count}");

            req.ClearCustomVariables();
            Assert(req.customVariables == null, "custom vars should be null after clear");

            await Task.CompletedTask;
        });

        await RunTest("Model — CreateSessionRequest channel field", async () =>
        {
            var req = new CreateSessionRequest { gameKey = "sys_test_123" };
            Assert(req.channel == null, "channel should be null by default");

            req.SetChannel("production");
            Assert(req.channel == "production", $"channel should be 'production', got '{req.channel}'");

            var json = JsonConvert.SerializeObject(req, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            Assert(json.Contains("\"channel\":\"production\""), $"channel should serialize in JSON, got: {Truncate(json, 120)}");

            // Without channel set, it should not appear in JSON
            var req2 = new CreateSessionRequest { gameKey = "sys_test_456" };
            var json2 = JsonConvert.SerializeObject(req2, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            Assert(!json2.Contains("channel"), $"channel should not appear when null, got: {Truncate(json2, 120)}");

            await Task.CompletedTask;
        });

        await RunTest("Model — CreditBalance helper methods", async () =>
        {
            var balance = new CreditBalance
            {
                subscriptionBalance = 100f,
                adHocBalance = 50f,
                subscriptionCreditsFrozen = false
            };
            Assert(balance.GetAvailableBalance() == 150f, $"expected 150, got {balance.GetAvailableBalance()}");
            Assert(balance.HasEnoughCredits(60, 0.0223f), "should have enough for 60 min");

            balance.subscriptionCreditsFrozen = true;
            Assert(balance.GetAvailableBalance() == 50f, $"frozen: expected 50, got {balance.GetAvailableBalance()}");

            await Task.CompletedTask;
        });

        await RunTest("Model — GameSession status helpers", async () =>
        {
            var session = new GameSession { status = "Active" };
            Assert(session.IsActive(), "should be active");
            Assert(!session.IsPending(), "should not be pending");

            session.status = "Pending";
            Assert(session.IsPending(), "should be pending");

            session.status = "Not Active";
            Assert(session.IsStopped(), "should be stopped");

            session.status = "Scheduled";
            Assert(session.IsScheduled(), "should be scheduled");

            Assert(session.GetServerPort() == 8100, "default port should be 8100");
            session.slavePort = 9000;
            Assert(session.GetServerPort() == 9000, "port should be 9000");

            await Task.CompletedTask;
        });

        // ── Summary ─────────────────────────────────────────────────
        sw.Stop();
        Console.WriteLine();
        Console.WriteLine("══════════════════════════════════════════════════");
        Console.Write("  Results: ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"{passed} passed");
        Console.ResetColor();
        if (failed > 0)
        {
            Console.Write(", ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{failed} failed");
            Console.ResetColor();
        }
        if (skipped > 0)
        {
            Console.Write(", ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{skipped} skipped");
            Console.ResetColor();
        }
        Console.WriteLine($"  ({sw.ElapsedMilliseconds}ms)");
        Console.WriteLine("══════════════════════════════════════════════════");

        if (failures.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failures:");
            foreach (var f in failures)
                Console.WriteLine($"  x {f}");
            Console.ResetColor();
        }

        Environment.Exit(failed > 0 ? 1 : 0);
    }

    // ── Session management handlers ─────────────────────────────────

    static async Task HandleStartSession(SplatterVaultClient client, TestConfig config)
    {
        if (File.Exists(SessionFilePath))
        {
            var existing = JsonConvert.DeserializeObject<SavedSession>(File.ReadAllText(SessionFilePath));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"WARNING: Active session already saved (ID: {existing?.SessionId}, type: {existing?.SessionType})");
            Console.ResetColor();
            Console.Write("Start a new session anyway? [y/N]: ");
            var answer = Console.ReadLine()?.Trim().ToLower();
            if (answer != "y" && answer != "yes")
            {
                Console.WriteLine("Aborted. Use --session-status to check it or --stop-session to stop it.");
                return;
            }
        }

        var req = new CreateSessionRequest
        {
            region = (config.Region ?? "NYC3"),
            gameKey = config.GameKey ?? "sys_1774636058786_30e0fc4d",
            friendlyName = config.FriendlyName ?? $"SDK-Test-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            channel = config.Channel
        };

        Console.WriteLine($"Starting {config.SessionType} session...");
        Console.WriteLine($"  Game Key: {req.gameKey}");
        Console.WriteLine($"  Region:   {req.region}");
        Console.WriteLine($"  Name:     {req.friendlyName}");
        if (!string.IsNullOrEmpty(req.channel))
            Console.WriteLine($"  Channel:  {req.channel}");
        Console.WriteLine();

        try
        {
            GameSession session;
            if (config.SessionType == "subscription")
                session = await client.CreateSubscriptionSessionAsync(req);
            else
                session = await client.CreateCreditSessionAsync(req);

            var saved = new SavedSession
            {
                SessionId = session.id,
                SessionType = config.SessionType,
                StartedAt = DateTime.UtcNow.ToString("o"),
                GameKey = req.gameKey,
                Region = req.region,
                FriendlyName = req.friendlyName
            };
            File.WriteAllText(SessionFilePath, JsonConvert.SerializeObject(saved, Formatting.Indented));

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Session started successfully!");
            Console.ResetColor();
            Console.WriteLine($"  ID:     {session.id}");
            Console.WriteLine($"  Code:   {session.code}");
            Console.WriteLine($"  Status: {session.status}");
            Console.WriteLine($"  Type:   {session.serverType}");
            Console.WriteLine();
            Console.WriteLine($"Session saved to: {SessionFilePath}");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("  dotnet run -- --api-key <key> --session-status   Check if it's ready");
            Console.WriteLine("  dotnet run -- --api-key <key> --stop-session     Stop it when done");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed to start session: {ex.Message}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    static async Task HandleSessionStatus(SplatterVaultClient client)
    {
        var saved = LoadSavedSession();
        if (saved == null) return;

        Console.WriteLine($"Checking session {saved.SessionId}...");
        Console.WriteLine();

        try
        {
            var session = await client.GetSessionAsync(saved.SessionId);

            Console.WriteLine($"  ID:            {session.id}");
            Console.WriteLine($"  Code:          {session.code}");
            Console.WriteLine($"  Status:        {session.status}");
            Console.WriteLine($"  Type:          {session.serverType}");
            Console.WriteLine($"  Game:          {session.gameType}");
            Console.WriteLine($"  Mode:          {session.mode}");
            Console.WriteLine($"  Region:        {session.region}");
            Console.WriteLine($"  Name:          {session.friendlyName}");
            Console.WriteLine($"  IP:            {session.slaveIp ?? "(not assigned)"}");
            Console.WriteLine($"  Port:          {(session.slavePort.HasValue ? session.slavePort.ToString() : "(not assigned)")}");
            Console.WriteLine($"  Server Start:  {session.serverStart ?? "(not started)"}");
            Console.WriteLine($"  Started at:    {saved.StartedAt}");

            if (!string.IsNullOrEmpty(saved.StartedAt))
            {
                var started = DateTime.Parse(saved.StartedAt);
                var elapsed = DateTime.UtcNow - started;
                Console.WriteLine($"  Elapsed:       {elapsed.Hours}h {elapsed.Minutes}m {elapsed.Seconds}s");
            }

            Console.WriteLine();
            if (session.IsActive())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Session is ACTIVE and ready.");
                Console.ResetColor();
                if (!string.IsNullOrEmpty(session.slaveIp))
                    Console.WriteLine($"Connect to: {session.slaveIp}:{session.GetServerPort()}");
            }
            else if (session.IsPending())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Session is PENDING — server is still provisioning...");
                Console.ResetColor();
            }
            else if (session.IsStopped())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Session is STOPPED.");
                Console.ResetColor();
                Console.WriteLine("Cleaning up saved session file.");
                File.Delete(SessionFilePath);
            }
            else
            {
                Console.WriteLine($"Session status: {session.status}");
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed to get session status: {ex.Message}");
            Console.ResetColor();
        }
    }

    static async Task HandleStopSession(SplatterVaultClient client, TestConfig config)
    {
        var saved = LoadSavedSession();
        if (saved == null) return;

        Console.WriteLine($"Stopping session {saved.SessionId} ({saved.SessionType})...");

        try
        {
            // Fetch current state first
            var session = await client.GetSessionAsync(saved.SessionId);

            if (session.IsStopped())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Session is already stopped.");
                Console.ResetColor();
                File.Delete(SessionFilePath);
                return;
            }

            if (!string.IsNullOrEmpty(saved.StartedAt))
            {
                var started = DateTime.Parse(saved.StartedAt);
                var elapsed = DateTime.UtcNow - started;
                Console.WriteLine($"  Session ran for: {elapsed.Hours}h {elapsed.Minutes}m {elapsed.Seconds}s");
            }

            var type = saved.SessionType ?? session.serverType ?? "credit";
            if (type.Equals("subscription", StringComparison.OrdinalIgnoreCase))
            {
                var stopped = await client.StopSubscriptionSessionAsync(saved.SessionId);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Session stopped.");
                Console.ResetColor();
                Console.WriteLine($"  Final status: {stopped.status}");
            }
            else
            {
                var result = await client.StopCreditSessionAsync(saved.SessionId);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Session stopped.");
                Console.ResetColor();
                Console.WriteLine($"  Total hours:  {result.totalHours}");
                Console.WriteLine($"  Total cost:   {result.totalCost} credits");
            }

            File.Delete(SessionFilePath);
            Console.WriteLine();
            Console.WriteLine("Saved session file cleaned up.");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed to stop session: {ex.Message}");
            Console.ResetColor();
        }
    }

    static SavedSession? LoadSavedSession()
    {
        if (!File.Exists(SessionFilePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No saved session found. Start one first with --start-session");
            Console.ResetColor();
            return null;
        }

        return JsonConvert.DeserializeObject<SavedSession>(File.ReadAllText(SessionFilePath));
    }

    // ── Test runner helpers ──────────────────────────────────────────

    static async Task RunTest(string name, Func<Task> test)
    {
        Console.Write($"  > {name} ... ");
        try
        {
            await test();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("PASS");
            Console.ResetColor();
            passed++;
        }
        catch (AssertionException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAIL: {ex.Message}");
            Console.ResetColor();
            failed++;
            failures.Add($"{name}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.ResetColor();
            failed++;
            failures.Add($"{name}: {ex.Message}");
        }
    }

    static void Skip(string name)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  - {name} ... SKIP");
        Console.ResetColor();
        skipped++;
    }

    static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new AssertionException(message);
    }

    static string Mask(string? key)
    {
        if (string.IsNullOrEmpty(key) || key.Length < 8) return "***";
        return key[..6] + "..." + key[^4..];
    }

    static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";

    static void PrintUsage()
    {
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run -- --api-key <key>                      Run all safe tests");
        Console.WriteLine("  dotnet run -- --api-key <key> --start-session      Start a session (saved to disk)");
        Console.WriteLine("  dotnet run -- --api-key <key> --session-status     Check saved session status");
        Console.WriteLine("  dotnet run -- --api-key <key> --stop-session       Stop the saved session");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --api-url <url>           API base URL (default: https://splattervault.com/rest)");
        Console.WriteLine("  --api-key <key>           API key (sv_... or sv_org_...)");
        Console.WriteLine("  --org-id <id>             Organization ID for org-scoped tests");
        Console.WriteLine("  --skip-destructive        Skip session create/stop in test suite");
        Console.WriteLine("  --session-type <type>     credit or subscription (default: credit)");
        Console.WriteLine("  --game-key <key>          Game config key (e.g., sys_1774636058786_30e0fc4d)");
        Console.WriteLine("  --region <region>         NYC3, LON1, TOR1, etc.");
        Console.WriteLine("  --mode <mode>             XBall, NXL, Deathmatch, etc.");
        Console.WriteLine("  --friendly-name <name>    Session friendly name");
        Console.WriteLine("  --channel <name>          Build channel (e.g., production, beta, dev)");
    }

    // ── Argument parsing ────────────────────────────────────────────

    static TestConfig ParseArgs(string[] args)
    {
        var config = new TestConfig();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--api-url" when i + 1 < args.Length:
                    config.ApiUrl = args[++i];
                    break;
                case "--api-key" when i + 1 < args.Length:
                    config.ApiKey = args[++i];
                    break;
                case "--org-id" when i + 1 < args.Length:
                    config.OrgId = int.Parse(args[++i]);
                    break;
                case "--skip-destructive":
                    config.SkipDestructive = true;
                    break;
                case "--start-session":
                    config.StartSession = true;
                    break;
                case "--stop-session":
                    config.StopSession = true;
                    break;
                case "--session-status":
                    config.SessionStatus = true;
                    break;
                case "--session-type" when i + 1 < args.Length:
                    config.SessionType = args[++i].ToLower();
                    break;
                case "--game-key" when i + 1 < args.Length:
                    config.GameKey = args[++i];
                    break;
                case "--region" when i + 1 < args.Length:
                    config.Region = args[++i];
                    break;
                // --mode removed: mode is now a launch arg override via customVariables
                case "--friendly-name" when i + 1 < args.Length:
                    config.FriendlyName = args[++i];
                    break;
                case "--channel" when i + 1 < args.Length:
                    config.Channel = args[++i];
                    break;
            }
        }

        return config;
    }
}

class TestConfig
{
    public string ApiUrl { get; set; } = "https://splattervault.com/rest";
    public string? ApiKey { get; set; }
    public int? OrgId { get; set; }
    public bool SkipDestructive { get; set; }

    // Session management modes
    public bool StartSession { get; set; }
    public bool StopSession { get; set; }
    public bool SessionStatus { get; set; }

    // Session creation options
    public string SessionType { get; set; } = "credit";
    public string? GameKey { get; set; }
    public string? Region { get; set; }
    public string? FriendlyName { get; set; }
    public string? Channel { get; set; }
}

class SavedSession
{
    public int SessionId { get; set; }
    public string? SessionType { get; set; }
    public string? StartedAt { get; set; }
    public string? GameKey { get; set; }
    public string? Region { get; set; }
    public string? FriendlyName { get; set; }
}

class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}
