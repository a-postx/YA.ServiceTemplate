using System.IO;

namespace YA.ServiceTemplate;

internal static class Node
{
    internal static readonly string Id = GetOrSetNodeId();

    private static string GetOrSetNodeId()
    {
        string appDataFolder = "AppData";

        Directory.CreateDirectory(Path.Combine(Program.RootPath, appDataFolder));

        string filePath = Path.Combine(Program.RootPath, appDataFolder, "nodeid");

        if (!File.Exists(filePath))
        {
            string id = Guid.NewGuid().ToString("N");
            File.WriteAllText(filePath, id);
            return id;
        }
        else
        {
            return File.ReadAllText(filePath).Trim();
        }
    }
}
