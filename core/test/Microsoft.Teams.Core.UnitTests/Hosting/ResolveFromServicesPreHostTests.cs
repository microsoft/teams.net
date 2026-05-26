// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Core.Hosting;

namespace Microsoft.Teams.Core.UnitTests.Hosting;

public class ResolveFromServicesPreHostTests
{
    // --- ResolveFromServicesPreHost ---

    [Fact]
    public void ResolveFromServicesPreHost_WithNoRegistration_ReturnsNull()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IConfiguration? result = AddBotApplicationExtensions.ResolveFromServicesPreHost<IConfiguration>(services);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveFromServicesPreHost_WithDirectInstance_ReturnsInstance()
    {
        // Arrange
        ServiceCollection services = new();
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        IConfiguration? result = AddBotApplicationExtensions.ResolveFromServicesPreHost<IConfiguration>(services);

        // Assert
        Assert.Same(configuration, result);
    }

    [Fact]
    public void ResolveFromServicesPreHost_WithFactoryRegistration_ResolvesViaProvider()
    {
        // Arrange
        ServiceCollection services = new();
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(_ => configuration);

        // Act
        IConfiguration? result = AddBotApplicationExtensions.ResolveFromServicesPreHost<IConfiguration>(services);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void ResolveFromServicesPreHost_WithMultipleRegistrations_ReturnsLast()
    {
        // Arrange — DI last-registration-wins behavior
        ServiceCollection services = new();
        IConfigurationRoot first = new ConfigurationBuilder().Build();
        IConfigurationRoot second = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["key"] = "value" }).Build();
        services.AddSingleton<IConfiguration>(first);
        services.AddSingleton<IConfiguration>(second);

        // Act
        IConfiguration? result = AddBotApplicationExtensions.ResolveFromServicesPreHost<IConfiguration>(services);

        // Assert
        Assert.Same(second, result);
    }

    // --- GetLoggerFromServices ---

    [Fact]
    public void GetLoggerFromServices_WithNoLoggerFactory_ReturnsNullLogger()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        ILogger logger = AddBotApplicationExtensions.GetLoggerFromServices(services);

        // Assert
        Assert.Same(NullLogger.Instance, logger);
    }

    [Fact]
    public void GetLoggerFromServices_WithAddLogging_ReturnsRealLogger()
    {
        // Arrange — typical ASP.NET Core registration via factory delegate
        ServiceCollection services = new();
        services.AddLogging();

        // Act
        ILogger logger = AddBotApplicationExtensions.GetLoggerFromServices(services);

        // Assert
        Assert.NotNull(logger);
        Assert.IsNotType<NullLogger>(logger);
    }

    [Fact]
    public void GetLoggerFromServices_WithAddLogging_LoggerRemainsUsableAfterReturn()
    {
        // Arrange — validates corinagum's concern: logger must work after method returns
        ServiceCollection services = new();
        services.AddLogging();

        // Act
        ILogger logger = AddBotApplicationExtensions.GetLoggerFromServices(services);

        // Assert — should not throw ObjectDisposedException
        logger.LogInformation("test message");
    }

    [Fact]
    public void GetLoggerFromServices_WithDirectInstance_ReturnsLoggerFromInstance()
    {
        // Arrange
        ServiceCollection services = new();
        LoggerFactory factory = new();
        services.AddSingleton<ILoggerFactory>(factory);

        // Act
        ILogger logger = AddBotApplicationExtensions.GetLoggerFromServices(services);

        // Assert
        Assert.NotNull(logger);
        Assert.IsNotType<NullLogger>(logger);
    }

    [Fact]
    public void GetLoggerFromServices_WithCustomCategory_UsesCategoryType()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddLogging();

        // Act
        ILogger logger = AddBotApplicationExtensions.GetLoggerFromServices(services, typeof(ResolveFromServicesPreHostTests));

        // Assert
        Assert.NotNull(logger);
        Assert.IsNotType<NullLogger>(logger);
    }
}
