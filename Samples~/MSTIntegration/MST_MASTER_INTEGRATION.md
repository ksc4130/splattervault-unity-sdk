# SplatterVault Integration for Existing MST Master

## Quick Start: 3 Steps

You already have MST Master running. Here's what to change:

### 1. Add SplatterVault Client

```csharp
using SplatterVault;

public class YourMasterServer : MonoBehaviour
{
    private SplatterVaultClient svClient;
    
    void Awake()
    {
        svClient = new SplatterVaultClient("sv_your_api_key_here");
    }
}
```

### 2. Replace Your Spawner

**Before (your current code):**
```csharp
// Your existing spawner that boots local servers
void SpawnServer(string roomName)
{
    // Start local server process
    ProcessStartInfo startInfo = new ProcessStartInfo();
    startInfo.FileName = "your-server.exe";
    Process.Start(startInfo);
}
```

**After (using SplatterVault):**
```csharp
// Now spawn on SplatterVault's infrastructure
async void SpawnServer(string roomName)
{
    var request = new CreateSessionRequest 
    { 
        friendlyName = roomName
    };
    request.SetRegion(Region.NYC3);
    request.SetGameType(GameType.PaintballPlayground);
    request.SetPaintballMode(PaintballMode.XBall);
    
    // Create session - server will auto-start
    var session = await svClient.CreateCreditSessionAsync(request);
    
    // Done! The headless PPVR server is booting and will
    // register itself with your MST Master automatically
}
```

### 3. That's It!

The headless PPVR server running on SplatterVault will:
- ✅ Boot up (30-60 seconds)
- ✅ Auto-register with your MST Master
- ✅ Show up in your server browser
- ✅ Accept player connections

---

## Complete Example

Here's a complete drop-in replacement for a typical MST spawner:

```csharp
using System;
using UnityEngine;
using SplatterVault;
using Barebones.MasterServer; // Your existing MST

public class SplatterVaultMSTSpawner : MonoBehaviour
{
    [SerializeField] private string apiKey = "sv_your_api_key_here";
    [SerializeField] private Region region = Region.NYC3;
    
    private SplatterVaultClient client;
    
    void Awake()
    {
        client = new SplatterVaultClient(apiKey);
    }
    
    // This is called by your MST Master when it needs a server
    public async void OnSpawnRequest(SpawnRequest request)
    {
        try
        {
            // Create the SplatterVault session
            var svRequest = new CreateSessionRequest 
            {
                friendlyName = request.RoomName ?? "Game Server"
            };
            svRequest.SetRegion(region);
            svRequest.SetGameType(GameType.PaintballPlayground);
            svRequest.SetPaintballMode(PaintballMode.XBall);
            
            var session = await client.CreateCreditSessionAsync(svRequest);
            
            // Notify MST that spawn succeeded
            request.OnSpawnSuccess(session.code);
            
            Debug.Log($"Spawned server: {session.code}");
            // The headless server will register with MST automatically
        }
        catch (Exception ex)
        {
            request.OnSpawnFailed(ex.Message);
            Debug.LogError($"Spawn failed: {ex.Message}");
        }
    }
    
    // Called when server should be stopped
    public async void OnKillRequest(string serverCode)
    {
        try
        {
            // Find the session
            var sessions = await client.ListSessionsAsync();
            var session = sessions.Find(s => s.code == serverCode);
            
            if (session != null)
            {
                await client.StopSessionAsync(session.id);
                Debug.Log($"Stopped server: {serverCode}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Stop failed: {ex.Message}");
        }
    }
}
```

---

## Game Modes

All PPVR game modes supported:

```csharp
request.SetPaintballMode(PaintballMode.XBall);        // XBall competitive
request.SetPaintballMode(PaintballMode.NXL);          // Pubs
request.SetPaintballMode(PaintballMode.KillConfirmed); // TAGS
request.SetPaintballMode(PaintballMode.OneVOne);      // 1v1
request.SetPaintballMode(PaintballMode.ESPN);         // 3v3
```

## Regions

Available regions:

```csharp
Region.NYC3  // New York (US East)
Region.TOR1  // Toronto (Canada)
Region.SFO1  // San Francisco (US West)
Region.LON1  // London (Europe)
```

---

## How It Works

```
1. Your MST Master calls SpawnServer()
   ↓
2. SDK creates session on SplatterVault API
   ↓
3. SplatterVault boots headless PPVR server
   (takes 30-60 seconds)
   ↓
4. Headless server auto-registers with your MST Master
   ↓
5. Players see server in your matchmaking
   ↓
6. Players connect through MST
   ↓
7. Game runs on SplatterVault infrastructure
```

**You don't change anything else!** Your existing:
- MST Master stays the same
- Matchmaking stays the same  
- Player connections stay the same
- Room management stays the same

You just swap the spawner to use SplatterVault's infrastructure instead of local servers.

---

## Testing

```csharp
[ContextMenu("Test Spawn")]
async void TestSpawn()
{
    var request = new CreateSessionRequest { friendlyName = "Test" };
    request.SetRegion(Region.NYC3);
    request.SetGameType(GameType.PaintballPlayground);
    request.SetPaintballMode(PaintballMode.XBall);
    
    var session = await client.CreateCreditSessionAsync(request);
    Debug.Log($"Server code: {session.code}");
    Debug.Log("Wait 30-60s for it to register with your MST Master");
}
```

---

## That's All!

**3 lines of code** replaces your local spawner with SplatterVault:

```csharp
var request = new CreateSessionRequest { ... };
request.SetPaintballMode(PaintballMode.XBall);
var session = await svClient.CreateCreditSessionAsync(request);
```

The headless server handles everything else automatically.
