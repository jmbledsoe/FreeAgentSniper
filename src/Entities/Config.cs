namespace FreeAgentSniper.Entities
{
    public class Config 
    {
        public int Id { get; set; }
        // User settings
        public int? LeagueId { get; set; }
        public int? TeamId { get; set; }

        public string RefreshToken { get; set; }

        // OAuth application settings
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}