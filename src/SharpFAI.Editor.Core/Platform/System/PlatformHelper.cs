namespace SharpFAI.Editor.Core.Platform.System;

public static class PlatformHelper
{
    public static string GetSystemName(this OperatingSystem system)
    {
        switch (system.Platform)
        {
            case PlatformID.Win32NT:
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.WinCE:
                return "Windows";
            case PlatformID.Unix:
                if (OperatingSystem.IsLinux()) return "Linux";
                if (OperatingSystem.IsMacOS()) return "MacOS";
                if (OperatingSystem.IsFreeBSD()) return "BSD";
                if (OperatingSystem.IsAndroid()) return "Android";
                return "Unix";
            case PlatformID.MacOSX:
                return "MacOS";
            default:
                return "Unknown";
        }
    }
}