// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.BotApps.Handlers;

/// <summary>
/// Delegate for handling message activities.
/// </summary>
/// <param name="context"></param>
/// <param name="cancellationToken"></param>
/// <returns></returns>
public delegate Task MessageHandler(Context context, CancellationToken cancellationToken = default);
