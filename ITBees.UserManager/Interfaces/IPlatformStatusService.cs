using ITBees.UserManager.Controllers.Models;

namespace ITBees.UserManager.Interfaces;

public interface IPlatformStatusService
{
    PlatformStatusVm GetCurrentStatus(string lang);
    PlatformStatusVm CreateNewPlatformStatus(PlatformStatusIm platformStatusIm);
    PlatformStatusVm ChangePlatformStatus(PlatformStatusUm platformStatusUm);
}