using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using FreeAgentSniper.Entities;
using FreeAgentSniper.Models;
using Newtonsoft.Json.Linq;

namespace FreeAgentSniper.Clients
{
    public static class PlayerStatus
    {
        public const string Available = "A";
        public const string FreeAgent = "FA";
        public const string Waivers = "W";
        public const string Taken = "T";
    }

    public class YahooClient : BaseClient
    {
        private const string GET_TOKEN_URL = "https://api.login.yahoo.com/oauth2/get_token";

        private readonly Config config;

        private string accessToken;
        private int? gameIdCache;

        public YahooClient(Config config)
        {
            this.config = config;

            BaseUrl = "https://fantasysports.yahooapis.com/fantasy/v2";
        }

        public async Task<int> GetGameId()
        {
            if (!gameIdCache.HasValue)
            {
                var response = await Send<JObject>(
                    HttpMethod.Get, 
                    "https://fantasysports.yahooapis.com/fantasy/v2/game/nfl",
                    new Dictionary<string, string> { ["format"] = "json" });

                gameIdCache = response["fantasy_content"]["game"][0]["game_id"].Value<int>();
            }

            return gameIdCache.Value;
        }

        public async Task<LeagueResponse> GetLeague(int leagueId)
        {
            var gameId = await GetGameId();

            var response = await Send<JObject>(
                HttpMethod.Get, 
                $"https://fantasysports.yahooapis.com/fantasy/v2/league/{gameId}.l.{leagueId}",
                new Dictionary<string, string> { ["format"] = "json" });

            return response["fantasy_content"]["league"][0].ToObject<LeagueResponse>();
        }

        public async Task<TeamResponse> GetTeam(int leagueId, int teamId)
        {
            var gameId = await GetGameId();

            var response = await Send<JObject>(
                HttpMethod.Get, 
                $"https://fantasysports.yahooapis.com/fantasy/v2/team/{gameId}.l.{leagueId}.t.{teamId}",
                new Dictionary<string, string> { ["format"] = "json" });

            return response["fantasy_content"]["team"][0].MergeArray().ToObject<TeamResponse>();
        }

        public async Task<ICollection<PlayerResponse>> SearchPlayers(int leagueId, string playerNameOrId)
        {
            return int.TryParse(playerNameOrId, out var playerId)
                    ? await GetPlayer(playerId)
                    : await SearchPlayersByName(leagueId, playerNameOrId);
        }

        public async Task<ICollection<PlayerResponse>> GetPlayer(int playerId)
        {
            var gameId = await GetGameId();

            var response = await Send<JObject>(
                HttpMethod.Get, 
                $"https://fantasysports.yahooapis.com/fantasy/v2/player/{gameId}.p.{playerId}",
                new Dictionary<string, string> { ["format"] = "json" });

            var player =  response["fantasy_content"]["player"][0].MergeArray().ToObject<PlayerResponse>();

            if (player.PlayerId == playerId)
                return new [] { player };
            else
                return new PlayerResponse[0];
        }

        public async Task<ICollection<PlayerResponse>> SearchPlayersByName(int leagueId, string playerName, string status = null)
        {
            var gameId = await GetGameId();

            // HACK: (JMB) Search doesn't like non-alphanumeric characters in names,
            // so if one is provided, split the name and search on the longest portion
            var onlyAlphasAndSpaces = new Regex(@"[A-Z ]+", RegexOptions.IgnoreCase);
            var partialPlayerName = onlyAlphasAndSpaces.Matches(playerName)
                .Select(m => m.Value.Trim())
                .OrderByDescending(v => v.Length)
                .FirstOrDefault();

            if (partialPlayerName != playerName)
            {
                Console.Error.WriteLine($"Warning: Only alpha characters are supported. Searching instead for '{partialPlayerName}'");

                playerName = partialPlayerName;
            }

            if (string.IsNullOrWhiteSpace(playerName))
                return new PlayerResponse[0];

            var url = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{gameId}.l.{leagueId}/players;search={playerName}";

            if (!string.IsNullOrEmpty(status))
                url = url + $";status={status}";

            var response = await Send<JObject>(
                HttpMethod.Get, 
                url,
                new Dictionary<string, string> { ["format"] = "json" });

            var playersArray = response["fantasy_content"]["league"][1]["players"];
            
            if (playersArray.Any())
                return playersArray
                    .ToObject<Dictionary<string, JToken>>()
                    .Where(kv => !kv.Key.Equals("count"))
                    .Select(kv => kv.Value["player"][0].MergeArray().ToObject<PlayerResponse>())
                    .ToList();
            else
                return new PlayerResponse[0];
        }

