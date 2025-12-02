using Microsoft.Teams.Schema;

namespace Microsoft.Teams.Handlers
{
    public delegate Task MessageHandler(TeamsActivity activity, CancellationToken cancellationToken = default);
}
