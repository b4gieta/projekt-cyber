using System.Text.RegularExpressions;

namespace projekt_cyber.Services
{
    public static class PasswordPolicyValidator
    {
        public static bool HasThreeRepeatedChars(string password)
        {
            return Regex.IsMatch(password, @"(.)\1\1");
        }
    }
}
