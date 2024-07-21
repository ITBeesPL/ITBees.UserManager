using System;
using ITBees.Models.Users;

namespace ITBees.UserManager.DbModels;

public class PlatformStatus
{
    public int Id { get; set; }
    public bool EverythingWorksFine { get; set; }
    public string MaintenanceMessage { get; set; }
    public DateTime? MaintenanceEnd { get; set; }
    public bool IsActive { get; set; }
    public UserAccount CreatedBy { get; set; }
    public Guid CreatedByGuid { get; set; }
    public DateTime CreatedDate { get; set; }
    public UserAccount ActivatedBy { get; set; }
    public Guid ActivatedByGuid { get; set; }
    public DateTime? ActivatedDate { get; set; }
    public UserAccount DeactivatedBy { get; set; }
    public Guid DeactivatedByGuid { get; set; }
    public DateTime? DeactivatedDate { get; set; }
}