using ElectronicComponentStore.Models;
using ElectronicComponentStore.Repositories;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.IO; // Добавлено

namespace ElectronicComponentStore.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly IComponentRepository _componentRepository;
    private ObservableCollection<ComponentType> _componentTypes = new();
    private ObservableCollection<Component> _components = new();
    private Component? _selectedComponent;
    private string _currentView = "Все компоненты";

    public ICommand LoadComponentsCommand { get; }
    public ICommand LoadComponentsByTypeCommand { get; }
    public ICommand ShowComponentDetailsCommand { get; }
    public ICommand AddComponentCommand { get; }
    public ICommand EditComponentCommand { get; }
    public ICommand DeleteComponentCommand { get; }
    public ICommand OpenDatasheetCommand { get; }

    public MainViewModel()
    {
        string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=ElectronicComponentsDB;Integrated Security=True";
        _componentRepository = new ComponentRepository(connectionString);

        LoadComponentsCommand = new RelayCommand(async _ => await LoadAllComponentsAsync());
        LoadComponentsByTypeCommand = new RelayCommand(async type => await LoadComponentsByTypeAsync(type?.ToString() ?? ""));
        ShowComponentDetailsCommand = new RelayCommand(async component => await ShowComponentDetailsAsync(component as Component));
        AddComponentCommand = new RelayCommand(_ => AddComponent());
        EditComponentCommand = new RelayCommand(_ => EditComponent(), _ => CanEditOrDelete());
        DeleteComponentCommand = new RelayCommand(async _ => await DeleteComponentAsync(), _ => CanEditOrDelete());
        OpenDatasheetCommand = new RelayCommand(_ => OpenDatasheet(), _ => CanOpenDatasheet());

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadComponentTypesAsync();
        await LoadAllComponentsAsync();
    }

    public ObservableCollection<ComponentType> ComponentTypes
    {
        get => _componentTypes;
        set => SetProperty(ref _componentTypes, value);
    }

    public ObservableCollection<Component> Components
    {
        get => _components;
        set => SetProperty(ref _components, value);
    }

    public Component? SelectedComponent
    {
        get => _selectedComponent;
        set => SetProperty(ref _selectedComponent, value);
    }

    public string CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    private async Task LoadComponentTypesAsync()
    {
        var types = await _componentRepository.GetComponentTypesAsync();
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ComponentTypes.Clear();
            foreach (var type in types)
            {
                ComponentTypes.Add(type);
            }
        });
    }

    private async Task LoadAllComponentsAsync()
    {
        var components = await _componentRepository.GetAllComponentsAsync();
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Components.Clear();
            foreach (var component in components)
            {
                Components.Add(component);
            }
            CurrentView = "Все компоненты";
        });
    }

    private async Task LoadComponentsByTypeAsync(string type)
    {
        if (!string.IsNullOrEmpty(type))
        {
            var components = await _componentRepository.GetComponentsByTypeAsync(type);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Components.Clear();
                foreach (var component in components)
                {
                    Components.Add(component);
                }
                CurrentView = $"Компоненты: {type}";
            });
        }
    }

    private async Task ShowComponentDetailsAsync(Component? component)
    {
        if (component != null)
        {
            var detailedComponent = await _componentRepository.GetComponentByIdAsync(component.Id);
            if (detailedComponent != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Components.Clear();
                    Components.Add(detailedComponent);
                    CurrentView = $"Компонент: {detailedComponent.Name}";
                });
            }
        }
    }

    private void AddComponent()
    {
        var newComponent = new Component
        {
            DateOfChanges = DateTime.Now
        };

        var dialog = new ComponentEditDialog(newComponent);
        if (dialog.ShowDialog() == true)
        {
            _ = AddComponentAsync(newComponent);
        }
    }

    private async Task AddComponentAsync(Component component)
    {
        await _componentRepository.AddComponentAsync(component);
        await LoadAllComponentsAsync();
        await LoadComponentTypesAsync();
    }

    private void EditComponent()
    {
        if (SelectedComponent != null)
        {
            var componentToEdit = new Component
            {
                Id = SelectedComponent.Id,
                Name = SelectedComponent.Name,
                Type = SelectedComponent.Type,
                CellNumber = SelectedComponent.CellNumber,
                Quantity = SelectedComponent.Quantity,
                DateOfChanges = DateTime.Now,
                Datasheet = SelectedComponent.Datasheet
            };

            var dialog = new ComponentEditDialog(componentToEdit);
            if (dialog.ShowDialog() == true)
            {
                _ = UpdateComponentAsync(componentToEdit);
            }
        }
    }

    private async Task UpdateComponentAsync(Component component)
    {
        await _componentRepository.UpdateComponentAsync(component);
        await LoadAllComponentsAsync();
        await LoadComponentTypesAsync();
    }

    private async Task DeleteComponentAsync()
    {
        if (SelectedComponent != null)
        {
            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить компонент '{SelectedComponent.Name}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await _componentRepository.DeleteComponentAsync(SelectedComponent.Id);
                await LoadAllComponentsAsync();
                await LoadComponentTypesAsync();
            }
        }
    }

    private void OpenDatasheet()
    {
        if (SelectedComponent != null && !string.IsNullOrEmpty(SelectedComponent.Datasheet))
        {
            try
            {
                // Получаем путь к папке с даташитами
                string datasheetsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Datasheets");
                string datasheetPath = Path.Combine(datasheetsFolder, SelectedComponent.Datasheet);

                if (File.Exists(datasheetPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = datasheetPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show($"Файл даташита не найден: {datasheetPath}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private bool CanEditOrDelete() => SelectedComponent != null;
    private bool CanOpenDatasheet() => SelectedComponent != null && !string.IsNullOrEmpty(SelectedComponent.Datasheet);
}