// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace A2ABot;

// Per-turn user identity captured by the Teams handler and threaded into the
// agent so the handoff tool can read it via AsyncLocal.
internal sealed record TurnIdentity(string AadObjectId, string UserName, string TenantId, string ServiceUrl);
