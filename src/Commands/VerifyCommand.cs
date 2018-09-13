using System;
using System.Linq;
using System.Threading.Tasks;
using FreeAgentSniper.Clients;
using FreeAgentSniper.Entities;
using Microsoft.Extensions.CommandLineUtils;

namespace FreeAgentSniper.Commands
{
    public class VerifyCommand : ICommand
    {
        public static void Configure(CommandLineApplication app, CommandLineOptions options)
        {
            app.Description = "Verify settings by connecting to the configured league";
            app.ExtendedHelpText =
                "Verifies configured settings for the application " + 
                "by pinging the Yahoo! fantasy football API.";
            app.HelpOption("--help|-h|-?");
            
            app.OnExecute(() => 
            {
                options.Command = new VerifyCommand(options);

                return 0;
            });
        }

        private readonly CommandLineOptions options;

        public VerifyCommand(CommandLineOptions options)
        {
            this.options = options;
        }

        public async Task<int> Run()
        {
            using (var db = LiteDbFactory.New())
            {
                var configCollection = db.GetCollection<Config>();
                var config = configCollection.FindAll().SingleOrDefault() ?? new Config();

                options.ApplyToConfig(config);

                if (string.IsNullOrWhiteSpace(config.ClientId))
                    Console.Error.WriteLine("Error: Client ID is not set");

                if (string.IsNullOrWhiteSpace(config.ClientSecret))
                    Console.Error.WriteLine("Error: Client secret is not set");

                if (string.IsNullOrWhiteSpace(config.RefreshToken))
                    Console.Error.WriteLine("Error: Refresh token is not set");

                if (!config.LeagueId.HasValue)
                    Console.Error.WriteLine("Error: League ID is not set");

                if (!config.TeamId.HasValue)
                    Console.Error.WriteLine("Error: Team ID is not set");

                Console.WriteLine("Verifying connection to Yahoo!...");

                var client = new YahooClient(config);

                await client.GetGameId();

                Console.WriteLine("Successfully connected to Yahoo!");

                var league = await client.GetLeague(config.LeagueId.GetValueOrDefault());

                Console.WriteLine($"Successfully found league {league.Name} (#{config.LeagueId})");

                var team = await client.GetTeam(config.LeagueId.GetValueOrDefault(), config.TeamId.GetValueOrDefault());

                if (team.IsOwnedByCurrentLogin)
                    Console.WriteLine($"Successfully found team {team.Name} (#{config.TeamId})");
                else
                    Console.Error.WriteLine($"Error: Team {team.Name} (#{config.TeamId}) is not owned by current user");
            }

            return 0;
        }
    }
}