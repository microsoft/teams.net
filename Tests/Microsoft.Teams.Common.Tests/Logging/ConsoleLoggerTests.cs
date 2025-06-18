
using System.Text;

using Microsoft.Teams.Common.Logging;
using Microsoft.Teams.Common.Text;

namespace Microsoft.Teams.Common.Tests.Logging;

public class ConsoleLoggerTests
{

    [Fact]
    public void ConsoleLogger_DefaultProps()
    {
        // Act
        var logger = new ConsoleLogger();

        // Assert
        Assert.NotNull(logger);
        Assert.Equal(typeof(ConsoleLogger), logger.GetType());
        Assert.True(logger.IsEnabled(LogLevel.Error));
        Assert.True(logger.IsEnabled(LogLevel.Warn));
        Assert.True(logger.IsEnabled(LogLevel.Info));
        Assert.False(logger.IsEnabled(LogLevel.Debug));
        Assert.Equal("testhost", logger.Name);
        Assert.Equal(LogLevel.Info, logger.Level);
    }

    [Fact]
    public void ConsoleLogger_RespectsLogEnvironmentVariable()
    {
        // Arrange
        Environment.SetEnvironmentVariable("LOG_LEVEL", "Warn");

        // Act
        var logger = new ConsoleLogger("TestLogger");

        // Assert
        Assert.False(logger.IsEnabled(LogLevel.Debug));
        Assert.False(logger.IsEnabled(LogLevel.Info));
        Assert.True(logger.IsEnabled(LogLevel.Warn));

        // Cleanup
        Environment.SetEnvironmentVariable("LOG_LEVEL", null);
    }

    [Fact]
    public void ConsoleLogger_LoggingSettings_NonMatchingPattern()
    {
        var loggingSettings = new LoggingSettings();
        loggingSettings.Enable = "Tests*";
        loggingSettings.Level = LogLevel.Error;
        // Act
        var logger = new ConsoleLogger(loggingSettings);

        // Assert
        Assert.NotNull(logger);
        Assert.Equal(typeof(ConsoleLogger), logger.GetType());
        Assert.Equal("testhost", logger.Name);
        Assert.False(logger.IsEnabled(LogLevel.Error));
    }

    [Fact]
    public void ConsoleLogger_LoggingSettings_PatternMatchingName()
    {
        // Arrange
        var loggingSettings = new LoggingSettings();
        loggingSettings.Enable = "testhost*";
        loggingSettings.Level = LogLevel.Warn;

        // Act
        var logger = new ConsoleLogger(loggingSettings);

        // Assert
        Assert.NotNull(logger);
        Assert.Equal("testhost", logger.Name);
        Assert.Equal(logger.Level, logger.Level);
        Assert.Equal(logger.GetType(), logger.GetType());
        Assert.True(logger.IsEnabled(LogLevel.Warn));
    }

    [Fact]
    public void ConsoleLogger_Create()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var message = "Test info message";

        // Act
        ConsoleLogger expectedConsoleLogger = (ConsoleLogger)logger.Create(message);

