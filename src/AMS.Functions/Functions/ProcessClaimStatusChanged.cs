using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace AMS.Functions;

/// <summary>
/// Azure Function triggered by Storage Queue when a claim status changes
/// </summary>
public class ProcessClaimStatusChanged
{
    private readonly ILogger<ProcessClaimStatusChanged> _logger;

    public ProcessClaimStatusChanged(ILogger<ProcessClaimStatusChanged> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ProcessClaimStatusChanged))]
    public async Task Run(
        [QueueTrigger("claim-status-changed", Connection = "AzureWebJobsStorage")] string queueMessage)
    {
        _logger.LogInformation("Processing claim status changed message: {Message}", queueMessage);
        
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
            var message = JsonSerializer.Deserialize<ClaimStatusChangedMessage>(jsonString);
            
            if (message == null)
            {
                _logger.LogError("Failed to deserialize claim status changed message");
                return;
            }
            
            _logger.LogInformation(
                "Claim {ClaimNumber} status changed: {OldStatus} -> {NewStatus}", 
                message.ClaimNumber, 
                message.OldStatus, 
                message.NewStatus);
            
            await SendStatusChangeEmailAsync(message);
            
            _logger.LogInformation("Claim status change processed for {ClaimNumber}", message.ClaimNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing claim status changed message");
            throw;
        }
    }
    
    private async Task SendStatusChangeEmailAsync(ClaimStatusChangedMessage message)
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
        
        var statusEmoji = message.NewStatus switch
        {
            "Approved" => "✅",
            "Rejected" => "❌",
            "UnderReview" => "🔍",
            "Paid" => "💰",
            _ => "📋"
        };
        
        var subject = $"{statusEmoji} Claim {message.ClaimNumber} Status: {message.NewStatus}";
        
        var approvedAmountText = message.ApprovedAmount.HasValue 
            ? $"Approved Amount: ${message.ApprovedAmount:N2}" 
            : "";
        
        var plainTextContent = $@"
Claim Status Update

Claim Number: {message.ClaimNumber}
Client: {message.ClientName}

Status Change: {message.OldStatus} → {message.NewStatus}
Changed At: {message.ChangedAt:yyyy-MM-dd HH:mm}

Claim Amount: ${message.ClaimAmount:N2}
{approvedAmountText}

Reviewer Notes:
{message.ReviewerNotes ?? "No notes provided"}
";
        
        var htmlContent = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
<h2 style='color: #2c3e50;'>{statusEmoji} Claim Status Update</h2>
<table style='border-collapse: collapse; width: 100%; max-width: 600px;'>
<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Claim Number:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{message.ClaimNumber}</td></tr>
<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Client:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{message.ClientName}</td></tr>
<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Previous Status:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{message.OldStatus}</td></tr>
<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>New Status:</strong></td><td style='padding: 8px; border: 1px solid #ddd; font-weight: bold; color: {(message.NewStatus == "Approved" ? "green" : message.NewStatus == "Rejected" ? "red" : "blue")};'>{message.NewStatus}</td></tr>
<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Claim Amount:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>${message.ClaimAmount:N2}</td></tr>
{(message.ApprovedAmount.HasValue ? $"<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Approved Amount:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>${message.ApprovedAmount:N2}</td></tr>" : "")}
<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Changed At:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{message.ChangedAt:yyyy-MM-dd HH:mm}</td></tr>
</table>
{(string.IsNullOrEmpty(message.ReviewerNotes) ? "" : $"<h3>Reviewer Notes:</h3><p style='background: #f9f9f9; padding: 10px; border-radius: 5px;'>{message.ReviewerNotes}</p>")}
</body>
</html>
";
        
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);
        
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Status change email sent for claim {ClaimNumber}", message.ClaimNumber);
        }
        else
        {
            _logger.LogWarning("Failed to send email. Status: {StatusCode}", response.StatusCode);
        }
    }
}

public class ClaimStatusChangedMessage
{
    public Guid ClaimId { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public decimal ClaimAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public string? ReviewerNotes { get; set; }
    public DateTime ChangedAt { get; set; }
}
