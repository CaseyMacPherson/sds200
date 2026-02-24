namespace SDS200.Cli.Abstractions;

/// <summary>
/// Enumeration of UI view modes in the application.
/// </summary>
public enum ViewMode
{
    /// <summary>Normal scanning display showing current frequency and system info.</summary>
    Main,

    /// <summary>Debug view showing raw radio traffic and keyboard input logs.</summary>
    Debug,

    /// <summary>Manual command entry mode for direct scanner communication.</summary>
    Command
}

