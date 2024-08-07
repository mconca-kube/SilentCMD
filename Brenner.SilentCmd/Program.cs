using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace Brenner.SilentCmd;

public static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    [SupportedOSPlatform("windows")]
    public async static Task<int> Main(string[] args)
    {
        using var engine = new Engine();
        return await engine.ExecuteAsync(args);
    }
}
