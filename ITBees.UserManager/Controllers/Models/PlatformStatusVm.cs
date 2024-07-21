using System;
using ITBees.UserManager.DbModels;

namespace ITBees.UserManager.Controllers.Models;

public class PlatformStatusVm
{
    public PlatformStatusVm() { }

    public PlatformStatusVm(PlatformStatus platformStatus)
    {
        EverythingWorksFine = platformStatus.EverythingWorksFine;
        MaintenanceMessage = platformStatus.MaintenanceMessage;
        MaintenanceEnd = platformStatus.MaintenanceEnd;
    }

    public bool EverythingWorksFine { get; set; }
    public string MaintenanceMessage { get; set; }
    public DateTime? MaintenanceEnd { get; set; }
}