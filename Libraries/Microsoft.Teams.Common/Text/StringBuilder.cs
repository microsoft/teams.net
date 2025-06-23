// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;

namespace Microsoft.Teams.Common.Text;

public static class StringBuilderExtensions
{
    public static StringBuilder Reset(this StringBuilder builder)
    {
        return builder.Append(ANSI.Reset);
    }

    public static StringBuilder Append(this StringBuilder builder, ANSI code, string text)
    {
        return builder.Append(code).Append(text).Append(ANSI.Reset);
    }

    public static StringBuilder Bold(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.Bold).Append(text).Append(ANSI.BoldReset);
    }

    public static StringBuilder Italic(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.Italic).Append(text).Append(ANSI.ItalicReset);
    }

    public static StringBuilder Underline(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.Underline).Append(text).Append(ANSI.UnderlineReset);
    }

    public static StringBuilder Strike(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.Strike).Append(text).Append(ANSI.StrikeReset);
    }

    public static StringBuilder Black(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.ForegroundBlack).Append(text).Append(ANSI.ForegroundReset);
    }

    public static StringBuilder BgBlack(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.BackgroundBlack).Append(text).Append(ANSI.BackgroundReset);
    }

    public static StringBuilder Red(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.ForegroundRed).Append(text).Append(ANSI.ForegroundReset);
    }

    public static StringBuilder BgRed(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.BackgroundRed).Append(text).Append(ANSI.BackgroundReset);
    }

    public static StringBuilder Green(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.ForegroundGreen).Append(text).Append(ANSI.ForegroundReset);
    }

    public static StringBuilder BgGreen(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.BackgroundGreen).Append(text).Append(ANSI.BackgroundReset);
    }

    public static StringBuilder Yellow(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.ForegroundYellow).Append(text).Append(ANSI.ForegroundReset);
    }

    public static StringBuilder BgYellow(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.BackgroundYellow).Append(text).Append(ANSI.BackgroundReset);
    }

    public static StringBuilder Blue(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.ForegroundBlue).Append(text).Append(ANSI.ForegroundReset);
    }

    public static StringBuilder BgBlue(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.BackgroundBlue).Append(text).Append(ANSI.BackgroundReset);
    }

    public static StringBuilder Magenta(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.ForegroundMagenta).Append(text).Append(ANSI.ForegroundReset);
    }

    public static StringBuilder BgMagenta(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.BackgroundMagenta).Append(text).Append(ANSI.BackgroundReset);
    }

    public static StringBuilder Cyan(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.ForegroundCyan).Append(text).Append(ANSI.ForegroundReset);
    }

    public static StringBuilder BgCyan(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.BackgroundCyan).Append(text).Append(ANSI.BackgroundReset);
    }

    public static StringBuilder White(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.ForegroundWhite).Append(text).Append(ANSI.ForegroundReset);
    }

    public static StringBuilder BgWhite(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.BackgroundWhite).Append(text).Append(ANSI.BackgroundReset);
    }

    public static StringBuilder Gray(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.ForegroundGray).Append(text).Append(ANSI.ForegroundReset);
    }

    public static StringBuilder Default(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.ForegroundDefault).Append(text).Append(ANSI.ForegroundReset);
    }

    public static StringBuilder BgDefault(this StringBuilder builder, string text)
    {
        return builder.Append(ANSI.BackgroundDefault).Append(text).Append(ANSI.BackgroundReset);
    }
}