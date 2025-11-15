using ElectronicComponentStore.Database;
using System.Windows;
using System.Threading.Tasks;

namespace ElectronicComponentStore;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Инициализация базы данных при запуске приложения
        var dbInitializer = new DatabaseInitializer("Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=True");
        await dbInitializer.InitializeDatabaseAsync();
    }
}