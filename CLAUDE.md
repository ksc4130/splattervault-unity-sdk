# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SplatterVault Unity SDK — Official C# SDK for Unity game developers to create and manage dedicated game servers through the SplatterVault (ppg-vpl) API. Supports credit-based and subscription-based sessions, organization billing, game configuration discovery, and Master Server Toolkit (MST) integration.

## Ecosystem Context

This SDK consumes the ppg-vpl backend API (`../ppg-vpl/api/`). It is one of several consumers:

- **ppds-ux** (`../ppg-vpl/ppds-ux/`) — Primary web frontend (full API surface)
- **vpl-ux** (`../vpl-ux/`) — Third-party league frontend (league/team/matchup subset)
- **dedicatedServer** (`../ppg-vpl/dedicatedServer/`) — Game server wrapper (Socket.IO)

**When API endpoints change** in `../ppg-vpl/api/`, this SDK's `SplatterVaultClient.cs` and `Models.cs` may need updates.

## Repository Structure

```
Runtime/
├── SplatterVaultClient.cs         — Main API client (all HTTP methods)
├── Models.cs                      — All request/response models
├── MSTExtensions.cs               — Master Server Toolkit integration
├── SplatterVault.Runtime.asmdef   — Unity assembly definition
Samples~/
├── BasicExamples/                 — SimpleSessionManager, TournamentManager
├── MSTIntegration/                — MST bridge component examples
Tests/
├── SplatterVault.Tests/           — Unit tests
```

## Package Info

- Package name: `com.splattervault.sdk`
- Version: 3.0.0
- Unity requirement: 2019.4+
- Runtime dependency: Newtonsoft.Json (included with Unity 2020+)
- Install via Unity Package Manager: `https://github.com/ksc4130/splattervault-unity-sdk.git`

## API Endpoints Consumed

All requests use API key authentication via `X-API-Key` header (not JWT Bearer tokens). Base URL defaults to `https://splattervault.com/rest`.

### Session Management

- `POST /credits/sessions` — Create credit session
- `POST /subscriptions/sessions` — Create subscription session
- `GET /game-sessions/{id}` — Get session by ID
- `GET /game-sessions/my-sessions` — List user's sessions (personal key)
- `GET /credits/sessions` — List credit sessions (org key)
- `GET /subscriptions/sessions` — List subscription sessions (org key)
- `POST /credits/sessions/{id}/stop` — Stop credit session (returns billing)
- `POST /subscriptions/sessions/{id}/stop` — Stop subscription session
- `PUT /credits/sessions/{id}/friendly-name` — Update credit session name
- `PUT /subscriptions/sessions/{id}/friendly-name` — Update subscription session name
- `PUT /credits/sessions/{id}/schedule` — Update credit session schedule
- `PUT /subscriptions/sessions/{id}/schedule` — Update subscription session schedule
- `POST /credits/sessions/{id}/cancel-schedule` — Cancel scheduled credit session
- `POST /subscriptions/sessions/{id}/cancel-schedule` — Cancel scheduled subscription session

### Credits & Billing

- `GET /credits` — Personal credit balance
- `GET /credits/stats` — Credit statistics with recent transactions

### Subscriptions

- `GET /subscriptions` — Subscription details (current + all)
- `GET /subscriptions/usage` — Usage stats (instances, balance, limits)

### Organization

- `GET /organizations/{orgId}/credits` — Org credit balance
- `GET /organizations/{orgId}/subscription` — Org subscription info

### Auth Context
- `GET /auth/me` — Resolve authenticated caller's context (org ID, permissions for org keys; user info for user keys)

### Organization (org-key-friendly aliases)
- `GET /org/credits` — Org credit balance (no org ID needed in URL for org API keys)
- `GET /org/subscription` — Org subscription info (no org ID needed in URL for org API keys)

### Game Configuration

- `GET /game-types/{gameKey}/configurable-args` — Discoverable launch arguments (type, options, min/max, validation)

## Key Models (Runtime/Models.cs)

- `CreateSessionRequest` — Session creation payload (`gameKey` serialized as `gameTypeConfigKey` via `[JsonProperty]`, region, channel, customVariables dict)
- `GameSession` — Session response (status strings: `"Active"`, `"Pending"`, `"Scheduled"`, `"Not Active"`; serverType: `"Credit"`, `"Subscription"`)
- `CreditBalance` — Balance with `GetAvailableBalance()` helper (handles frozen subscription credits)
- `StopSessionResult` — Billing info (totalHours, totalCost)
- `StructuredLaunchArg` — Game config arg definition (types: select, number, text, boolean, hidden)
- `OrgCreditStats` / `OrgSubscriptionInfo` — Organization billing models
- `ApiResponse<T>` / `ApiError` — Standard API envelope
- `Region` enum — NYC1, NYC3, TOR1, SFO1, SFO2, SFO3, LON1

## Authentication

Uses API key auth (`X-API-Key` header), NOT JWT Bearer tokens:

- **Personal keys**: `sv_...` prefix — bills to personal credits
- **Organization keys**: `sv_org_...` prefix — auto-detected via `StartsWith("sv_org_")`, auto-injects `organizationId` on session creation. Use `CreateAsync()` to auto-resolve org ID from the key via `GET /auth/me`.

The API key middleware in ppg-vpl API resolves the key to a user context server-side. This is different from how ppds-ux and vpl-ux authenticate (which use JWT from login flow).

## Important Conventions

1. **JSON serialization** — Uses Newtonsoft.Json with `NullValueHandling.Ignore`. The `gameKey` field serializes as `gameTypeConfigKey` via `[JsonProperty]`.
2. **Status string constants** — The SDK hard-codes status checks: `IsActive()` checks `== "Active"`, `IsPending()` checks `== "Pending"`, etc. If these status values change in the API, the SDK breaks.
3. **Dual callback + async pattern** — All methods return `Task<T>` AND accept optional `onSuccess`/`onError` callbacks for Unity coroutine compatibility.
4. **Org key auto-routing** — `GetMySessionsAsync()` fetches from different endpoints depending on whether it's a personal or org key.
5. **UnityWebRequest** — All HTTP done via `UnityWebRequest` (not HttpClient), required for Unity's threading model.

## API Change Impact

If the ppg-vpl API changes any of the consumed endpoints:

- **Response field renames/removals** — Update `Models.cs` (fields are deserialized by name)
- **Status enum changes** — Update `IsActive()`, `IsPending()`, `IsScheduled()`, `IsStopped()` in `GameSession` class
- **Endpoint path changes** — Update URL strings in `SplatterVaultClient.cs`
- **Auth mechanism changes** — Update `SetRequestHeader("X-API-Key", apiKey)` calls
- **New required request fields** — Update `CreateSessionRequest` and any other request models

## No CLI Build/Test Commands

This is a Unity package, not a standalone project. It is built and tested within Unity projects that import it. Tests run via the Unity Test Runner.
