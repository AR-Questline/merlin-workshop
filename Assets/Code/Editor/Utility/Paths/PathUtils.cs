using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Paths {
    public static class PathUtils {
        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path or <c>toPath</c> if the paths are not related.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static string FilesystemToAssetPath(string toPath)
        {
            if (string.IsNullOrEmpty(toPath)) throw new ArgumentNullException(nameof(toPath));

            Uri fromUri = new Uri(Directory.GetCurrentDirectory());
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            var assetIndex = relativePath.IndexOf("Asset");
            relativePath = relativePath.Substring(assetIndex);

            return relativePath;
        }

        public static string AssetToFileSystemPath(string assetPath) {
            string dataPath = Application.dataPath;
            string relativePath = assetPath.Substring(7, assetPath.Length - 7);
            return Path.Combine(dataPath, relativePath);
        }

        public static string ParentDirectory(string directoryPath) {
            return (new DirectoryInfo(directoryPath)).Parent?.FullName;
        }

        /// <summary>
        /// Retrieves selected folder on Project view.
        /// </summary>
        /// <returns></returns>
        public static string GetSelectedPathOrFallback() {
            string path = "";

            foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets)) {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && (File.Exists(path) || Directory.Exists(path))) {
                    break;
                }
            }

            return path;
        }
        
        /// <summary>
        /// Gather files from path. Works with asset path and directory path.
        /// Default works in recursive.
        /// </summary>
        public static IEnumerable<string> GetFiles(string path, bool recursive = true)
        {
            // Check if 
            if (File.Exists(path)) {
                yield return path;
                yield break;
            }

            if (!recursive) {
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                if (files != null) {
                    foreach (string file in files) {
                        yield return file;
                    }
                }
                yield break;
            }
            
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                try
                {
                    foreach (string subDir in Directory.GetDirectories(path))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                if (files != null) {
                    foreach (string file in files) {
                        yield return file;
                    }
                }
            }
        }

        static readonly string InvalidPathChars = new (Path.GetInvalidFileNameChars().Union(Path.GetInvalidPathChars()).ToArray());
        static readonly Regex InvalidPathRegex = new ($"[{Regex.Escape(InvalidPathChars)}]", RegexOptions.Compiled);
        public static string ValidFileName(string filename) {
            filename = InvalidPathRegex.Replace(filename, "");
            filename = filename.Replace(" ", "");
            return filename;
        }
    }
}