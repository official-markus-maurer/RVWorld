using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using RomVaultCore.Utils;

namespace RomVaultCore.Scanner;

public interface IChdExtractor
{
    bool ExtractDvd(string chdPath, string outIsoPath, out string error);
    bool ExtractCd(string chdPath, string outDescriptorPath, out string error);
    bool Info(string chdPath, out string infoText);
}

public sealed class ChdmanChdExtractor : IChdExtractor
{
    private readonly string _chdman;
    private readonly string _workingDir;

    public ChdmanChdExtractor(string chdmanPath, string workingDir)
    {
        _chdman = chdmanPath;
        _workingDir = workingDir;
    }

    public bool ExtractDvd(string chdPath, string outIsoPath, out string error)
    {
        string absChd = Path.GetFullPath(NormalizePossiblyConcatenatedPath(chdPath));
        string absOut = Path.GetFullPath(outIsoPath);
        return Run($"{_chdman}", $"extractdvd -i \"{absChd}\" -o \"{absOut}\" -f", out error);
    }

    public bool ExtractCd(string chdPath, string outDescriptorPath, out string error)
    {
        string absChd = Path.GetFullPath(NormalizePossiblyConcatenatedPath(chdPath));
        string absOut = Path.GetFullPath(outDescriptorPath);
        return Run($"{_chdman}", $"extractcd -i \"{absChd}\" -o \"{absOut}\" -f", out error);
    }

    public bool Info(string chdPath, out string infoText)
    {
        string absChd = Path.GetFullPath(NormalizePossiblyConcatenatedPath(chdPath));
        return Run($"{_chdman}", $"info -i \"{absChd}\" ", out infoText);
    }

    private static string NormalizePossiblyConcatenatedPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;
        string p = path.Trim();
        int last = -1;
        for (int i = 0; i + 2 < p.Length; i++)
        {
            char c0 = p[i];
            char c1 = p[i + 1];
            char c2 = p[i + 2];
            if (((c0 >= 'A' && c0 <= 'Z') || (c0 >= 'a' && c0 <= 'z')) && c1 == ':' && (c2 == '\\' || c2 == '/'))
                last = i;
        }
        if (last > 0)
            return p.Substring(last);
        return p;
    }

    private bool Run(string exe, string args, out string output)
    {
        output = "";
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = _workingDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (Process p = new Process { StartInfo = psi, EnableRaisingEvents = true })
            {
                System.Text.StringBuilder stdout = new System.Text.StringBuilder();
                System.Text.StringBuilder stderr = new System.Text.StringBuilder();
                p.OutputDataReceived += (_, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data))
                        return;
                    stdout.AppendLine(e.Data);
                };
                p.ErrorDataReceived += (_, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data))
                        return;
                    stderr.AppendLine(e.Data);
                };
                p.Start();
                ChdmanProcessTracker.Register(p);
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                while (!p.WaitForExit(250))
                {
                }
                p.WaitForExit();
                output = (stdout.ToString() + Environment.NewLine + stderr.ToString()).Trim();
                return p.ExitCode == 0;
            }
        }
        catch (Exception ex)
        {
            output = ex.Message;
            return false;
        }
    }
}
