using ElectronicComponentStore.Configuration;
using ElectronicComponentStore.Models;
using ElectronicComponentStore.Repositories;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.IO;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace ElectronicComponentStore.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly IComponentRepository _componentRepository;
    private readonly ConfigurationService _configurationService;
    private ObservableCollection<ComponentType> _componentTypes = new();
    private ObservableCollection<Component> _components = new();
    private Component? _selectedComponent;
    private string _currentView = "Все компоненты";
    private string _datasheetsFolder;

    public ICommand LoadComponentsCommand { get; }
    public ICommand LoadComponentsByTypeCommand { get; }
    public ICommand ShowComponentDetailsCommand { get; }
    public ICommand AddComponentCommand { get; }
    public ICommand EditComponentCommand { get; }
    public ICommand DeleteComponentCommand { get; }
    public ICommand OpenDatasheetCommand { get; }
    public ICommand SelectDatasheetCommand { get; }
    public ICommand ChangeDatasheetsFolderCommand { get; }

    public MainViewModel()
    {
        _configurationService = new ConfigurationService();
        var appSettings = _configurationService.GetAppSettings();
        var dbSettings = _configurationService.GetDatabaseSettings();

        _datasheetsFolder = appSettings.DatasheetsFolder;
        if (!Directory.Exists(_datasheetsFolder))
        {
            Directory.CreateDirectory(_datasheetsFolder);
        }

        _componentRepository = new ComponentRepository(dbSettings.DefaultConnection);

        LoadComponentsCommand = new RelayCommand(async _ => await LoadAllComponentsAsync());
        LoadComponentsByTypeCommand = new RelayCommand(async type => await LoadComponentsByTypeAsync(type?.ToString() ?? ""));
        ShowComponentDetailsCommand = new RelayCommand(async component => await ShowComponentDetailsAsync(component as Component));
        AddComponentCommand = new RelayCommand(_ => AddComponent());
        EditComponentCommand = new RelayCommand(_ => EditComponent(), _ => CanEditOrDelete());
        DeleteComponentCommand = new RelayCommand(async _ => await DeleteComponentAsync(), _ => CanEditOrDelete());
        OpenDatasheetCommand = new RelayCommand(_ => OpenDatasheet(), _ => CanOpenDatasheet());
        SelectDatasheetCommand = new RelayCommand(SelectDatasheetWithParameter);
        ChangeDatasheetsFolderCommand = new RelayCommand(_ => ChangeDatasheetsFolder());

        _ = InitializeAsync();
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

    public string DatasheetsFolder
    {
        get => _datasheetsFolder;
        set => SetProperty(ref _datasheetsFolder, value);
    }

    private async Task InitializeAsync()
    {
        await LoadComponentTypesAsync();
        await LoadAllComponentsAsync();
    }

    private void ChangeDatasheetsFolder()
    {
        using var folderDialog = new WinForms.FolderBrowserDialog();
        folderDialog.Description = "Выберите папку для хранения даташитов";
        folderDialog.SelectedPath = _datasheetsFolder;
        folderDialog.ShowNewFolderButton = true;

        if (folderDialog.ShowDialog() == WinForms.DialogResult.OK)
        {
            try
            {
                string newFolder = folderDialog.SelectedPath;
                _configurationService.UpdateDatasheetsFolder(newFolder);
                DatasheetsFolder = newFolder;
                _datasheetsFolder = newFolder;

                if (!Directory.Exists(newFolder))
                {
                    Directory.CreateDirectory(newFolder);
                }

                MessageBox.Show(
                    $"Папка для даташитов изменена на: {newFolder}",
                    "Настройки обновлены",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при изменении папки: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void SelectDatasheetWithParameter(object? parameter)
    {
        Component? targetComponent = parameter as Component ?? SelectedComponent;
        SelectDatasheetForComponent(targetComponent);
    }

    // ИСПРАВЛЕННЫЙ МЕТОД: Теперь сохраняет Datasheet в базу данных
    private async void SelectDatasheetForComponent(Component? component)
    {
        if (component == null)
        {
            MessageBox.Show("Сначала выберите компонент", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var openFileDialog = new OpenFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
            Title = "Выберите файл даташита",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                string fileName = Path.GetFileName(openFileDialog.FileName);
                string destinationPath = Path.Combine(_datasheetsFolder, fileName);

                // Копируем файл в папку Datasheets
                File.Copy(openFileDialog.FileName, destinationPath, true);

                // ОБНОВЛЯЕМ КОМПОНЕНТ В БАЗЕ ДАННЫХ
                component.Datasheet = fileName;
                component.DateOfChanges = DateTime.Now;

                // Если компонент уже существует в базе (имеет Id), обновляем его
                if (component.Id > 0)
                {
                    await _componentRepository.UpdateComponentAsync(component);
                    await LoadAllComponentsAsync(); // Перезагружаем данные
                }

                // Уведомляем об изменении
                OnPropertyChanged(nameof(Components));
                OnPropertyChanged(nameof(SelectedComponent));

                MessageBox.Show($"Файл даташита '{fileName}' успешно добавлен", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при копировании файла: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void SelectDatasheet()
    {
        SelectDatasheetForComponent(SelectedComponent);
    }

    private async Task LoadComponentTypesAsync()
    {
        try
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
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке типов компонентов: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadAllComponentsAsync()
    {
        try
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
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке компонентов: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadComponentsByTypeAsync(string type)
    {
        if (!string.IsNullOrEmpty(type))
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке компонентов типа {type}: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task ShowComponentDetailsAsync(Component? component)
    {
        if (component != null)
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке деталей компонента: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void AddComponent()
    {
        var newComponent = new Component
        {
            DateOfChanges = DateTime.Now
        };

        var dialog = new ComponentEditDialog(newComponent, this);
        if (dialog.ShowDialog() == true)
        {
            _ = AddComponentAsync(newComponent);
        }
    }

    private async Task AddComponentAsync(Component component)
    {
        try
        {
            await _componentRepository.AddComponentAsync(component);
            await LoadAllComponentsAsync();
            await LoadComponentTypesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при добавлении компонента: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
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

            var dialog = new ComponentEditDialog(componentToEdit, this);
            if (dialog.ShowDialog() == true)
            {
                _ = UpdateComponentAsync(componentToEdit);
            }
        }
    }

    private async Task UpdateComponentAsync(Component component)
    {
        try
        {
            await _componentRepository.UpdateComponentAsync(component);
            await LoadAllComponentsAsync();
            await LoadComponentTypesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при обновлении компонента: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
                try
                {
                    await _componentRepository.DeleteComponentAsync(SelectedComponent.Id);
                    await LoadAllComponentsAsync();
                    await LoadComponentTypesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении компонента: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void OpenDatasheet()
    {
        if (SelectedComponent != null && !string.IsNullOrEmpty(SelectedComponent.Datasheet))
        {
            try
            {
                string datasheetPath = Path.Combine(_datasheetsFolder, SelectedComponent.Datasheet);

                if (File.Exists(datasheetPath))
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = datasheetPath,
                        UseShellExecute = true
                    };
                    Process.Start(processStartInfo);
                }
                else
                {
                    MessageBox.Show($"Файл даташита не найден: {datasheetPath}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private bool CanEditOrDelete() => SelectedComponent != null;
    private bool CanOpenDatasheet() => SelectedComponent != null && !string.IsNullOrEmpty(SelectedComponent.Datasheet);
}