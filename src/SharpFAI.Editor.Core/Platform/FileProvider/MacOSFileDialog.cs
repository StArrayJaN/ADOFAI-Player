#pragma warning disable CS8603 // Possible null reference return

using System.Runtime.InteropServices;
using System.Text;

namespace SharpFAI.Editor.Core.Platform.FileProvider;

/// <summary>
/// macOS 平台的文件对话框实现，使用 Objective-C 运行时调用原生 API
/// </summary>
public class MacOSFileDialog : IFileDialog
{
    private const string ObjCLibrary = "libobjc.dylib";
    private const string AppKitFramework = "/System/Library/Frameworks/AppKit.framework/AppKit";
    private static bool _isInitialized = false;
    private static IntPtr _filterHelperClass = IntPtr.Zero;

    private static readonly Dictionary<IntPtr, FilterChangeData> _filterDataMap =
        new Dictionary<IntPtr, FilterChangeData>();

    private class FilterChangeData
    {
        public IntPtr Panel;
        public List<List<string>> Extensions = new();
    }

    // 过滤器变化回调
    private delegate void FilterChangedDelegate(IntPtr self, IntPtr cmd, IntPtr sender);

    private static void OnFilterChanged(IntPtr self, IntPtr cmd, IntPtr sender)
    {
        try
        {
            if (_filterDataMap.TryGetValue(self, out var data))
            {
                var selectedIndex = (int)objc_msgSend_long(sender, GetSelector("indexOfSelectedItem"));

                if (selectedIndex >= 0 && selectedIndex < data.Extensions.Count)
                {
                    var selectedExtensions = data.Extensions[selectedIndex];
                    if (selectedExtensions.Count > 0 && selectedExtensions[0] != "*")
                    {
                        var nsArray = CreateNSArray(selectedExtensions);
                        objc_msgSend(data.Panel, GetSelector("setAllowedFileTypes:"), nsArray);
                    }
                    else
                    {
                        objc_msgSend(data.Panel, GetSelector("setAllowedFileTypes:"), IntPtr.Zero);
                    }
                }
            }
        }
        catch
        {
            // 忽略错误
        }
    }

