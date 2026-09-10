namespace WMS.Application.DTOs.Import;

public class ImportResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<ImportErrorDto> Errors { get; set; } = new();
}

public class ImportErrorDto
{
    public int Row { get; set; }
    public string Field { get; set; } = null!;
    public string Message { get; set; } = null!;
}
