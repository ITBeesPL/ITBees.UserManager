using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ITBees.Interfaces.Platforms;
using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.Translations;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Services.Registration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace ITBees.UserManager.Services.AppleLogins
{
    public class AppleLoginService<T> : IAppleLoginService<T> where T : IdentityUser
    {
        private readonly IPlatformSettingsService _platformSettingsService;
        private readonly HttpClient _httpClient;
        private readonly ILoginService<T> _loginService;
        private readonly ILogger<AppleLoginService<T>> _logger;
        private readonly IReadOnlyRepository<UserAccount> _userOnlyRepository;
        private readonly INewUserRegistrationFromApple _newUserRegistrationFromApple;

        public AppleLoginService(IPlatformSettingsService platformSettingsService,
            HttpClient httpClient,
            ILoginService<T> loginService,
            ILogger<AppleLoginService<T>> logger,
            IReadOnlyRepository<UserAccount> userOnlyRepository,
            INewUserRegistrationFromApple newUserRegistrationFromApple)
        {
            _platformSettingsService = platformSettingsService;
            _httpClient = httpClient;
            _loginService = loginService;
            _logger = logger;
            _userOnlyRepository = userOnlyRepository;
            _newUserRegistrationFromApple = newUserRegistrationFromApple;
        }

        public async Task<TokenVm> LoginOrRegister(AppleTokenResponse appleAuthorizationToken, string lang)
        {
            var result = ParseAppleTokenClaims(appleAuthorizationToken.IdToken);

            if (result.EmailVerified == false)
            {
                _logger.LogError($"Email not confirmed at apple : {result.Email}");
                throw new UnauthorizedAccessException(Translate.Get(() => Translations.LoginWithApple.Errors.EmailNotConfirmed, lang));
            }

            var userAccount = _userOnlyRepository.GetData(x => x.Email == result.Email).FirstOrDefault();
            if (userAccount == null)
            {
                var resultAccount = await _newUserRegistrationFromApple.CreateNewUserAccountFromAppleLogin(result);
                return resultAccount;
            }

            return await _loginService.LoginAfterEmailConfirmation(result.Email,lang);
        }

        public async Task<AppleTokenResponse> ValidateAuthorizationCodeAsync(string authorizationCode, string clientId = "", string redirectURI = "")
        {
            if (string.IsNullOrEmpty(clientId))
                clientId = _platformSettingsService.GetSetting("AppleLogin_clientId");

            if (string.IsNullOrEmpty(redirectURI))
                _platformSettingsService.GetSetting("AppleLogin_redirectUri");

            var clientSecret = GenerateClientSecret(clientId);
            var requestBody = new Dictionary<string, string>
            {
                {"client_id", clientId},
                {"client_secret", clientSecret},
                {"code", authorizationCode},
                {"grant_type", "authorization_code"},
                {"redirect_uri", redirectURI}
            };

            var requestContent = new FormUrlEncodedContent(requestBody);
            var response = await _httpClient.PostAsync("https://appleid.apple.com/auth/token", requestContent);

            if (!response.IsSuccessStatusCode)
            {
                var message = $"Incorrect key exchange Status: {response.StatusCode}";
                _logger.LogError($"{message} for authorizationCode {authorizationCode}");
                throw new HttpRequestException(message);
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<AppleTokenResponse>(jsonContent);

            return tokenResponse;
        }

        private AppleTokenClaims ParseAppleTokenClaims(string idToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadToken(idToken) as JwtSecurityToken;

            var claims = new AppleTokenClaims
            {
                Issuer = token.Claims.FirstOrDefault(c => c.Type == "iss")?.Value,
                Audience = token.Claims.FirstOrDefault(c => c.Type == "aud")?.Value,
                ExpirationTime = long.Parse(token.Claims.FirstOrDefault(c => c.Type == "exp")?.Value ?? "0"),
                IssuedAt = long.Parse(token.Claims.FirstOrDefault(c => c.Type == "iat")?.Value ?? "0"),
                Subject = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value,
                AccessTokenHash = token.Claims.FirstOrDefault(c => c.Type == "at_hash")?.Value,
                Email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
                EmailVerified = bool.Parse(token.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value ?? "false"),
                AuthenticationTime = long.Parse(token.Claims.FirstOrDefault(c => c.Type == "auth_time")?.Value ?? "0"),
                NonceSupported = bool.Parse(token.Claims.FirstOrDefault(c => c.Type == "nonce_supported")?.Value ?? "false"),
                FirstName = string.Empty,
                LastName = string.Empty,
            };

            return claims;
        }

        private string GenerateClientSecret(string clientId = "")
        {
            string teamId = _platformSettingsService.GetSetting("AppleLogin_teamId");

            if (string.IsNullOrEmpty(clientId))
                clientId = _platformSettingsService.GetSetting("AppleLogin_clientId"); ;

            string keyId = _platformSettingsService.GetSetting("AppleLogin_keyId");
            string keyContent = _platformSettingsService.GetSetting("AppleLogin_keyContent");
            string keyPath = _platformSettingsService.GetSetting("AppleLogin_keyPath");

            keyContent = keyContent.Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace(" ", "");

            string keyContentFromFile = string.Empty;

            if (string.IsNullOrEmpty(keyPath))
            {
                keyContentFromFile = keyContent;
            }
            else
            {
                keyContentFromFile = File.ReadAllText(keyPath);
            }

            var ecdsa = ECDsa.Create();

            try
            {
                ecdsa.ImportPkcs8PrivateKey(Convert.FromBase64String(keyContentFromFile.Replace("-----BEGIN PRIVATE KEY-----", "").Replace("-----END PRIVATE KEY-----", "").Replace("\n", "")), out _);

            }
            catch (FormatException ex)
            {
                _logger.LogError("Unable to import key PKCS8: " + ex.Message);
                throw new Exception("Unable to import key PKCS8: " + ex.Message);
            }


            var signingCredentials = new SigningCredentials(new ECDsaSecurityKey(ecdsa) { KeyId = keyId }, SecurityAlgorithms.EcdsaSha256);

            var tokenHandler = new JwtSecurityTokenHandler();
            var now = DateTime.UtcNow;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                IssuedAt = now,
                Expires = now.AddMinutes(5),
                Issuer = teamId,
                Audience = "https://appleid.apple.com",
                SigningCredentials = signingCredentials,
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("sub", clientId),
                })
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

    }
}