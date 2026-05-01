namespace AMS.Core.Interfaces;

/// <summary>
/// Interface for Azure Blob Storage operations
/// Used for storing claim documents
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Upload a file to blob storage
    /// </summary>
    /// <param name="containerName">The blob container name</param>
    /// <param name="blobName">Unique name for the blob</param>
    /// <param name="content">File content as stream</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <returns>URL to access the blob</returns>
    Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType);
    
    /// <summary>
    /// Download a file from blob storage
    /// </summary>
    /// <param name="containerName">The blob container name</param>
    /// <param name="blobName">Name of the blob to download</param>
    /// <returns>File content as stream</returns>
    Task<Stream> DownloadAsync(string containerName, string blobName);
    
    /// <summary>
    /// Delete a file from blob storage
    /// </summary>
    /// <param name="containerName">The blob container name</param>
    /// <param name="blobName">Name of the blob to delete</param>
    Task DeleteAsync(string containerName, string blobName);
    
    /// <summary>
    /// Get a SAS URL for temporary access to a blob
    /// </summary>
    /// <param name="containerName">The blob container name</param>
    /// <param name="blobName">Name of the blob</param>
    /// <param name="expiresIn">How long the URL should be valid</param>
    /// <returns>SAS URL for accessing the blob</returns>
    Task<string> GetSasUrlAsync(string containerName, string blobName, TimeSpan expiresIn);
}
