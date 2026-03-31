# SplatterVault Unity SDK

Official Unity SDK for creating and managing dedicated game servers through the SplatterVault platform. Supports credit-based and subscription-based sessions, organization billing, dynamic game configuration, and Master Server Toolkit integration.

## Installation

### Unity Package Manager (recommended)

In Unity: **Window > Package Manager > + > Add package from git URL** and enter:

```
https://github.com/ksc4130/splattervault-unity-sdk.git
```

Or add directly to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.splattervault.sdk": "https://github.com/ksc4130/splattervault-unity-sdk.git#v3.0.0"
  }
}
```

### Manual

Copy the `Runtime/` folder into your Unity project's `Assets/SplatterVault/` directory.

### Requirements

- Unity 2019.4 or later
- .NET 4.x or .NET Standard 2.0
- Newtonsoft.Json (included with Unity 2020+, or install via Package Manager)
- Internet access enabled in Player Settings

## Quick Start

```csharp
using SplatterVault;

// 1. Create a client with your API key
var client = new SplatterVaultClient("sv_your_api_key");

// 2. Discover available game options
var args = await client.GetConfigurableArgsAsync("your_game_key");
// Returns configurable launch arguments: mode (select), maxPlayers (number), etc.

// 3. Create a session
var request = new CreateSessionRequest
{
    gameKey = "your_game_key",   // from your SplatterVault dashboard
    friendlyName = "My Server"
};
request.AddCustomVariable("-mstRoomMode", "XBall");  // set game mode
request.AddCustomVariable("-maxPlayers", "10");       // set max players

GameSession session = await client.CreateCreditSessionAsync(request);
Debug.Log($"Server starting! Code: {session.code}");

// 4. Wait for server to be ready
while (session.IsPending())
{
    await Task.Delay(5000);
    session = await client.GetSessionAsync(session.id);
}

// 5. Connect players
if (session.IsActive())
    Debug.Log($"Connect to {session.slaveIp}:{session.GetServerPort()}");

// 6. Stop when done
StopSessionResult result = await client.StopCreditSessionAsync(session.id);
Debug.Log($"Cost: {result.totalCost} credits ({result.totalHours} hours)");
```

## Step-by-Step Guide

### Step 1: Get Your API Key and Game Key

1. Sign up at [splattervault.com](https://splattervault.com)
2. Go to your dashboard and generate an API key
   - **Personal key** (`sv_...`): bills to your personal credits
   - **Organization key** (`sv_org_...`): bills to your org's credits
3. Find your **game key** in the game configuration section (e.g., `sys_1774636058786_30e0fc4d`)

### Step 2: Initialize the Client

```csharp
using SplatterVault;

// Personal API key
var client = new SplatterVaultClient("sv_your_api_key");

// Organization API key with org ID
var client = new SplatterVaultClient("sv_org_your_key", organizationId: 1);

// Custom API URL (for local development)
var client = new SplatterVaultClient("sv_your_key", baseUrl: "http://localhost:3000/rest");
```

### Step 3: Discover Game Options

Each game defines configurable launch arguments. Query them to build dynamic UI:

```csharp
var args = await client.GetConfigurableArgsAsync("your_game_key");

foreach (var arg in args)
{
    Debug.Log($"{arg.label} ({arg.type}): {arg.description}");

    if (arg.type == "select" && arg.options != null)
    {
        foreach (var opt in arg.options)
            Debug.Log($"  - {opt.label} = {opt.value}");
    }

    if (arg.type == "number")
        Debug.Log($"  Range: {arg.min} - {arg.max}");
}
```

**Argument types:**
| Type | Description | UI Widget |
|------|-------------|-----------|
| `select` | Predefined choices (e.g., game mode) | Dropdown |
| `number` | Numeric with min/max (e.g., max players) | Slider / Input |
| `text` | Free text with optional regex validation | Text field |
| `boolean` | Toggle (e.g., friendly fire) | Checkbox |
| `hidden` | Platform-managed, not shown to users | None |

Arguments with `semantic: "mode"` represent the game mode selection.

### Step 4: Create a Session

```csharp
var request = new CreateSessionRequest
{
    gameKey = "your_game_key",
    isPublic = false,
    friendlyName = "Practice Match"
};
request.SetRegion(Region.NYC3);

