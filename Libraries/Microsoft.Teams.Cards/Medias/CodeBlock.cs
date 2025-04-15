using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType CodeBlock = new("CodeBlock");
    public bool IsCodeBlock => CodeBlock.Equals(Value);
}

[JsonConverter(typeof(JsonConverter<CodeLanguage>))]
public partial class CodeLanguage(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly CodeLanguage Bash = new("Bash");
    public bool IsBash => Bash.Equals(Value);

    public static readonly CodeLanguage C = new("C");
    public bool IsC => C.Equals(Value);

    public static readonly CodeLanguage Cpp = new("Cpp");
    public bool IsCpp => Cpp.Equals(Value);

    public static readonly CodeLanguage CSharp = new("CSharp");
    public bool IsCSharp => CSharp.Equals(Value);

    public static readonly CodeLanguage Css = new("Css");
    public bool IsCss => Css.Equals(Value);

    public static readonly CodeLanguage Dos = new("Dos");
    public bool IsDos => Dos.Equals(Value);

    public static readonly CodeLanguage Go = new("Go");
    public bool IsGo => Go.Equals(Value);

    public static readonly CodeLanguage Graphql = new("Graphql");
    public bool IsGraphql => Graphql.Equals(Value);

    public static readonly CodeLanguage Html = new("Html");
    public bool IsHtml => Html.Equals(Value);

    public static readonly CodeLanguage Java = new("Java");
    public bool IsJava => Java.Equals(Value);

    public static readonly CodeLanguage JavaScript = new("JavaScript");
    public bool IsJavaScript => JavaScript.Equals(Value);

    public static readonly CodeLanguage Json = new("Json");
    public bool IsJson => Json.Equals(Value);

    public static readonly CodeLanguage ObjectiveC = new("ObjectiveC");
    public bool IsObjectiveC => ObjectiveC.Equals(Value);

    public static readonly CodeLanguage Perl = new("Perl");
    public bool IsPerl => Perl.Equals(Value);

    public static readonly CodeLanguage PHP = new("Php");
    public bool IsPHP => PHP.Equals(Value);

    public static readonly CodeLanguage PlainText = new("PlainText");
    public bool IsPlainText => PlainText.Equals(Value);

    public static readonly CodeLanguage PowerShell = new("PowerShell");
    public bool IsPowerShell => PowerShell.Equals(Value);

    public static readonly CodeLanguage Python = new("Python");
    public bool IsPython => Python.Equals(Value);

    public static readonly CodeLanguage SQL = new("Sql");
    public bool IsSQL => SQL.Equals(Value);

    public static readonly CodeLanguage TypeScript = new("TypeScript");
    public bool IsTypeScript => TypeScript.Equals(Value);

    public static readonly CodeLanguage VbNet = new("VbNet");
    public bool IsVbNet => VbNet.Equals(Value);

    public static readonly CodeLanguage Verilog = new("Verilog");
    public bool IsVerilog => Verilog.Equals(Value);

    public static readonly CodeLanguage Vhdl = new("Vhdl");
    public bool IsVhdl => Vhdl.Equals(Value);

    public static readonly CodeLanguage XML = new("Xml");
    public bool IsXML => XML.Equals(Value);
}

/// <summary>
/// Displays a block of code with syntax highlighting
/// </summary>
public class CodeBlock() : Element(CardType.CodeBlock)
{
    /// <summary>
    /// which programming language to use.
    /// </summary>
    [JsonPropertyName("language")]
    [JsonPropertyOrder(12)]
    public CodeLanguage? Language { get; set; }

    /// <summary>
    /// code to display/highlight.
    /// </summary>
    [JsonPropertyName("codeSnippet")]
    [JsonPropertyOrder(13)]
    public string? CodeSnippet { get; set; }

    /// <summary>
    /// which line number to display on the first line.
    /// </summary>
    [JsonPropertyName("startLineNumber")]
    [JsonPropertyOrder(14)]
    public int? StartLineNumber { get; set; }

    public CodeBlock WithLanguage(CodeLanguage value)
    {
        Language = value;
        return this;
    }

    public CodeBlock WithCode(string value)
    {
        CodeSnippet = value;
        return this;
    }

    public CodeBlock WithStartLineNumber(int value)
    {
        StartLineNumber = value;
        return this;
    }
}