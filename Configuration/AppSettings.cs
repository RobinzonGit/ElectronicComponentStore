namespace ElectronicComponentStore.Configuration;

public class AppSettings
{
    public string DatasheetsFolder { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
}

public class DatabaseSettings
{
    public string DefaultConnection { get; set; } = string.Empty;
}