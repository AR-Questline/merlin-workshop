#if ADDRESSABLES_V1

using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace UnityEditor.Localization.Addressables
{
    [InitializeOnLoad]
    class AssetTableAnalyzeRule : TableAnalyzeRule<AssetTable>
    {
        public override string ruleName => "Check Localization Asset Tables";

        static AssetTableAnalyzeRule() => AnalyzeSystem.RegisterNewRule<AssetTableAnalyzeRule>();

        readonly Dictionary<string, HashSet<LocaleIdentifier>> m_AssetDependencies = new Dictionary<string, HashSet<LocaleIdentifier>>();

        protected override void Analyze(AddressableAssetSettings settings, GroupResolver _)
        {
            m_AssetDependencies.Clear();
            base.Analyze(settings, AddressableGroupRules.Instance.AssetTablesResolver);

            // Check assets are using the correct labels
            foreach (var assetDependency in m_AssetDependencies)
            {
                using (ListPool<LocaleIdentifier>.Get(out var locales))
                {
                    var entry = settings.FindAssetEntry(assetDependency.Key);
                    var path = AssetDatabase.GUIDToAssetPath(assetDependency.Key);

                    foreach (var entryLabel in entry.labels)
                    {
                        if (!AddressHelper.TryGetLocaleLabelToId(entryLabel, out var id))
                            continue;

                        if (!assetDependency.Value.Contains(id))
                        {
                            Results.Add(new AnalyzeResultWithFixAction
                            {
                                resultName = $"Assets:{path}:Unused Locale Label `{id}`",
                                severity = MessageType.Warning,
                                FixAction = () => entry.labels.Remove(entryLabel)
                            });
                        }
                        else
                        {
                            locales.Add(id);
                            assetDependency.Value.Remove(id);
                        }
                    }

                    // Missing labels
                    foreach (var localeIdentifier in assetDependency.Value)
                    {
                        var expectedLabel = AddressHelper.FormatAssetLabel(localeIdentifier.Code);
                        Results.Add(new AnalyzeResultWithFixAction
                        {
                            resultName = $"Assets:{path}:Missing Locale Label `{expectedLabel}`",
                            severity = MessageType.Error,
                            FixAction = () => entry.SetLabel(expectedLabel, true, true)
                        });
                    }

                    // Group name
                    var expectedGroupName = AddressableGroupRules.Instance.AssetResolver.GetExpectedGroupName(locales, entry.MainAsset, settings);
                    if (entry.parentGroup.Name != expectedGroupName)
                    {
                        var copy = locales.ToArray(); // We need to copy as we reuse the list
                        Results.Add(new AnalyzeResultWithFixAction
                        {
                            resultName = $"Assets:{path}:Incorrect Group:Expected `{expectedGroupName}` but was `{entry.parentGroup.Name}`",
                            severity = MessageType.Warning,
                            FixAction = () => AddressableGroupRules.Instance.AssetResolver.AddToGroup(entry.MainAsset, copy, settings, false)
                        });
                    }
                }
            }
        }

        protected override void CheckContents(AssetTable table, string label, AddressableAssetSettings settings, LocalizationTableCollection collection)
        {
            var assetTableCollection = (AssetTableCollection)collection;
            foreach (var assetTableEntry in table.Values)
            {
                if (assetTableEntry.IsEmpty)
                    continue;

                var entryLabel = $"{label}:{assetTableEntry.Key}";

                var path  = AssetDatabase.GUIDToAssetPath(assetTableEntry.Guid);
                var entry = settings.FindAssetEntry(assetTableEntry.Guid);
                if (entry == null)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                    if (asset == null)
                    {
                        Results.Add(new AnalyzeResultWithFixAction
                        {
                            resultName = $"{entryLabel}:Asset Is Missing:{assetTableEntry.Address} {path}",
                            severity = MessageType.Info,
                        });
                    }
                    else
                    {
                        Results.Add(new AnalyzeResultWithFixAction
                        {
                            resultName = $"{entryLabel}:Asset Not Marked as Addressable:{path}",
                            severity = MessageType.Error,
                            FixAction = () => assetTableCollection.AddAssetToTable(table, assetTableEntry.KeyId, asset)
                        });
                    }
                    continue;
                }
                else if (assetTableEntry.IsSubAsset)
                {
                    // Check sub-asset exists
                    bool foundSubAsset = false;
                    foreach (var subAsset in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
                    {
                        if (subAsset.name == assetTableEntry.SubAssetName)
                        {
                            foundSubAsset = true;
                            break;
                        }
                    }

                    if (!foundSubAsset)
                    {
                        Results.Add(new AnalyzeResultWithFixAction
                        {
                            resultName = $"{entryLabel}:Sub-Asset {assetTableEntry.SubAssetName} Is Missing:{assetTableEntry.Guid} {path}",
                            severity = MessageType.Error,
                        });
                    }
                }
                else
                {
                    // Upgrade potential sub-assets
                    var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                    var assetType = assetTableCollection.GetEntryAssetType(assetTableEntry.KeyId);

                    if (!assetType.IsAssignableFrom(mainAssetType))
                    {
                        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                        {
                            if (asset.GetType().IsAssignableFrom(assetType))
                            {
                                var subAsset = asset;
                                Results.Add(new AnalyzeResultWithFixAction
                                {
                                    resultName = $"{entryLabel}:Main Asset {mainAssetType} does not match expected type {assetType}. A sub-asset name {assetTableEntry.SubAssetName} Is Missing:{assetTableEntry.Guid} {path}",
                                    severity = MessageType.Warning,
                                    FixAction = () => assetTableCollection.AddAssetToTable(table, assetTableEntry.KeyId, subAsset)
                                });
                            }
                        }
                    }
                }

                // Record the locale but check the label at the end once we have done all tables
                if (!m_AssetDependencies.TryGetValue(assetTableEntry.Guid, out var hashSet))
                {
                    hashSet = new HashSet<LocaleIdentifier>();
                    m_AssetDependencies[assetTableEntry.Guid] = hashSet;
                }

                hashSet.Add(table.LocaleIdentifier);
            }
        }
    }
}

#endif
