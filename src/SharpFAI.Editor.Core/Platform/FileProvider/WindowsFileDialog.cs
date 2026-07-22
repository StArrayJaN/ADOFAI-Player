#pragma warning disable CS8603 // Possible null reference return

using System.Runtime.InteropServices;
using System.Text;

namespace SharpFAI.Editor.Core.Platform.FileProvider;

/// <summary>
/// Windows 平台的文件对话框实现
/// </summary>
public class WindowsFileDialog : IFileDialog
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OPENFILENAME
    {
        public int lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public string lpstrFilter;
        public string lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public string lpstrFile;
        public int nMaxFile;
        public string lpstrFileTitle;
        public int nMaxFileTitle;
        public string lpstrInitialDir;
        public string lpstrTitle;
        public int Flags;
        public short nFileOffset;
        public short nFileExtension;
        public string lpstrDefExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public string lpTemplateName;
        public IntPtr pvReserved;
        public int dwReserved;
        public int FlagsEx;
    }

    [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler();
        void GetParent();
        void GetDisplayName(uint sigdnName, out IntPtr ppszName);
        void GetAttributes();
        void Compare();
    }

    [ComImport, Guid("42f85136-db7e-439c-85f1-e4075d135fc8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOpenDialog
    {
        [PreserveSig] int Show(IntPtr parent);
        void SetFileTypes();
        void SetFileTypeIndex();
        void GetFileTypeIndex();
        void Advise();
        void Unadvise();
        void SetOptions(uint fos);
        void GetOptions();
        void SetDefaultFolder();
        void SetFolder(IShellItem psi);
        void GetFolder();
        void GetCurrentSelection();
        void SetFileName();
        void GetFileName();
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel();
        void SetFileNameLabel();
        void GetResult(out IShellItem ppsi);
    }

    [ComImport, Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
    private class FileOpenDialog { }

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetOpenFileNameW(ref OPENFILENAME ofn);

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetSaveFileNameW(ref OPENFILENAME ofn);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string path, IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItem item);

    private const int OFN_EXPLORER = 0x00080000;
    private const int OFN_FILEMUSTEXIST = 0x00001000;
    private const int OFN_PATHMUSTEXIST = 0x00000800;
    private const int OFN_OVERWRITEPROMPT = 0x00000002;
    private const uint FOS_PICKFOLDERS = 0x00000020;
    private const uint FOS_FORCEFILESYSTEM = 0x00000040;
    private const uint SIGDN_FILESYSPATH = 0x80058000;

    /// <inheritdoc />
    public string OpenFile(string title, string basePath, OpenFileFilter filter)
    {
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            var ofn = new OPENFILENAME
            {
                lStructSize = Marshal.SizeOf<OPENFILENAME>(),
                lpstrFile = new string('\0', 2048),
                nMaxFile = 2048,
                lpstrInitialDir = basePath,
                lpstrTitle = title,
                Flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST,
                lpstrFilter = BuildOpenFilter(filter)
            };

            return GetOpenFileNameW(ref ofn) ? ofn.lpstrFile.Split('\0')[0] : null;
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    /// <inheritdoc />
    public string SaveFile(string title, string basePath, string defaultName, SaveFileFilter filter)
    {
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            // 保存文件时强制禁用 "All Files" 选项
            filter.IncludeAllFiles = false;

            var ofn = new OPENFILENAME
            {
                lStructSize = Marshal.SizeOf<OPENFILENAME>(),
                lpstrFile = string.IsNullOrEmpty(defaultName)
                    ? new string('\0', 2048)
                    : defaultName + new string('\0', 2048 - defaultName.Length),
                nMaxFile = 2048,
                lpstrInitialDir = basePath,
                lpstrTitle = title,
                Flags = OFN_EXPLORER | OFN_PATHMUSTEXIST | OFN_OVERWRITEPROMPT,
                lpstrFilter = BuildSaveFilter(filter),
                nFilterIndex = 1
            };

            if (!GetSaveFileNameW(ref ofn))
                return null;

            var result = ofn.lpstrFile.Split('\0')[0];

            // 自动附加后缀
            if (filter.Filter != null && filter.Filter.Count > 0)
            {
                // 获取选中的过滤器索引（从1开始）
                var selectedIndex = ofn.nFilterIndex - 1;
                var filterList = filter.Filter.ToList();

                if (selectedIndex >= 0 && selectedIndex < filterList.Count)
                {
                    var selectedExt = filterList[selectedIndex].Value;
                    var extension = selectedExt.StartsWith("*.") ? selectedExt.Substring(2) : selectedExt.TrimStart('.');

                    // 检查文件是否已有扩展名
                    if (!string.IsNullOrEmpty(extension) && !result.EndsWith("." + extension, StringComparison.OrdinalIgnoreCase))
                    {
                        result += "." + extension;
                    }
                }
            }

            return result;
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    /// <inheritdoc />
    public string OpenFolder(string title, string basePath)
    {
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            var dialog = (IFileOpenDialog)new FileOpenDialog();
            dialog.SetOptions(FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM);

            if (!string.IsNullOrEmpty(title))
                dialog.SetTitle(title);

            if (!string.IsNullOrEmpty(basePath) && Directory.Exists(basePath))
            {
                var guid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");
                if (SHCreateItemFromParsingName(basePath, IntPtr.Zero, guid, out var shellItem) == 0)
                    dialog.SetFolder(shellItem);
            }

            if (dialog.Show(IntPtr.Zero) == 0)
            {
                dialog.GetResult(out var item);
                item.GetDisplayName(SIGDN_FILESYSPATH, out var pathPtr);
                var path = Marshal.PtrToStringUni(pathPtr);
                Marshal.FreeCoTaskMem(pathPtr);
                return path;
            }
        }
        catch { }

        return null;
    }

    private static string BuildOpenFilter(OpenFileFilter filter)
    {
        if (filter.Filter == null || filter.Filter.Count == 0)
            return "All Files\0*.*\0\0";

        var sb = new StringBuilder();
        
        // 遍历字典中的每个过滤器
        foreach (var kvp in filter.Filter)
        {
            var name = kvp.Key;
            var exts = kvp.Value;
            
            if (exts == null || exts.Count == 0)
                continue;
            
            var extensions = string.Join(";", exts.Select(e => e.StartsWith("*.") ? e : "*." + e));
            
            // 在名称后追加扩展名列表
            var displayName = $"{name} ({extensions.Replace(";", ", ")})";
            sb.Append($"{displayName}\0{extensions}\0");
        }
        
        if (filter.IncludeAllFiles)
            sb.Append("All Files (*.*)\0*.*\0");

        sb.Append('\0');
        return sb.ToString();
    }

    private static string BuildSaveFilter(SaveFileFilter filter)
    {
        if (filter.Filter == null || filter.Filter.Count == 0)
            return "All Files\0*.*\0\0";

        var sb = new StringBuilder();
        
        foreach (var kvp in filter.Filter)
        {
            var name = kvp.Key;
            var ext = kvp.Value;
            
            if (string.IsNullOrEmpty(ext))
                continue;
            
            var extension = ext.StartsWith("*.") ? ext : "*." + ext;
            var displayName = $"{name} ({extension})";
            sb.Append($"{displayName}\0{extension}\0");
        }
        
        if (filter.IncludeAllFiles)
            sb.Append("All Files (*.*)\0*.*\0");

        sb.Append('\0');
        return sb.ToString();
    }
}