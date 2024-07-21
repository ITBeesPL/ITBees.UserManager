using System;

namespace ITBees.UserManager.Controllers.Models;

public class PlatformStatusIm
{
    public string MaintenanceMessage { get; set; }
    public bool EverythingWorksFine { get; set; }
    public DateTime? MaintenanceEnd { get; set; }
    public bool IsActive { get; set; }
}