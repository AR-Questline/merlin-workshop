using System.IO;
using UnityEditor;

namespace Awaken.TG.Editor.Assets
{
    public static class AssetPaths
    {        
        public static string GetPathForAsset(string baseName, string path)
        {
            if (string.IsNullOrEmpty(path)) 
            {
                path = GetSelectedPathOrFallback();
            }
            if (Path.HasExtension(path))
            {
                path = Path.GetDirectoryName(path);
            }
            return AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, baseName));
        }

        public static string GetSelectedPathOrFallback()
        {
            string path = "Assets/Data";
		
            foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if ( !string.IsNullOrEmpty(path) && File.Exists(path) ) 
                {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }

            return path;
        }
    }
}
