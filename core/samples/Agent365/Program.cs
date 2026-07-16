// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();
ILogger logger = webApp.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Agent365");

teamsApp.OnAgentLifecycle(async (context, cancellationToken) =>
{
    LogLifecycleEnvelope(context.Activity, "all", logger);
    await context.SendAsync(
        $"Received Agent 365 lifecycle event `{context.Activity.ValueType}`.",
        cancellationToken);
});

teamsApp.OnAgenticUserIdentityCreated((context, _) =>
{
    AgenticUserIdentityCreatedValue? value = context.Activity.Value;
    LogTypedLifecycleEnvelope(context.Activity, "identity_created", logger);
    logger.LogInformation(
        "[Agent365 lifecycle:identity_created] details expirationDateTime={ExpirationDateTime} managerUserId={ManagerUserId} managerEmail={ManagerEmail}",
        value?.ExpirationDateTime,
        value?.Manager?.UserId,
        value?.Manager?.Email);
    return Task.CompletedTask;
});

teamsApp.OnAgenticUserIdentityUpdated((context, _) =>
{
    AgenticUserIdentityUpdatedValue? value = context.Activity.Value;
    LogTypedLifecycleEnvelope(context.Activity, "identity_updated", logger);
    logger.LogInformation(
        "[Agent365 lifecycle:identity_updated] details propertyName={PropertyName} propertyValue={PropertyValue}",
        value?.UpdatedProperty.PropertyName,
        value?.UpdatedProperty.PropertyValue);
    return Task.CompletedTask;
});

teamsApp.OnAgenticUserManagerUpdated((context, _) =>
{
    AgenticUserManagerUpdatedValue? value = context.Activity.Value;
    LogTypedLifecycleEnvelope(context.Activity, "manager_updated", logger);
    logger.LogInformation(
        "[Agent365 lifecycle:manager_updated] details managerId={ManagerId}",
        value?.Manager?.ManagerId);
    return Task.CompletedTask;
});

teamsApp.OnAgenticUserEnabled((context, _) =>
{
    LogTypedLifecycleEnvelope(context.Activity, "enabled", logger);
    return Task.CompletedTask;
});

teamsApp.OnAgenticUserDisabled((context, _) =>
{
    LogTypedLifecycleEnvelope(context.Activity, "disabled", logger);
    return Task.CompletedTask;
});

teamsApp.OnAgenticUserDeleted((context, _) =>
{
    AgenticUserDeletedValue? value = context.Activity.Value;
    LogTypedLifecycleEnvelope(context.Activity, "deleted", logger);
    logger.LogInformation(
        "[Agent365 lifecycle:deleted] details deletionReason={DeletionReason}",
        value?.DeletionReason);
    return Task.CompletedTask;
});

teamsApp.OnAgenticUserUndeleted((context, _) =>
{
    LogTypedLifecycleEnvelope(context.Activity, "undeleted", logger);
    return Task.CompletedTask;
});

teamsApp.OnAgenticUserWorkloadOnboardingUpdated((context, _) =>
{
    AgenticUserWorkloadOnboardingUpdatedValue? value = context.Activity.Value;
    LogTypedLifecycleEnvelope(context.Activity, "workload_onboarding_updated", logger);
    logger.LogInformation(
        "[Agent365 lifecycle:workload_onboarding_updated] details workloadName={WorkloadName} workloadOnboardingState={WorkloadOnboardingState}",
        value?.WorkloadName,
        value?.WorkloadOnboardingState);
    return Task.CompletedTask;
});

teamsApp.OnMessage(async (context, cancellationToken) =>
{
    logger.LogInformation(
        "[Agent365 reactive] Message received text={Text} from={FromId} recipient={RecipientId}",
        context.Activity.Text,
        context.Activity.From?.Id,
        context.Activity.Recipient?.Id);

    await context.TypingAsync(cancellationToken);
    await context.SendAsync($"You said \"{context.Activity.Text}\"", cancellationToken);
});

webApp.Run();

static void LogTypedLifecycleEnvelope<TValue>(
    AgentLifecycleEventActivity<TValue> activity,
    string handlerName,
    ILogger logger) where TValue : AgentLifecycleValueBase
{
    TValue? value = activity.Value;
    logger.LogInformation(
        "[Agent365 lifecycle:{HandlerName}] envelope name={Name} valueType={ValueType} channelId={ChannelId} from={FromId} agenticUserId={RecipientAgenticUserId} agenticAppId={RecipientAgenticAppId} agenticAppBlueprintId={RecipientAgenticAppBlueprintId} tenantId={RecipientTenantId}",
        handlerName,
        activity.Name,
        activity.ValueType,
        activity.ChannelId,
        activity.From?.Id,
        activity.Recipient?.AgenticUserId,
        activity.Recipient?.AgenticAppId,
        activity.Recipient?.AgenticAppBlueprintId,
        activity.Recipient?.TenantId);

    logger.LogInformation(
        "[Agent365 lifecycle:{HandlerName}] value tenantId={TenantId} agenticUserId={AgenticUserId} agenticAppInstanceId={AgenticAppInstanceId} agentIdentityBlueprintId={AgentIdentityBlueprintId} version={Version}",
        handlerName,
        value?.TenantId,
        value?.AgenticUserId,
        value?.AgenticAppInstanceId,
        value?.AgentIdentityBlueprintId,
        value?.Version);
}

static void LogLifecycleEnvelope(
    AgentLifecycleEventActivity activity,
    string handlerName,
    ILogger logger)
{
    logger.LogInformation(
        "[Agent365 lifecycle:{HandlerName}] envelope name={Name} valueType={ValueType} channelId={ChannelId} from={FromId} agenticUserId={RecipientAgenticUserId} agenticAppId={RecipientAgenticAppId} agenticAppBlueprintId={RecipientAgenticAppBlueprintId} tenantId={RecipientTenantId}",
        handlerName,
        activity.Name,
        activity.ValueType,
        activity.ChannelId,
        activity.From?.Id,
        activity.Recipient?.AgenticUserId,
        activity.Recipient?.AgenticAppId,
        activity.Recipient?.AgenticAppBlueprintId,
        activity.Recipient?.TenantId);
}
