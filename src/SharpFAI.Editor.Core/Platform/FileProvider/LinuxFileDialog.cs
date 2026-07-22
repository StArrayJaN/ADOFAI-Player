#pragma warning disable CS8603 // Possible null reference return
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type

using System.Runtime.InteropServices;

namespace SharpFAI.Editor.Core.Platform.FileProvider;

#if !NET5_0_OR_GREATER
internal static class MarshalExtensions
{
    public static string? PtrToStringUTF8(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
            return null;

        var length = 0;
        while (Marshal.ReadByte(ptr, length) != 0)
            length++;

        if (length == 0)
            return string.Empty;

        var buffer = new byte[length];
        Marshal.Copy(ptr, buffer, 0, length);
        return Encoding.UTF8.GetString(buffer);
    }
}
#endif

/// <summary>
/// Linux 平台的文件对话框实现，使用 GTK3 库
/// </summary>
public class LinuxFileDialog : IFileDialog
{
    private enum GtkFileChooserAction
    {
        Open = 0,
        Save = 1,
        SelectFolder = 2,
        CreateFolder = 3
    }

    private enum GtkResponseType
    {
        None = -1,
        Reject = -2,
        Accept = -3,
        DeleteEvent = -4,
        Ok = -5,
        Cancel = -6,
        Close = -7,
        Yes = -8,
        No = -9,
        Apply = -10,
        Help = -11
    }

    [DllImport("libgtk-3.so.0")]
    private static extern bool gtk_init_check(ref int argc, IntPtr argv);

    [DllImport("libgdk-3.so.0")]
    private static extern IntPtr gdk_display_open(string display_name);

    [DllImport("libgtk-3.so.0")]
    private static extern IntPtr gtk_file_chooser_dialog_new(
        string title,
        IntPtr parent,
        GtkFileChooserAction action,
        IntPtr first_button_text);

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_dialog_add_button(IntPtr dialog, string button_text, GtkResponseType response_id);

    [DllImport("libgtk-3.so.0")]
    private static extern GtkResponseType gtk_dialog_run(IntPtr dialog);

    [DllImport("libgtk-3.so.0")]
    private static extern IntPtr gtk_file_chooser_get_filename(IntPtr chooser);

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_file_chooser_set_current_folder(IntPtr chooser, string filename);

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_file_chooser_set_current_name(IntPtr chooser, string name);
    
    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_file_chooser_set_do_overwrite_confirmation(IntPtr chooser, bool do_overwrite);

    [DllImport("libgtk-3.so.0")]
    private static extern IntPtr gtk_file_filter_new();

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_file_filter_set_name(IntPtr filter, string name);

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_file_filter_add_pattern(IntPtr filter, string pattern);

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_file_chooser_add_filter(IntPtr chooser, IntPtr filter);

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_widget_destroy(IntPtr widget);

    [DllImport("libglib-2.0.so.0")]
    private static extern void g_free(IntPtr mem);

    private static bool _gtkInitialized = false;
    private static readonly object _initLock = new object();

    private void EnsureGtkInitialized()
    {
        lock (_initLock)
        {
            if (!_gtkInitialized)
            {
                // 检查并打开 DISPLAY
                string displayVariable = Environment.GetEnvironmentVariable("DISPLAY");
                if (string.IsNullOrEmpty(displayVariable))
                    throw new Exception("DISPLAY environment variable is not set");
                
                IntPtr display = gdk_display_open(displayVariable);
                if (display == IntPtr.Zero)
                    throw new Exception($"Failed to open display: {displayVariable}");

                int argc = 0;
                _gtkInitialized = gtk_init_check(ref argc, IntPtr.Zero);
                if (!_gtkInitialized)
                    throw new Exception("Failed to initialize GTK");
            }
        }
    }

    /// <inheritdoc />
    public string OpenFile(string title, string basePath, OpenFileFilter filter)
    {
        EnsureGtkInitialized();

        IntPtr dialog = gtk_file_chooser_dialog_new(
            title,
            IntPtr.Zero,
            GtkFileChooserAction.Open,
            IntPtr.Zero);

        gtk_dialog_add_button(dialog, "_Cancel", GtkResponseType.Cancel);
        gtk_dialog_add_button(dialog, "_Open", GtkResponseType.Accept);

        if (!string.IsNullOrEmpty(basePath))
            gtk_file_chooser_set_current_folder(dialog, basePath);

        AddOpenFileFilter(dialog, filter);

        string result = null;
        if (gtk_dialog_run(dialog) == GtkResponseType.Accept)
        {
            IntPtr filename = gtk_file_chooser_get_filename(dialog);
#if NET5_0_OR_GREATER
            result = Marshal.PtrToStringUTF8(filename);
#else
            result = MarshalExtensions.PtrToStringUTF8(filename);
#endif
            g_free(filename);
        }

        gtk_widget_destroy(dialog);
        return result;
    }

