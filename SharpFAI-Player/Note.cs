namespace SharpFAI_Player;

[AttributeUsage(AttributeTargets.All,AllowMultiple = true)]
public class Note(string a) : Attribute;

public static class Tools
{
    public static string ExportAssets(this string name)
    {
        string targetDir = AppDomain.CurrentDomain.BaseDirectory;
        string targetPath = Path.Combine(targetDir,"Resources", name);
        return targetPath;
    }
}