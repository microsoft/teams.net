// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace A2ABot.A2A;

// Payload carried in the A2A DataPart when one bot hands a user off to the
// other. The receiver uses AadObjectId + TenantId + ServiceUrl to create a
// 1:1 conversation with the user and message them proactively.
internal record HandoffMessage(
    string Kind,
    string AadObjectId,
    string UserName,
    string Summary,
    string From,
    string TenantId,
    string ServiceUrl);
