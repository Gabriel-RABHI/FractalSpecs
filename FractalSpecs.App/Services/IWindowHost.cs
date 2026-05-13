namespace FractalSpecs.App.Services;

public interface IWindowHost
{
    bool IsDesktop { get; }
    void Minimize();
    void Maximize();
    void Close();
}
