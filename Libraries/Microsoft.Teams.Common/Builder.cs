namespace Microsoft.Teams.Common;

public interface IBuilder<T>
{
    public T Build();
}