namespace SharpFAI_Player;

[AttributeUsage(AttributeTargets.All,AllowMultiple = true)]
public class Note(string a) : Attribute;

public static class Tools
{
    public static string ExportAssets(this string name)
    {
        string targetDir = AppDomain.CurrentDomain.BaseDirectory;
        string targetPath = Path.Combine(targetDir, name);
        if (!File.Exists(targetPath))
        {
            FileStream stream = new(targetPath, FileMode.Create);
            typeof(Tools).Assembly.GetManifestResourceStream("SharpFAI-Player.Resources." + name).CopyTo(stream);
            stream.Close();
        }
        return targetPath;
    }
}