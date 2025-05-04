using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Teams.Common.Extensions;

public static class MethodInfoExtensions
{
    public static async Task<object?> InvokeAsync(this MethodInfo methodinfo, object? target, object?[]? args)
    {
        var res = methodinfo.Invoke(target, args);

        if (res is Task<object?> taskWithValue)
        {
            return await taskWithValue.ConfigureAwait(false);
        }

        if (res is Task task)
        {
            await task.ConfigureAwait(false);
            return null;
        }

        return res;
    }

    public static Delegate CreateDelegate(this MethodInfo methodInfo, object target)
    {
        Func<Type[], Type> getType;
        var isAction = methodInfo.ReturnType.Equals(typeof(void));
        var types = methodInfo.GetParameters().Select(p => p.ParameterType);

        if (isAction)
        {
            getType = Expression.GetActionType;
        }
        else
        {
            getType = Expression.GetFuncType;
            types = types.Concat([methodInfo.ReturnType]);
        }

        if (methodInfo.IsStatic)
        {
            return Delegate.CreateDelegate(getType(types.ToArray()), methodInfo);
        }

        return Delegate.CreateDelegate(getType(types.ToArray()), target, methodInfo.Name);
    }
}