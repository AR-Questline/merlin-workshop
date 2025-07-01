using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Editor.Utility.Assets;
using Awaken.TG.Main.Templates;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets.Templates {
    public static class AddressableTemplatesCreator {

        public static void Convert(TemplatesTreeView treeView) {
            List<TemplateViewItem> toConvert = GetTemplates(treeView);

            foreach (TemplateViewItem templateViewItem in toConvert) {
                ConvertAsset(templateViewItem);
            }
            AssetDatabase.SaveAssets();
        }

        static List<TemplateViewItem> GetTemplates(TemplatesTreeView treeView) {
            var toConvert = new List<TemplateViewItem>();
            
            treeView.Root.GetEnabledItemsRecursively(toConvert);
            return toConvert;
        }

        static void ConvertAsset(TemplateViewItem item) {
            MoveFile(item);
            
            Object obj = AssetsUtils.LoadAssetByGuid<Object>(item.Guid);
            ConvertAsset(obj, item.Guid);
        }

        public static void CreateOrUpdateAsset(Object obj, bool batch = false, string inGroup = null) {
            string guid = AssetsUtils.ObjectToGuid(obj);
            ConvertAsset(obj, guid, batch, inGroup);
        }
        
        public static void ConvertAsset(Object obj, string guid, bool batch = false, string inGroup = null) {
            inGroup ??= TemplatesToAddressablesMapping.AddressableGroup(obj);
            AddressableHelper.AddEntry(
                new AddressableEntryDraft.Builder(obj)
                    .WithGuid(guid)
                    .WithAddressProvider((o, e) => AddressProvider(o, guid, inGroup))
                    .WithLabel(GetLabel(obj))
                    .InGroup(inGroup)
                    .Build(),
                !batch
            );
        }

        static string GetLabel(Object obj) {
            if (obj is ScriptableObject) {
                return TemplatesUtil.AddressableLabelSO;
            }
            if (obj is GameObject) {
                return TemplatesUtil.AddressableLabel;
            }
            throw new ArgumentException($"Trying to make Template addressable from invalid object {obj}");
        }

        static string AddressProvider(Object obj, string guid, string inGroup) {
            return $"{inGroup}/{obj.name}{TemplatesUtil.GUIDSeparator}{guid}";
        }

        static void MoveFile(TemplateViewItem item) {
            string currentPath = AssetDatabase.GUIDToAssetPath(item.Guid);
            string target = item.GetPath();
            if (currentPath == target) {
                return;
            }
            
            CheckDirectories(target);
            string result = AssetDatabase.MoveAsset(currentPath, target);
            if (!result.IsNullOrWhitespace()) {
                throw new Exception($"Error during moving asset {item.displayName}: {result}");
            }
            RemoveEmptyDirectories(currentPath);
        }

        static void CheckDirectories(string path) {
            string[] directories = path.Split('/');
            string current = "Assets";
            for (int i = 1; i < directories.Length - 1; i++) {
                if (!AssetDatabase.IsValidFolder(Path.Combine(current, directories[i]))) {
                    AssetDatabase.CreateFolder(current, directories[i]);
                }

                current = Path.Combine(current, directories[i]);
            }
        }

        static void RemoveEmptyDirectories(string path) {
            string[] directories = path.Split('/');

            for (int i = directories.Length - 2; i >= 1; i--) {
                string current = GetDirectoryPath(directories, i);
                if (Directory.Exists(current) && !Directory.GetFiles(current).Any()) {
                    Directory.Delete(current);
                    File.Delete($"{current}.meta");
                } else {
                    return;
                }
            }
        }

        static string GetDirectoryPath(string[] directories, int index) {
            string path = Application.dataPath;

            for (int i = 1; i <= index; i++) {
                path = Path.Combine(path, directories[i]);
            }

            return path;
        }
    }
}