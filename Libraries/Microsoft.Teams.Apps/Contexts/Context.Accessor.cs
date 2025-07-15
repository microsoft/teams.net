// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps;

public partial interface IContext
{
    /// <summary>
    /// Accessor
    /// 
    /// based on https://github.com/dotnet/aspnetcore/blob/main/src/Http/Http/src/HttpContextAccessor.cs
    /// </summary>
    public class Accessor
    {
        private static readonly AsyncLocal<ContextHolder> _async = new();

        public IContext<IActivity>? Value
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
            public IContext<IActivity>? Context { get; set; }

            public void Clear()
            {
                Context = null;
            }
        }
    }
}