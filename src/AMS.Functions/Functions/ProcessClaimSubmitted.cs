using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace AMS.Functions;

/// <summary>
/// Azure Function triggered by Storage Queue when a claim is submitted
/// 
/// FLOW:
/// 1. Web API creates claim and sends message to "claim-submitted" queue
/// 2. This function is triggered automatically
/// 3. Function sends confirmation email via SendGrid
/// 4. Email is sent to basshashank@gmail.com (notification email)
/// </summary>
public class ProcessClaimSubmitted
{
    private readonly ILogger<ProcessClaimSubmitted> _logger;

    public ProcessClaimSubmitted(ILogger<ProcessClaimSubmitted> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Triggered when a message arrives in the "claim-submitted" queue
    /// The message can be Base64 encoded JSON or plain JSON
    /// </summary>
    [Function(nameof(ProcessClaimSubmitted))]
    public async Task Run(
        [QueueTrigger("claim-submitted", Connection = "AzureWebJobsStorage")] string queueMessage)
    {
        _logger.LogInformation("Processing claim submitted message: {Message}", queueMessage);
        
        try
        {
            // Try to decode as Base64 first, fallback to plain JSON
            string jsonString;
            try
            {
                jsonString = Encoding.UTF8.GetString(Convert.FromBase64String(queueMessage));
            }
            catch (FormatException)
            {
                // Message is plain JSON, not Base64
                jsonString = queueMessage;
            }
            
            _logger.LogInformation("Decoded JSON: {Json}", jsonString);
            var message = JsonSerializer.Deserialize<ClaimSubmittedMessage>(jsonString);
            
            if (message == null)
            {
                _logger.LogError("Failed to deserialize claim submitted message");
                return;
            }
            
            _logger.LogInformation("Processing claim: {ClaimNumber} for client: {ClientName}", 
                message.ClaimNumber, message.ClientName);
            
            // Send email notification
            await SendEmailNotificationAsync(message);
            
            _logger.LogInformation("Claim {ClaimNumber} processed successfully", message.ClaimNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing claim submitted message");
            throw; // Rethrow to trigger retry
        }
    }
    
    private async Task SendEmailNotificationAsync(ClaimSubmittedMessage message)
    {
        var sendGridApiKey = Environment.GetEnvironmentVariable("SendGridApiKey");
        var notificationEmail = Environment.GetEnvironmentVariable("NotificationEmail") ?? "basshashank@gmail.com";
        
        if (string.IsNullOrEmpty(sendGridApiKey))
        {
            _logger.LogWarning("SendGrid API key not configured. Email not sent.");
            return;
        }
        
        var client = new SendGridClient(sendGridApiKey);
        var from = new EmailAddress("basshashank@gmail.com", "Agency Management System");
        var to = new EmailAddress(notificationEmail);
        var subject = $"New Claim Submitted: {message.ClaimNumber}";
        
        var plainTextContent = $@"
New Claim Submitted

Claim Number: {message.ClaimNumber}
Policy Number: {message.PolicyNumber}
Client: {message.ClientName}
Client Email: {message.ClientEmail}

Claim Amount: ${message.ClaimAmount:N2}
Incident Date: {message.IncidentDate:yyyy-MM-dd}
Filed Date: {message.FiledDate:yyyy-MM-dd HH:mm}

Description:
{message.Description}

Please review this claim in the Agency Management System.
";
        
        var htmlContent = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
<h2 style='color: #2c3e50;'>New Claim Submitted</h2>
<table style='border-collapse: collapse; width: 100%; max-width: 600px;'>
<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Claim Number:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{message.ClaimNumber}</td></tr>
<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Policy Number:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{message.PolicyNumber}</td></tr>
<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Client:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{message.ClientName}</td></tr>
<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Client Email:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{message.ClientEmail}</td></tr>
<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Claim Amount:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>${message.ClaimAmount:N2}</td></tr>
<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Incident Date:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{message.IncidentDate:yyyy-MM-dd}</td></tr>
<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Filed Date:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{message.FiledDate:yyyy-MM-dd HH:mm}</td></tr>
</table>
<h3>Description:</h3>
<p style='background: #f9f9f9; padding: 10px; border-radius: 5px;'>{message.Description}</p>
<p style='color: #7f8c8d; font-size: 12px;'>Please review this claim in the Agency Management System.</p>
</body>
</html>
";
        
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);
        
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Email notification sent for claim {ClaimNumber}", message.ClaimNumber);
        }
        else
        {
            _logger.LogWarning("Failed to send email. Status: {StatusCode}", response.StatusCode);
        }
    }
}

public class ClaimSubmittedMessage
{
    public Guid ClaimId { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public Guid PolicyId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public decimal ClaimAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; }
    public DateTime FiledDate { get; set; }
}
