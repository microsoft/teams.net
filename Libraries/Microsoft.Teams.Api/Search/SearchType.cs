using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Search;

[JsonConverter(typeof(JsonConverter<SearchType>))]
public class SearchType(string value) : StringEnum(value)
{
    /// <summary>
    /// The type for Search.
    /// Implies a standard, paginated search operation that expects
    /// one or more templated results to be returned.
    /// </summary>
    public static readonly SearchType Search = new("search");
    public bool IsSearch => Search.Equals(Value);

    /// <summary>
    /// The type for bot SearchAnswer.
    /// Implies a simpler search that does not include pagination,
    /// and most typically only returns a single search result.
    /// </summary>
    public static readonly SearchType SearchAnswer = new("searchAnswer");
    public bool IsSearchAnswer => SearchAnswer.Equals(Value);

    /// <summary>
    /// The type for Typeahead.
    /// Implies a search for a small set of values, most often used
    /// for dynamic auto-complete or type-ahead UI controls.
    /// This search supports pagination.
    /// </summary>
    public static readonly SearchType Typeahead = new("typeahead");
    public bool IsTypeahead => Typeahead.Equals(Value);
}