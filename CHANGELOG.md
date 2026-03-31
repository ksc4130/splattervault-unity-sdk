# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.0.0] - 2026-03-31

### Breaking Changes
- **Removed game-specific enums**: `GameType`, `PaintballMode`, `SnapshotMode`, `ValheimMode` and all `ToApiString()` extensions. The SDK no longer hardcodes knowledge of specific games.
- **`CreateSessionRequest.gameType` removed** — replaced by `gameKey`
- **`CreateSessionRequest.mode` removed** — mode is now a launch argument, pass via `AddCustomVariable()`
- **`CreateSessionRequest.gameTypeConfigKey` removed** — replaced by `gameKey`
- Helper methods removed: `SetGameType()`, `SetPaintballMode()`, `SetSnapshotMode()`, `SetValheimMode()`, `SetGameTypeConfigKey()`, `SetMode()`

### Added
- `CreateSessionRequest.gameKey` — game config key (e.g., `"sys_1774636058786_30e0fc4d"`), serializes as `gameTypeConfigKey` for the API
- `GetConfigurableArgsAsync(gameKey)` — fetch available launch arguments for a game (mode, max players, etc.)
- `StructuredLaunchArg` and `SelectOption` models for dynamic game configuration UI
- **Organization API key support** — `sv_org_` prefixed keys work across all endpoints
- Organization constructor, `GetOrgCreditBalanceAsync()`, `GetOrgSubscriptionAsync()`
- `OrgCreditStats`, `OrgSubscriptionInfo`, `SubscriptionDetails`, `CancelScheduleResult` models
- `GameSession.organizationId`, `stopReason`, `stopReasonDetails` fields
- .NET 8 integration test harness in `Tests/` directory

### Fixed
- `GameSession.stopReasonDetails` changed from `string` to `object` (API returns JSONB)

### Migration from v2.x
```csharp
// OLD (v2.x)
request.SetGameType(GameType.PaintballPlayground);
request.SetPaintballMode(PaintballMode.XBall);

// NEW (v3.0)
request.gameKey = "sys_1774636058786_30e0fc4d";  // from your dashboard
request.AddCustomVariable("-mstRoomMode", "XBall");  // mode is a launch arg

// Discover available options dynamically:
var args = await client.GetConfigurableArgsAsync(gameKey);
```

## [2.0.0] - 2026-03-20

### Breaking Changes
- `StopSessionAsync` now requires a `GameSession` object instead of a session ID, to route to the correct API endpoint based on `serverType`
- `UpdateSessionFriendlyNameAsync` now requires a `GameSession` object instead of a session ID
- `CreditBalance` fields changed from `int` to `float` to match API decimal values
- `CreditBalance.GetBalanceInHours()` replaced with `GetBalanceInHours(float creditsPerMinute)` — rates vary by server size (0.25–11.75)
- `CreditBalance.HasEnoughCredits(int)` replaced with `HasEnoughCredits(float minutes, float creditsPerMinute)`
- `GetConnectionString` no longer accepts a port parameter — uses the actual `slavePort` from the API
- Removed `JsonHelper` class (all deserialization now uses Newtonsoft.Json)

### Fixed
- **CRITICAL:** `StopSessionAsync` was calling `DELETE /game-sessions/:id` (admin-only delete endpoint) instead of `POST /credits/sessions/:id/stop` or `POST /subscriptions/sessions/:id/stop`
- **CRITICAL:** Credit balance/amount fields were `int` but API returns decimals (e.g., `19401.93`) — values were being truncated
- **HIGH:** `CreateSubscriptionSessionAsync` used `JsonUtility.ToJson` which silently dropped `Dictionary` fields (`customVariables`)
- **HIGH:** `UpdateSessionFriendlyNameAsync` called `PATCH /game-sessions/:id` (admin endpoint) instead of `PUT /{serverType}/sessions/:id/friendly-name`
- **HIGH:** All JSON deserialization used `JsonUtility` which cannot handle nullable fields, decimals, or nested objects — replaced with `Newtonsoft.Json` throughout
- `GetConnectionString` hardcoded port `7777` instead of using actual `slavePort` from API (production uses `8100`)
- `MSTServerInfo.port` hardcoded `8100` instead of reading from session

