namespace AMS.Core.Interfaces;

/// <summary>
/// Interface for Azure Storage Queue operations
/// Used for sending messages to queues for async processing
/// </summary>
public interface IQueueService
{
    /// <summary>
    /// Send a message to a queue
    /// </summary>
    /// <typeparam name="T">Type of message object</typeparam>
    /// <param name="queueName">Name of the queue</param>
    /// <param name="message">Message object (will be serialized to JSON)</param>
    Task SendMessageAsync<T>(string queueName, T message);
    
    /// <summary>
    /// Send a message to the claim-submitted queue
    /// </summary>
    Task SendClaimSubmittedAsync(ClaimSubmittedMessage message);
    
    /// <summary>
    /// Send a message to the claim-status-changed queue
    /// </summary>
    Task SendClaimStatusChangedAsync(ClaimStatusChangedMessage message);
}

/// <summary>
/// Message sent when a new claim is submitted
/// </summary>
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

/// <summary>
/// Message sent when a claim status changes
/// </summary>
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