// Pass any configurable arguments as custom variables
request.AddCustomVariable("-mstRoomMode", "XBall");
request.AddCustomVariable("-maxPlayers", "10");

// Set auto-stop time (optional)
request.SetScheduledEndTime(DateTime.UtcNow.AddHours(2));

// Create with credits or subscription
GameSession session = await client.CreateCreditSessionAsync(request);
// or: await client.CreateSubscriptionSessionAsync(request);

Debug.Log($"Session {session.id} created, status: {session.status}");
```

### Step 5: Monitor Session Status

Sessions go through these states: **Scheduled** -> **Pending** -> **Active** -> **Not Active**

```csharp
var session = await client.GetSessionAsync(sessionId);

if (session.IsPending())
    Debug.Log("Server is provisioning...");
else if (session.IsActive())
    Debug.Log($"Ready! Connect to {session.slaveIp}:{session.GetServerPort()}");
else if (session.IsStopped())
    Debug.Log("Session has ended");
```

### Step 6: Stop a Session

```csharp
// By session ID (credit sessions return billing info)
StopSessionResult result = await client.StopCreditSessionAsync(session.id);
Debug.Log($"Billed: {result.totalCost} credits for {result.totalHours} hours");

// Auto-route by server type (credit vs subscription)
await client.StopSessionAsync(session);
```

### Step 7: Check Your Balance

```csharp
// Personal credits
CreditBalance balance = await client.GetCreditBalanceAsync();
Debug.Log($"Available: {balance.GetAvailableBalance()} credits");
Debug.Log($"~{balance.GetBalanceInHours(0.25f):F1} hours at 0.25 credits/min");

// Organization credits
OrgCreditStats orgBalance = await client.GetOrgCreditBalanceAsync();
Debug.Log($"Org balance: {orgBalance.GetAvailableBalance()}");
```

## Examples

### List Active Sessions

```csharp
var sessions = await client.GetMySessionsAsync();
foreach (var s in sessions)
{
    Debug.Log($"[{s.id}] {s.friendlyName ?? s.code} - {s.status} ({s.gameType})");
    if (s.IsActive())
        Debug.Log($"  Connect: {s.slaveIp}:{s.GetServerPort()}");
}
```

### Schedule a Tournament

```csharp
DateTime startTime = DateTime.UtcNow.AddHours(1);

for (int i = 0; i < 4; i++)
{
    var matchStart = startTime.AddMinutes(i * 40);
    var matchEnd = matchStart.AddMinutes(30);

    var request = new CreateSessionRequest
    {
        gameKey = "your_game_key",
        isPublic = true,
        friendlyName = $"Match {i + 1}"
    };
    request.SetScheduledStartTime(matchStart);
    request.SetScheduledEndTime(matchEnd);

    var session = await client.CreateCreditSessionAsync(request);
    Debug.Log($"Scheduled match {i + 1}: code {session.code}, starts {matchStart:HH:mm}");
}
```

### Organization Server Management

```csharp
// Org API keys auto-scope all requests to the organization
var client = new SplatterVaultClient("sv_org_your_key", 1);

// Session creation bills to org credits automatically
var session = await client.CreateCreditSessionAsync(new CreateSessionRequest
{
    gameKey = "your_game_key",
    friendlyName = "Team Practice"
});

// Check org balance
var credits = await client.GetOrgCreditBalanceAsync();
Debug.Log($"Org has {credits.GetAvailableBalance()} credits remaining");

// Check org subscription
var sub = await client.GetOrgSubscriptionAsync();
Debug.Log($"Subscription: {sub.current?.tier ?? "none"}");
```

### Dynamic Game Configuration UI

```csharp
// Build a settings panel from available arguments
var args = await client.GetConfigurableArgsAsync("your_game_key");
var overrides = new Dictionary<string, object>();

