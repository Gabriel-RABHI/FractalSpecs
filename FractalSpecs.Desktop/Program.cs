using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using FractalSpecs.App.Components;
using FractalSpecs.App.Services;
using FractalSpecs.Desktop.Services;

namespace FractalSpecs.Desktop;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        appBuilder.Services.AddLogging();
        appBuilder.Services.AddSingleton<DesktopWindowHost>();
        appBuilder.Services.AddSingleton<IWindowHost>(sp => sp.GetRequiredService<DesktopWindowHost>());

        // Register root component and selector
        appBuilder.RootComponents.Add<FractalSpecs.App.Components.Routes>("#app");
        appBuilder.RootComponents.Add<Microsoft.AspNetCore.Components.Web.HeadOutlet>("head::after");

        var app = appBuilder.Build();

        // Initialize Window Host
        app.Services.GetRequiredService<DesktopWindowHost>().Window = app.MainWindow;

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
