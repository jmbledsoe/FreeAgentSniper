using Newtonsoft.Json;

namespace FreeAgentSniper.Models
{
    public class LeagueResponse
    {
        [JsonProperty("league_key")]
        public string LeagueKey { get; set; }
        [JsonProperty("league_id")]
        public int LeagueId { get; set; }
        public string Name { get; set; }
    }
}