using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeAgentSniper.Entities;
using Microsoft.Extensions.CommandLineUtils;

namespace FreeAgentSniper.Commands
{
    public class DeleteCommand : ICommand
    {
        public static void Configure(CommandLineApplication app, CommandLineOptions options)
        {
            app.Description = "Delete one or more add/drop claims";
            app.ExtendedHelpText =
                "Delete one or more add/drop claims by specifing their transaction IDs " + 
                "obtained from the list command, or delete all add/drop claims by specifying a switch.";
            app.HelpOption("--help|-h|-?");
            
            var claimOption = app.Option(
                "-c|--claim",
                "ID of the claim to reprioritize",
                CommandOptionType.MultipleValue);

            var allOption = app.Option(
                "--all",
                "Delete all claims",
                CommandOptionType.NoValue);

            app.OnExecute(() => 
            {
                var claimIds = claimOption.ValidateInt32Values();

                options.Command = new DeleteCommand(options, claimIds, allOption.HasValue());

                return 0;
            });
        }

        private readonly CommandLineOptions options;
        private readonly ICollection<int?> claimIds;
        private readonly bool all;

        public DeleteCommand(CommandLineOptions options, ICollection<int?> claimIds, bool all)
        {
            this.options = options;
            this.claimIds = claimIds;
            this.all = all;
        }
        public Task<int> Run() => Task.FromResult(RunSynchronous());
        private int RunSynchronous()
        {
            if (claimIds.Any(id => !id.HasValue))
                return 1;

            if (all && claimIds.Any())
            {
                Console.Error.WriteLine("Error: Either specific claim IDs or all claims can be deleted, but not both");
            
                return 1;
            }

            using (var db = LiteDbFactory.New())
            {
                var claimCollection = db.GetCollection<Claim>();

                if (all)
                {
                    claimCollection.Delete(c => true);
                }
                else
                {
                    var claimsToDelete = new List<Claim>();

                    foreach (var claimId in claimIds)
                    {
                        if (!claimCollection.Delete(claimId.Value))
                            Console.Error.WriteLine($"Warning: Claim {claimId} could not be found");    
                    }
                }

                claimCollection.FindAll().ToArray().WriteTable();
            }

            return 0;   
        }
    }
}