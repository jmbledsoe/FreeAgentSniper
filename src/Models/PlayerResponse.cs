using Newtonsoft.Json;

namespace FreeAgentSniper.Models
{
    public class PlayerResponse
    {
        [JsonProperty("player_key")]
        public string PlayerKey { get; set; }
        [JsonProperty("player_id")]
        public int PlayerId { get; set; }
        public PlayerName Name { get; set; }
        public class PlayerName 
        {
            public string Full { get; set; }
            public string First { get; set; }
            public string Last { get; set; }
        }
        [JsonProperty("editorial_team_full_name")]
        public string TeamFullName { get; set; }
        [JsonProperty("editorial_team_abbr")]
        public string TeamAbbr { get; set; }
        [JsonProperty("display_position")]
        public string Position { get; set; }
    }
}