using System;
using System.Runtime.InteropServices;
using OpenTK;
using Android.Util;

namespace SharpFAI.Editor.Platform.Android;

public class GLBindingsContext : IBindingsContext
{
    private const string Tag = nameof(GLBindingsContext);

    [DllImport("dl")]
    private static extern IntPtr dlsym(IntPtr handle, string name);

    [DllImport("dl")]
    private static extern IntPtr dlopen(string fileName, int flags);

    [DllImport("dl")]
    private static extern IntPtr dlerror();

    private static IntPtr handle;
    private const int RTLD_NOW = 2;
    private const int RTLD_GLOBAL = 0x00100;

    static GLBindingsContext()
    {
        // 尝试加载 GLES3 库
        handle = dlopen("libGLESv3.so", RTLD_NOW | RTLD_GLOBAL);

        if (handle == IntPtr.Zero)
        {
            // 如果失败，尝试其他可能的库名
            handle = dlopen("libGLESv2.so", RTLD_NOW | RTLD_GLOBAL);
        }

        if (handle == IntPtr.Zero)
        {
            IntPtr error = dlerror();
            string errorMsg = error != IntPtr.Zero
                ? Marshal.PtrToStringAnsi(error) ?? "Unknown error"
                : "Unknown error";
            Log.Error(Tag, $"Failed to load OpenGL ES library: {errorMsg}");
            throw new Exception($"Failed to load OpenGL ES library: {errorMsg}");
        }

        Log.Info(Tag, "OpenGL ES library loaded successfully");
    }

    public IntPtr GetProcAddress(string procName)
    {
        if (handle == IntPtr.Zero)
        {
            Log.Warn(Tag, $"Cannot get proc address for {procName}: library not loaded");
            return IntPtr.Zero;
        }

        IntPtr addr = dlsym(handle, procName);

        if (addr == IntPtr.Zero)
        {
            Log.Warn(Tag, $"Failed to get proc address for: {procName}");
        }

        return addr;
    }
}