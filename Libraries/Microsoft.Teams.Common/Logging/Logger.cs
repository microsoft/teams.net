// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Common.Logging;

public partial interface ILogger<T> : ILogger, ICloneable;
public partial interface ILogger
{
    public void Error(params object?[] args);
    public void Warn(params object?[] args);
    public void Info(params object?[] args);
    public void Debug(params object?[] args);
    public void Log(LogLevel level, params object?[] args);
    public ILogger Create(string name);
    public ILogger Child(string name);
    public ILogger Peer(string name);
    public ILogger Copy();
    public bool IsEnabled(LogLevel level);
    public ILogger SetLevel(LogLevel level);
}