using System;
using System.Runtime.Versioning;

namespace Brenner.SilentCmd;

public static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    [SupportedOSPlatform("windows")]
    public static int Main(string[] args)
    {
        using var engine = new Engine();
        return engine.Execute(args);
    }
}
