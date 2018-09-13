using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FreeAgentSniper.Clients;
using FreeAgentSniper.Entities;
using Microsoft.Extensions.CommandLineUtils;

namespace FreeAgentSniper.Commands
{
    public class ScheduleCommand : ICommand
    {
        private const int DEFAULT_WAIT = 60;

        public static void Configure(CommandLineApplication app, CommandLineOptions options)
        {
            app.Description = "Schedule add/drop transactions to be executed";
            app.ExtendedHelpText = 
                "Once all free agent claims are set up, you can run this command overnight " +
                "to execute your claims as soon as the configured players become free agents.";
            app.HelpOption("--help|-h|-?");
            
            var waitOption = app.Option(
                "-w|--wait",
                $"Time (in seconds) to wait between attempts to execute claims (default is {DEFAULT_WAIT}",
                CommandOptionType.SingleValue);

            var verboseOption = app.Option(
                "-v|--verbose",
                "Enable verbose logging",
                CommandOptionType.NoValue);

            app.OnExecute(() => 
            {
                var wait = waitOption.ValidateInt32Value() ?? DEFAULT_WAIT;
                var verbose = verboseOption.HasValue();

                options.Command = new ScheduleCommand(options, wait, verbose);

                return 0;
            });
        }

        private readonly CommandLineOptions options;
        private readonly int wait;
        private readonly bool writeVerbose;

        public ScheduleCommand(CommandLineOptions options, int wait, bool writeVerbose)
        {
            this.options = options;
            this.wait = wait;
            this.writeVerbose = writeVerbose;
        }

        public async Task<int> Run()
        {
            using (var db = LiteDbFactory.New())
            {
                var configCollection = db.GetCollection<Config>();
                var config = configCollection.FindAll().SingleOrDefault() ?? new Config();

                options.ApplyToConfig(config);

                if (!config.LeagueId.HasValue)
                {
                    Console.Error.WriteLine("Error: League ID is not set");

                    return 1;
                }

                if (!config.TeamId.HasValue)
                {
                    Console.Error.WriteLine("Error: Team ID is not set");

                    return 1;
                }

                var client = new YahooClient(config);
                
                var claimCollection = db.GetCollection<Claim>();
                var claims = claimCollection.FindAll().OrderBy(c => c.Priority).ToList();

                string claimsText(int claimCount) => claimCount.ToString("G") + (claimCount == 1 ? " claim" : " claims");

                WriteMessage($"Attempting to process {claimsText(claims.Count)}...");

                while (claims.Any())
                {
                    foreach (var claim in claims)
                    {
                        // Check if the added player is a free agent
                        var potentialAddedPlayers = await client.SearchPlayersByName(
                            config.LeagueId.Value,
                            claim.Add.FullName, 
                            PlayerStatus.FreeAgent);

                        var addedPlayer = potentialAddedPlayers
                            .SingleOrDefault(p => p.PlayerId == claim.Add.PlayerId);

                        if (addedPlayer != null)
                        {
                            WriteMessage($"{claim.Add.Description} is a free-agent. Submitting claim...");

                            try
                            {
                                await client.SubmitClaim(
                                    config.LeagueId.Value, 
                                    config.TeamId.Value,
                                    claim.Add.PlayerId,
                                    claim.Drop.PlayerId);

                                WriteMessage($"{claim.Add.Description} successfully added! {claim.Drop.Description} dropped");
                            }
                            catch (ClientException ce)
                            {
                                WriteMessage($"Error: {claim.Add.Description} could not be added, dropping {claim.Drop.Description}", Console.Error);
                                WriteMessage($"Error: {ce.Message}", Console.Error);
                            }
                            
                            // Whether the claim succeeded or failed, delete it
                            claimCollection.Delete(claim.Id);
                        }
                        else
                        {
                            WriteMessage($"{claim.Add.Description} is not (yet) a free-agent", verbose: true);
                        }
                    }   

                    // Reload the claims collection, to pick up new claims and eliminate deleted ones
                    claims = claimCollection.FindAll().OrderBy(c => c.Priority).ToList();

                    // If outstanding claims remain, try again after a delay
                    if (claims.Any())
                    {
                        WriteMessage($"{claimsText(claims.Count)} still pending. Sleeping for {wait:G} seconds...", verbose: true);
                        
                        await Task.Delay(wait * 1000);

                        WriteMessage($"Attempting to process {claimsText(claims.Count)}...", verbose: true);
                    }
                }

                WriteMessage("All pending claims were processed (or failed). Exiting...");
            }

            return 0;
        }

        private void WriteMessage(string message, TextWriter writer = null, bool verbose = false)
        {
            if (writeVerbose || !verbose)
                (writer ?? Console.Out).WriteLine($"{DateTime.Now:T}: {message}");
        }
    }
}