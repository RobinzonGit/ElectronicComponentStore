using ElectronicComponentStore.Models;
using ElectronicComponentStore.ViewModels;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace ElectronicComponentStore;

public partial class ComponentEditDialog : Window
{
    public Component Component { get; private set; }
    public MainViewModel MainViewModel { get; }

    public ComponentEditDialog(Component component, MainViewModel mainViewModel)
    {
        InitializeComponent();
        Component = component;
        MainViewModel = mainViewModel;
        DataContext = this;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Component.Name))
        {
            MessageBox.Show("Название компонента не может быть пустым.", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(Component.Type))
        {
            MessageBox.Show("Тип компонента не может быть пустым.", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Обновляем дату изменений перед сохранением
        Component.DateOfChanges = DateTime.Now;

        DialogResult = true;
        Close();
    }
}