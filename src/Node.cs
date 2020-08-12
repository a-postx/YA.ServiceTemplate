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
            string filePath = Path.Combine(Program.RootPath, General.AppDataFolderName, "nodeid");

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
