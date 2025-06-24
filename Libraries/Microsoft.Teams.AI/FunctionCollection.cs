// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.AI;

/// <summary>
/// a collection of functions
/// </summary>
public class FunctionCollection : Dictionary<string, IFunction>
{
    /// <summary>
    /// the names of the functions in
    /// the collection
    /// </summary>
    public IList<string> Names => Keys.ToList();

    /// <summary>
    /// the functions in the collection
    /// as a list
    /// </summary>
    public IList<IFunction> List => Values.ToList();

    /// <summary>
    /// check if the collection contains
    /// some function name
    /// </summary>
    /// <param name="name">the function name</param>
    public bool Has(string name) => ContainsKey(name);

    /// <summary>
    /// get a function
    /// </summary>
    /// <param name="name">the function name</param>
    public IFunction? Get(string name) => !Has(name) ? null : this[name];

    /// <summary>
    /// add a function
    /// </summary>
    /// <param name="function">the function to add</param>
    public void Add(IFunction function) => this[function.Name] = function;
}