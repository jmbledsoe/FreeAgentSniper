using System;
using System.Linq;
using System.Threading.Tasks;
using FreeAgentSniper.Clients;
using FreeAgentSniper.Entities;
using Microsoft.Extensions.CommandLineUtils;

namespace FreeAgentSniper.Commands
{
    public class SearchCommand : ICommand
    {
        public static void Configure(CommandLineApplication app, CommandLineOptions options)
        {
            app.Description = "Search for a player by name";
            app.ExtendedHelpText = 
                "Searches the configured league for all players with the " +
                "given name and lists them, including their teams and positions.";
            app.HelpOption("--help|-h|-?");
            
            var nameArgument = app.Argument(
                "name",
                "Name or id of player to search for",
                true);

            app.OnExecute(() => 
            {
                options.Command = new SearchCommand(options, string.Join(" ", nameArgument.Values));

                return 0;
            });
        }

        private readonly CommandLineOptions options;
        private readonly string playerNameOrId;

        public SearchCommand(CommandLineOptions options, string playerNameOrId)
        {
            this.options = options;            
            this.playerNameOrId = playerNameOrId;
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

                Console.WriteLine("Searching for players...");

                var client = new YahooClient(config);

                var players = await client.SearchPlayers(config.LeagueId.Value, playerNameOrId);

                players.WriteTable();
            }

            return 0;
        }

        
    }
}