// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Common;

public interface IBuilder<T>
{
    public T Build();
}