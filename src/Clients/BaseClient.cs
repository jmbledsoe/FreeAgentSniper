using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FreeAgentSniper.Clients
{
    public abstract class BaseClient
    {
        protected static readonly HttpClient client = new HttpClient();

        protected static readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };

        public string BaseUrl { get; set; }
        protected virtual Task<HttpRequestMessage> BeforeSend(HttpRequestMessage request) => Task.FromResult(request);
        protected virtual Task<HttpResponseMessage> AfterSend(HttpResponseMessage response, Func<Task<HttpRequestMessage>> requestBuilder) => Task.FromResult(response);

        protected HttpContent CreateJsonContent<T>(T content)
        {
            // TODO: (JMB) All sorts of fancy stream-based serilization would be keen
            var json = JsonConvert.SerializeObject(content, settings);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        protected async Task<HttpResponseMessage> Send(HttpMethod method, string relativeUrl, IDictionary<string, string> query = null, HttpContent content = null)
        {
            Func<Task<HttpRequestMessage>> requestBuilder = async () =>
            {
                var requestUrl = relativeUrl.StartsWith("http") // HACK: (JMB) Allow absolute or relative URLs
                    ? relativeUrl 
                    : BaseUrl.TrimEnd('/') + relativeUrl;

                if (query != null && query.Any())
                    requestUrl = QueryHelpers.AddQueryString(requestUrl, query);

                var req = new HttpRequestMessage(method, requestUrl)
                {
                    Content = content
                };

                req = await BeforeSend(req);

                return req;
            };
            
            var request = await requestBuilder();
            
            var response = await client.SendAsync(request);
            
            response = await AfterSend(response, requestBuilder);

            response.EnsureSuccessStatusCode();

            return response;
        }

        protected async Task<TResponse> Send<TResponse>(HttpMethod method, string relativeUrl, IDictionary<string, string> query = null, HttpContent content = null)
        {
            using (var response = await Send(method, relativeUrl, query, content))
            {
                return await Deserialize<TResponse>(response);
            }
        }

        protected static async Task<TResponse> Deserialize<TResponse>(HttpResponseMessage response)
        {
            using (var responseContentStream = await response.Content.ReadAsStreamAsync())
            using (var responseContentReader = new JsonTextReader(new StreamReader(responseContentStream)))
            {
                var serializer = JsonSerializer.Create(settings);

                return serializer.Deserialize<TResponse>(responseContentReader);
            }
        }
    }
}