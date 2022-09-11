namespace MiniApp.Dtos.RequestLog;

public class CreateRequestLogDto
{
    public string? QueryString { get; set; }

    public string? Body { get; set; }

    public string Method { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;
    
    public string? Response { get; set; }

    public string? Exception { get; set; }
}