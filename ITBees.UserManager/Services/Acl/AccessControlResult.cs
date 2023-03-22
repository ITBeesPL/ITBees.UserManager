namespace ITBees.UserManager.Services.Acl
{
    public class AccessControlResult
    {
        public AccessControlResult(bool canDoResult, string message)
        {
            CanDoResult = canDoResult;
            Message = message;
        }

        public bool CanDoResult { get; set; }
        public string Message { get; set; }
    }
}