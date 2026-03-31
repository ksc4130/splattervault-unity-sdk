# SplatterVault Unity SDK

Unity SDK for interacting with the SplatterVault API to create and manage game sessions programmatically.

## Installation

### Unity Package Manager (recommended)

Add to your `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.splattervault.sdk": "https://github.com/ksc4130/splattervault-unity-sdk.git"
  }
}
```

Or in Unity: **Window > Package Manager > + > Add package from git URL** and enter:
```
https://github.com/ksc4130/splattervault-unity-sdk.git
```

### Manual
1. Copy the `SplatterVault` folder into your Unity project's `Assets` folder
2. The SDK requires Unity 2019.4 or later
3. Ensure your project has internet access enabled in Player Settings

## Quick Start

```csharp
using SplatterVault;

// Personal API key
var client = new SplatterVaultClient("sv_your_api_key_here");

// Or organization API key (auto-scopes all requests to the org)
var orgClient = new SplatterVaultClient("sv_org_your_key_here", 1);

// Create a session
var request = new CreateSessionRequest
{
    region = "NYC3",
    gameType = "PaintballPlayground",
    mode = "XBall",
    isPublic = false,
    friendlyName = "My Unity Game Session"
};

GameSession session = await client.CreateCreditSessionAsync(request);
Debug.Log($"Session created! Code: {session.code}, Status: {session.status}");

// Check status later
GameSession updated = await client.GetSessionAsync(session.id);
if (updated.IsActive())
    Debug.Log($"Connect to: {updated.slaveIp}:{updated.GetServerPort()}");

// Stop when done
StopSessionResult result = await client.StopCreditSessionAsync(session.id);
Debug.Log($"Cost: {result.totalCost} credits for {result.totalHours} hours");
```

## Features

- Create credit-based and subscription-based game sessions
- Get session details and list user sessions
- Stop sessions (auto-routes by server type)
- Update friendly names and schedules
- Scheduling support (start/end times)
- Credit balance and usage stats
- **Organization API key support** — `sv_org_` keys auto-scope to org
- Organization credit balance and subscription info
- Custom game type configurations with variables
- Full async/await support
- Newtonsoft.Json-based deserialization
- Master Server Toolkit (MST) integration

## API Reference

### SplatterVaultClient

Main client class for API interactions.

#### Constructor
```csharp
// Personal API key
var client = new SplatterVaultClient("sv_your_key");

// Organization API key with explicit org ID
var client = new SplatterVaultClient("sv_org_your_key", organizationId: 1);

// Optional: override base URL (defaults to https://splattervault.com/rest)
var client = new SplatterVaultClient("sv_your_key", baseUrl: "http://localhost:3000/rest");
```

#### Properties
```csharp
bool IsOrganizationKey { get; }     // true for sv_org_ keys
int? OrganizationId { get; set; }   // org ID for org-scoped endpoints
```

#### Methods

**CreateCreditSessionAsync**
```csharp
public async Task<GameSession> CreateCreditSessionAsync(
    CreateSessionRequest request,
    Action<GameSession> onSuccess = null,
    Action<string> onError = null
)
```

**CreateSubscriptionSessionAsync**
```csharp
public async Task<GameSession> CreateSubscriptionSessionAsync(
    CreateSessionRequest request,
    Action<GameSession> onSuccess = null,
    Action<string> onError = null
)
```

**GetSessionAsync**
```csharp
public async Task<GameSession> GetSessionAsync(
    int sessionId,
    Action<GameSession> onSuccess = null,
    Action<string> onError = null
)
```

**GetMySessionsAsync**
```csharp
public async Task<List<GameSession>> GetMySessionsAsync(
    Action<List<GameSession>> onSuccess = null,
    Action<string> onError = null
)
```

**StopSessionAsync**
```csharp
public async Task<bool> StopSessionAsync(
    int sessionId,
    Action<bool> onSuccess = null,
    Action<string> onError = null
)
```

**GetCreditBalanceAsync**
```csharp
public async Task<CreditBalance> GetCreditBalanceAsync(
    Action<CreditBalance> onSuccess = null,
    Action<string> onError = null
)
```

## Examples

### Example 1: Create a Session with Auto-Stop

