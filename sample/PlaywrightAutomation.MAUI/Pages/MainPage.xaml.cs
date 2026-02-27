using PlaywrightAutomation.MAUI.Models;
using PlaywrightAutomation.MAUI.PageModels;

namespace PlaywrightAutomation.MAUI.Pages;

public partial class MainPage : ContentPage
{
	public MainPage(MainPageModel model)
	{
		InitializeComponent();
		BindingContext = model;
	}
}