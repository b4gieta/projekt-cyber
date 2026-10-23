namespace projekt_cyber.Models
{
    public class PasswordPolicySettings
    {
        public int Id { get; set; } = 1;
        public bool RejectThreeRepeatedChars { get; set; } = true;
        public int IdleTimeoutMinutes { get; set; } = 30;
    }
}
