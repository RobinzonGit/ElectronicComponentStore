using ElectronicComponentStore.Database;
using ElectronicComponentStore.ViewModels;
using System.Windows;
//using System.Windows.Forms; // Для FolderBrowserDialog

namespace ElectronicComponentStore;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        _ = InitializeDatabaseAsync();
        DataContext = new MainViewModel();
    }

    private async Task InitializeDatabaseAsync()
    {
        var dbInitializer = new DatabaseInitializer("Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=True");
        await dbInitializer.InitializeDatabaseAsync();
    }
}