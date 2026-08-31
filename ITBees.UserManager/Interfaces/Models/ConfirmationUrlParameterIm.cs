namespace ITBees.UserManager.Interfaces.Models
{
    public class ConfirmationUrlParameterIm
    {
        public ConfirmationUrlParameterIm()
        {
        }

        public ConfirmationUrlParameterIm(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }

        public string Value { get; set; }
    }
}
