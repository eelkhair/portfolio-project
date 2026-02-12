using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace JobBoard.AI.Application.Actions.Drafts.RewriteItem;

public sealed class RewriteItemRequest
{
    public RewriteField Field { get; init; }

    /// <summary>
    /// Single value to rewrite
    /// </summary>
    public string Value { get; set; } = default!;

    /// <summary>
    /// Optional context to guide the rewrite
    /// </summary>
    public RewriteItemContext? Context { get; init; }

    /// <summary>
    /// Optional style controls
    /// </summary>
    public RewriteItemStyle? Style { get; init; }
}

public sealed class RewriteItemContext
{
    public string? Title { get; set; }
    public string? AboutRole { get; set; }

    public IReadOnlyList<string>? Responsibilities { get; set; }
    public IReadOnlyList<string>? Qualifications { get; set; }

    public string? CompanyName { get; set; }
}
public sealed class RewriteItemStyle
{
    public RewriteTone? Tone { get; set; }
    public RewriteFormality? Formality { get; set; }

    public string? Audience { get; set; }

    public int? MaxWords { get; set; }
    public int? NumParagraphs { get; set; }

    /// <summary>
    /// ISO language code (e.g. "en")
    /// </summary>
    public string? Language { get; set; }

    public IReadOnlyList<string>? AvoidPhrases { get; set; }

    public int? BulletsPerSection { get; set; }

    public bool? IncludeEEOBoilerplate { get; set; }
}

public enum RewriteField
{
    [EnumMember(Value = "title")]
    Title,

    [EnumMember(Value = "aboutRole")]
    AboutRole,

    [EnumMember(Value = "responsibilities")]
    Responsibilities,

    [EnumMember(Value = "qualifications")]
    Qualifications
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RewriteTone
{
    [EnumMember(Value = "neutral")]
    Neutral,

    [EnumMember(Value = "professional")]
    Professional,

    [EnumMember(Value = "friendly")]
    Friendly,

    [EnumMember(Value = "concise")]
    Concise,

    [EnumMember(Value = "enthusiastic")]
    Enthusiastic
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RewriteFormality
{
    [EnumMember(Value = "casual")]
    Casual,

    [EnumMember(Value = "neutral")]
    Neutral,

    [EnumMember(Value = "formal")]
    Formal
}