using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;

namespace ElectronicComponentStore.Configuration;

public class ConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly string _appSettingsPath;

    public ConfigurationService()
    {
        _appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    public AppSettings GetAppSettings()
    {
        return _configuration.GetSection("AppSettings").Get<AppSettings>()
               ?? new AppSettings();
    }

    public DatabaseSettings GetDatabaseSettings()
    {
        return new DatabaseSettings
        {
            DefaultConnection = _configuration.GetConnectionString("DefaultConnection")
                              ?? "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=ElectronicComponentsDB;Integrated Security=True"
        };
    }

    public void UpdateDatasheetsFolder(string newPath)
    {
        try
        {
            // Читаем существующий JSON
            var json = File.ReadAllText(_appSettingsPath);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Создаем новый JSON с обновленным путем
            var newJson = new Dictionary<string, object>();

            // Копируем все существующие секции
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name == "AppSettings")
                {
                    var appSettings = new Dictionary<string, object>();
                    foreach (var appSetting in property.Value.EnumerateObject())
                    {
                        if (appSetting.Name == "DatasheetsFolder")
                        {
                            appSettings[appSetting.Name] = newPath;
                        }
                        else
                        {
                            appSettings[appSetting.Name] = appSetting.Value.GetString() ?? "";
                        }
                    }
                    newJson[property.Name] = appSettings;
                }
                else if (property.Name == "ConnectionStrings")
                {
                    var connectionStrings = new Dictionary<string, object>();
                    foreach (var connectionString in property.Value.EnumerateObject())
                    {
                        connectionStrings[connectionString.Name] = connectionString.Value.GetString() ?? "";
                    }
                    newJson[property.Name] = connectionStrings;
                }
                else
                {
                    newJson[property.Name] = property.Value.GetString() ?? "";
                }
            }

            // Записываем обновленный JSON
            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonString = JsonSerializer.Serialize(newJson, options);
            File.WriteAllText(_appSettingsPath, jsonString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Ошибка при обновлении настроек: {ex.Message}", ex);
        }
    }
}