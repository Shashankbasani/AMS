using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using AMS.Core.Interfaces;

namespace AMS.Infrastructure.Services;

/// <summary>
/// Azure Blob Storage Service Implementation
/// 
/// WHAT IS AZURE BLOB STORAGE?
/// - A cloud storage service for unstructured data (files, images, documents)
/// - "Blob" = Binary Large Object
/// - Organized in "containers" (like folders)
/// - Each file is a "blob"
/// 
/// WHY USE IT?
/// - Scalable: Can store petabytes of data
/// - Cheap: Much cheaper than database storage for files
/// - Fast: Files are served from Azure CDN
/// - Secure: Supports encryption and access control
/// 
/// HOW WE USE IT:
/// - Claim documents are uploaded to Blob Storage
/// - We store the blob URL in our SQL database
/// - When user needs the file, we generate a SAS URL (temporary access)
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    
    /// <summary>
    /// Constructor - receives the BlobServiceClient from DI
    /// The BlobServiceClient is configured with the connection string in Program.cs
    /// </summary>
    public BlobStorageService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }
    
    /// <summary>
    /// Upload a file to blob storage
    /// 
    /// FLOW:
    /// 1. Get or create the container
    /// 2. Get a reference to the blob
    /// 3. Upload the stream
    /// 4. Return the blob URL
    /// </summary>
    public async Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType)
    {
        // Get the container (creates it if it doesn't exist)
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
        
        // Get a reference to the blob
        var blobClient = containerClient.GetBlobClient(blobName);
        
        // Upload the file with the specified content type
        var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };
        await blobClient.UploadAsync(content, new BlobUploadOptions { HttpHeaders = blobHttpHeaders });
        
        // Return the blob URI
        return blobClient.Uri.ToString();
    }
    
    /// <summary>
    /// Download a file from blob storage
    /// Returns the file content as a stream
    /// </summary>
    public async Task<Stream> DownloadAsync(string containerName, string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        
        var download = await blobClient.DownloadAsync();
        return download.Value.Content;
    }
    
    /// <summary>
    /// Delete a file from blob storage
    /// </summary>
    public async Task DeleteAsync(string containerName, string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        
        await blobClient.DeleteIfExistsAsync();
    }
    
    /// <summary>
    /// Generate a SAS (Shared Access Signature) URL
    /// 
    /// WHAT IS SAS?
    /// - A URL with a special token that grants temporary access to a blob
    /// - The URL includes permissions (read, write, delete) and expiration time
    /// - Even if the container is private, anyone with the SAS URL can access the blob
    /// 
    /// WHY USE SAS?
    /// - Keep blobs private by default
    /// - Grant temporary access when needed
    /// - Revoke access by letting the URL expire
    /// </summary>
    public Task<string> GetSasUrlAsync(string containerName, string blobName, TimeSpan expiresIn)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        
        // Create SAS token with read permission
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b", // "b" for blob
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn)
        };
        
        sasBuilder.SetPermissions(BlobSasPermissions.Read);
        
        // Generate the SAS URI
        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        
        return Task.FromResult(sasUri.ToString());
    }
}
