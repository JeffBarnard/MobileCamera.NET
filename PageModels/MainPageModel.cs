using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MobileCamera.NET.Models;
using Plugin.BluetoothLE;
using Plugin.NFC;
using System.Collections.ObjectModel;

namespace MobileCamera.NET.PageModels
{
    public partial class MainPageModel : ObservableObject, IProjectTaskPageModel
    {
        private bool _isNavigatedTo;
        private bool _dataLoaded;
        private readonly ProjectRepository _projectRepository;
        private readonly TaskRepository _taskRepository;
        private readonly CategoryRepository _categoryRepository;
        private readonly ModalErrorHandler _errorHandler;
        private readonly SeedDataService _seedDataService;


        public ObservableCollection<string> BleScanResults { get; set; } = new ObservableCollection<string>();

        [ObservableProperty]
        private List<CategoryChartData> _todoCategoryData = [];

        [ObservableProperty]
        private List<Brush> _todoCategoryColors = [];

        [ObservableProperty]
        private List<ProjectTask> _tasks = [];

        [ObservableProperty]
        private List<Project> _projects = [];

        [ObservableProperty]
        bool _isBusy;

        [ObservableProperty]
        bool _isRefreshing;

        [ObservableProperty]
        private string _today = DateTime.Now.ToString("dddd, MMM d");

        public bool HasCompletedTasks
            => Tasks?.Any(t => t.IsCompleted) ?? false;

        public MainPageModel(SeedDataService seedDataService, ProjectRepository projectRepository,
            TaskRepository taskRepository, CategoryRepository categoryRepository, ModalErrorHandler errorHandler)
        {
            _projectRepository = projectRepository;
            _taskRepository = taskRepository;
            _categoryRepository = categoryRepository;
            _errorHandler = errorHandler;
            _seedDataService = seedDataService;
        }

        private async Task LoadData()
        {
            try
            {
                IsBusy = true;

                Projects = await _projectRepository.ListAsync();

                var chartData = new List<CategoryChartData>();
                var chartColors = new List<Brush>();

                var categories = await _categoryRepository.ListAsync();
                foreach (var category in categories)
                {
                    chartColors.Add(category.ColorBrush);

                    var ps = Projects.Where(p => p.CategoryID == category.ID).ToList();
                    int tasksCount = ps.SelectMany(p => p.Tasks).Count();

                    chartData.Add(new(category.Title, tasksCount));
                }

                TodoCategoryData = chartData;
                TodoCategoryColors = chartColors;

                Tasks = await _taskRepository.ListAsync();
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(HasCompletedTasks));
            }
        }

        private async Task InitData(SeedDataService seedDataService)
        {
            bool isSeeded = Preferences.Default.ContainsKey("is_seeded");

            if (!isSeeded)
            {
                await seedDataService.LoadSeedDataAsync();
            }

            Preferences.Default.Set("is_seeded", true);
            await Refresh();

        }

        [RelayCommand]
        private async Task Refresh()
        {
            try
            {
                IsRefreshing = true;
                await LoadData();
            }
            catch (Exception e)
            {
                _errorHandler.HandleError(e);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private void NavigatedTo() =>
            _isNavigatedTo = true;

        [RelayCommand]
        private void NavigatedFrom() =>
            _isNavigatedTo = false;

        [RelayCommand]
        private async Task Appearing()
        {
            if (!_dataLoaded)
            {
                await InitData(_seedDataService);
                _dataLoaded = true;
                await Refresh();
            }
            // This means we are being navigated to
            else if (!_isNavigatedTo)
            {
                await Refresh();
            }

            // NFC            
            //CrossNFC.Legacy = false;

            // Event raised when a ndef message is received.
            CrossNFC.Current.OnMessageReceived += Current_OnMessageReceived;
            // Event raised when a ndef message has been published.
            CrossNFC.Current.OnMessagePublished += Current_OnMessagePublished;
            // Event raised when a tag is discovered. Used for publishing.
            CrossNFC.Current.OnTagDiscovered += Current_OnTagDiscovered;
            // Event raised when NFC listener status changed
            CrossNFC.Current.OnTagListeningStatusChanged += Current_OnTagListeningStatusChanged;
                        
            // Event raised when NFC state has changed.
            CrossNFC.Current.OnNfcStatusChanged += Current_OnNfcStatusChanged;

            CrossNFC.Current.StartListening();

            // Bluetooth discovery
            CrossBleAdapter.Current.Scan().Subscribe(scanResult =>
            {
                if (!BleScanResults.Contains(scanResult.Device.Name))
                    BleScanResults.Add(scanResult.Device.Name);
            });
        }

        private void Current_OnTagListeningStatusChanged(bool isListening)
        {
            
        }

        private void Current_OnNfcStatusChanged(bool isEnabled)
        {
            
        }

        private void Current_OnTagDiscovered(ITagInfo tagInfo, bool format)
        {
            Application.Current!.MainPage!.DisplayAlert("NFC", tagInfo.Identifier.ToString(), "CANCEL");
        }

        private void Current_OnMessagePublished(ITagInfo tagInfo)
        {
            
        }

        private void Current_OnMessageReceived(ITagInfo tagInfo)
        {
            Application.Current!.MainPage!.DisplayAlert("NFC", tagInfo.SerialNumber, "CANCEL");
        }

        [RelayCommand]
        private Task TaskCompleted(ProjectTask task)
        {
            OnPropertyChanged(nameof(HasCompletedTasks));
            return _taskRepository.SaveItemAsync(task);
        }

        [RelayCommand]
        private Task AddTask()
            => Shell.Current.GoToAsync($"task");

        [RelayCommand]
        private Task NavigateToProject(Project project)
            => Shell.Current.GoToAsync($"project?id={project.ID}");

        [RelayCommand]
        private Task NavigateToTask(ProjectTask task)
            => Shell.Current.GoToAsync($"task?id={task.ID}");

        [RelayCommand]
        private async Task CleanTasks()
        {
            var completedTasks = Tasks.Where(t => t.IsCompleted).ToList();
            foreach (var task in completedTasks)
            {
                await _taskRepository.DeleteItemAsync(task);
                Tasks.Remove(task);
            }

            OnPropertyChanged(nameof(HasCompletedTasks));
            Tasks = new(Tasks);
            await AppShell.DisplayToastAsync("All cleaned up!");
        }
    }
}