using FractalSpecs.App.Services;

namespace FractalSpecs.Server.Services;

public class WebWindowHost : IWindowHost
{
    public bool IsDesktop => false;

    public void Minimize()
    {
        // No-op on the web
    }

    public void Maximize()
    {
        // No-op on the web
    }

    public void Close()
    {
        // No-op on the web
    }
}
