﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using GmailApi.Models;
using Newtonsoft.Json;

namespace GmailApi
{
    public class TokenManager
    {
        public const string AuthorizationServerUrl = "https://www.googleapis.com/oauth2/v3/token";// "https://accounts.google.com/o/oauth2/token";

        private static readonly ConcurrentDictionary<string, Oauth2Token> Tokens = new ConcurrentDictionary<string, Oauth2Token>();
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tokenFile;
        private Oauth2Token _token;

        public TokenManager(string clientId, string clientSecret)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentNullException("clientId");

            _clientId = clientId;
            _clientSecret = clientSecret;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            _tokenFile = Path.Combine(appData, "GmailService\\", clientId.GetValidFilename() + ".json");
            _token = Tokens.GetOrAdd(_tokenFile, (Oauth2Token)null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>An AccessToken</returns>
        public string GetToken()
        {
            lock (_token)
            {
                if (_token == null)
                {
                    string jsonText = File.ReadAllText(_tokenFile);
                    _token = JsonConvert.DeserializeObject<Oauth2Token>(jsonText);
                }

                // Check if token is still valid
                if (DateTime.UtcNow < _token.ExpirationDate)
                    return _token.AccessToken;

                const string url = AuthorizationServerUrl;
                string content = string.Concat(
                    "refresh_token=", HttpUtility.UrlEncode(_token.RefreshToken),
                    "&client_id=", HttpUtility.UrlEncode(_clientId),
                    "&client_secret=", HttpUtility.UrlEncode(_clientSecret),
                    "&grant_type=", HttpUtility.UrlEncode("refresh_token")
                    );

                var stringContent = new StringContent(content);
                stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                var client = new HttpClient();
                var result = client.PostAsync(url, stringContent).Result;
                string json = result.Content.ReadAsStringAsync().Result;

                result.EnsureSuccessStatusCode();

                SaveToken(json);
                return _token.AccessToken;
            }
        }

        private void SaveToken(string json)
        {
            string currentRefreshToken = _token.RefreshToken;
            _token = JsonConvert.DeserializeObject<Oauth2Token>(json);
            _token.RefreshToken = currentRefreshToken;
            _token.ExpirationDate = DateTime.UtcNow.AddSeconds(_token.ExpiresIn);

            WriteToFile();
        }

        private void WriteToFile()
        {
            // Will create the directory/directories if it doesn't exist
            var tokenFile = new FileInfo(_tokenFile);
            if (tokenFile.Directory != null && !tokenFile.Directory.Exists)
                tokenFile.Directory.Create();

            string tokenString = JsonConvert.SerializeObject(_token);
            File.WriteAllText(_tokenFile, tokenString);
        }

        [Obsolete("For testing only")]
        public Oauth2Token GetToken(string authorizationCode)
        {
            const string url = AuthorizationServerUrl;
            string content = string.Concat(
                "code=", HttpUtility.UrlEncode(authorizationCode),
                "&client_id=", HttpUtility.UrlEncode(_clientId),
                "&client_secret=", HttpUtility.UrlEncode(_clientSecret),
                "&redirect_uri=", HttpUtility.UrlEncode("http://localhost"),
                "&grant_type=", HttpUtility.UrlEncode("authorization_code")
                );

            var stringContent = new StringContent(content);
            stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var client = new HttpClient();
            var result = client.PostAsync(url, stringContent).Result;
            string json = result.Content.ReadAsStringAsync().Result;

            result.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<Oauth2Token>(json);
        }

        [Obsolete("For testing only")]
        public string GetAuthorizationCode()
        {
            string url = string.Concat("https://accounts.google.com/o/oauth2/auth",
                "?client_id=", HttpUtility.UrlEncode(_clientId),
                "&redirect_uri=", HttpUtility.UrlEncode("http://localhost"),
                "&response_type=", HttpUtility.UrlEncode("code"),
                "&scope=", HttpUtility.UrlEncode("https://www.googleapis.com/auth/gmail.modify https://www.googleapis.com/auth/gmail.readonly https://www.googleapis.com/auth/gmail.compose"),
                "&access_type=", HttpUtility.UrlEncode("offline")
                );

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            var res = client.GetAsync(url).Result;
            string content = res.Content.ReadAsStringAsync().Result;

            res.EnsureSuccessStatusCode();

            var file = new FileInfo("login.html");

            File.WriteAllText(file.FullName, content);
            Process.Start(file.FullName);

            Console.WriteLine("Enter URl after accepting request:");
            Console.WriteLine(Environment.NewLine);

            var resultUrl = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(resultUrl))
                throw new Exception("User did not enter valid Authorization code");

            var query = new Uri(resultUrl).Query;
            string code = HttpUtility.ParseQueryString(query).Get("code");

            if (string.IsNullOrWhiteSpace(code))
                throw new Exception();

            return code;
        }

        /// <summary>
        /// Set the refresh token. This is only required for the first request.
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <param name="force"></param>
        public void Setup(string refreshToken, bool force)
        {
            if (force || !File.Exists(_tokenFile))
            {
                var token = new Oauth2Token
                {
                    TokenType = "Bearer",
                    RefreshToken = refreshToken
                };

                _token = Tokens.AddOrUpdate(_tokenFile, token, (key, oldvalue) => token);
                WriteToFile();
            }
        }

        public bool HasTokenSetup()
        {
            return new FileInfo(_tokenFile).Exists;
        }
    }
}
