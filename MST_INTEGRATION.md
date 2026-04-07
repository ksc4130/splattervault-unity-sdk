# Master Server Toolkit Integration Guide

This guide explains how to use SplatterVault with Unity's Master Server Toolkit (MST).

## Overview

SplatterVault and MST serve complementary roles:
- **SplatterVault**: Provides dedicated server infrastructure (hosting, scaling, billing)
- **MST**: Handles networking, matchmaking, and lobby management

Together, they give you enterprise-grade hosting with Unity-native networking.

## What's Included

### 1. MSTExtensions.cs
Helper methods for MST integration:
- `GetMSTServerInfo()` - Format server data for MST
- `WaitForServerReady()` - Poll until server is active
- `CreateAndWaitForMSTServer()` - One-call server creation
- `IsReadyForMST()` - Check server state
- `GetConnectionString()` - Get player connection info

### 2. MSTIntegrationExample.cs
Complete working example showing:
- Server creation flow
- MST registration
- Player connection handling
- Server cleanup

### 3. SplatterVaultMSTBridge.cs
Automated bridge component featuring:
- **Drag-and-drop** Unity component
- **Automatic** server lifecycle management
- **Unity Events** for easy integration
- **Auto-stop** on empty server
- **Inspector** helper functions

## Quick Start

### Option 1: Use the Bridge Component (Easiest)

1. **Add the component** to a GameObject:
   ```
   GameObject → Add Component → SplatterVault MST Bridge
   ```

2. **Configure** in Inspector:
   - Set your API key
   - Choose region and game type
   - Enable "Auto Register With MST"

3. **Call from your code**:
   ```csharp
   // Get reference to bridge
   var bridge = GetComponent<SplatterVaultMSTBridge>();
   
   // Listen to events
   bridge.OnServerReady.AddListener(OnServerCreated);
   
   // Create server
   bridge.CreateServer("My Game");
   
   // Update player count as they join/leave
   bridge.UpdatePlayerCount(playerCount);
   
   // Stop when done
   bridge.StopServer();
   ```

4. **Customize MST integration**:
   - Open `SplatterVaultMSTBridge.cs`
   - Find the `RegisterWithMST()` method
   - Uncomment and adapt the example code for your MST setup

### Option 2: Manual Integration

Use the extension methods directly:

```csharp
using SplatterVault;
using SplatterVault.MST;

// Create client
var client = new SplatterVaultClient(apiKey);

// Create and wait for server
var serverInfo = await client.CreateAndWaitForMSTServer(
    request,
    onProgress: (status) => Debug.Log(status)
);

// Register with MST (your code here)
RegisterWithYourMSTSetup(serverInfo);
```

## Integration Patterns

### Pattern 1: Rooms Module

If using MST's Rooms Module:

```csharp
private void RegisterWithMST()
{
    var roomOptions = new RoomOptions
    {
        Name = serverInfo.serverName,
        RoomIp = serverInfo.ipAddress,
        RoomPort = serverInfo.port,
        MaxPlayers = serverInfo.maxPlayers,
        Properties = serverInfo.ToMSTProperties()
    };
    
    roomsModule.RegisterRoom(roomOptions);
}
```

### Pattern 2: Spawners Module

If using MST's Spawners Module:

```csharp
private void RegisterWithMST()
{
    var spawnTask = new SpawnTaskController();
    spawnTask.Properties = serverInfo.ToMSTProperties();
    
    masterServerConnection.RegisterSpawnedProcess(
        spawnTask,
        serverInfo.GetConnectionAddress()
    );
}
```

### Pattern 3: Custom Integration

```csharp
private void RegisterWithMST()
{
    var properties = serverInfo.ToMSTProperties();
    
    // Your custom MST registration logic
    myMSTSystem.RegisterServer(
        serverInfo.ipAddress,
        serverInfo.port,
        properties
    );
}
```

## Key Concepts

### Server Lifecycle

1. **Create** - SplatterVault provisions infrastructure
2. **Wait** - Server starts and becomes active (30-60 seconds)
3. **Register** - Server info added to MST
4. **Play** - Players connect via MST matchmaking
5. **Stop** - Unregister from MST, then stop SV server

### State Management

The bridge component manages these states automatically:
- `Idle` - No server
- `CreatingServer` - Requesting server
- `WaitingForServer` - Server starting
- `RegisteringWithMST` - Adding to MST
- `Running` - Ready for players
- `Stopping` - Cleaning up

### Player Connection Flow

1. Player searches for game in MST
2. MST finds your registered server
3. MST provides connection info to player
4. Player connects directly to SplatterVault server
5. Game networking handled by your game code

## Features

### Auto-Stop on Empty
```csharp
bridge.autoStopOnEmpty = true;
bridge.emptyServerTimeout = 300f; // 5 minutes
```

### Unity Events
```csharp
bridge.OnServerReady.AddListener((serverInfo) => {
    Debug.Log($"Server ready: {serverInfo.serverCode}");
});

bridge.OnError.AddListener((error) => {
    Debug.LogError($"Error: {error}");
});
```

### Status Updates
```csharp
bridge.OnStatusChanged.AddListener((status) => {
    statusText.text = status;
});
```

## Best Practices

1. **Always wait** for server to be ready before registering with MST
2. **Unregister from MST** before stopping SV server
3. **Handle errors** gracefully with retry logic
4. **Update player count** to enable auto-stop
5. **Use Unity Events** for UI updates

## Troubleshooting

### Server not appearing in MST
- Check that `IsReadyForMST()` returns true
- Verify MST registration code is called
- Confirm MST is properly configured

### Players can't connect
- Verify `serverInfo.ipAddress` is public IP
- Check firewall rules on ports
- Ensure game port (7777) matches your setup

### Server stops unexpectedly
- Check if auto-stop on empty is enabled
- Verify player count updates are being called
- Check SplatterVault credit balance

## Example: Complete Game Flow

```csharp
public class GameManager : MonoBehaviour
{
    private SplatterVaultMSTBridge bridge;
    
    void Start()
    {
        bridge = GetComponent<SplatterVaultMSTBridge>();
        bridge.OnServerReady.AddListener(OnServerReady);
    }
    
    public void HostGame()
    {
        bridge.CreateServer("My Awesome Game");
    }
    
    void OnServerReady(MSTServerInfo info)
    {
        // Server is registered with MST
        // Players can now find and join
        ShowServerCode(info.serverCode);
    }
    
    void OnPlayerJoined()
    {
        int count = NetworkManager.PlayerCount;
        bridge.UpdatePlayerCount(count);
    }
    
    void OnGameOver()
    {
        bridge.StopServer();
    }
}
```

## Support

- SplatterVault SDK: See `README.md`
- MST: Visit Unity MST documentation
- Issues: Report via Discord `/reportbug`

## License

This integration is provided as-is for use with SplatterVault and MST.
