using Meadow.JsonRpc;
using System.Collections.Generic;
using System.Management.Automation;

namespace Meadow.Cli.Commands
{
    // TODO: setup file watcher for config updates


    [Cmdlet(ApprovedVerbs.Watch, "Config")]
    [Alias("watchConfig")]
    public class WatchConfigCommand : PSCmdlet
    {
        protected override void EndProcessing()
        {
            var config = this.ReadConfig();

        }
    }
}
