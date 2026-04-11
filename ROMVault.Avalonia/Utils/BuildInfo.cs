using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace ROMVault.Avalonia.Utils;

public static class BuildInfo
{
    public static string VersionString
    {
        get
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var v = asm.GetName().Version ?? new Version(0, 0, 0, 0);
            string s = $"{v.Major}.{v.Minor}.{v.Build}";
            if (v.Revision > 0)
                s += "." + v.Revision;
            return s;
        }
    }

    public static string BuildTimestampString
    {
        get
        {
            try
            {
                string exe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (!string.IsNullOrWhiteSpace(exe) && File.Exists(exe))
                    return File.GetLastWriteTime(exe).ToString("yyyy-MM-dd HH:mm");
            }
            catch
            {
            }
            return "";
        }
    }

    public static string DisplayString
    {
        get
        {
            string ts = BuildTimestampString;
            if (string.IsNullOrWhiteSpace(ts))
                return "v" + VersionString;
            return "v" + VersionString + " (" + ts + ")";
        }
    }
}