        public async Task SubmitClaim(int leagueId, int teamId, int addPlayerId, int dropPlayerId)
        {
            var gameId = await GetGameId();

            var addPlayerKey = $"{gameId}.p.{addPlayerId}";
            var dropPlayerKey = $"{gameId}.p.{dropPlayerId}";
            var teamKey = $"{gameId}.l.{leagueId}.t.{teamId}";

            var xmlDoc = BuildTransactionPostContent(addPlayerKey, dropPlayerKey, teamKey);
            var xmlContent = new StringContent(xmlDoc.OuterXml, Encoding.UTF8, "application/xml");
            
             await Send(
                HttpMethod.Post, 
                $"https://fantasysports.yahooapis.com/fantasy/v2/league/{gameId}.l.{leagueId}/transactions",
                new Dictionary<string, string> { ["format"] = "json" },
                xmlContent);
        }

        private XmlDocument BuildTransactionPostContent(string addPlayerKey, string dropPlayerKey, string teamKey)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"
                <fantasy_content>
                    <transaction>
                        <type>add/drop</type>
                        <players></players>
                    </transaction>
                </fantasy_content>
            ");

            var playersElement = xmlDoc["fantasy_content"]["transaction"]["players"];
            
            playersElement.AppendChild(CreatePlayerElement(xmlDoc, addPlayerKey, "add", teamKey));
            playersElement.AppendChild(CreatePlayerElement(xmlDoc, dropPlayerKey, "drop", teamKey));

            return xmlDoc;
        }

        private XmlElement CreatePlayerElement(XmlDocument xmlDoc, string playerKey, string type, string teamKey)
        {
            var playerElement = xmlDoc.CreateElement("player");
            var playerKeyElement = xmlDoc.CreateElement("player_key");
            var transactionDataElement = xmlDoc.CreateElement("transaction_data");
            var typeElement = xmlDoc.CreateElement("type");
            var teamKeyElement = xmlDoc.CreateElement(type == "add" ? "destination_team_key" : "source_team_key");
            
            playerKeyElement.InnerText = playerKey;
            typeElement.InnerText = type;
            teamKeyElement.InnerText = teamKey;

            transactionDataElement.AppendChild(typeElement);
            transactionDataElement.AppendChild(teamKeyElement);

             playerElement.AppendChild(playerKeyElement);
             playerElement.AppendChild(transactionDataElement);
            
            return playerElement;
        }

        protected override async Task<HttpRequestMessage> BeforeSend(HttpRequestMessage request)
        {
            // Prevent infinite recursion
            if (request.RequestUri.AbsoluteUri == GET_TOKEN_URL)
                return request;

            if (accessToken == null)
                await RefreshToken();

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return request;
        }

        protected override async Task<HttpResponseMessage> AfterSend(HttpResponseMessage response, Func<Task<HttpRequestMessage>> requestBuilder)
        {
            if (response.IsSuccessStatusCode)
                return response;

            switch (response.StatusCode)
            {
                case (HttpStatusCode.Unauthorized):
                {
                    // Try to refresh the access token and execute the request again
                    await RefreshToken();

                    // (Build a new request since request objects cannot be resent)
                    var retryRequest = await requestBuilder();
                    
                    return await client.SendAsync(retryRequest);
                }
                default:
                    // API errors and OAuth errors come back slightly differently,
                    // so deserialzie the response as JObject and figure out from there
                    var errorObject = await Deserialize<JObject>(response);
                    
                    if (errorObject.ContainsKey("error_description"))
                        throw new ClientException(errorObject["error_description"].Value<string>());

                    if (errorObject.ContainsKey("error"))
                        throw new ClientException(errorObject["error"]["description"].Value<string>());
                    
                    throw new ClientException(errorObject.ToString());
            }
        }

        private async Task RefreshToken()
        {
            var form = new Dictionary<string, string>
            {
                ["client_id"] = config.ClientId,
                ["client_secret"] = config.ClientSecret,
                ["redirect_url"] = "oob",
                ["refresh_token"] = config.RefreshToken,
                ["grant_type"] = "refresh_token"
            };

            var response = await Send<GetTokenResponse>(
                HttpMethod.Post, 
                GET_TOKEN_URL, 
                content: new FormUrlEncodedContent(form));
            
            accessToken = response.AccessToken;
        }
    }
}