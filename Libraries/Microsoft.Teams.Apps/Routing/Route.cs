using System.Reflection;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Common.Extensions;

namespace Microsoft.Teams.Apps.Routing;

public interface IRoute
{
    public bool Select(IActivity activity);
    public Task<object?> Invoke(IContext<IActivity> context);
}

public class Route : IRoute
{
    public string? Name { get; set; }
    public required Func<IActivity, bool> Selector { get; set; }
    public required Func<IContext<IActivity>, Task<object?>> Handler { get; set; }

    public bool Select(IActivity activity) => Selector(activity);
    public async Task<object?> Invoke(IContext<IActivity> context) => await Handler(context);
}

public class AttributeRoute : IRoute
{
    public required ActivityAttribute Attr { get; set; }
    public required MethodInfo Method { get; set; }
    public object? Object { get; set; }

    public bool Select(IActivity activity) => Attr.Select(activity);
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        foreach (var param in Method.GetParameters())
        {
            var attribute = param.GetCustomAttribute<ContextAccessorAttribute>(true);
            var generic = param.ParameterType.GenericTypeArguments.FirstOrDefault();
            var isContext = generic?.IsAssignableTo(Attr.Type) ?? false;

            if (attribute is null && !isContext)
            {
                result.AddError(param.Name ?? "??", "type must be `IContext<TActivity>` or an `IContext` accessor attribute");
            }
        }

        return result;
    }

    public Task<object?> Invoke(IContext<IActivity> context)
    {
        var args = Method.GetParameters().Select(param =>
        {
            var attribute = param.GetCustomAttribute<ContextAccessorAttribute>(true);
            return attribute is null ? Attr.Coerce(context) : attribute.GetValue(context, param);
        });

        return Method.InvokeAsync(Object, args?.ToArray());
    }

    public class ValidationResult
    {
        /// <summary>
        /// the errors that were found
        /// </summary>
        public IList<ParameterError> Errors { get; set; } = [];

        /// <summary>
        /// is the result valid
        /// </summary>
        public bool Valid => Errors.Count == 0;

        /// <summary>
        /// combine all the errors into
        /// one message string
        /// </summary>
        public override string ToString() => string.Join(Environment.NewLine, Errors.Select(err => $"{err.Name} => {err.Message}"));

        /// <summary>
        /// add a parameter error to the result
        /// </summary>
        public void AddError(string name, string message)
        {
            Errors.Add(new(name, message));
        }

        public record ParameterError(string Name, string Message);
    }
}