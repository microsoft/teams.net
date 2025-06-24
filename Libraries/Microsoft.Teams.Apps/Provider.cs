// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps;

internal partial interface IProvider
{
    public object? Resolve();
}

internal partial class ValueProvider(object? value) : IProvider
{
    public object? UseValue { get; set; } = value;

    public object? Resolve()
    {
        return UseValue;
    }
}

internal partial class ValueProvider<T>(T? value) : IProvider
{
    public T? UseValue { get; set; } = value;

    public object? Resolve()
    {
        return UseValue;
    }
}

internal partial class FactoryProvider(FactoryProvider.FactoryProviderDelegate factory) : IProvider
{
    public FactoryProviderDelegate UseFactory { get; set; } = factory;

    public object? Resolve()
    {
        return UseFactory();
    }

    public delegate object? FactoryProviderDelegate();
}

internal partial class FactoryProvider<T>(FactoryProvider<T>.FactoryProviderDelegate factory) : IProvider
{
    public FactoryProviderDelegate UseFactory { get; set; } = factory;

    public object? Resolve()
    {
        return UseFactory();
    }

    public delegate T? FactoryProviderDelegate();
}