// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Teams.AI.Models.OpenAI.Extensions;

public static class ServiceProviderExtensions
{
    public static OpenAIChatModel GetOpenAIChatModel(this IServiceProvider provider)
    {
        return provider.GetRequiredService<OpenAIChatModel>();
    }

    public static OpenAIChatPrompt GetOpenAIChatPrompt(this IServiceProvider provider)
    {
        return provider.GetRequiredService<OpenAIChatPrompt>();
    }
}