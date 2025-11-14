using ElectronicComponentStore.Models;
using System.Windows;
using System.IO; // Добавлено

namespace ElectronicComponentStore;

public partial class ComponentEditDialog : Window
{
    public Component Component { get; private set; }

    public ComponentEditDialog(Component component)
    {
        InitializeComponent();
        Component = component;
        DataContext = Component;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Component.Name))
        {
            MessageBox.Show("Название компонента не может быть пустым.", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        DialogResult = true;
        Close();
    }
}