using System;
using FreeAgentSniper.Clients;

namespace FreeAgentSniper
{
    class Program
    {
        static int Main(string[] args)
        {
            var options = CommandLineOptions.Parse(args);

            if (options?.Command == null)
                return 1;
            
            try
            {
                return options.Command.Run().GetAwaiter().GetResult();
            }
            catch (ClientException ce)
            {
                Console.Error.WriteLine($"Error: {ce.Message}");

                return 1;
            }
            catch (Exception e)
            {Console.Error.WriteLine(e.ToString());
                

                return 1;
            }
        }
    }
}
