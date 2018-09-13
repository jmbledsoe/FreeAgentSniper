using System;
using FreeAgentSniper.Commands;
using FreeAgentSniper.Entities;
using Microsoft.Extensions.CommandLineUtils;

namespace FreeAgentSniper
{
public class CommandLineOptions
    {
        public static CommandLineOptions Parse(string[] args)
        {
            var options = new CommandLineOptions();

            var app = new CommandLineApplication
            {
                Name = "dotnet FreeAgentSniper.dll",
                FullName = "Free Agent Sniper",
                ExtendedHelpText = 
                    "Tired of your league-mates waking up first thing Wednesday morning and sniping your free agents? " +
                    "Don't want to waste your waiver priority on a mediocre player? Then use Free Agent Sniper to perform " + 
                    "add/drops for you as soon as players come off of waivers. Search for players, set up transactions, and " +
                    "start the schedule to grab your players before your buddies wake up Wednesday morning!"
            };

            app.HelpOption("-?|-h|--help");

            RootCommand.Configure(app, options);

            var leagueOption = app.Option(
                "-l|--league", 
                "Yahoo! league ID of the league to use for this command",  
                CommandOptionType.SingleValue);

            var teamOption = app.Option(
                "-t|--team", 
                "Yahoo! team ID for the team to use for this command",  
                CommandOptionType.SingleValue);

            var refreshTokenOption = app.Option(
                "-r|--refresh-token", 
                "OAuth 2.0 refresh token to use when authenticating this command",  
                CommandOptionType.SingleValue);

            try
            {
                var result = app.Execute(args);

                // HACK: (JMB) Don't set options if they're set by a subcommand e.g. config
                if (!options.LeagueId.HasValue)
                    options.LeagueId = leagueOption.ValidateInt32Value();
                if (!options.TeamId.HasValue)
                    options.TeamId = teamOption.ValidateInt32Value();
                if (string.IsNullOrEmpty(options.RefreshToken))
                    options.RefreshToken = refreshTokenOption.Value();

                var leagueIsValid = !leagueOption.HasValue() || options.LeagueId.HasValue;
                var teamIdIsValid = !teamOption.HasValue() || options.TeamId.HasValue;

                if (!leagueIsValid || !teamIdIsValid || result != 0)
                {
                    return null;
                }
            }
            catch (CommandParsingException cpe)
            {
                Console.Error.WriteLine(cpe.Message);

                return null;
            }

            return options;
        }

        public ICommand Command { get; set; }
        public int? LeagueId { get; set; }
        public int? TeamId { get; set; }
        public string RefreshToken { get; set; }

        public void ApplyToConfig(Config config)
        {
            if (LeagueId.HasValue)
                config.LeagueId = LeagueId;

            if (TeamId.HasValue)
                config.TeamId = TeamId;

            if (!string.IsNullOrEmpty(RefreshToken))
                config.RefreshToken = RefreshToken;
        }
    }
}