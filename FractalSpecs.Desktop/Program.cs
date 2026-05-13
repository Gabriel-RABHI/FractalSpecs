using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using Photino.Blazor.CustomWindow.Extensions;
using FractalSpecs.App.Components;

namespace FractalSpecs.Desktop;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        appBuilder.Services.AddLogging();
        appBuilder.Services.AddCustomWindow();

        // Register root component and selector
        appBuilder.RootComponents.Add<App>("#app");
        appBuilder.RootComponents.Add<Microsoft.AspNetCore.Components.Web.HeadOutlet>("head::after");

        var app = appBuilder.Build();

        // Customize window
        app.MainWindow
            .SetTitle("FractalSpecs Desktop")
            .SetUseOsDefaultSize(false)
            .SetSize(1024, 768)
            .SetChromeless(true);

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
        };

        app.Run();
    }
}
