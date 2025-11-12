// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Plugins.Agents;

/// <summary>
/// Accessor
/// 
/// based on https://github.com/dotnet/aspnetcore/blob/main/src/Http/Http/src/HttpContextAccessor.cs
/// </summary>
public class TurnContextAccessor
{
    private static readonly AsyncLocal<ContextHolder> _async = new();

    public Microsoft.Agents.Builder.ITurnContext? Value
    {
        get => _async.Value?.Context;
        internal set
        {
            _async.Value?.Clear();

            if (value is not null)
            {
                _async.Value = new() { Context = value };
            }
        }
    }

    private sealed class ContextHolder
    {
        public Microsoft.Agents.Builder.ITurnContext? Context { get; set; }

        public void Clear()
        {
            Context = null;
        }
    }
}