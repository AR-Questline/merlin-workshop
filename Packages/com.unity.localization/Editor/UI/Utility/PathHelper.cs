using UnityEngine;

namespace UnityEditor.Localization
{
    class PathHelper
    {
        internal static string MakePathRelative(string path)
        {
            var dataPath = Application.dataPath;
            var root = dataPath.Substring(0, dataPath.Length - "Assets".Length);
            if (path.StartsWith(root))
            {
                path = path.Substring(root.Length);
            }

            return path;
        }
    }
}