### Added
- `StopCreditSessionAsync` and `StopSubscriptionSessionAsync` for explicit endpoint control
- `StopSessionResult` class with `session`, `totalHours`, `totalCost` fields (returned by credit stop endpoint)
- `ServerSizeInfo` class (`id`, `friendlyName`, `creditsPerMinute`)
- `GameSession.hostname`, `slavePort`, `serverStoppedAt`, `serverSizeId`, `buildId`, `volumeId`, `creditsDeducted`, `serverSize` fields
- `GameSession.IsPending()`, `IsStopped()`, `GetServerPort()` helpers
- `CreditBalance.subscriptionBalance`, `adHocBalance`, `subscriptionCreditsFrozen`, `isInGracePeriod` fields
- `CreditBalance.GetAvailableBalance()` — respects frozen subscription credits
- `CreditStats.subscriptionBalance`, `adHocBalance`, `subscriptionCreditsFrozen` fields
- `Subscription.currentInstances` field
- `CreateSessionRequest.serverSizeId` field
- `Region.NYC1`, `Region.SFO2`, `Region.SFO3` enum values
- `PutAsync` HTTP method (used by friendly-name update endpoints)
- `.meta` files for all assets (deterministic GUIDs for stable references)

### Changed
- All JSON serialization/deserialization now uses `Newtonsoft.Json` consistently
- `UsageStats.creditBalance` and `creditHours` changed from `int` to `float`
- `CreditTransaction.amount` changed from `int` to `float`

## [1.1.0] - 2025-11-18

### Added
- Custom game type configuration support
- `gameTypeConfigKey` field in `CreateSessionRequest` for custom game type configs
- `customVariables` dictionary in `CreateSessionRequest` for custom variable values
- Helper methods for custom variables:
  - `SetGameTypeConfigKey(string configKey)` - Set custom config key
  - `AddCustomVariable(string name, object value)` - Add single custom variable
  - `SetCustomVariables(Dictionary<string, object> variables)` - Set multiple variables at once
  - `ClearCustomVariables()` - Clear all custom variables
- Documentation section on custom game type configurations
- Examples showing custom configuration usage with variables

### Changed
- Updated README with custom configuration examples and best practices
- Updated Models documentation to include new custom configuration fields

### Technical Details
- Fully backward compatible - all new fields are optional
- Custom variables support any object type (strings, numbers, booleans, etc.)
- Unique keys follow format: `usr_{userId}_{hash}` for user configs, `sys_{gameType}_{hash}` for system configs

## [1.0.0] - 2025-11-09

### Added
- Initial release of SplatterVault Unity SDK
- Complete API client for SplatterVault game session management
- Strongly-typed enums for game types, regions, and modes
- Full async/await support with UnityWebRequest
- Master Server Toolkit (MST) integration suite
- Comprehensive error handling and retry logic
- Session management features:
  - Create credit-based sessions
  - Create subscription-based sessions
  - Get session details
  - List user sessions
  - Stop sessions
  - Update friendly names
- Scheduling support:
  - Immediate start with/without auto-stop
  - Scheduled start with/without auto-stop
  - Flexible DateTime-based scheduling
- Credit management:
  - Check credit balance
  - Get credit statistics
  - Balance conversion (credits to hours)
- Subscription support:
  - Get subscription details
  - Usage statistics
- MST Integration:
  - Extension methods for MST compatibility
  - Automated bridge component
  - Server polling and ready detection
  - Unity Events integration
  - Auto-stop on empty server
- Example scripts:
  - SimpleSessionManager - Basic session management
  - TournamentManager - Advanced tournament scheduling
  - MSTIntegrationExample - MST integration patterns
  - SplatterVaultMSTBridge - Drag-and-drop bridge component
- Documentation:
  - Complete README with API reference
  - Installation guide
  - MST integration guide
  - Publishing guide for UPM

### Features
- Type-safe API with compile-time validation
- Game-specific mode enums (PaintballMode, SnapshotMode, ValheimMode)
- Region-specific enums (NYC3, TOR1, SFO1, LON1)
- Automatic JSON parsing and error handling
- Session state checking (IsActive, IsScheduled, IsReadyForMST)
- Connection string helpers
- MST property conversion
- Inspector-visible status fields
- Context menu helpers for debugging

### Technical Details
- Minimum Unity version: 2019.4
- .NET compatibility: .NET 4.x or .NET Standard 2.0
- No external dependencies
- Fully async/await compatible
- UnityWebRequest-based HTTP client
