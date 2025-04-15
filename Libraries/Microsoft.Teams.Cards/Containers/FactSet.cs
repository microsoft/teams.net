using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType FactSet = new("FactSet");
    public bool IsFactSet => FactSet.Equals(Value);
}

/// <summary>
/// The `FactSet` element displays a series of facts (i.e. name/value pairs) in a tabular form.
/// </summary>
public class FactSet(params Fact[] facts) : Element(CardType.FactSet)
{
    /// <summary>
    /// The array of `Fact`'s
    /// </summary>
    [JsonPropertyName("facts")]
    [JsonPropertyOrder(12)]
    public IList<Fact> Facts { get; set; } = facts;

    public FactSet AddFacts(params Fact[] facts)
    {
        foreach (var fact in facts)
        {
            Facts.Add(fact);
        }

        return this;
    }
}

/// <summary>
/// Describes a `Fact` in a `FactSet` as a key/value pair.
/// </summary>
public class Fact()
{
    /// <summary>
    /// The title of the fact.
    /// </summary>
    [JsonPropertyName("title")]
    [JsonPropertyOrder(0)]
    public required string Title { get; set; }

    /// <summary>
    /// The value of the fact.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(1)]
    public required string Value { get; set; }
}