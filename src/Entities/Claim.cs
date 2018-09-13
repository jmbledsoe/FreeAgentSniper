using FreeAgentSniper.Models;

namespace FreeAgentSniper.Entities
{
    public class Claim 
    {
        public int Id { get; set; }
        
        public int Priority { get; set; }

        public Player Add { get; set; }
        public Player Drop { get; set; }
    }
    
    public class Player
    {
        public Player() {}
        public Player(PlayerResponse response)
        {
            PlayerKey = response.PlayerKey;
            PlayerId = response.PlayerId;
            FullName = response.Name.Full;
            TeamAbbr = response.TeamAbbr;
            Position = response.Position;
        }

        public string Description => $"{FullName} ({Position}, {TeamAbbr})";

        public string PlayerKey { get; set; }
        public int PlayerId { get; set; }
        public string FullName { get; set; }
        public string TeamAbbr { get; set; }
        public string Position { get; set; }
    }
}