# SplatterVault + Photon Bolt Integration

## Quick Start: 4 Steps

You have a Bolt game and want to run dedicated servers on SplatterVault. Here's what to do.

### 1. Add SplatterVault SDK

Import the SplatterVault Unity SDK package into your project.

### 2. Add the Session Manager

Attach `SplatterVaultBoltSessionManager` to a GameObject in your scene. Set your API key and game key in the Inspector.

### 3. Add the Bolt Callback Router

Create `BoltSessionCallbacks.cs` (copy from the bottom of `SplatterVaultBoltSessionManager.cs`) and attach it to the **same GameObject**. This routes Bolt's `GlobalEventListener` callbacks to the session manager.

### 4. Wire Up Your UI

```csharp
// Create a server and auto-connect
sessionManager.CreateSession("My Game Room");

// Or join an existing server by code
sessionManager.JoinSession("ABC123");

// Stop and disconnect
sessionManager.StopSession();
```

That's it. The session manager handles: create server -> wait for boot -> start Bolt -> connect -> cleanup.

---

## How It Works

```
1. Your UI calls CreateSession()
   |
2. SDK creates session on SplatterVault API
   |
3. SplatterVault boots your game server
   (takes 30-60 seconds)
   |
4. SDK polls until server status = "Active"
   |
5. BoltLauncher.StartClient() is called
   |
6. BoltStartDone() fires -> BoltNetwork.Connect(ip:port)
   |
7. Connected() fires -> you're in the game
   |
8. Your game runs on SplatterVault infrastructure
```

---

## Architecture: Why a Companion Script?

Photon Bolt's `GlobalEventListener` is a MonoBehaviour. You can't inherit from both `GlobalEventListener` and have the session manager logic in the same class. The solution is two scripts on the same GameObject:

| Script | Role |
|--------|------|
| `SplatterVaultBoltSessionManager` | Session lifecycle, SDK calls, state machine |
| `BoltSessionCallbacks` | Routes Bolt callbacks to the session manager |

```
[GameObject]
  ├── SplatterVaultBoltSessionManager  (API key, game key, events)
  └── BoltSessionCallbacks             (GlobalEventListener -> manager)
```

---

## Choosing the Right Sample

| Sample | Use When |
|--------|----------|
| `BoltIntegrationExample.cs` | Learning / prototyping. Shows the raw flow step by step. |
| `SplatterVaultBoltBridge.cs` | Simple integration. One-click create-and-connect with events. |
| `SplatterVaultBoltSessionManager.cs` | Production. Full lifecycle, join-by-code, player tracking, auto-stop. |

---

## Join By Code

Players can join an existing server using the session code:

```csharp
// Host creates the session
sessionManager.CreateSession("Friday Night Match");

// Host shares the code (shown in UI)
string code = sessionManager.GetServerCode(); // e.g., "A7X9"

// Other players join by code
sessionManager.JoinSession("A7X9");
```

---

## Unity Events

Wire these up in the Inspector for UI integration:

| Event | Fires When | Payload |
|-------|-----------|---------|
| `OnSessionCreated` | Server provisioned | `SessionInfo` |
| `OnSessionReady` | Server is Active | `SessionInfo` |
| `OnBoltConnected` | Bolt connection established | `SessionInfo` |
| `OnSessionStopped` | Session stopped | Server code (string) |
| `OnPlayerCountChanged` | Player count changes | Count (int) |
| `OnError` | Any error | Error message (string) |
| `OnStatusUpdate` | Status changes | Status message (string) |

**Example: Loading screen**
```csharp
public class LoadingUI : MonoBehaviour
{
    public Text statusLabel;
    public GameObject loadingPanel;

    // Wire these in Inspector
    public void OnStatusUpdate(string status)
    {
        statusLabel.text = status;
    }

    public void OnConnected(SplatterVaultBoltSessionManager.SessionInfo info)
    {
        loadingPanel.SetActive(false);
    }

    public void OnError(string error)
    {
        statusLabel.text = $"Error: {error}";
    }
}
```

---

## Player Tracking

Call `SetPlayerCount()` from your game logic so the session manager can auto-stop empty servers:

```csharp
// In your Bolt game logic
public override void Connected(BoltConnection connection)
{
    playerCount++;
    sessionManager.SetPlayerCount(playerCount);
}

public override void Disconnected(BoltConnection connection)
{
    playerCount--;
    sessionManager.SetPlayerCount(playerCount);
}
```

Enable **Auto Stop On Empty** in the Inspector and set the timeout. Default is 300 seconds (5 minutes).

---

## Regions

```csharp
Region.NYC3  // New York (US East) — default
Region.TOR1  // Toronto (Canada)
Region.SFO1  // San Francisco (US West)
Region.LON1  // London (Europe)
```

---

## Key Differences from MST Integration

| | MST | Bolt |
|---|-----|------|
| **Server discovery** | MST room registration + matchmaking | Direct IP:port connect or join-by-code |
| **Connection flow** | Create -> Register room -> Players find via MST | Create -> Get IP:port -> BoltNetwork.Connect() |
| **Companion script** | Optional (MST handles callbacks) | Required (GlobalEventListener routing) |
| **Scene loading** | Manual | `BoltNetwork.LoadScene()` in config |

The SplatterVault SDK provides the same core functionality for both: create a dedicated server, wait for it to boot, get the IP:port. The difference is only in how players discover and connect to the server.

---

## Testing Without Bolt

All samples work without Bolt installed — Bolt-specific code is commented out. The session manager will:
1. Create a real SplatterVault server
2. Log the IP:port you would connect to
3. Simulate the connected state

This lets you verify your API key, game key, and server creation flow before wiring up Bolt.

---

## Complete Minimal Example

```csharp
using UnityEngine;
using SplatterVault;
using Bolt;
using UdpKit;

public class QuickBoltSetup : GlobalEventListener
{
    private SplatterVaultClient client;
    private GameSession session;
    
    [SerializeField] private string apiKey = "sv_your_api_key_here";
    [SerializeField] private string gameKey = "your_game_key_here";

    async void Start()
    {
        client = new SplatterVaultClient(apiKey);
    }

    // Call from a UI button
    public async void HostGame()
    {
        // 1. Create server
        var request = new CreateSessionRequest
        {
            gameKey = gameKey,
            friendlyName = "Quick Match"
        };
        request.SetRegion(Region.NYC3);
        
        session = await client.CreateCreditSessionAsync(request);
        
        // 2. Wait for server to boot
        session = await client.WaitForServerReady(session.id);
        
        // 3. Start Bolt client
        BoltLauncher.StartClient();
    }

    // Bolt callback: initialization done
    public override void BoltStartDone()
    {
        var endpoint = new UdpEndPoint(
            UdpIPv4Address.Parse(session.slaveIp),
            (ushort)session.GetServerPort()
        );
        BoltNetwork.Connect(endpoint);
    }

    // Bolt callback: connected to server
    public override void Connected(BoltConnection connection)
    {
        Debug.Log("Connected! You're in the game.");
    }

    // Cleanup
    async void OnApplicationQuit()
    {
        if (session != null)
            await client.StopSessionAsync(session);
    }
}
```

20 lines of game code. SplatterVault handles the infrastructure.
