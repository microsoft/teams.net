namespace Microsoft.Teams.Common.Text;

public class ANSI(string value) : StringEnum(value)
{
    public static readonly ANSI Reset = new("\x1b[0m");

    public static readonly ANSI Bold = new("\x1b[1m");
    public static readonly ANSI BoldReset = new("\x1b[22m");
    public static readonly ANSI Italic = new("\x1b[3m");
    public static readonly ANSI ItalicReset = new("\x1b[23m");
    public static readonly ANSI Underline = new("\x1b[4m");
    public static readonly ANSI UnderlineReset = new("\x1b[24m");
    public static readonly ANSI Strike = new("\x1b[9m");
    public static readonly ANSI StrikeReset = new("\x1b[29m");

    public static readonly ANSI ForegroundReset = new("\x1b[0m");
    public static readonly ANSI BackgroundReset = new("\x1b[0m");
    public static readonly ANSI ForegroundBlack = new("\x1b[30m");
    public static readonly ANSI BackgroundBlack = new("\x1b[40m");
    public static readonly ANSI ForegroundRed = new("\x1b[31m");
    public static readonly ANSI BackgroundRed = new("\x1b[41m");
    public static readonly ANSI ForegroundGreen = new("\x1b[32m");
    public static readonly ANSI BackgroundGreen = new("\x1b[42m");
    public static readonly ANSI ForegroundYellow = new("\x1b[33m");
    public static readonly ANSI BackgroundYellow = new("\x1b[43m");
    public static readonly ANSI ForegroundBlue = new("\x1b[34m");
    public static readonly ANSI BackgroundBlue = new("\x1b[44m");
    public static readonly ANSI ForegroundMagenta = new("\x1b[35m");
    public static readonly ANSI BackgroundMagenta = new("\x1b[45m");
    public static readonly ANSI ForegroundCyan = new("\x1b[36m");
    public static readonly ANSI BackgroundCyan = new("\x1b[46m");
    public static readonly ANSI ForegroundWhite = new("\x1b[37m");
    public static readonly ANSI BackgroundWhite = new("\x1b[47m");
    public static readonly ANSI ForegroundGray = new("\x1b[90m");
    public static readonly ANSI ForegroundDefault = new("\x1b[39m");
    public static readonly ANSI BackgroundDefault = new("\x1b[49m");
}