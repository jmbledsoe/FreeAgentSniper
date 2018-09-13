using System.Linq;
using System.Threading.Tasks;
using FreeAgentSniper.Entities;
using Microsoft.Extensions.CommandLineUtils;

namespace FreeAgentSniper.Commands
{
    public class ListCommand : ICommand
    {
        public static void Configure(CommandLineApplication app, CommandLineOptions options)
        {
            app.Description = "List pending add/drop claims";
            app.ExtendedHelpText =
                "List all pending add/drop claims, in priority order. The claims include " + 
                "their internal claim IDs, which can be used for reprioritizing or deleting claims.";
            app.HelpOption("--help|-h|-?");
            
            app.OnExecute(() => 
            {
                options.Command = new ListCommand(options);

                return 0;
            });
        }

        private readonly CommandLineOptions options;

        public ListCommand(CommandLineOptions options)
        {
            this.options = options;
        }

        public Task<int> Run()
        {
            using (var db = LiteDbFactory.New())
            {
                var claimCollection = db.GetCollection<Claim>();
                
                claimCollection.FindAll().ToArray().WriteTable();
            }

            return Task.FromResult(0);
        }
    }
}