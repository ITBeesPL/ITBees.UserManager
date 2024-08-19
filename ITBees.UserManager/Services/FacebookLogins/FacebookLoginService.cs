using System;
using System.Net.Http;
using System.Threading.Tasks;
using ITBees.Translations;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ITBees.UserManager.Services.FacebookLogins
{
    public class FacebookLoginService<T> : IFacebookLoginService<T> where T : IdentityUser
    {
        private readonly ILogger<FacebookLoginService<T>> _logger;
        private readonly HttpClient _httpClient;
        private readonly ILoginService<T> _loginService;

        public FacebookLoginService(ILogger<FacebookLoginService<T>> logger, 
            HttpClient httpClient,
            ILoginService<T> loginService)
        {
            _logger = logger;
            _httpClient = httpClient;
            _loginService = loginService;
        }

        public async Task<FacebookLoginResult> ValidateAccessToken(string accessToken)
        {
            var verifyTokenEndPoint = $"https://graph.facebook.com/me?access_token={accessToken}&fields=id,name,email";
            var verifyTokenResponse = await _httpClient.GetAsync(verifyTokenEndPoint);
            if (!verifyTokenResponse.IsSuccessStatusCode)
            {
                var invalidFacebookToken = "Invalid Facebook token.";
                _logger.LogError($"{invalidFacebookToken} access token : {accessToken} {verifyTokenResponse.ToString()}");
                throw new Exception(invalidFacebookToken);
            }

            var content = await verifyTokenResponse.Content.ReadAsStringAsync();
            var deserializeObject = JsonConvert.DeserializeObject<FacebookLoginResult>(content);

            return deserializeObject;
        }

        public Task<TokenVm> LoginOrRegister(FacebookLoginResult result, string lang)
        {
            if (string.IsNullOrEmpty(result.Email))
            {
                _logger.LogError($"Email not could not be empty");
                throw new UnauthorizedAccessException(Translate.Get(() => Translations.LoginWithFacebook.Errors.EmailCouldNotBeEmpty, lang));
            }

            return _loginService.LoginAfterEmailConfirmation(result.Email, lang);
        }
    }
}