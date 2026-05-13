using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using FractalSpecs.App.Components;

namespace FractalSpecs.Desktop;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        appBuilder.Services.AddLogging();

        // Register root component and selector
        appBuilder.RootComponents.Add<FractalSpecs.App.Components.Routes>("#app");
        appBuilder.RootComponents.Add<Microsoft.AspNetCore.Components.Web.HeadOutlet>("head::after");

        var app = appBuilder.Build();

        // Customize window
        app.MainWindow
            .SetTitle("FractalSpecs Desktop")
            .SetUseOsDefaultSize(false)
            .SetSize(1024, 768);

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
        };

        app.Run();
    }
}
