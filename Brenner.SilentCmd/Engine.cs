﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using Brenner.SilentCmd.Properties;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace Brenner.SilentCmd;

[SupportedOSPlatform("windows")]
internal class Engine : IDisposable
{
    private readonly Configuration _config = new();
    private readonly LogWriter _logWriter = new();

    /// <summary>
    /// Executes the batch file defined in the arguments
    /// </summary>
    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            _config.ParseArguments(args);
            _logWriter.Initialize(_config.LogFilePath, _config.LogAppend);

            if (_config.ShowHelp)
            {
                ShowHelp();
                return 0;
            }

            await DelayIfNecessaryAsync();
            ResolveBatchFilePath();

            _logWriter.WriteLine(Resources.StartingCommand, _config.BatchFilePath);

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(_config.BatchFilePath, _config.BatchFileArguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,   // CreateNoWindow only works, if shell is not used
                CreateNoWindow = true
            };
            process.OutputDataReceived += OutputHandler;
            process.ErrorDataReceived += OutputHandler;
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Exception e)
        {
            _logWriter.WriteLine(Resources.Error, e.Message);
            return 1;
        }
        finally
        {
            _logWriter.WriteLine(Resources.FinishedCommand, _config.BatchFilePath);
        }
    }

    private async Task DelayIfNecessaryAsync()
    {
        if (_config.Delay <= TimeSpan.FromSeconds(0)) return;

        _logWriter.WriteLine("Delaying execution by {0} seconds", _config.Delay.TotalSeconds);
        await Task.Delay(_config.Delay);
    }

    private static void ShowHelp()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        AssemblyName name = assembly.GetName();
        string userManual = string.Format(Resources.UserManual, name.Version);
        MessageBox.Show(userManual, Resources.ProgramTitle);
    }

    private void ResolveBatchFilePath()
    {
        if (string.IsNullOrEmpty(_config.BatchFilePath)) return;

        if (!string.IsNullOrEmpty(Path.GetDirectoryName(_config.BatchFilePath))) return;

        if (string.IsNullOrEmpty(Path.GetExtension(_config.BatchFilePath)))
        {
            if (FindPath(_config.BatchFilePath + ".bat")) return;
            FindPath(_config.BatchFilePath + ".cmd");
        }
        else
        {
            FindPath(_config.BatchFilePath);
        }
    }

    /// <returns>True if file was found</returns>
    private bool FindPath(string filename)
    {
        string currentPath = Path.Combine(Environment.CurrentDirectory, filename);
        if (File.Exists(currentPath)) return true;

        var enviromentPath = System.Environment.GetEnvironmentVariable("PATH");

        var paths = enviromentPath?.Split(';');
        var fullPath = paths?.Select(x => Path.Combine(x, filename))
                           .Where(x => File.Exists(x))
                           .FirstOrDefault();

        if (!string.IsNullOrEmpty(fullPath))
        {
            _config.BatchFilePath = fullPath;
            return true;
        }

        return false;
    }

    private void OutputHandler(object sender, DataReceivedEventArgs e)
    {
        _logWriter.WriteLine(e.Data ?? "");
    }

    public void Dispose()
    {
        _logWriter?.Dispose();
    }
}
