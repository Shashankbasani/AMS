using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AMS.Core.Interfaces;

namespace AMS.WebAPI.Services;

/// <summary>
/// Azure Storage Queue Service Implementation
/// 
/// WHAT ARE AZURE STORAGE QUEUES?
/// - Simple message queues for asynchronous processing
/// - Messages are stored reliably and processed later
/// - Different from Service Bus (simpler, cheaper, fewer features)
/// 
/// HOW IT WORKS:
/// 1. Web API sends message to queue (fast, non-blocking)
/// 2. Azure Function picks up message from queue
/// 3. Function processes message (send email, update systems, etc.)
/// 
/// WHY USE QUEUES?
/// - Decouple components (API doesn't wait for email to send)
/// - Handle spikes in load (queue buffers requests)
/// - Retry failed operations automatically
/// </summary>
public class QueueService : IQueueService
{
    private readonly QueueServiceClient _queueServiceClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QueueService> _logger;
    
    public QueueService(
        QueueServiceClient queueServiceClient,
        IConfiguration configuration,
        ILogger<QueueService> logger)
    {
        _queueServiceClient = queueServiceClient;
        _configuration = configuration;
        _logger = logger;
    }
    
    /// <summary>
    /// Send a message to any queue
    /// </summary>
    public async Task SendMessageAsync<T>(string queueName, T message)
    {
        try
        {
            // Get queue client (creates queue if it doesn't exist)
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            await queueClient.CreateIfNotExistsAsync();
            
            // Serialize message to JSON
            var jsonMessage = JsonSerializer.Serialize(message);
            
            // Encode as Base64 (required for Azure Storage Queues)
            var base64Message = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonMessage));
            
            // Send message to queue
            await queueClient.SendMessageAsync(base64Message);
            
            _logger.LogInformation("Message sent to queue {QueueName}: {MessageType}", 
                queueName, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to queue {QueueName}", queueName);
            throw;
        }
    }
    
    /// <summary>
    /// Send claim submitted notification
    /// </summary>
    public async Task SendClaimSubmittedAsync(ClaimSubmittedMessage message)
    {
        var queueName = _configuration["AzureQueues:ClaimSubmitted"] ?? "claim-submitted";
        await SendMessageAsync(queueName, message);
        
        _logger.LogInformation("Claim submitted message sent for claim {ClaimNumber}", message.ClaimNumber);
    }
    
    /// <summary>
    /// Send claim status changed notification
    /// </summary>
    public async Task SendClaimStatusChangedAsync(ClaimStatusChangedMessage message)
    {
        var queueName = _configuration["AzureQueues:ClaimStatusChanged"] ?? "claim-status-changed";
        await SendMessageAsync(queueName, message);
        
        _logger.LogInformation("Claim status changed message sent for claim {ClaimNumber}: {OldStatus} -> {NewStatus}", 
            message.ClaimNumber, message.OldStatus, message.NewStatus);
    }
}
