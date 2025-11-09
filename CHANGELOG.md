# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
