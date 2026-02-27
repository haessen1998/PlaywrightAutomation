using CommunityToolkit.Mvvm.Input;
using PlaywrightAutomation.MAUI.Models;

namespace PlaywrightAutomation.MAUI.PageModels;

public interface IProjectTaskPageModel
{
	IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
	bool IsBusy { get; }
}