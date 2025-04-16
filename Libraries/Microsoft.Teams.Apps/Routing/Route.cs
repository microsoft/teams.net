using System.Reflection;

using Microsoft.Teams.Api.Activities;
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

    public bool Select(IActivity activity) => Attr.Select(activity);
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        foreach (var param in Method.GetParameters())
        {
            var attribute = param.GetCustomAttribute<IContext.PropertyAttribute>();
            var generic = param.ParameterType.GenericTypeArguments.FirstOrDefault();
            var isContext = generic?.IsAssignableTo(Attr.Type) ?? false;

            if (attribute == null && !isContext)
                result.AddError(param.Name ?? "??", "type must be `IContext<TActivity>` or an `IContext` property attribute");
        }

        return result;
    }

    public async Task<object?> Invoke(IContext<IActivity> context)
    {
        var log = context.Log.Child(Method.Name);
        var contextClient = new IContext.Client(context);
        var args = Method.GetParameters().Select(param =>
        {
            var attribute = param.GetCustomAttribute<IContext.PropertyAttribute>();
            return attribute == null ? Attr.Coerce(context) : attribute.Resolve(context, param);
        });

        if (Attr.Log.HasFlag(IContext.Property.Context))
        {
            log.Debug(context);
        }
        else
        {
            if (Attr.Log.HasFlag(IContext.Property.AppId))
                log.Debug(context.AppId);

            if (Attr.Log.HasFlag(IContext.Property.Activity))
                log.Debug(context.Activity);
        }

        var res = Method.Invoke(null, args?.ToArray());
        var task = res as Task<object?>;
        return task != null ? await task : null;
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