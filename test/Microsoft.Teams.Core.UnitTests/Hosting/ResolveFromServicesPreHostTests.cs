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

    // --- LogFromServices ---

    [Fact]
    public void LogFromServices_WithNoLoggerFactory_CallsActionWithNullLogger()
    {
        // Arrange
        ServiceCollection services = new();
        ILogger? captured = null;

        // Act
        AddBotApplicationExtensions.LogFromServices(services, l => captured = l);

        // Assert
        Assert.Same(NullLogger.Instance, captured);
    }

    [Fact]
    public void LogFromServices_WithAddLogging_CallsActionWithRealLogger()
    {
        // Arrange — typical ASP.NET Core registration via factory delegate
        ServiceCollection services = new();
        services.AddLogging();
        ILogger? captured = null;

        // Act
        AddBotApplicationExtensions.LogFromServices(services, l => captured = l);

        // Assert
        Assert.NotNull(captured);
        Assert.IsNotType<NullLogger>(captured);
    }

    [Fact]
    public void LogFromServices_WithAddLogging_LoggingDoesNotThrow()
    {
        // Arrange — validates that logging within the action does not throw ObjectDisposedException
        // even though the temporary ServiceProvider is disposed after the action completes
        ServiceCollection services = new();
        services.AddLogging();

        // Act / Assert — should not throw
        AddBotApplicationExtensions.LogFromServices(services, l => l.LogInformation("test message"));
    }

    [Fact]
    public void LogFromServices_WithDirectInstance_CallsActionWithRealLogger()
    {
        // Arrange
        ServiceCollection services = new();
        LoggerFactory factory = new();
        services.AddSingleton<ILoggerFactory>(factory);
        ILogger? captured = null;

        // Act
        AddBotApplicationExtensions.LogFromServices(services, l => captured = l);

        // Assert
        Assert.NotNull(captured);
        Assert.IsNotType<NullLogger>(captured);
    }

    [Fact]
    public void LogFromServices_WithCustomCategory_UsesCategoryType()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddLogging();
        ILogger? captured = null;

        // Act
        AddBotApplicationExtensions.LogFromServices(services, l => captured = l, typeof(ResolveFromServicesPreHostTests));

        // Assert
        Assert.NotNull(captured);
        Assert.IsNotType<NullLogger>(captured);
    }
}
