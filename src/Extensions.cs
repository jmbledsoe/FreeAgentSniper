using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BetterConsoleTables;
using FreeAgentSniper.Entities;
using FreeAgentSniper.Models;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;

namespace FreeAgentSniper
{
    public static class Extensions 
    {
        private static JsonMergeSettings mergeSettings = new JsonMergeSettings();

        public static JObject MergeArray(this JToken token)
        {
            var returnObject = new JObject();


            foreach (var childToken in token.Children().OfType<JObject>())
            {
                returnObject.Merge(childToken, mergeSettings);
            }

            return returnObject;
        }

        public static int? ValidateInt32Value(this CommandOption option, bool required = false)
        {
            if (option.HasValue() || required)
            {
                if (int.TryParse(option.Value(), out var value))
                    return value;
                else
                    Console.Error.WriteLine($"Error: Value '{option.Value()}' for option {option.Template} must be an integer");
            }

            return null;
        }

        public static List<int?> ValidateInt32Values(this CommandOption option, bool required = false)
        {
            return  option.Values.Select<string, int?>(v => 
            {
                if (int.TryParse(option.Value(), out var value))
                    return value;
                else
                    Console.Error.WriteLine($"Error: Value '{option.Value()}' for option {option.Template} must be an integer");

                return null;
            }).ToList();   
        }

        public static int? ValidateInt32Value(this CommandArgument argument)
        {
            if (int.TryParse(argument.Value, out var value))
                return value;
            else
                Console.Error.WriteLine($"Error: Value '{argument.Value}' for argument {argument.Name} must be an integer");
            
            return null;
        }

        public static void WriteTable(this ICollection<PlayerResponse> players, TextWriter writer = null)
        {
            var table = new Table("id", "name", "pos", "team")
            {
                Config = TableConfiguration.Unicode()
            };

            if (players.Any())
            {
                table.AddRows(players
                    .OrderBy(p => p.Name.Last, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(p => p.Name.First, StringComparer.OrdinalIgnoreCase)
                    .Select(p => new[] { p.PlayerId.ToString(), p.Name.Full, p.Position, p.TeamAbbr }));
            }
            else
            {
                table.AddRow("", "", "", "");
            }

            (writer ?? Console.Out).Write(new ConsoleTables(table));
        }

        public static void WriteTable(this ICollection<Claim> claims, TextWriter writer = null)
        {
            var table = new Table("id", "add player", "drop player")
            {
                Config = TableConfiguration.Unicode()
            };

            if (claims.Any())
            {
                table.AddRows(claims
                    .OrderBy(c => c.Priority)
                    .ThenBy(c => c.Id)
                    .Select(c => new[] 
                    { 
                        c.Id.ToString(),
                        c.Add.Description,
                        c.Drop.Description
                    }));
            }
            else
            {
                table.AddRow("", "", "");
            }

            (writer ?? Console.Out).Write(new ConsoleTables(table));
        }
    }
}