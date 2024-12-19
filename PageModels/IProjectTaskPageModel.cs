using CommunityToolkit.Mvvm.Input;
using MobileCamera.NET.Models;

namespace MobileCamera.NET.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}