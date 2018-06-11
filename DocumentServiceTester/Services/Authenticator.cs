using System;
using System.Net;
using DocumentServiceTester.Models;
using Newtonsoft.Json;
using RestSharp;

namespace DocumentServiceTester.Services
{
    public class Authenticator
    {
        public TokenResponse GetBearerToken(string username, string password, string clientId, string clientSecret)
        {
            var restClient = new RestClient(Constants.GlobalXHost);
            
            var request = new RestRequest(Constants.AuthRoute, Method.POST);

            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddHeader("Accept", "application/json");

            request.AddParameter("client_id", clientId);
            request.AddParameter("client_secret", clientSecret);
            request.AddParameter("grant_type", Constants.GrantType);
            request.AddParameter("scope", Constants.Scope);
            request.AddParameter("username", username);
            request.AddParameter("password", password);

            var response = restClient.Execute(request);

            if (response.StatusCode != HttpStatusCode.Accepted && response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Could not authenticate to GlobalX, response from server: {response.StatusCode}: {response.Content}");
            }

            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(response.Content);

            return tokenResponse;
        }
    }
}