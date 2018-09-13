using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace FreeAgentSniper.Commands
{
    public class RootCommand : ICommand
    {
        public static void Configure(CommandLineApplication app, CommandLineOptions options)
        {
            app.Command("config", cla => ConfigCommand.Configure(cla, options));
            app.Command("verify", cla => VerifyCommand.Configure(cla, options));
            app.Command("search", cla => SearchCommand.Configure(cla, options));
            app.Command("claim", cla => ClaimCommand.Configure(cla, options));
            app.Command("list", cla => ListCommand.Configure(cla, options));
            app.Command("prioritize", cla => PrioritizeCommand.Configure(cla, options));
            app.Command("delete", cla => DeleteCommand.Configure(cla, options));
            app.Command("schedule", cla => ScheduleCommand.Configure(cla, options));
            
            app.OnExecute(() => 
            {
               options.Command = new RootCommand(app);

               return 0;
            });
        }

        private readonly CommandLineApplication app;

        public RootCommand(CommandLineApplication app)
        {
            this.app = app;
        }

        public Task<int> Run()
        {
            app.ShowHelp();

            return Task.FromResult(1);
        }
    }
}