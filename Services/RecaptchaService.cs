using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace projekt_cyber.Services
{
    public class RecaptchaSettings
    {
        public string SiteKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
    }

    public interface IRecaptchaService
    {
        Task<bool> VerifyAsync(string? token);
    }

    public class RecaptchaService : IRecaptchaService
    {
        private readonly HttpClient _http;
        private readonly RecaptchaSettings _settings;

        public RecaptchaService(HttpClient http, IOptions<RecaptchaSettings> options)
        {
            _http = http;
            _settings = options.Value;
        }

        public async Task<bool> VerifyAsync(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = _settings.SecretKey,
                ["response"] = token
            });

            try
            {
                var response = await _http.PostAsync(
                    "https://www.google.com/recaptcha/api/siteverify", content);

                if (!response.IsSuccessStatusCode) return false;

                var result = await response.Content.ReadFromJsonAsync<RecaptchaResponse>();
                return result?.Success == true;
            }
            catch
            {
                return false;
            }
        }

        private class RecaptchaResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }
        }
    }
}