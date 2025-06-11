using System.Reflection;

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

public class RemoveDefaultMessageController : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        Type messageController = typeof(Microsoft.Teams.Plugins.AspNetCore.Controllers.MessageController);

        List<TypeInfo> matches = feature.Controllers.Where(c => c.AsType() == messageController).ToList();
        foreach (TypeInfo match in matches)
        {
            feature.Controllers.Remove(match);
        }
    }
}