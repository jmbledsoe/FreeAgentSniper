using System;
using System.Linq;
using System.Threading.Tasks;
using FreeAgentSniper.Entities;
using Microsoft.Extensions.CommandLineUtils;

namespace FreeAgentSniper.Commands
{
    public class PrioritizeCommand : ICommand
    {
        public static void Configure(CommandLineApplication app, CommandLineOptions options)
        {
            // Prioritize pending transaction (by ID from list)
            // -p|--prioritize <transaction ID> <# up or down>
            
            app.Description = "Reprioritize a pending add/drop claim";
            app.ExtendedHelpText = 
                "In general, add/drop claims are executed in the order in which they are entered. " +
                "However, you may wish to change the order in which claims are executed after they " + 
                "are entered. Use this command to move a claim up or down some numer of slots in the " +
                "priority list. The command will perform a list command showing the new priorities.";
            
            // Negative = towards priority #1
            // Positive = away from priority #1
            app.Command("up", ConfigureSubcommand(options, -1));
            app.Command("down", ConfigureSubcommand(options, 1));

            app.OnExecute(() => 
            {
                app.ShowHelp();
                
                return 1;
            });
        }

        private static Action<CommandLineApplication> ConfigureSubcommand(
            CommandLineOptions options, 
            int sign)
        {
            return app => 
            {
                var verb = sign > 0 ? "Increase" : "Decrease";
                app.Description = $"{verb} priority a pending add/drop claim";
                app.HelpOption("--help|-h|-?");

                var claimOption = app.Option(
                    "-c|--claim",
                    "ID of the claim to reprioritize",
                    CommandOptionType.SingleValue);

                var skipArgument = app.Argument(
                    "skip",
                    "How many other claims to skip (up or down)");

                app.OnExecute(() => 
                {
                    var claimId = claimOption.ValidateInt32Value(required: true);
                    var skip = !string.IsNullOrEmpty(skipArgument.Value)
                        ? skipArgument.ValidateInt32Value()
                        : 1;

                    skip *= sign;

                    options.Command = new PrioritizeCommand(options, claimId, skip);

                    return 0;
                });
            };
        }

        private readonly CommandLineOptions options;
        private readonly int? claimId;
        private readonly int? skip;

        public PrioritizeCommand(CommandLineOptions options, int? claimId, int? skip)
        {
            this.options = options;
            this.claimId = claimId;
            this.skip = skip;
        }

        public Task<int> Run() => Task.FromResult(RunSynchronous());

        private int RunSynchronous()
        {
            if (!claimId.HasValue || !skip.HasValue)
                return 1;

            using (var db = LiteDbFactory.New())
            {
                var claimCollection = db.GetCollection<Claim>();
                var claims = claimCollection.FindAll().ToDictionary(c => c.Id);
                var claimPriorities = claims.Values.ToDictionary(c => c.Id, c => (double)c.Priority);
                
                if (!claimPriorities.ContainsKey(claimId.Value))
                {
                    Console.Error.WriteLine($"Error: Claim {claimId} could not be found");

                    return 1;
                }

                // Move the specified claim to the proper priority location
                claimPriorities[claimId.Value] += skip.Value + Math.Sign(skip.Value) * 0.5;
                
                // Reprioritize claims to have integer priorities
                var orderedClaims = claimPriorities
                    .OrderBy(kv => kv.Value)
                    .Select(kv => claims[kv.Key])
                    .ToArray();
            
                for (var i = 0; i < orderedClaims.Length; i++)
                    orderedClaims[i].Priority = i + 1;

                claimCollection.Update(orderedClaims);

                orderedClaims.WriteTable();
            }

            return 0;   
        }
    }
}