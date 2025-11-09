# SplatterVault Unity SDK - Installation Guide

## Quick Installation

### Option 1: Manual Installation (Recommended)

1. Copy the `SplatterVault` folder to your Unity project's `Assets` folder
2. The SDK will be automatically detected by Unity
3. Start using it immediately in your scripts!

```
YourUnityProject/
  Assets/
    SplatterVault/           ← Copy this folder here
      Models.cs
      SplatterVaultClient.cs
    Examples/                ← Optional examples
      SimpleSessionManager.cs
      TournamentManager.cs
```

### Option 2: Unity Package Manager (Git URL)

1. Open Unity Editor
2. Go to Window → Package Manager
3. Click the `+` button
4. Select "Add package from git URL"
5. Enter: `https://github.com/your-org/splattervault-unity-sdk.git`

## Verification

After installation, verify the SDK is working:

1. Create a new C# script in Unity
2. Add this code:

```csharp
using SplatterVault;
using UnityEngine;

public class TestSDK : MonoBehaviour
{
    void Start()
    {
        var client = new SplatterVaultClient("sv_test");
        Debug.Log("SplatterVault SDK loaded successfully!");
    }
}
```

3. Attach the script to a GameObject
4. Press Play - you should see the success message in the Console

## Next Steps

1. **Get Your API Key**
   - Go to https://splattervault.com
   - Navigate to Settings → API Keys
   - Create a new API key
   - Wait for admin approval

2. **Try the Examples**
   - Check the `Examples` folder
   - `SimpleSessionManager.cs` - Basic usage
   - `TournamentManager.cs` - Advanced features

3. **Read the Documentation**
   - See `README.md` for full API reference
   - Check code comments for detailed explanations

## Requirements

- Unity 2019.4 or later
- .NET 4.x or .NET Standard 2.0
- Internet access enabled in Player Settings

## Configuration

### Securing Your API Key

**IMPORTANT:** Never commit your API key to version control!

#### Recommended Approach 1: ScriptableObject

```csharp
[CreateAssetMenu(fileName = "SplatterVaultConfig", menuName = "Config/SplatterVault")]
public class SplatterVaultConfig : ScriptableObject
{
    [SerializeField] private string apiKey;
    public string ApiKey => apiKey;
}
```

Add to `.gitignore`:
```
Assets/Resources/SplatterVaultConfig.asset
```

#### Recommended Approach 2: Environment Variables

```csharp
string apiKey = Environment.GetEnvironmentVariable("SPLATTERVAULT_API_KEY");
```

## Troubleshooting

### "Cannot find type or namespace 'SplatterVault'"

**Solution:** Make sure you copied the `SplatterVault` folder to your `Assets` directory and Unity has finished compiling.

### "The type or namespace name 'Task' could not be found"

**Solution:** 
1. Go to Edit → Project Settings → Player
2. Under "Other Settings" → "Api Compatibility Level"
3. Select ".NET 4.x" or ".NET Standard 2.0"

### "Async method lacks 'await' operators"

**Solution:** This is just a warning. You can safely suppress it or add `await` to your async calls.

### Connection Errors

**Solution:**
1. Check internet connectivity
2. Verify API key is correct and approved
3. Check Unity's firewall settings
4. Enable "Internet Access" in Player Settings

## Support

- Documentation: See `README.md`
- API Docs: https://splattervault.com/#/api-docs
- Discord: Report issues with `/reportbug`
- Email: support@splattervault.com

## What's Included

```
unity-sdk/
├── README.md                          # Complete documentation
├── INSTALLATION.md                    # This file
├── package.json                       # Unity Package Manager manifest
├── SplatterVault/
│   ├── Models.cs                      # Data models
│   └── SplatterVaultClient.cs         # Main API client
└── Examples/
    ├── SimpleSessionManager.cs        # Basic example
    └── TournamentManager.cs           # Tournament example
```

## Version History

### v1.0.0 (Initial Release)
- ✅ Create credit/subscription sessions
- ✅ Get session details
- ✅ List user sessions
- ✅ Stop sessions
- ✅ Update friendly names
- ✅ Scheduling support
- ✅ Credit balance checking
- ✅ Async/await support
- ✅ Error handling
- ✅ Complete examples

## License

This SDK is provided as-is for use with the SplatterVault platform.
