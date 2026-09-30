using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace SplatterVault
{
    /// <summary>
    /// Available server regions
    /// </summary>
    public enum Region
    {
        // US East
        NYC1,   // New York 1
        NYC3,   // New York 3
        ATL1,   // Atlanta
        RIC1,   // Richmond
        TOR1,   // Toronto

        // US West
        SFO2,   // San Francisco 2
        SFO3,   // San Francisco 3

        // Europe
        LON1,   // London
        AMS3,   // Amsterdam
        FRA1,   // Frankfurt

        // Asia-Pacific
        SGP1,   // Singapore
        BLR1,   // Bangalore
        SYD1    // Sydney
    }

    /// <summary>
    /// Extension methods for enum to string conversion
    /// </summary>
    public static class EnumExtensions
    {
        public static string ToApiString(this Region region)
        {
            return region.ToString();
        }
    }

    /// <summary>
    /// Request model for creating a new game session
    /// </summary>
    [Serializable]
    public class CreateSessionRequest
    {
        public string region = Region.NYC3.ToApiString();

        /// <summary>
        /// Game configuration key (e.g., "sys_1774636058786_30e0fc4d").
        /// Get this from your SplatterVault dashboard.
        /// Serializes as "gameTypeConfigKey" for the API.
        /// </summary>
        [JsonProperty("gameTypeConfigKey")]
        public string gameKey;

        public string friendlyName;
        public string scheduledStartTime; // ISO 8601 format
        public string scheduledEndTime;   // ISO 8601 format
        public int? serverSizeId;         // Optional: server size ID (defaults per game type)
        public int? organizationId;       // Optional: bill to organization credits instead of personal
        public int? buildId;              // Optional: use a specific build
        public string channel;            // Optional: use the build deployed to this channel (e.g., "stable", "beta")

        /// <summary>
        /// Launch argument overrides. Keys are the arg flag (e.g., "-mstRoomMode"),
        /// values are the override value. Use GetConfigurableArgsAsync() to discover
        /// available arguments for a game.
        /// Transmitted to the API as "environmentVariables".
        /// </summary>
        [JsonProperty("environmentVariables")]
        public Dictionary<string, object> customVariables;

        /// <summary>
        /// When true, the session is automatically stopped if the game server process exits
        /// (either cleanly with code 0 or via crash). When omitted, the platform falls back to
        /// the game-type default configured by the game owner. Explicit false overrides a true
        /// game-type default — set this if you want the session to keep running across game
        /// process restarts.
        /// Serializes as "autoDestroyOnProcessExit".
        /// </summary>
        [JsonProperty("autoDestroyOnProcessExit", NullValueHandling = NullValueHandling.Ignore)]
        public bool? autoDestroyOnProcessExit;

        /// <summary>
        /// Stop the session automatically after this many minutes with no network connections
        /// (5–1440). Omit (null) to keep today's behaviour: the session is never idle-stopped.
        /// The clock starts when the server is ready, so a server nobody joins is stopped after
        /// this long. Pool-served sessions are returned to their pool. The stopped session reports
        /// stopReason "IDLE_TIMEOUT" (see <see cref="GameSession.IsIdleTimedOut"/>).
        /// The range is checked by the server: any other value makes the create call fail with 400.
        /// Serializes as "idleTimeoutMinutes".
        /// </summary>
        [JsonProperty("idleTimeoutMinutes", NullValueHandling = NullValueHandling.Ignore)]
        public int? idleTimeoutMinutes;

        /// <summary>
        /// Set the region using strongly-typed enum
        /// </summary>
        public void SetRegion(Region region)
        {
            this.region = region.ToApiString();
        }

        /// <summary>
        /// Set the scheduled start time from a DateTime
        /// </summary>
        public void SetScheduledStartTime(DateTime dateTime)
        {
            scheduledStartTime = dateTime.ToUniversalTime().ToString("o");
        }

        /// <summary>
        /// Set the scheduled end time (auto-stop) from a DateTime
        /// </summary>
        public void SetScheduledEndTime(DateTime dateTime)
        {
            scheduledEndTime = dateTime.ToUniversalTime().ToString("o");
        }

        /// <summary>
        /// Set the organization ID to bill session to org credits
        /// </summary>
        public void SetOrganizationId(int orgId)
        {
            organizationId = orgId;
        }

        /// <summary>
        /// Set the build ID to use a specific game build
        /// </summary>
        public void SetBuildId(int id)
        {
            buildId = id;
        }

        /// <summary>
        /// Set the build channel name (e.g., "stable", "beta", "dev").
        /// The server will use the build currently deployed to this channel.
        /// If omitted, the game's default channel is used.
        /// </summary>
        public void SetChannel(string channelName)
        {
            channel = channelName;
        }

        /// <summary>
        /// Configure whether the session should auto-stop when the game process exits.
        /// Pass true to opt in, false to opt out of the game-type default, or leave unset
        /// to inherit the game-type default.
        /// </summary>
        public void SetAutoDestroyOnProcessExit(bool value)
        {
            autoDestroyOnProcessExit = value;
        }

        /// <summary>
        /// Stop the session automatically after this many minutes with no network connections.
        /// The server accepts 5 to 1440 and rejects anything else with 400. Set
        /// <see cref="idleTimeoutMinutes"/> back to null to turn the idle timeout off.
        /// </summary>
        public void SetIdleTimeoutMinutes(int minutes)
        {
            idleTimeoutMinutes = minutes;
        }

        /// <summary>
        /// Add a launch argument override. The flag must exactly match a `flag` value
        /// in the game's launchArguments schema (e.g. "-mstMasterIp"); flags not in the
        /// schema have no effect. Non-user-configurable (hidden/infra) flags CAN be
        /// overridden — use GetConfigurableArgsAsync() only to discover player-facing args.
        ///
        /// Values are validated server-side against the schema (number min/max, select
        /// options, text pattern, required) — except when the request is made with the
        /// owning game's API key, in which case the owner may pass any value for a
        /// declared arg (validation is bypassed).
        /// </summary>
        /// <param name="flag">Argument flag (e.g., "-mstRoomMode", "-maxPlayers")</param>
        /// <param name="value">Override value</param>
        public void AddCustomVariable(string flag, object value)
        {
            if (customVariables == null)
                customVariables = new Dictionary<string, object>();
            customVariables[flag] = value;
        }

        /// <summary>
        /// Set multiple launch argument overrides at once
        /// </summary>
        public void SetCustomVariables(Dictionary<string, object> variables)
        {
            customVariables = variables;
        }

        /// <summary>
        /// Clear all launch argument overrides
        /// </summary>
        public void ClearCustomVariables()
        {
            customVariables = null;
        }
    }

    /// <summary>
    /// Game session model (response from API)
    /// </summary>
    [Serializable]
    public class GameSession
    {
        public int id;
        public string code;
        public string serverName;
        public string hostname;
        public string friendlyName;
        public string status;
        public string gameType;  // Resolved game type name (read-only from API)
        public string region;
        public string mode;      // Resolved mode (read-only from API)
        public string scheduledStartTime;
        public string scheduledEndTime;
        public string serverStart;
        public string serverStoppedAt;
        public string slaveIp;
        public int? slavePort;
        public int createdById;
        public string serverType; // "Credit", "Subscription", etc.
        public int serverSizeId;
        public int? buildId;
        public int? volumeId;
        public bool creditsDeducted;
        public int? organizationId;
        public string stopReason;
        public object stopReasonDetails;

        /// <summary>
        /// Idle timeout set at creation, in minutes. Null = the session is never idle-stopped.
        /// </summary>
        public int? idleTimeoutMinutes;

        /// <summary>
        /// Start of the current no-connection period (ISO 8601, UTC), as last reported by the
        /// server. Null while players are connected, before monitoring starts, or when the
        /// session has no idle timeout. Use <see cref="GetIdleSince"/> for a DateTime.
        /// </summary>
        public string idleSince;

        /// <summary>
        /// When the server last reported connection activity for this session (ISO 8601, UTC).
        /// Stays null on a session with an idle timeout until monitoring starts; if it stays
        /// null long after the session is Active, the server is not monitoring it and it will
        /// not be idle-stopped.
        /// </summary>
        public string lastPeerSampleAt;

        /// <summary>
        /// Last idle-monitor state: "starting", "active", "empty" or "unknown". Null when the
        /// session is not monitored.
        /// </summary>
        public string idleMonitorState;

        /// <summary>
        /// Server size details (populated when API includes the relation)
        /// </summary>
        public ServerSizeInfo serverSize;

        public DateTime? GetScheduledStartTime()
        {
            if (string.IsNullOrEmpty(scheduledStartTime)) return null;
            return DateTime.Parse(scheduledStartTime);
        }

        public DateTime? GetScheduledEndTime()
        {
            if (string.IsNullOrEmpty(scheduledEndTime)) return null;
            return DateTime.Parse(scheduledEndTime);
        }

        public DateTime? GetServerStartTime()
        {
            if (string.IsNullOrEmpty(serverStart)) return null;
            return DateTime.Parse(serverStart);
        }

        /// <summary>
        /// Start of the current no-connection period as a UTC DateTime, or null.
        /// </summary>
        public DateTime? GetIdleSince() => ParseUtcTimestamp(idleSince);

        /// <summary>
        /// When the server last reported connection activity, as a UTC DateTime, or null.
        /// </summary>
        public DateTime? GetLastPeerSampleAt() => ParseUtcTimestamp(lastPeerSampleAt);

        public bool IsActive() => status == "Active";
        public bool IsPending() => status == "Pending";
        public bool IsScheduled() => status == "Scheduled";
        public bool IsStopped() => status == "Not Active";

        /// <summary>
        /// True when the session was stopped by its idle timeout (stopReason "IDLE_TIMEOUT").
        /// </summary>
        public bool IsIdleTimedOut() => stopReason == "IDLE_TIMEOUT";

        public int GetServerPort() => slavePort ?? 8100;

        private static DateTime? ParseUtcTimestamp(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            DateTime parsed;
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
                return parsed;
            return null;
        }
    }

    /// <summary>
    /// A structured launch argument definition from the game config.
    /// Returned by GetConfigurableArgsAsync() — use these to build
    /// dynamic UI for game-specific options.
    /// </summary>
    [Serializable]
    public class StructuredLaunchArg
    {
        /// <summary>The argument flag (e.g., "-maxPlayers", "-mstRoomMode")</summary>
        public string flag;

        /// <summary>Default value or interpolation template (e.g., "{{maxPlayers}}")</summary>
        public string value;

        /// <summary>Argument type: "text", "number", "boolean", "select", "hidden"</summary>
        public string type;

        /// <summary>Whether this arg is shown in session creation UI</summary>
        public bool userConfigurable;

        /// <summary>Display label (e.g., "Max Players")</summary>
        public string label;

        /// <summary>Help text / description</summary>
        public string description;

        /// <summary>Whether a value must be provided</summary>
        public bool required;

        /// <summary>Available options for type="select"</summary>
        public List<SelectOption> options;

        /// <summary>Minimum value for type="number"</summary>
        public float? min;

        /// <summary>Maximum value for type="number"</summary>
        public float? max;

        /// <summary>Value when enabled for type="boolean"</summary>
        public string trueValue;

        /// <summary>Value when disabled for type="boolean" (null = omit flag)</summary>
        public string falseValue;

        /// <summary>Regex pattern for type="text" validation</summary>
        public string pattern;

        /// <summary>Semantic hint: "mode", "password", "serverName"</summary>
        public string semantic;

        /// <summary>When true, this arg is omitted for public sessions</summary>
        public bool excludeWhenPublic;
    }

    /// <summary>
    /// Option for select-type launch arguments
    /// </summary>
    [Serializable]
    public class SelectOption
    {
        public string label;
        public string value;
    }

    /// <summary>
    /// Server size info returned as a nested object on session responses
    /// </summary>
    [Serializable]
    public class ServerSizeInfo
    {
        public int id;
        public string friendlyName;
        public float creditsPerMinute;
    }

    /// <summary>
    /// Result from stopping a credit-based session
    /// </summary>
    [Serializable]
    public class StopSessionResult
    {
        public GameSession session;
        public float totalHours;
        public float totalCost;
    }

    /// <summary>
    /// Credit balance information
    /// </summary>
    [Serializable]
    public class CreditBalance
    {
        public int id;
        public float balance;
        public float subscriptionBalance;
        public float adHocBalance;
        public bool subscriptionCreditsFrozen;
        public bool isInGracePeriod;
        public float totalPurchased;
        public float totalUsed;
        public bool alertsEnabled;
        public float alertThreshold;
        public string lastAlertSent;
        public string createdAt;
        public string updatedAt;

        public float GetAvailableBalance()
        {
            if (subscriptionCreditsFrozen)
                return adHocBalance;
            return subscriptionBalance + adHocBalance;
        }

        public float GetBalanceInHours(float creditsPerMinute)
        {
            if (creditsPerMinute <= 0) return 0;
            return GetAvailableBalance() / creditsPerMinute / 60f;
        }

        public bool HasEnoughCredits(float minutes, float creditsPerMinute)
        {
            return GetAvailableBalance() >= minutes * creditsPerMinute;
        }
    }

    [Serializable]
    public class CreditStats
    {
        public float balance;
        public float subscriptionBalance;
        public float adHocBalance;
        public bool subscriptionCreditsFrozen;
        public bool canStartSession;
        public float totalPurchased;
        public float totalUsed;
        public float monthlyUsage;
        public List<CreditTransaction> recentTransactions;
    }

    [Serializable]
    public class CreditTransaction
    {
        public int id;
        public float amount;
        public string type;
        public string description;
        public string createdAt;
    }

    [Serializable]
    public class ApiResponse<T>
    {
        public T data;
        public string message;
        public string name;
        public int status;
        public List<string> errors;
    }

    [Serializable]
    public class ApiError
    {
        public string name;
        public string message;
        public int status;
        public List<string> errors;
    }

    [Serializable]
    public class Subscription
    {
        public int id;
        public string tier;
        public string status;
        public string periodStart;
        public string periodEnd;
        public int monthlyCredits;
        public int currentInstances;
    }

    [Serializable]
    public class SubscriptionDetails
    {
        public Subscription current;
        public List<Subscription> all;
    }

    [Serializable]
    public class UsageStats
    {
        public int currentInstances;
        public int maxInstances;
        public float creditBalance;
        public float creditHours;
        public int monthlyCredits;
        public int totalSessions;
        public int activeSessionsThisMonth;
    }

    [Serializable]
    public class OrgCreditStats
    {
        public float balance;
        public float subscriptionBalance;
        public float adHocBalance;
        public float totalPurchased;
        public float totalUsed;
        public bool autoBuyEnabled;
        public float? autoBuyThreshold;
        public float? autoBuyCreditAmount;

        public float GetAvailableBalance()
        {
            return subscriptionBalance + adHocBalance;
        }
    }

    [Serializable]
    public class OrgSubscriptionInfo
    {
        public Subscription current;
        public List<Subscription> all;
    }

    [Serializable]
    public class CancelScheduleResult
    {
        public string message;
        public GameSession session;
    }

    /// <summary>
    /// Authentication context returned by GET /auth/me.
    /// Describes the caller's identity and, for org API keys, the resolved organization.
    /// </summary>
    [Serializable]
    public class AuthContext
    {
        /// <summary>"org_api_key", "user_api_key", or "user"</summary>
        public string type;
        public int? organizationId;
        public string[] permissions;
        public int? userId;
        public string email;
        public string displayName;
        public string organizationName;
    }

    /// <summary>
    /// Meta account link status for the authenticated user
    /// </summary>
    [Serializable]
    public class MetaLinkStatus
    {
        public bool linked;
        public string metaUsername;
        public string orgScopedId;
    }

    /// <summary>
    /// Response from POST /auth/meta/login
    /// </summary>
    [Serializable]
    public class MetaLoginResponse
    {
        public bool success;
        public string token;
        public string refreshToken;
        public string error;
        public MetaLoginUser user;
    }

    /// <summary>
    /// User info returned in the Meta login response
    /// </summary>
    [Serializable]
    public class MetaLoginUser
    {
        public int id;
        public string email;
        public string displayName;
    }

    /// <summary>
    /// Response from POST /auth/refresh-token
    /// </summary>
    [Serializable]
    public class TokenRefreshResponse
    {
        public bool success;
        public string error;
        public TokenRefreshData data;
    }

    /// <summary>
    /// Nested data object in the token refresh response
    /// </summary>
    [Serializable]
    public class TokenRefreshData
    {
        public string token;
        public string refreshToken;
    }
}