    /// <inheritdoc />
    public string SaveFile(string title, string basePath, string defaultName, SaveFileFilter filter)
    {
        // 保存文件时强制禁用 "All Files" 选项
        filter.IncludeAllFiles = false;
        
        EnsureGtkInitialized();

        IntPtr dialog = gtk_file_chooser_dialog_new(
            title,
            IntPtr.Zero,
            GtkFileChooserAction.Save,
            IntPtr.Zero);

        gtk_dialog_add_button(dialog, "_Cancel", GtkResponseType.Cancel);
        gtk_dialog_add_button(dialog, "_Save", GtkResponseType.Accept);

        gtk_file_chooser_set_do_overwrite_confirmation(dialog, true);

        if (!string.IsNullOrEmpty(basePath))
            gtk_file_chooser_set_current_folder(dialog, basePath);

        // 设置默认文件名
        if (!string.IsNullOrEmpty(defaultName))
            gtk_file_chooser_set_current_name(dialog, defaultName);

        AddSaveFileFilter(dialog, filter);

        string result = null;
        if (gtk_dialog_run(dialog) == GtkResponseType.Accept)
        {
            IntPtr filename = gtk_file_chooser_get_filename(dialog);
#if NET5_0_OR_GREATER
            result = Marshal.PtrToStringUTF8(filename);
#else
            result = MarshalExtensions.PtrToStringUTF8(filename);
#endif
            g_free(filename);
            
            // 自动附加后缀
            if (!string.IsNullOrEmpty(result) && filter.Filter != null && filter.Filter.Count > 0)
            {
                var hasExtension = false;
                
                // 检查文件是否已有任何有效扩展名
                foreach (var kvp in filter.Filter)
                {
                    var ext = kvp.Value;
                    if (!string.IsNullOrEmpty(ext))
                    {
                        var extension = ext.StartsWith("*.") ? ext.Substring(2) : ext.TrimStart('.');
                        if (result.EndsWith("." + extension, StringComparison.OrdinalIgnoreCase))
                        {
                            hasExtension = true;
                            break;
                        }
                    }
                }
                
                // 如果没有扩展名，添加第一个过滤器的扩展名
                if (!hasExtension)
                {
                    var firstFilter = filter.Filter.First();
                    var ext = firstFilter.Value;
                    if (!string.IsNullOrEmpty(ext))
                    {
                        var extension = ext.StartsWith("*.") ? ext.Substring(2) : ext.TrimStart('.');
                        result += "." + extension;
                    }
                }
            }
        }

        gtk_widget_destroy(dialog);
        return result;
    }

    /// <inheritdoc />
    public string OpenFolder(string title, string basePath)
    {
        EnsureGtkInitialized();

        IntPtr dialog = gtk_file_chooser_dialog_new(
            title,
            IntPtr.Zero,
            GtkFileChooserAction.SelectFolder,
            IntPtr.Zero);

        gtk_dialog_add_button(dialog, "_Cancel", GtkResponseType.Cancel);
        gtk_dialog_add_button(dialog, "_Select", GtkResponseType.Accept);

        if (!string.IsNullOrEmpty(basePath))
            gtk_file_chooser_set_current_folder(dialog, basePath);

        string result = null;
        if (gtk_dialog_run(dialog) == GtkResponseType.Accept)
        {
            IntPtr filename = gtk_file_chooser_get_filename(dialog);
#if NET5_0_OR_GREATER
            result = Marshal.PtrToStringUTF8(filename);
#else
            result = MarshalExtensions.PtrToStringUTF8(filename);
#endif
            g_free(filename);
        }

        gtk_widget_destroy(dialog);
        return result;
    }

    private void AddOpenFileFilter(IntPtr dialog, OpenFileFilter filter)
    {
        if (filter.Filter != null && filter.Filter.Count > 0)
        {
            // 遍历字典中的每个过滤器
            foreach (var kvp in filter.Filter)
            {
                var name = kvp.Key;
                var patterns = kvp.Value;
                
                if (patterns == null || patterns.Count == 0)
                    continue;
                
                IntPtr gtkFilter = gtk_file_filter_new();
                
                // 构建扩展名列表显示
                var extList = string.Join(", ", patterns.Select(p => 
                    p.StartsWith("*.") ? p : "*." + p));
                var displayName = $"{name} ({extList})";
                
                gtk_file_filter_set_name(gtkFilter, displayName);

                foreach (var pattern in patterns)
                {
                    string gtkPattern = pattern.StartsWith("*.") ? pattern : "*." + pattern;
                    gtk_file_filter_add_pattern(gtkFilter, gtkPattern);
                }

                gtk_file_chooser_add_filter(dialog, gtkFilter);
            }
        }

        if (filter.IncludeAllFiles)
        {
            IntPtr allFilesFilter = gtk_file_filter_new();
            gtk_file_filter_set_name(allFilesFilter, "All Files (*.*)");
            gtk_file_filter_add_pattern(allFilesFilter, "*");
            gtk_file_chooser_add_filter(dialog, allFilesFilter);
        }
    }

    private void AddSaveFileFilter(IntPtr dialog, SaveFileFilter filter)
    {
        if (filter.Filter != null && filter.Filter.Count > 0)
        {
            // 遍历字典中的每个过滤器
            foreach (var kvp in filter.Filter)
            {
                var name = kvp.Key;
                var ext = kvp.Value;
                
                if (string.IsNullOrEmpty(ext))
                    continue;
                
                IntPtr gtkFilter = gtk_file_filter_new();
                
                // 构建扩展名显示
                var extension = ext.StartsWith("*.") ? ext : "*." + ext;
                var displayName = $"{name} ({extension})";
                
                gtk_file_filter_set_name(gtkFilter, displayName);
                gtk_file_filter_add_pattern(gtkFilter, extension);

                gtk_file_chooser_add_filter(dialog, gtkFilter);
            }
        }

        if (filter.IncludeAllFiles)
        {
            IntPtr allFilesFilter = gtk_file_filter_new();
            gtk_file_filter_set_name(allFilesFilter, "All Files (*.*)");
            gtk_file_filter_add_pattern(allFilesFilter, "*");
            gtk_file_chooser_add_filter(dialog, allFilesFilter);
        }
    }
}