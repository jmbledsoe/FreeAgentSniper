using System;
using System.Linq;
using System.Threading.Tasks;
using FreeAgentSniper.Entities;
using Microsoft.Extensions.CommandLineUtils;

namespace FreeAgentSniper.Commands
{
    public class ConfigCommand : ICommand
    {
        public static void Configure(CommandLineApplication app, CommandLineOptions options)
        {
            app.Description = "Stores configuration settings";
            app.ExtendedHelpText =
                "Configuration settings such as league ID and OAuth refresh token are stored " +
                "in the application's database for use with other commands.";
            app.HelpOption("--help|-h|-?");
            
            var leagueOption = app.Option(
                "-l|--league", 
                "Yahoo! league ID of the league to store for subsequent commands",  
                CommandOptionType.SingleValue);

            var teamOption = app.Option(
                "-t|--team", 
                "Yahoo! team ID for the team to store for subsequent commands",  
                CommandOptionType.SingleValue);

            var refreshTokenOption = app.Option(
                "-r|--refresh-token", 
                "OAuth 2.0 refresh token to store for subsequent commands",  
                CommandOptionType.SingleValue);

            var clientIdOption = app.Option(
                "-c|--client-id", 
                "OAuth 2.0 client ID for the application",  
                CommandOptionType.SingleValue);

            var clientSecretOption = app.Option(
                "-s|--client-secret", 
                "OAuth 2.0 client secret for the application",  
                CommandOptionType.SingleValue);

            app.OnExecute(() => 
            {
                // Override options shared between root and config commands
                if (leagueOption.HasValue())
                {
                    options.LeagueId = leagueOption.ValidateInt32Value();

                    if (!options.LeagueId.HasValue)
                        return 1;
                }

                if (teamOption.HasValue())
                {
                    options.TeamId = teamOption.ValidateInt32Value();

                    if (!options.TeamId.HasValue)
                        return 1;
                }

                if (refreshTokenOption.HasValue())                    
                    options.RefreshToken = refreshTokenOption.Value();

                options.Command = new ConfigCommand(
                    options, 
                    clientIdOption.Value(), 
                    clientSecretOption.Value());

                return 0;
            });
        }

        private readonly CommandLineOptions options;
        private readonly string clientId;
        private readonly string clientSecret;

        public ConfigCommand(CommandLineOptions options, string clientId, string clientSecret)
        {
            this.options = options;
            this.clientId = clientId;
            this.clientSecret = clientSecret;
        }

        public Task<int> Run()
        {
            Console.WriteLine("Storing configuration values...");

            using (var db = LiteDbFactory.New())
            {
                var configCollection = db.GetCollection<Config>();
                
                var config = configCollection.FindAll().SingleOrDefault() ?? new Config();

                options.ApplyToConfig(config);

                if (!string.IsNullOrEmpty(clientId))
                    config.ClientId = clientId;
                
                if (!string.IsNullOrEmpty(clientSecret))
                    config.ClientSecret = clientSecret;

                configCollection.Upsert(config);
            }

            Console.WriteLine("Configuration values stored successfully");

            return Task.FromResult(0);
        }
    }
}