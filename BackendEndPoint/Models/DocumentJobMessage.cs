namespace DocProcessingApi.Models;

public class DocumentJobMessage
{
    public string JobId { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public string BlobUri { get; set; } = default!;
}
