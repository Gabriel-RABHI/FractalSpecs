using FractalSpecs.App.Services;
using Photino.Blazor;
using Photino.NET;

namespace FractalSpecs.Desktop.Services;

public class DesktopWindowHost : IWindowHost
{
    public PhotinoWindow? Window { get; set; }

    public bool IsDesktop => true;

    public void Minimize()
    {
        Window?.SetMinimized(true);
    }

    public void Maximize()
    {
        if (Window != null)
        {
            Window.SetMaximized(!Window.Maximized);
        }
    }

    public void Close()
    {
        Window?.Close();
    }
}
