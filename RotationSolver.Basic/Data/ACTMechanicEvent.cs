using System.Text.Json.Serialization;

namespace RotationSolver.Basic.Data;

/// <summary>
/// Represents a mechanic event from ACT/Triggernometry for predictive mitigation.
/// </summary>
public record ACTMechanicEvent
{
    /// <summary>
    /// The type of mechanic detected.
    /// </summary>
    [JsonPropertyName("type")]
    public MechanicType Type { get; init; }

    /// <summary>
    /// The name of the mechanic (e.g., "Tankbuster", "Cleave", "Stack Marker").
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The source of the mechanic (boss/enemy name or ID).
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// The target of the mechanic (player name, "party", "aoe", etc.).
    /// </summary>
    [JsonPropertyName("target")]
    public string Target { get; init; } = string.Empty;

    /// <summary>
    /// Time until mechanic resolves (in seconds).
    /// </summary>
    [JsonPropertyName("timeUntilImpact")]
    public float TimeUntilImpact { get; init; }

    /// <summary>
    /// The damage type expected (physical, magical, or unknown).
    /// </summary>
    [JsonPropertyName("damageType")]
    public MechanicDamageType DamageType { get; init; }

    /// <summary>
    /// Expected damage severity level.
    /// </summary>
    [JsonPropertyName("severity")]
    public MechanicSeverity Severity { get; init; }

    /// <summary>
    /// Optional mechanic ID for tracking specific mechanics.
    /// </summary>
    [JsonPropertyName("mechanicId")]
    public string? MechanicId { get; init; }

    /// <summary>
    /// Timestamp when the event was received.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime ReceivedAt { get; init; } = DateTime.Now;

    /// <summary>
    /// Whether this event has been processed/acted upon.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsProcessed { get; set; } = false;

    /// <summary>
    /// Checks if this event is still valid (not expired).
    /// </summary>
    public bool IsValid => !IsProcessed && (DateTime.Now - ReceivedAt).TotalSeconds < TimeUntilImpact + 5;

    /// <summary>
    /// Gets the remaining time until impact.
    /// </summary>
    public float RemainingTime => Math.Max(0, TimeUntilImpact - (float)(DateTime.Now - ReceivedAt).TotalSeconds);
}

/// <summary>
/// Types of mechanics that can be detected.
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
public enum MechanicType
{
    /// <summary>
    /// Single-target heavy damage on tank (tankbuster).
    /// </summary>
    Tankbuster,

    /// <summary>
    /// Party-wide AoE damage.
    /// </summary>
    PartyAoE,

    /// <summary>
    /// Stack marker requiring players to stack.
    /// </summary>
    StackMarker,

    /// <summary>
    /// Spread marker requiring players to spread.
    /// </summary>
    SpreadMarker,

    /// <summary>
    /// Directed AoE at specific player(s).
    /// </summary>
    TargetedAoE,

    /// <summary>
    /// Line/cone cleave attack.
    /// </summary>
    Cleave,

    /// <summary>
    /// Raid-wide damage.
    /// </summary>
    RaidWide,

    /// <summary>
    /// Proximity damage.
    /// </summary>
    Proximity,

    /// <summary>
    /// Other or unknown mechanic type.
    /// </summary>
    Other
}

/// <summary>
/// Damage types for mechanics.
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
public enum MechanicDamageType
{
    /// <summary>
    /// Damage type unknown.
    /// </summary>
    Unknown,

    /// <summary>
    /// Physical damage.
    /// </summary>
    Physical,

    /// <summary>
    /// Magical damage.
    /// </summary>
    Magical,

    /// <summary>
    /// Both physical and magical (hybrid).
    /// </summary>
    Hybrid,

    /// <summary>
    /// Pure damage (bypasses most mitigation).
    /// </summary>
    Pure
}

/// <summary>
/// Severity levels for mechanic damage.
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
public enum MechanicSeverity
{
    /// <summary>
    /// Minor damage, not worth major cooldowns.
    /// </summary>
    Minor,

    /// <summary>
    /// Moderate damage, use standard mitigation.
    /// </summary>
    Moderate,

    /// <summary>
    /// Heavy damage, use strong mitigation.
    /// </summary>
    Heavy,

    /// <summary>
    /// Deadly damage, use all available mitigation.
    /// </summary>
    Lethal
}
