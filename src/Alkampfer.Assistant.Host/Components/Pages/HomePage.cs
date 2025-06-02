using MudBlazor;

namespace Alkampfer.Assistant.Host.Components.Pages;

public partial class HomePage : BasePage
{
    public int Test { get; set; }
    public string TestValue { get; set; } = "";

    public HomePage()
    {
        TestValue = "START!!";
    }

    internal async Task ShowTestSnackbar()
    {
        Snackbar.Add("This is a test");

        Test++;
        TestValue = $"Test value: {Test}";
    }
}