foreach (var arg in args)
{
    switch (arg.type)
    {
        case "select":
            // Show dropdown with arg.options
            string selected = ShowDropdown(arg.label, arg.options);
            overrides[arg.flag] = selected;
            break;

        case "number":
            // Show slider with arg.min / arg.max
            float value = ShowSlider(arg.label, arg.min ?? 0, arg.max ?? 100);
            overrides[arg.flag] = value.ToString();
            break;

        case "boolean":
            // Show toggle
            bool enabled = ShowToggle(arg.label);
            overrides[arg.flag] = enabled ? (arg.trueValue ?? "True") : (arg.falseValue ?? "False");
            break;

        case "text":
            // Show text input
            string text = ShowTextField(arg.label);
            overrides[arg.flag] = text;
            break;
    }
}

// Create session with user's selections
var request = new CreateSessionRequest { gameKey = "your_game_key" };
request.SetCustomVariables(overrides);
var session = await client.CreateCreditSessionAsync(request);
```

### MST Integration

The SDK includes extension methods for Master Server Toolkit:

```csharp
using SplatterVault.MST;

// Create server and wait for it to be ready (polls every 5s, up to 5 min)
MSTServerInfo serverInfo = await client.CreateAndWaitForMSTServer(
    new CreateSessionRequest { gameKey = "your_game_key", isPublic = true },
    onProgress: (status) => Debug.Log(status)
);

// Register with MST
var roomOptions = new RoomOptions
{
    Name = serverInfo.serverName,
    RoomIp = serverInfo.ipAddress,
    RoomPort = serverInfo.port,
    MaxPlayers = serverInfo.maxPlayers,
    Properties = serverInfo.ToMSTProperties()
};
```

## API Reference

### SplatterVaultClient

#### Constructor
```csharp
new SplatterVaultClient(string apiKey)
new SplatterVaultClient(string apiKey, int organizationId)
new SplatterVaultClient(string apiKey, string baseUrl)  // override base URL
```

#### Properties
| Property | Type | Description |
|----------|------|-------------|
| `IsOrganizationKey` | `bool` | True for `sv_org_` keys |
| `OrganizationId` | `int?` | Org ID for org-scoped endpoints |

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetConfigurableArgsAsync(gameKey)` | `List<StructuredLaunchArg>` | Get available launch arguments for a game |
| `CreateCreditSessionAsync(request)` | `GameSession` | Create credit-billed session |
| `CreateSubscriptionSessionAsync(request)` | `GameSession` | Create subscription-billed session |
| `GetSessionAsync(sessionId)` | `GameSession` | Get session details |
| `GetMySessionsAsync()` | `List<GameSession>` | List your sessions |
| `StopCreditSessionAsync(sessionId)` | `StopSessionResult` | Stop credit session (returns billing) |
| `StopSubscriptionSessionAsync(sessionId)` | `GameSession` | Stop subscription session |
| `StopSessionAsync(session)` | `StopSessionResult` | Auto-routes by serverType |
| `UpdateSessionFriendlyNameAsync(session, name)` | `GameSession` | Rename a session |
| `UpdateSessionScheduleAsync(session, start, end)` | `GameSession` | Update schedule times |
| `CancelSessionScheduleAsync(session)` | `GameSession` | Cancel scheduled session |
| `GetCreditBalanceAsync()` | `CreditBalance` | Get personal credit balance |
| `GetCreditStatsAsync()` | `CreditStats` | Get detailed credit stats |
| `GetSubscriptionAsync()` | `SubscriptionDetails` | Get subscription info |
| `GetUsageStatsAsync()` | `UsageStats` | Get usage statistics |
| `GetOrgCreditBalanceAsync(orgId?)` | `OrgCreditStats` | Get org credit balance |
| `GetOrgSubscriptionAsync(orgId?)` | `OrgSubscriptionInfo` | Get org subscription |

All methods also accept optional `Action<T> onSuccess` and `Action<string> onError` callbacks.

### Models

