# SplatterVault Unity SDK

Unity SDK for interacting with the SplatterVault API to create and manage game sessions programmatically.

## Installation

1. Copy the `SplatterVault` folder into your Unity project's `Assets` folder
2. The SDK requires Unity 2019.4 or later
3. Ensure your project has internet access enabled in Player Settings

## Quick Start

```csharp
using SplatterVault;

// Initialize the client with your API key
var client = new SplatterVaultClient("sv_your_api_key_here");

// Create a session
var request = new CreateSessionRequest
{
    region = "NYC3",
    gameType = "PaintballPlayground",
    mode = "XBall",
    isPublic = false,
    friendlyName = "My Unity Game Session"
};

await client.CreateCreditSessionAsync(request, (session) => {
    Debug.Log($"Session created! Code: {session.code}");
}, (error) => {
    Debug.LogError($"Error: {error}");
});
```

## Features

- ✅ Create credit-based game sessions
- ✅ Create subscription-based game sessions
- ✅ Get session details
- ✅ List user sessions
- ✅ Stop sessions
- ✅ Update friendly names
- ✅ Scheduling support (start/end times)
- ✅ Get credit balance
- ✅ Async/await support
- ✅ Error handling
- ✅ Response models

## API Reference

### SplatterVaultClient

Main client class for API interactions.

#### Constructor
```csharp
public SplatterVaultClient(string apiKey, string baseUrl = "https://api.splattervault.com/rest")
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
