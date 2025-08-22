using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using DocProcessingApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Azure.Storage.Sas;


namespace DocProcessingApi.Controllers;

[ApiController]
[Route("upload")]
public class UploadController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly BlobServiceClient _blobClient;
    private readonly QueueClient _queueClient;

    public UploadController(IConfiguration config)
    {
        _config = config;
        var connectionString = _config["AzureStorage:ConnectionString"];
        Console.WriteLine("Connection String: " + _config["AzureStorage:ConnectionString"]);

        _blobClient = new BlobServiceClient(connectionString);
        _queueClient = new QueueClient(connectionString, _config["AzureStorage:QueueName"]);
        _queueClient.CreateIfNotExists();
    }

    [HttpPost]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    { 
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var jobId = Guid.NewGuid().ToString();

        // Ensure the container exists
        var container = _blobClient.GetBlobContainerClient(_config["AzureStorage:BlobContainerName"]);
        await container.CreateIfNotExistsAsync();

        // Upload the file to Blob Storage
        var blobName = $"{jobId}_{file.FileName}";
        var blobClient = container.GetBlobClient(blobName);

        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: true);

        var sasUri = blobClient.GenerateSasUri(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.AddMinutes(10)
        );

        // Build the job message
        var message = new DocumentJobMessage
        {
            JobId = jobId,
            FileName = file.FileName,
            BlobUri = sasUri.ToString()
        };

        // Serialize to JSON and Base64 encode it
        var jsonMessage = JsonSerializer.Serialize(message);
        var base64Message = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonMessage));

        // Enqueue the message
        await _queueClient.SendMessageAsync(base64Message);

        return Ok(new { jobId, status = "queued" });
    }
}
