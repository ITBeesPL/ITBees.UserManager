using System;
using System.Collections.Generic;
using System.Linq;
using ITBees.Interfaces.Repository;
using ITBees.UserManager.DbModels;
using ITBees.UserManager.Interfaces;
using ITBees.UserManager.Interfaces.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ITBees.UserManager.Services.Registration
{
    public class ConfirmationUrlParametersStore : IConfirmationUrlParametersStore
    {
        public const int StoredParametersLifetimeInDays = 14;

        private const int MaxParameters = 10;
        private const int MaxNameLength = 64;
        private const int MaxValueLength = 512;

        private readonly IReadOnlyRepository<UserConfirmationUrlParameters> _userConfirmationUrlParametersRoRepo;
        private readonly IWriteOnlyRepository<UserConfirmationUrlParameters> _userConfirmationUrlParametersWoRepo;
        private readonly ILogger<ConfirmationUrlParametersStore> _logger;

        public ConfirmationUrlParametersStore(
            IReadOnlyRepository<UserConfirmationUrlParameters> userConfirmationUrlParametersRoRepo,
            IWriteOnlyRepository<UserConfirmationUrlParameters> userConfirmationUrlParametersWoRepo,
            ILogger<ConfirmationUrlParametersStore> logger)
        {
            _userConfirmationUrlParametersRoRepo = userConfirmationUrlParametersRoRepo;
            _userConfirmationUrlParametersWoRepo = userConfirmationUrlParametersWoRepo;
            _logger = logger;
        }

        public void Save(Guid userAccountGuid, List<ConfirmationUrlParameterIm> confirmationUrlParameters)
        {
            var sanitized = Sanitize(confirmationUrlParameters);
            if (sanitized.Count == 0)
            {
                return;
            }

            var parametersJson = JsonConvert.SerializeObject(sanitized);
            var now = DateTime.UtcNow;

            var updated = _userConfirmationUrlParametersWoRepo.UpdateData(
                x => x.UserAccountGuid == userAccountGuid,
                x =>
                {
                    x.ParametersJson = parametersJson;
                    x.Created = now;
                });

            if (updated == null || updated.Count == 0)
            {
                _userConfirmationUrlParametersWoRepo.InsertData(new UserConfirmationUrlParameters()
                {
                    UserAccountGuid = userAccountGuid,
                    ParametersJson = parametersJson,
                    Created = now
                });
            }
        }

        public List<ConfirmationUrlParameterIm> Get(Guid userAccountGuid)
        {
            var stored = _userConfirmationUrlParametersRoRepo
                .GetData(x => x.UserAccountGuid == userAccountGuid)
                .FirstOrDefault();

            if (stored == null || string.IsNullOrWhiteSpace(stored.ParametersJson))
            {
                return null;
            }

            if (stored.Created.AddDays(StoredParametersLifetimeInDays) < DateTime.UtcNow)
            {
                _logger.LogInformation(
                    "Stored confirmation url parameters for user {userAccountGuid} expired (created {created}), ignoring them.",
                    userAccountGuid, stored.Created);
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<List<ConfirmationUrlParameterIm>>(stored.ParametersJson);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e,
                    "Could not read stored confirmation url parameters for user {userAccountGuid}, ignoring them.",
                    userAccountGuid);
                return null;
            }
        }

        private static List<ConfirmationUrlParameterIm> Sanitize(
            List<ConfirmationUrlParameterIm> confirmationUrlParameters)
        {
            var result = new List<ConfirmationUrlParameterIm>();
            if (confirmationUrlParameters == null)
            {
                return result;
            }

            var alreadyAdded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var parameter in confirmationUrlParameters)
            {
                if (result.Count == MaxParameters)
                {
                    break;
                }

                if (parameter == null || string.IsNullOrWhiteSpace(parameter.Name))
                {
                    continue;
                }

                var name = RemoveControlCharacters(parameter.Name).Trim();
                var value = RemoveControlCharacters(parameter.Value ?? string.Empty).Trim();

                if (name.Length == 0 || name.Length > MaxNameLength || value.Length > MaxValueLength)
                {
                    continue;
                }

                if (alreadyAdded.Add(name) == false)
                {
                    continue;
                }

                result.Add(new ConfirmationUrlParameterIm(name, value));
            }

            return result;
        }

        private static string RemoveControlCharacters(string value)
        {
            return new string(value.Where(x => char.IsControl(x) == false).ToArray());
        }
    }
}
