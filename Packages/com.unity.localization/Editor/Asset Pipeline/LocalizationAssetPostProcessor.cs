using System;
using UnityEngine;
using System.Linq;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEditor.AddressableAssets.Settings;

namespace UnityEditor.Localization
{
    #if !UNITY_2021_2_OR_NEWER
    [InitializeOnLoad]
    #endif
    class LocalizationAssetPostProcessor : AssetPostprocessor
    {
        #if !UNITY_2022_1_OR_NEWER
        static LocalizationAssetPostProcessor()
        {
            AddressablesAssetPostProcessor.OnPostProcess.Register(OnPostprocessAllAssets, 1000);
        }

        #else
        // We need to make sure that we run after Addressables has processed its assets. (LOC-400)
        [UnityEditor.Callbacks.RunAfterPackage("com.unity.addressables")]
        #endif

        #pragma warning disable CA1801 // CA1801 Review unused parameters
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // TODO: Handle deleted assets. We should use the asset guid to determine the asset type and what we need to do if anything.
            foreach (var assetPath in deletedAssets)
            {
                if (assetPath.EndsWith("asset"))
                {
                    LocalizationEditorSettings.Instance.TableCollectionCache.Clear();
                    break;
                }
            }

            // We are invoking the AddLocale method separately before calling the collection.ImportCollectionIntoProject() as it was creating duplicate locales into the project.
            // Because the locales were still not added into the cache for the LocalizationTableCollection to verify it and to only create locales that are missing.
            // adding the locale functionality with the other asset import was causing the issue.
            var locales = importedAssets.Where(assetPath => assetPath.EndsWith("asset") && typeof(Locale).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(assetPath))).ToList();
            locales.ForEach(assetPath =>
            {
                var locale = AssetDatabase.LoadAssetAtPath<Locale>(assetPath);
                Debug.Assert(locale != null, "Failed to load Locale asset.");
                LocalizationEditorSettings.AddLocale(locale, false);
            });

            foreach (var assetPath in importedAssets)
            {
                if (assetPath.EndsWith("asset"))
                {
                    var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                    if (typeof(LocalizationTableCollection).IsAssignableFrom(assetType))
                    {
                        var collection = AssetDatabase.LoadAssetAtPath<LocalizationTableCollection>(assetPath);
                        collection.ImportCollectionIntoProject();
                    }
                    else if (typeof(SharedTableData).IsAssignableFrom(assetType))
                    {
                        var sharedTableData = AssetDatabase.LoadAssetAtPath<SharedTableData>(assetPath);
                        if (sharedTableData != null)
                        {
                            Debug.Assert(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sharedTableData, out string sharedTableDataGuid, out long _), "Failed to extract SharedTableData Guid", sharedTableData);
                            var guid = Guid.Parse(sharedTableDataGuid);

                            if (sharedTableData.TableCollectionNameGuid != Guid.Empty)
                                Debug.Assert(sharedTableData.TableCollectionNameGuid == guid, "SharedTableData Name Guid does not match the assets Guid. This may cause issues matching the correct TableCollectionName.", sharedTableData);
                            else
                                sharedTableData.TableCollectionNameGuid = guid;

                            // If the collection asset was deleted and then restored we may have a collection using an invalid handle (LOC-182)
                            var collection = LocalizationEditorSettings.GetCollectionForSharedTableData(sharedTableData);
                            if (collection != null)
                            {
                                bool modified = false;
                                if (collection.SharedData == null)
                                {
                                    modified = true;
                                    collection.SharedData = sharedTableData;
                                }

                                for (int i = 0; i < collection.Tables.Count; i++)
                                {
                                    var tableReference = collection.Tables[i];
                                    if (tableReference.asset == null)
                                    {
                                        collection.RemoveTable(tableReference.asset);
                                        continue;
                                    }

                                    if (tableReference.asset?.SharedData != null)
                                        continue;

                                    modified = true;
                                    tableReference.asset.SharedData = sharedTableData;
                                }

                                if (modified)
                                    LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
                            }
                        }
                    }
                }
            }
        }

        #pragma warning restore CA1801
    }

    class LocalizationAssetModificationProcessor : AssetModificationProcessor
    {
        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions _)
        {
            if (assetPath.EndsWith("asset"))
            {
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (typeof(LocalizationTableCollection).IsAssignableFrom(assetType))
                {
                    var tableCollection = AssetDatabase.LoadAssetAtPath<LocalizationTableCollection>(assetPath);
                    tableCollection.RemoveCollectionFromProject();
                }
                else if (typeof(SharedTableData).IsAssignableFrom(assetType))
                {
                    var sharedData = AssetDatabase.LoadAssetAtPath<SharedTableData>(assetPath);
                    var collection = LocalizationEditorSettings.GetCollectionForSharedTableData(sharedData);
                    collection?.RemoveCollectionFromProject();
                }
                else if (typeof(LocalizationTable).IsAssignableFrom(assetType))
                {
                    var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(assetPath);
                    var collection = LocalizationEditorSettings.GetCollectionFromTable(table);
                    collection?.RemoveTable(table);
                }
                else if (typeof(Locale).IsAssignableFrom(assetType))
                {
                    var locale = AssetDatabase.LoadAssetAtPath<Locale>(assetPath);
                    LocalizationEditorSettings.RemoveLocale(locale, false);
                }
            }
            return AssetDeleteResult.DidNotDelete;
        }
    }
}
