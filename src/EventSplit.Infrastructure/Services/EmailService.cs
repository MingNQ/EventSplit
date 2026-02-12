using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EventSplit.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventSplit.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly HttpClient _httpClient;
    private const string ResendApiUrl = "https://api.resend.com/emails";

    public EmailService(IConfiguration config, ILogger<EmailService> logger, HttpClient httpClient)
    {
        _config = config;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        var apiKey = _config["Resend:ApiKey"];
        var fromEmail = _config["Resend:From"];

        // If Resend is not configured, log and skip
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(fromEmail))
        {
            _logger.LogWarning("Resend not configured. Skipping email to {To}: {Subject}", to, subject);
            return;
        }

        try
        {
            var payload = new
            {
                from = fromEmail,
                to = new[] { to },
                subject,
                html = htmlBody
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, ResendApiUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
            }
            else
            {
                _logger.LogError("Resend API error {StatusCode}: {Body}", response.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
            // Don't throw - email failure should not break the app flow
        }
    }
}
