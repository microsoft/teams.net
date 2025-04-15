namespace Microsoft.Teams.AI;

/// <summary>
/// a collection of functions
/// </summary>
public class FunctionCollection
{
    /// <summary>
    /// the number of functions in the
    /// collection
    /// </summary>
    public int Count => Store.Count;

    /// <summary>
    /// the names of the functions in
    /// the collection
    /// </summary>
    public IList<string> Names => Store.Keys.ToList();

    /// <summary>
    /// the functions in the collection
    /// as a list
    /// </summary>
    public IList<IFunction> List => Store.Values.ToList();

    protected IDictionary<string, IFunction> Store { get; set; }

    public FunctionCollection()
    {
        Store = new Dictionary<string, IFunction>();
    }

    /// <summary>
    /// check if the collection contains
    /// some function name
    /// </summary>
    /// <param name="name">the function name</param>
    public bool Has(string name) => Store.ContainsKey(name);

    /// <summary>
    /// get a function
    /// </summary>
    /// <param name="name">the function name</param>
    public IFunction? Get(string name) => !Has(name) ? null : Store[name];

    /// <summary>
    /// add a function
    /// </summary>
    /// <param name="function">the function to add</param>
    public void Add(IFunction function) => Store[function.Name] = function;
}