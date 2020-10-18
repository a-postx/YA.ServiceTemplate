using System;
using System.IO;
using YA.ServiceTemplate.Constants;

namespace YA.ServiceTemplate
{
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
                File.WriteAllText(filePath, Id);
                return Guid.NewGuid().ToString("N");
            }
            else
            {
                return File.ReadAllText(filePath).Trim();
            }
        }
    }
}
