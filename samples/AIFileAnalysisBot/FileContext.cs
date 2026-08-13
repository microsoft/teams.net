// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Teams.Apps.Files;

namespace AIFileAnalysisBot;

/// <summary>
/// Whether this sample can send a downloaded file to the model, and as what.
/// </summary>
internal enum FileKind
{
    Image,
    Text,
    Unsupported,
}

/// <summary>
/// A downloaded file that <see cref="FileContext.Classify"/> accepted, paired with its kind.
/// </summary>
internal sealed record AnalyzableFile(DownloadedFile File, FileKind Kind);

/// <summary>
/// A model request built from the user's message and their analyzable files.
/// </summary>
/// <param name="Content">The content parts sent as the user message.</param>
/// <param name="Warnings">User-facing explanations for files that were skipped or truncated.</param>
/// <param name="FileCount">Number of files whose content reached the model request.</param>
internal sealed record AnalysisRequest(IList<AIContent> Content, IList<string> Warnings, int FileCount);

internal static class FileContext
{
    // SAMPLE GUARDRAIL: every constant below is a product choice made by this sample, not a Teams SDK or Azure OpenAI
    // limit. They exist to keep one Teams message from turning into an unbounded model request. Pick your own values.
    //
    // DownloadAsync buffers the whole file before any of these are checked, so they bound what reaches the model, not
    // network transfer or process memory.
    private const int MaxFiles = 5;
    private const int MaxTextBytesPerFile = 100 * 1024;
    private const int MaxTotalTextBytes = 250 * 1024;
    private const int MaxImageBytes = 1024 * 1024;

    // SAMPLE GUARDRAIL: the formats this sample is willing to forward. The file API itself delivers any attached
    // file type.
    private static readonly HashSet<string> ImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/gif",
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "c", "cpp", "cs", "css", "csv", "go", "h", "html", "java", "js", "json", "jsx", "md", "py",
        "rb", "rs", "sh", "sql", "toml", "ts", "tsx", "txt", "xml", "yaml", "yml",
    };

    private static readonly Regex TextualContentType =
        new(@"\b(json|xml|javascript|yaml|csv|markdown)\b", RegexOptions.Compiled);

    /// <summary>
    /// SAMPLE GUARDRAIL: decides whether a downloaded file can be sent to the model.
    ///
    /// The response MIME type is preferred, but the platform-supplied extension is a necessary fallback, and that part
    /// is a real file-receive detail rather than a sample preference: Teams commonly omits or misclassifies source
    /// files, reporting .ts as video/vnd.dlna.mpeg-tts for example.
    /// </summary>
    public static FileKind Classify(DownloadedFile file, string? extension)
    {
        string contentType = BaseContentType(file.ContentType);

        if (ImageContentTypes.Contains(contentType))
        {
            return FileKind.Image;
        }

        if (IsTextContentType(contentType) || GetTextExtension(extension, file.Filename) is not null)
        {
            return FileKind.Text;
        }

        return FileKind.Unsupported;
    }

    /// <summary>
    /// Converts already-downloaded files into model content parts.
    ///
    /// The conversion itself is the AI integration. The caps it enforces along the way are SAMPLE GUARDRAILs, and each
    /// one that drops or shortens a file returns a warning so the user is never left guessing what the model saw.
    /// </summary>
    public static AnalysisRequest Prepare(string userText, IList<AnalyzableFile> files)
    {
        List<AIContent> parts =
        [
            new TextContent(string.IsNullOrWhiteSpace(userText)
                ? "Please analyze the attached file content."
                : userText.Trim()),
        ];

        List<string> warnings = [];
        int fileCount = 0;
        int totalTextBytes = 0;

        foreach (AnalyzableFile entry in files.Take(MaxFiles))
        {
            DownloadedFile downloaded = entry.File;

            if (entry.Kind == FileKind.Image)
            {
                if (downloaded.Bytes.Length > MaxImageBytes)
                {
                    warnings.Add($"{downloaded.Filename} was not sent to the model because it is larger than 1 MB.");
                    continue;
                }

                parts.Add(new TextContent($"Attached image: {downloaded.Filename}"));

                // FILE RECEIVE: the downloaded bytes are sent inline instead of handing the model the pre-authorized
                // tempauth download URL, which is a short-lived credential.
                parts.Add(new DataContent(downloaded.Bytes, BaseContentType(downloaded.ContentType)));
                fileCount++;
                continue;
            }

            int remainingBytes = MaxTotalTextBytes - totalTextBytes;
            if (remainingBytes <= 0)
            {
                warnings.Add(
                    $"{downloaded.Filename} was not sent to the model because the combined text-file limit was reached.");
                continue;
            }

            int includedBytes = Math.Min(downloaded.Bytes.Length, Math.Min(MaxTextBytesPerFile, remainingBytes));
            string text = System.Text.Encoding.UTF8.GetString(downloaded.Bytes, 0, includedBytes);
            bool truncated = includedBytes < downloaded.Bytes.Length;
            totalTextBytes += includedBytes;

            parts.Add(new TextContent(string.Join('\n',
            [
                $"Attached file: {downloaded.Filename}",
                string.Empty,
                "<file>",
                text,
                truncated ? "\n[File content truncated by the sample.]" : string.Empty,
                "</file>",
            ])));

            if (truncated)
            {
                warnings.Add($"{downloaded.Filename} was truncated before being sent to the model.");
            }

            fileCount++;
        }

        if (files.Count > MaxFiles)
        {
            warnings.Add(
                $"{files.Count - MaxFiles} additional file(s) were not sent to the model because this sample accepts " +
                $"up to {MaxFiles} files per message.");
        }

        return new AnalysisRequest(parts, warnings, fileCount);
    }

    private static string BaseContentType(string contentType)
        => contentType.Split(';', 2)[0].Trim().ToLowerInvariant();

    private static bool IsTextContentType(string contentType)
        => contentType.StartsWith("text/", StringComparison.Ordinal)
           || TextualContentType.IsMatch(contentType);

    private static string? GetTextExtension(string? extension, string filename)
    {
        string normalized = !string.IsNullOrEmpty(extension)
            ? extension.TrimStart('.')
            : Path.GetExtension(filename).TrimStart('.');

        return TextExtensions.Contains(normalized) ? normalized : null;
    }
}
