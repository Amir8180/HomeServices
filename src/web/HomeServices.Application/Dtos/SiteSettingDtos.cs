namespace HomeServices.Application.Dtos;

public class SiteSettingDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Group { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpsertSiteSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Group { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}