        // Assert
        Assert.Equal(message, expectedConsoleLogger.Name);
        Assert.Equal(LogLevel.Info, expectedConsoleLogger.Level);
    }

    [Fact]
    public void ConsoleLogger_Create_Interface()
    {
        // Arrange
        var logger = new ConsoleLogger();
        var message = "Test error message";
        // Act
        var expectedLogger = logger.Create(message);

        // Assert
        Assert.NotNull(expectedLogger);
        Assert.Equal(typeof(ConsoleLogger), expectedLogger.GetType());
        Assert.Equal(logger.GetType(), expectedLogger.GetType());
        Assert.Equal("Microsoft.Teams.Common.Logging.ConsoleLogger", expectedLogger.ToString());
    }

    [Fact]
    public void ConsoleLogger_Child_CreatesChildLoggerWithCorrectNameAndLevel()
    {
        // Arrange
        var parentLogger = new ConsoleLogger("ParentLogger", LogLevel.Warn);

        // Act
        var childLogger = (ConsoleLogger)parentLogger.Child("Child");

        // Assert
        Assert.NotNull(childLogger);
        Assert.Equal("ParentLogger.Child", childLogger.Name);
        Assert.Equal(parentLogger.Level, childLogger.Level);
        Assert.Equal(parentLogger.GetType(), childLogger.GetType());
    }


    [Fact]
    public void ConsoleLogger_Peer_CreatesChildLoggerWithDefaultNameAndLevel()
    {
        // Arrange
        var parentLogger = new ConsoleLogger("ParentLogger", LogLevel.Warn);

        // Act
        var peerLogger = (ConsoleLogger)parentLogger.Peer("Peer");

        // Assert
        Assert.NotNull(peerLogger);
        Assert.Equal(".Peer", peerLogger.Name);
        Assert.Equal(parentLogger.Level, peerLogger.Level);
        Assert.Equal(parentLogger.GetType(), peerLogger.GetType());
    }


    [Fact]
    public void ConsoleLogger_Peer_CreatesChildLoggerWithCorrectNamespaceNameAndLevel()
    {
        // Arrange
        var parentLogger = new ConsoleLogger("Microsoft.Teams.ConsoleLogs", LogLevel.Warn);

        // Act
        var peerLogger = (ConsoleLogger)parentLogger.Peer("PeerLogs");

        // Assert
        Assert.NotNull(peerLogger);
        // TODO: Check what name should be expected when Consolelogger is created with "Microsoft", currently it is ".PeerLogs"
        Assert.Equal("Microsoft.Teams.PeerLogs", peerLogger.Name);
        Assert.Equal(parentLogger.Level, peerLogger.Level);
        Assert.Equal(parentLogger.GetType(), peerLogger.GetType());
    }

    [Fact]
    public void ConsoleLogger_SetLevel_UpdatesLevelAndReturnsSelf()
    {
        // Arrange
        var logger = new ConsoleLogger("TestLogger", LogLevel.Info);

        // Act
        var returnedLogger = logger.SetLevel(LogLevel.Debug);

        // Assert
        Assert.Equal(LogLevel.Debug, logger.Level);
        Assert.Same(logger, returnedLogger);
        Assert.True(logger.IsEnabled(LogLevel.Debug));
        Assert.True(logger.IsEnabled(LogLevel.Info));
        Assert.True(logger.IsEnabled(LogLevel.Warn));
        Assert.True(logger.IsEnabled(LogLevel.Error));
    }

    [Fact]
    public void ConsoleLogger_SetLevel_UpdatesLevelErrorOnly()
    {
        // Arrange
        var logger = new ConsoleLogger("TestLogger", LogLevel.Info);

        // Act
        var returnedLogger = logger.SetLevel(LogLevel.Error);

        // Assert
        Assert.Equal(LogLevel.Error, logger.Level);
        Assert.Same(logger, returnedLogger);
        Assert.False(logger.IsEnabled(LogLevel.Debug));
        Assert.False(logger.IsEnabled(LogLevel.Info));
        Assert.False(logger.IsEnabled(LogLevel.Warn));
        Assert.True(logger.IsEnabled(LogLevel.Error));
    }

    [Fact]
    public void ConsoleLogger_LogsDebugMessageToConsole()
    {
        // Arrange
        var logger = new ConsoleLogger("TestLogger", LogLevel.Debug);

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            // Act
            logger.Debug("registered function error ");

            // Assert
            var output = sw.ToString().Replace("\r\n", "\n"); ;
            Assert.Contains("DEBUG", output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("TestLogger", output);
            var expectedString = "\u001b[35m\u001b[1m[DEBUG]\u001b[22m\u001b[0m \u001b[35m\u001b[1mTestLogger\u001b[22m\u001b[0m\u001b[0m registered function error \n";

            Assert.Equal(expectedString, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void ConsoleLogger_LogsInfoMessageToConsole()
    {
        // Arrange
        var logger = new ConsoleLogger("TestLogger", LogLevel.Info);
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            // Act
            logger.Info("Starting to connect ");

            // Assert
            var output = sw.ToString().Replace("\r\n", "\n"); ;
            Assert.Contains("INFO", output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("TestLogger", output);
            var expectedString = "\u001b[36m\u001b[1m[INFO]\u001b[22m\u001b[0m \u001b[36m\u001b[1mTestLogger\u001b[22m\u001b[0m\u001b[0m Starting to connect \n";

            Assert.Equal(expectedString, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void ConsoleLogger_LogsWarningMessageToConsole()
    {
        // Arrange
        var logger = new ConsoleLogger("TestLogger", LogLevel.Warn);
        var testMessage = new StringBuilder()
                .Bold(
                    new StringBuilder()
                        .Yellow("⚠️  Devtools ⚠️")
                        .ToString()
                );
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            // Act
            logger.Warn(testMessage);

            // Assert
            var output = sw.ToString().Replace("\r\n", "\n"); ;
            Assert.Contains("WARN", output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("TestLogger", output);
            var expectedString = "\u001b[33m\u001b[1m[WARN]\u001b[22m\u001b[0m \u001b[33m\u001b[1mTestLogger\u001b[22m\u001b[0m\u001b[0m \u001b[1m\u001b[33m⚠️  Devtools ⚠️\u001b[0m\u001b[22m\n";

            Assert.Equal(expectedString, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void ConsoleLogger_DoesNotLogWarningMessageWhenWarnLevenNotEnabled()
    {
        // Arrange
        var logger = new ConsoleLogger("TestLogger", LogLevel.Error);
        var testMessage = "Some warning";
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            // Act
            logger.Warn(testMessage);

            // Assert
            var output = sw.ToString();
            Assert.Empty(output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void ConsoleLogger_LogsErrorMessageToConsole()
    {
        // Arrange
        var logger = new ConsoleLogger("TestLogger");
        var testMessage = new StringBuilder()
                .Bold(
                    new StringBuilder()
                        .Red("!! Error !!")
                        .ToString()
                );
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            // Act
            logger.Error(testMessage);

            // Assert
            var output = sw.ToString().Replace("\r\n", "\n"); ;
            Assert.Contains("ERROR", output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("TestLogger", output);
            var expectedString = "\u001b[31m\u001b[1m[ERROR]\u001b[22m\u001b[0m \u001b[31m\u001b[1mTestLogger\u001b[22m\u001b[0m\u001b[0m \u001b[1m\u001b[31m!! Error !!\u001b[0m\u001b[22m\n";

            Assert.Equal(expectedString, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}