```csharp
var request = new CreateSessionRequest
{
    region = "NYC3",
    gameType = "PaintballPlayground",
    mode = "XBall",
    isPublic = false,
    friendlyName = "Practice Session",
    scheduledEndTime = DateTime.UtcNow.AddHours(2) // Auto-stop in 2 hours
};

await client.CreateCreditSessionAsync(request, 
    onSuccess: (session) => {
        Debug.Log($"Session will auto-stop at {session.scheduledEndTime}");
    },
    onError: (error) => {
        Debug.LogError(error);
    }
);
```

### Example 2: Create a Scheduled Session

```csharp
var request = new CreateSessionRequest
{
    region = "NYC3",
    gameType = "PaintballPlayground",
    mode = "XBall",
    isPublic = true,
    friendlyName = "Tournament Match",
    scheduledStartTime = DateTime.UtcNow.AddHours(1), // Start in 1 hour
    scheduledEndTime = DateTime.UtcNow.AddHours(3)    // End 2 hours after start
};

await client.CreateSubscriptionSessionAsync(request);
```

### Example 3: List and Monitor Sessions

```csharp
var sessions = await client.GetMySessionsAsync();

foreach (var session in sessions)
{
    Debug.Log($"{session.friendlyName ?? session.code}: {session.status}");
    
    if (session.status == "Active")
    {
        // Join this session
        JoinGameSession(session.code);
    }
}
```

### Example 4: Check Credits Before Creating Session

```csharp
var credits = await client.GetCreditBalanceAsync();

if (credits.balance >= 60) // 60 credits = 1 hour
{
    await client.CreateCreditSessionAsync(new CreateSessionRequest
    {
        region = "NYC3",
        gameType = "PaintballPlayground",
        friendlyName = "Quick Match"
    });
}
else
{
    Debug.LogWarning("Insufficient credits!");
}
```

### Example 5: Organization API Key

```csharp
// Org API keys auto-scope all requests to the organization
var client = new SplatterVaultClient("sv_org_your_key_here", 1);

// Create a session billed to org credits
var session = await client.CreateCreditSessionAsync(new CreateSessionRequest
{
    region = "NYC3",
    gameType = "PaintballPlayground",
    mode = "XBall",
    friendlyName = "Org Practice Server"
});

// Check org credit balance
var orgCredits = await client.GetOrgCreditBalanceAsync();
Debug.Log($"Org balance: {orgCredits.GetAvailableBalance()}");

// List all org sessions (fetches from both credit + subscription endpoints)
var sessions = await client.GetMySessionsAsync();
Debug.Log($"Org has {sessions.Count} sessions");
```

### Example 6: Using Custom Game Type Configurations

```csharp
// Create a session with a custom game type configuration
var request = new CreateSessionRequest();
request.SetRegion(Region.NYC3);

// Use your custom game type config key (obtained from dashboard)
request.SetGameTypeConfigKey("usr_123_abc456xyz");

// Set custom variables defined in your game type config
request.AddCustomVariable("MAP_NAME", "desert_arena");
request.AddCustomVariable("MAX_ROUNDS", 10);
request.AddCustomVariable("DIFFICULTY", "hard");

request.isPublic = true;
request.friendlyName = "Custom Map Tournament";

await client.CreateCreditSessionAsync(request,
    onSuccess: (session) => {
        Debug.Log($"Custom session created! Join code: {session.code}");
    }
);
```

### Example 7: Custom Variables with Dictionary

```csharp
var request = new CreateSessionRequest();
request.SetGameTypeConfigKey("usr_123_abc456xyz");

// Set multiple variables at once
var variables = new Dictionary<string, object>
{
    { "MAP_NAME", "forest_battle" },
    { "GAME_MODE", "capture_the_flag" },
    { "TIME_LIMIT", 1800 }, // 30 minutes in seconds
    { "FRIENDLY_FIRE", false }
};

request.SetCustomVariables(variables);
request.SetRegion(Region.NYC3);
request.isPublic = false;

await client.CreateSubscriptionSessionAsync(request);
```

## Models