    [DllImport(ObjCLibrary)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, NSRect rect);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, NSRect rect, bool pullsDown);

    [DllImport(ObjCLibrary)]
    private static extern void objc_msgSend(IntPtr receiver, IntPtr selector, long arg1);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, bool arg1);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern long objc_msgSend_long(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern long objc_msgSend_long(IntPtr receiver, IntPtr selector, long arg1);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr objc_allocateClassPair(IntPtr superclass, string name, IntPtr extraBytes);

    [DllImport(ObjCLibrary)]
    private static extern bool class_addMethod(IntPtr cls, IntPtr name, IntPtr imp, string types);

    [DllImport(ObjCLibrary)]
    private static extern void objc_registerClassPair(IntPtr cls);

    [DllImport("libdl.dylib")]
    private static extern IntPtr dlopen(string path, int mode);

    private const int RTLD_LAZY = 1;
    private const long NSModalResponseOK = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct NSRect
    {
        public double x;
        public double y;
        public double width;
        public double height;

        public NSRect(double x, double y, double width, double height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
    }

    private static IntPtr GetClass(string className) => objc_getClass(className);
    private static IntPtr GetSelector(string selectorName) => sel_registerName(selectorName);

    private static void EnsureInitialized()
    {
        if (_isInitialized) return;

        // 加载 AppKit 框架
        var appKitHandle = dlopen(AppKitFramework, RTLD_LAZY);

        if (appKitHandle == IntPtr.Zero)
            return;

        // 初始化 NSApplication
        var nsApplicationClass = GetClass("NSApplication");

        if (nsApplicationClass != IntPtr.Zero)
        {
            var sharedApp = objc_msgSend(nsApplicationClass, GetSelector("sharedApplication"));

            if (sharedApp != IntPtr.Zero)
            {
                // 设置激活策略为常规应用
                objc_msgSend(sharedApp, GetSelector("setActivationPolicy:"), (IntPtr)0);

                // 激活应用程序
                objc_msgSend(sharedApp, GetSelector("activateIgnoringOtherApps:"), true);
            }
        }

        // 创建自定义类用于处理过滤器变化
        if (_filterHelperClass == IntPtr.Zero)
        {
            var nsObjectClass = GetClass("NSObject");
            _filterHelperClass = objc_allocateClassPair(nsObjectClass, "FilterHelper_" + Guid.NewGuid().ToString("N"),
                IntPtr.Zero);

            if (_filterHelperClass != IntPtr.Zero)
            {
                FilterChangedDelegate callback = OnFilterChanged;
                var methodImp = Marshal.GetFunctionPointerForDelegate(callback);
                class_addMethod(_filterHelperClass, GetSelector("onFilterChanged:"), methodImp, "v@:@");
                objc_registerClassPair(_filterHelperClass);
            }
        }

        _isInitialized = true;
    }

    private static IntPtr CreateNSString(string str)
    {
        if (string.IsNullOrEmpty(str))
            return IntPtr.Zero;

        var nsStringClass = GetClass("NSString");
        var nsString = objc_msgSend(nsStringClass, GetSelector("alloc"));
        var utf8Bytes = Encoding.UTF8.GetBytes(str);
        var utf8Ptr = Marshal.AllocHGlobal(utf8Bytes.Length + 1);
        Marshal.Copy(utf8Bytes, 0, utf8Ptr, utf8Bytes.Length);
        Marshal.WriteByte(utf8Ptr, utf8Bytes.Length, 0);
        var result = objc_msgSend(nsString, GetSelector("initWithUTF8String:"), utf8Ptr);
        Marshal.FreeHGlobal(utf8Ptr);
        return result;
    }

    private static string NSStringToString(IntPtr nsString)
    {
        if (nsString == IntPtr.Zero)
            return null;

        var utf8Ptr = objc_msgSend(nsString, GetSelector("UTF8String"));
        if (utf8Ptr == IntPtr.Zero)
            return null;

        var length = 0;
        while (Marshal.ReadByte(utf8Ptr, length) != 0)
            length++;

        var bytes = new byte[length];
        Marshal.Copy(utf8Ptr, bytes, 0, length);
        return Encoding.UTF8.GetString(bytes);
    }

    private static IntPtr CreateNSArray(List<string> items)
    {
        if (items == null || items.Count == 0)
            return IntPtr.Zero;

        var arrayClass = GetClass("NSMutableArray");
        var array = objc_msgSend(arrayClass, GetSelector("array"));

        foreach (var item in items)
        {
            var nsItem = CreateNSString(item);
            objc_msgSend(array, GetSelector("addObject:"), nsItem);
        }

        return array;
    }

    // 解析打开文件过滤器字典
    private static void ParseOpenFilter(OpenFileFilter filter, out List<string> filterNames,
        out List<List<string>> extensions)
    {
        filterNames = new List<string>();
        extensions = new List<List<string>>();

        if (filter.Filter == null || filter.Filter.Count == 0)
            return;

        foreach (var kvp in filter.Filter)
        {
            var name = kvp.Key;
            var exts = kvp.Value;

            if (exts == null || exts.Count == 0)
                continue;

            var cleanExts = new List<string>();
            foreach (var ext in exts)
            {
                var cleanExt = ext.TrimStart('*', '.');
                cleanExts.Add(cleanExt);
            }

            var displayName = $"{name} (*.{string.Join(", *.", cleanExts)})";
            filterNames.Add(displayName);
            extensions.Add(cleanExts);
        }

        if (filter.IncludeAllFiles)
        {
            filterNames.Add("All Files (*.*)");
            extensions.Add(new List<string> { "*" });
        }
    }

    // 解析保存文件过滤器字典
    private static void ParseSaveFilter(SaveFileFilter filter, out List<string> filterNames,
        out List<List<string>> extensions)
    {
        filterNames = new List<string>();
        extensions = new List<List<string>>();

        if (filter.Filter == null || filter.Filter.Count == 0)
            return;

        foreach (var kvp in filter.Filter)
        {
            var name = kvp.Key;
            var ext = kvp.Value;

            if (string.IsNullOrEmpty(ext))
                continue;

            var cleanExt = ext.TrimStart('*', '.');
            var displayName = $"{name} (*.{cleanExt})";
            filterNames.Add(displayName);
            extensions.Add(new List<string> { cleanExt });
        }

        if (filter.IncludeAllFiles)
        {
            filterNames.Add("All Files (*.*)");
            extensions.Add(new List<string> { "*" });
        }
    }

    private static IntPtr CreateOpenPanel(string title, string directory, OpenFileFilter filter, bool canChooseFiles,
        bool canChooseFolders)
    {
        ParseOpenFilter(filter, out var filterNames, out var extensions);

        var panel = objc_msgSend(GetClass("NSOpenPanel"), GetSelector("openPanel"));

        if (filterNames.Count > 0 && canChooseFiles)
        {
            // 创建 accessory view
            var accessoryView = objc_msgSend(GetClass("NSView"), GetSelector("alloc"));
            accessoryView = objc_msgSend(accessoryView, GetSelector("initWithFrame:"), new NSRect(0, 0, 200, 24));

            // 创建标签
            var label = objc_msgSend(GetClass("NSTextField"), GetSelector("alloc"));
            label = objc_msgSend(label, GetSelector("initWithFrame:"), new NSRect(0, 0, 60, 22));
            objc_msgSend(label, GetSelector("setEditable:"), false);
            objc_msgSend(label, GetSelector("setBordered:"), false);
            objc_msgSend(label, GetSelector("setBezeled:"), false);
            objc_msgSend(label, GetSelector("setDrawsBackground:"), false);
            objc_msgSend(label, GetSelector("setStringValue:"), CreateNSString("File type:"));

            // 创建下拉按钮
            var popupButton = objc_msgSend(GetClass("NSPopUpButton"), GetSelector("alloc"));
            popupButton = objc_msgSend(popupButton, GetSelector("initWithFrame:pullsDown:"), new NSRect(61, 2, 140, 22),
                false);
            objc_msgSend(popupButton, GetSelector("addItemsWithTitles:"), CreateNSArray(filterNames));

            // 创建 target 对象并设置 action
            if (_filterHelperClass != IntPtr.Zero)
            {
                var target = objc_msgSend(_filterHelperClass, GetSelector("alloc"));
                target = objc_msgSend(target, GetSelector("init"));

                // 存储过滤器数据
                _filterDataMap[target] = new FilterChangeData
                {
                    Panel = panel,
                    Extensions = extensions
                };

                objc_msgSend(popupButton, GetSelector("setTarget:"), target);
                objc_msgSend(popupButton, GetSelector("setAction:"), GetSelector("onFilterChanged:"));
            }

            // 添加到 accessory view
            objc_msgSend(accessoryView, GetSelector("addSubview:"), label);
            objc_msgSend(accessoryView, GetSelector("addSubview:"), popupButton);

            // 设置 accessory view
            objc_msgSend(panel, GetSelector("setAccessoryView:"), accessoryView);

            // 设置初始过滤器
            var firstExtensions = extensions[0];
            if (firstExtensions.Count > 0 && firstExtensions[0] != "*")
            {
                objc_msgSend(panel, GetSelector("setAllowedFileTypes:"), CreateNSArray(firstExtensions));
            }
        }

        if (!string.IsNullOrEmpty(title))
        {
            objc_msgSend(panel, GetSelector("setMessage:"), CreateNSString(title));
        }

        objc_msgSend(panel, GetSelector("setCanChooseFiles:"), canChooseFiles);
        objc_msgSend(panel, GetSelector("setCanChooseDirectories:"), canChooseFolders);
        objc_msgSend(panel, GetSelector("setAllowsMultipleSelection:"), false);

        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            var url = objc_msgSend(GetClass("NSURL"), GetSelector("fileURLWithPath:"), CreateNSString(directory));
            objc_msgSend(panel, GetSelector("setDirectoryURL:"), url);
        }

        objc_msgSend(panel, GetSelector("setCanCreateDirectories:"), true);

        return panel;
    }

    private static IntPtr CreateSavePanel(string title, string directory, string defaultName, SaveFileFilter filter)
    {
        ParseSaveFilter(filter, out var filterNames, out var extensions);

        var panel = objc_msgSend(GetClass("NSSavePanel"), GetSelector("savePanel"));

        if (filterNames.Count > 0)
        {
            // 创建 accessory view
            var accessoryView = objc_msgSend(GetClass("NSView"), GetSelector("alloc"));
            accessoryView = objc_msgSend(accessoryView, GetSelector("initWithFrame:"), new NSRect(0, 0, 220, 24));

            // 创建标签
            var label = objc_msgSend(GetClass("NSTextField"), GetSelector("alloc"));
            label = objc_msgSend(label, GetSelector("initWithFrame:"), new NSRect(0, 0, 80, 22));
            objc_msgSend(label, GetSelector("setEditable:"), false);
            objc_msgSend(label, GetSelector("setBordered:"), false);
            objc_msgSend(label, GetSelector("setBezeled:"), false);
            objc_msgSend(label, GetSelector("setDrawsBackground:"), false);
            objc_msgSend(label, GetSelector("setStringValue:"), CreateNSString("Save as type:"));

            // 创建下拉按钮
            var popupButton = objc_msgSend(GetClass("NSPopUpButton"), GetSelector("alloc"));
            popupButton = objc_msgSend(popupButton, GetSelector("initWithFrame:pullsDown:"), new NSRect(81, 2, 140, 22),
                false);
            objc_msgSend(popupButton, GetSelector("addItemsWithTitles:"), CreateNSArray(filterNames));

            // 创建 target 对象并设置 action
            if (_filterHelperClass != IntPtr.Zero)
            {
                var target = objc_msgSend(_filterHelperClass, GetSelector("alloc"));
                target = objc_msgSend(target, GetSelector("init"));

                // 存储过滤器数据
                _filterDataMap[target] = new FilterChangeData
                {
                    Panel = panel,
                    Extensions = extensions
                };

                objc_msgSend(popupButton, GetSelector("setTarget:"), target);
                objc_msgSend(popupButton, GetSelector("setAction:"), GetSelector("onFilterChanged:"));
            }

            // 添加到 accessory view
            objc_msgSend(accessoryView, GetSelector("addSubview:"), label);
            objc_msgSend(accessoryView, GetSelector("addSubview:"), popupButton);

            // 设置 accessory view
            objc_msgSend(panel, GetSelector("setAccessoryView:"), accessoryView);

            // 设置初始过滤器
            var firstExtensions = extensions[0];
            if (firstExtensions.Count > 0 && firstExtensions[0] != "*")
            {
                objc_msgSend(panel, GetSelector("setAllowedFileTypes:"), CreateNSArray(firstExtensions));
            }
        }

        if (!string.IsNullOrEmpty(title))
        {
            objc_msgSend(panel, GetSelector("setMessage:"), CreateNSString(title));
        }

        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            var url = objc_msgSend(GetClass("NSURL"), GetSelector("fileURLWithPath:"), CreateNSString(directory));
            objc_msgSend(panel, GetSelector("setDirectoryURL:"), url);
        }

        if (!string.IsNullOrEmpty(defaultName))
        {
            objc_msgSend(panel, GetSelector("setNameFieldStringValue:"), CreateNSString(defaultName));
        }

        return panel;
    }

    /// <inheritdoc />
    public string OpenFile(string title, string basePath, OpenFileFilter filter)
    {
        EnsureInitialized();

        var panel = CreateOpenPanel(title, basePath, filter, true, false);
        if (panel == IntPtr.Zero)
            return null;

        var result = objc_msgSend_long(panel, GetSelector("runModal"));

        if (result == NSModalResponseOK)
        {
            var urls = objc_msgSend(panel, GetSelector("URLs"));
            var count = objc_msgSend_long(urls, GetSelector("count"));

            if (count > 0)
            {
                var url = objc_msgSend(urls, GetSelector("objectAtIndex:"), (IntPtr)0);
                var path = objc_msgSend(url, GetSelector("path"));
                return NSStringToString(path);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public string SaveFile(string title, string basePath, string defaultName, SaveFileFilter filter)
    {
        EnsureInitialized();

        // 保存文件时强制禁用 "All Files" 选项
        filter.IncludeAllFiles = false;

        var panel = CreateSavePanel(title, basePath, defaultName, filter);
        if (panel == IntPtr.Zero)
            return null;

        var result = objc_msgSend_long(panel, GetSelector("runModal"));

        if (result == NSModalResponseOK)
        {
            var url = objc_msgSend(panel, GetSelector("URL"));
            if (url != IntPtr.Zero)
            {
                var path = objc_msgSend(url, GetSelector("path"));
                return NSStringToString(path);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public string OpenFolder(string title, string basePath)
    {
        EnsureInitialized();

        var emptyFilter = new OpenFileFilter
            { Filter = new Dictionary<string, List<string>>(), IncludeAllFiles = false };
        var panel = CreateOpenPanel(title, basePath, emptyFilter, false, true);
        if (panel == IntPtr.Zero)
            return null;

        var result = objc_msgSend_long(panel, GetSelector("runModal"));

        if (result == NSModalResponseOK)
        {
            var urls = objc_msgSend(panel, GetSelector("URLs"));
            var count = objc_msgSend_long(urls, GetSelector("count"));

            if (count > 0)
            {
                var url = objc_msgSend(urls, GetSelector("objectAtIndex:"), (IntPtr)0);
                var path = objc_msgSend(url, GetSelector("path"));
                return NSStringToString(path);
            }
        }

        return null;
    }
}