#### CreateSessionRequest
| Field | Type | Description |
|-------|------|-------------|
| `gameKey` | `string` | Game config key (required) |
| `region` | `string` | Server region (default: "NYC3") |
| `isPublic` | `bool` | Public visibility (default: false) |
| `friendlyName` | `string` | Display name |
| `scheduledStartTime` | `string` | ISO 8601 start time |
| `scheduledEndTime` | `string` | ISO 8601 auto-stop time |
| `serverSizeId` | `int?` | Server size (defaults per game) |
| `organizationId` | `int?` | Org billing (auto-injected for org keys) |
| `buildId` | `int?` | Specific game build |
| `customVariables` | `Dictionary<string, object>` | Launch argument overrides |

#### GameSession (response)
| Field | Type | Description |
|-------|------|-------------|
| `id` | `int` | Session ID |
| `code` | `string` | Join code |
| `status` | `string` | Pending, Active, Scheduled, Not Active |
| `gameType` | `string` | Resolved game type name |
| `mode` | `string` | Resolved game mode |
| `region` | `string` | Server region |
| `slaveIp` | `string` | Server IP address |
| `slavePort` | `int?` | Server port |
| `serverType` | `string` | Credit or Subscription |
| `serverSize` | `ServerSizeInfo` | Size details with creditsPerMinute |
| `friendlyName` | `string` | Display name |
| `organizationId` | `int?` | Owning organization |

**Helpers:** `IsActive()`, `IsPending()`, `IsScheduled()`, `IsStopped()`, `GetServerPort()`, `GetScheduledStartTime()`, `GetScheduledEndTime()`

#### StructuredLaunchArg
| Field | Type | Description |
|-------|------|-------------|
| `flag` | `string` | Argument flag (e.g., "-maxPlayers") |
| `type` | `string` | text, number, boolean, select, hidden |
| `label` | `string` | Display label |
| `description` | `string` | Help text |
| `required` | `bool` | Must be provided |
| `options` | `List<SelectOption>` | Choices for select type |
| `min` / `max` | `float?` | Range for number type |
| `semantic` | `string` | Hint: "mode", "password", "serverName" |

### Regions

```csharp
Region.NYC1   // New York 1
Region.NYC3   // New York 3
Region.TOR1   // Toronto
Region.SFO1   // San Francisco 1
Region.SFO2   // San Francisco 2
Region.SFO3   // San Francisco 3
Region.LON1   // London
```

## Error Handling

All SDK methods throw exceptions on failure. Use try/catch or the callback pattern:

```csharp
// Try/catch pattern
try
{
    var session = await client.CreateCreditSessionAsync(request);
}
catch (Exception ex)
{
    // ex.Message contains: "API Error (403): Insufficient credits to start a session"
    Debug.LogError(ex.Message);
}

// Callback pattern
await client.CreateCreditSessionAsync(request,
    onSuccess: (session) => Debug.Log($"Created: {session.code}"),
    onError: (error) => Debug.LogError(error)
);
```

**Common errors:**
| Code | Meaning |
|------|---------|
| 401 | Invalid or expired API key |
| 403 | Insufficient credits or permissions |
| 404 | Session or game config not found |
| 500 | Server error (report to support) |

## Troubleshooting

### "API key is required"
Set your API key before creating the client. Don't commit keys to version control.

### "Game type configuration not found"
Your `gameKey` doesn't match an active config. Check it on your dashboard.

### "Insufficient credits"
Check balance with `GetCreditBalanceAsync()`. Purchase credits on the dashboard.

### IL2CPP Builds (iOS/Android)
Add a `link.xml` to prevent code stripping:
```xml
<linker>
  <assembly fullname="Newtonsoft.Json" preserve="all"/>
  <assembly fullname="SplatterVault.Runtime" preserve="all"/>
</linker>
```

## Support

- Dashboard: [splattervault.com](https://splattervault.com)
- Report bugs: `/reportbug` in Discord
- API docs: [splattervault.com/#/api-docs](https://splattervault.com/#/api-docs)

## License

MIT License. See [LICENSE.md](LICENSE.md) for details.
