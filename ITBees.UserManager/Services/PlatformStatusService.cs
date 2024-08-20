using System;
using System.Linq;
using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.UserManager.Controllers.Models;
using ITBees.UserManager.DbModels;
using ITBees.UserManager.Interfaces;

namespace ITBees.UserManager.Services;

public class PlatformStatusService : IPlatformStatusService
{
    private readonly IWriteOnlyRepository<PlatformStatus> _platformStatusRwRepo;
    private readonly IReadOnlyRepository<PlatformStatus> _platformStatusRoRepo;
    private readonly IAspCurrentUserService _aspCurrentUserService;

    public PlatformStatusService(IWriteOnlyRepository<PlatformStatus> platformStatusRwRepo,
        IReadOnlyRepository<PlatformStatus> platformStatusRoRepo,
        IAspCurrentUserService aspCurrentUserService)
    {
        _platformStatusRwRepo = platformStatusRwRepo;
        _platformStatusRoRepo = platformStatusRoRepo;
        _aspCurrentUserService = aspCurrentUserService;
    }
    public PlatformStatusVm GetCurrentStatus(string lang)
    {
        var platformStatus = _platformStatusRoRepo.GetData(x => x.IsActive).FirstOrDefault();
        if (platformStatus == null)
        {
            return new PlatformStatusVm()
            {
                EverythingWorksFine = true,
                MaintenanceEnd = null,
                MaintenanceMessage = String.Empty
            };
        }

        return new PlatformStatusVm(platformStatus);
    }

   public PlatformStatusVm ChangePlatformStatus(PlatformStatusUm platformStatusUm)
    {
        var currentUser = CheckMyAccess();
        PlatformStatus result = null;
        if (platformStatusUm.IsActive)
        {
            result = _platformStatusRwRepo.UpdateData(x => x.Id == platformStatusUm.Id, x =>
            {
                x.MaintenanceMessage = platformStatusUm.MaintenanceMessage;
                x.MaintenanceEnd = platformStatusUm.MaintenanceEnd;
                x.ActivatedByGuid = currentUser.Guid;
                x.ActivatedDate = DateTime.Now;
                x.EverythingWorksFine = platformStatusUm.EverythingWorksFine;
                x.IsActive = platformStatusUm.IsActive;
            }).First();
        }
        else
        {
            result = _platformStatusRwRepo.UpdateData(x => x.Id == platformStatusUm.Id, x =>
            {
                x.MaintenanceMessage = platformStatusUm.MaintenanceMessage;
                x.MaintenanceEnd = platformStatusUm.MaintenanceEnd;
                x.DeactivatedByGuid = currentUser.Guid;
                x.DeactivatedDate = DateTime.Now;
                x.EverythingWorksFine = platformStatusUm.EverythingWorksFine;
                x.IsActive = platformStatusUm.IsActive;
            }).First();
        }

        return new PlatformStatusVm(_platformStatusRoRepo.GetData(x => x.Id == result.Id, x => x.CreatedBy, x=>x.ActivatedBy, x=>x.DeactivatedBy).First());

    }

    public PlatformStatusVm CreateNewPlatformStatus(PlatformStatusIm platformStatusIm)
    {
        var currentUser = CheckMyAccess();

        var result = _platformStatusRwRepo.InsertData(new PlatformStatus()
        {
            MaintenanceMessage = platformStatusIm.MaintenanceMessage,
            MaintenanceEnd = platformStatusIm.MaintenanceEnd,
            CreatedDate = DateTime.Now,
            CreatedByGuid = currentUser.Guid,
            EverythingWorksFine = platformStatusIm.EverythingWorksFine,
            IsActive = platformStatusIm.IsActive
        });
        _platformStatusRwRepo.UpdateData(x => x.Id != result.Id && x.DeactivatedByGuid == null, x =>
        {
            x.IsActive = false;
            x.DeactivatedDate = DateTime.Now;
            x.DeactivatedByGuid = currentUser.Guid;
        });

        return new PlatformStatusVm(_platformStatusRoRepo.GetData(x => x.Id == result.Id, x => x.CreatedBy).First());
    }

    private CurrentUser CheckMyAccess()
    {
        var currentUser = _aspCurrentUserService.GetCurrentUser();
        if (currentUser == null || currentUser.UserRoles.Contains("PlatformOperator") == false)
        {
            throw new UnauthorizedAccessException();
        }

        return currentUser;
    }
}