### CreateSessionRequest
```csharp
public class CreateSessionRequest
{
    public string region;              // "NYC3", "TOR1", "SFO1", "LON1"
    public string gameType;            // "PaintballPlayground", "Snapshot", etc.
    public string mode;                // "XBall", "NXL", etc.
    public bool isPublic;              // true for public, false for private
    public string friendlyName;        // Optional custom name
    public DateTime? scheduledStartTime; // Optional scheduled start
    public DateTime? scheduledEndTime;   // Optional auto-stop time
    
    // Custom Game Type Configuration Support (NEW)
    public string gameTypeConfigKey;   // Optional: unique key for custom game type
    public Dictionary<string, object> customVariables; // Optional: custom variable values
    
    // Helper Methods
    void SetGameTypeConfigKey(string configKey);
    void AddCustomVariable(string name, object value);
    void SetCustomVariables(Dictionary<string, object> variables);
    void ClearCustomVariables();
}
```

### GameSession
```csharp
public class GameSession
{
    public int id;
    public string code;
    public string serverName;
    public string friendlyName;
    public string status;              // "Pending", "Active", "Scheduled", etc.
    public string gameType;
    public string region;
    public string mode;
    public bool isPublic;
    public DateTime? scheduledStartTime;
    public DateTime? scheduledEndTime;
    public DateTime serverStart;
}
```

### CreditBalance
```csharp
public class CreditBalance
{
    public int balance;
    public int totalPurchased;
    public int totalUsed;
}
```

## Custom Game Type Configurations

The SDK now supports custom game type configurations, allowing you to define your own game types with custom variables that can be used in server launch arguments, environment variables, and configuration files.

### Getting Started with Custom Configurations

1. **Request Access**: Use the dashboard to request permission to create custom game types
2. **Create Configuration**: Once approved, create your custom game type with variables
3. **Get Unique Key**: Copy your unique game type config key (e.g., `usr_123_abc456xyz`)
4. **Use in SDK**: Pass the key and variable values when creating sessions

### Custom Variables

Custom variables allow you to parameterize your server configurations. Common use cases:

- **Map Selection**: Let users choose which map to load
- **Game Modes**: Define custom game mode variations
- **Server Settings**: Control difficulty, time limits, player counts, etc.
- **Feature Flags**: Enable/disable specific features

### Example: Map Selection System

```csharp
// Your custom game type config defines these variables:
// - MAP_NAME (select): "Desert Arena", "Forest Battle", "Urban Combat"
// - MAX_PLAYERS (number): 8-32
// - FRIENDLY_FIRE (boolean): true/false

var request = new CreateSessionRequest();
request.SetGameTypeConfigKey("usr_123_abc456xyz");

// User selects options in your UI
request.AddCustomVariable("MAP_NAME", selectedMap);
request.AddCustomVariable("MAX_PLAYERS", playerCount);
request.AddCustomVariable("FRIENDLY_FIRE", friendlyFireEnabled);

await client.CreateCreditSessionAsync(request);
```

### Backward Compatibility

The new custom configuration fields are completely optional. Existing code continues to work:

```csharp
// Legacy approach - still works!
var request = new CreateSessionRequest
{
    gameType = "PaintballPlayground",
    mode = "XBall",
    region = "NYC3"
};
```

## Best Practices

1. **Store API Key Securely**
   - Don't hardcode API keys in your scripts
   - Use Unity's PlayerPrefs or a config file
   - Never commit API keys to version control

2. **Error Handling**
   - Always provide error callbacks
   - Show user-friendly error messages
   - Log errors for debugging

3. **Session Management**
   - Stop sessions when done to save credits
   - Use auto-stop times to prevent forgotten sessions
   - Cache session data to avoid redundant API calls

4. **Performance**
   - Don't poll the API too frequently
   - Use the session code to connect to servers
   - Cache session lists and refresh only when needed

## Troubleshooting

### "Unauthorized" Error
- Check that your API key is correct and approved
- Verify the API key has not been revoked

### "Insufficient Credits" Error
- Check your credit balance with `GetCreditBalanceAsync()`
- Purchase more credits through the dashboard

### "Connection Failed" Error
- Verify internet connectivity
- Check that the API URL is correct
- Ensure firewall/antivirus isn't blocking Unity

## Support

For issues or questions:
- Check the [API Documentation](https://splattervault.com/#/api-docs)
- Report bugs via `/reportbug` in Discord
- Contact support

## License

This SDK is provided as-is for use with the SplatterVault platform.
