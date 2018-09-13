using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeAgentSniper.Clients;
using FreeAgentSniper.Entities;
using FreeAgentSniper.Models;
using Microsoft.Extensions.CommandLineUtils;

namespace FreeAgentSniper.Commands
{
    public class ClaimCommand : ICommand
    {
        public static void Configure(CommandLineApplication app, CommandLineOptions options)
        {
            app.Description = "Create an add/drop claim";
            app.ExtendedHelpText =
                "Create an add/drop claim, which will be executed when free agency opens.";
            app.HelpOption("--help|-h|-?");
            
            var addOption = app.Option(
                "-a|--add",
                "Name or ID of player to add",
                CommandOptionType.SingleValue);
            
            var dropOption = app.Option(
                "-d|--drop",
                "Name or ID of player to drop",
                CommandOptionType.SingleValue);

            app.OnExecute(() => 
            {
                options.Command = new ClaimCommand(options, addOption.Value(), dropOption.Value());

                return 0;
            });
        }

        private readonly CommandLineOptions options;
        private readonly string addNameOrId;
        private readonly string dropNameOrId;

        public ClaimCommand(CommandLineOptions options, string addNameOrId, string dropNameOrId)
        {
            this.options = options;
            this.addNameOrId = addNameOrId;
            this.dropNameOrId = dropNameOrId;
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

                var addPlayers = await client.SearchPlayers(config.LeagueId.Value, addNameOrId);
                var dropPlayers = await client.SearchPlayers(config.LeagueId.Value, dropNameOrId);

                if (VerifySinglePlayer(addPlayers, "add") && VerifySinglePlayer(dropPlayers, "drop"))
                {
                    var claimsCollection = db.GetCollection<Claim>();
                    var existingClaims = claimsCollection.FindAll();

                    var claim = new Claim
                    {
                        Add = new Player(addPlayers.Single()),
                        Drop = new Player(dropPlayers.Single()),
                        Priority = existingClaims
                            .OrderByDescending(ec => ec.Priority)
                            .Select(ec => ec.Priority)
                            .FirstOrDefault() + 1
                    };

                    claimsCollection.Insert(claim);

                    Console.WriteLine("Claim successfully added");

                    claimsCollection.FindAll().ToArray().WriteTable();

                    return 0;
                }
                else
                {
                    return 1;
                }
            }
        }

        private bool VerifySinglePlayer(ICollection<PlayerResponse> players, string argumentName)
        {
            if (players.Count == 0)
            {
                Console.Error.WriteLine($"Error: No {argumentName} player could be found");

                return false;
            }
            else if (players.Count > 1)
            {
                Console.Error.WriteLine($"Error: Multiple {argumentName} players were found");
                
                players.WriteTable(Console.Error);

                return false;
            }
            else
            {
                return true;
            }
            
        }
    }
}