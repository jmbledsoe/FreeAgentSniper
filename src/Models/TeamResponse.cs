using Newtonsoft.Json;

namespace FreeAgentSniper.Models
{
    public class TeamResponse
    {
        [JsonProperty("team_key")]
        public string TeamKey { get; set; }
        [JsonProperty("team_id")]
        public int TeamId { get; set; }
        public string Name { get; set; }
        [JsonProperty("is_owned_by_current_login")]
        public bool IsOwnedByCurrentLogin { get; set; }
    }
}