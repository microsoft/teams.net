// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Bot.Core;

namespace Microsoft.Teams.BotApps.Handlers;

/// <summary>
/// Represents a method that handles an invocation request and returns a response asynchronously.
/// </summary>
/// <param name="context">The context for the invocation, containing request data and metadata required to process the operation. Cannot be
/// null.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation. The default value is <see
/// cref="CancellationToken.None"/>.</param>
/// <returns>A task that represents the asynchronous operation. The task result contains the response to the invocation.</returns>
public delegate Task<InvokeResponse> InvokeHandler(Context context, CancellationToken cancellationToken = default);



