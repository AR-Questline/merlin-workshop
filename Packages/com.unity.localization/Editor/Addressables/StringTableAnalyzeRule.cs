#if ADDRESSABLES_V1

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.Addressables
{
    [InitializeOnLoad]
    class StringTableAnalyzeRule : TableAnalyzeRule<StringTable>
    {
        public override string ruleName => "Check Localization String Tables";

        static StringTableAnalyzeRule() => AnalyzeSystem.RegisterNewRule<StringTableAnalyzeRule>();
    }

    class TableAnalyzeRule<TTable> : AnalyzeRuleBase where TTable : LocalizationTable
    {
        protected internal override void PerformAnalysis(AddressableAssetSettings settings)
        {
            Analyze(settings, AddressableGroupRules.Instance.StringTablesResolver);
        }

        protected virtual void Analyze(AddressableAssetSettings settings, GroupResolver resolver)
        {
            try
            {
                EditorUtility.DisplayProgressBar(ruleName, "Finding Tables", 0);
                var tables = AssetDatabase.FindAssets($"t:{typeof(TTable).Name}");

                // Collate the groups so we can check them at the end.
                var groups = new HashSet<AddressableAssetGroup>();

                for (var i = 0; i < tables.Length; ++i)
                {
                    var progress = i / (float)tables.Length;

                    var guid = tables[i];
                    var entry = settings.FindAssetEntry(guid);
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var table = AssetDatabase.LoadAssetAtPath<TTable>(path);
                    var label = $"{table} - {path}";

                    EditorUtility.DisplayProgressBar(ruleName, $"Checking Table {path}", progress);

                    var collection = LocalizationEditorSettings.GetCollectionForSharedTableData(table.SharedData);
                    if (collection == null)
                    {
                        Results.Add(new AnalyzeResultWithFixAction
                        {
                            resultName = $"{table} - {path}:Loose Table.",
                            severity = MessageType.Info,
                            // TODO: Create collection for it?
                        });
                        continue;
                    }

                    CheckContents(table, label, settings, collection);

                    if (entry == null)
                    {
                        Results.Add(new AnalyzeResultWithFixAction
                        {
                            resultName = $"{label}:Not Marked as Addressable",
                            severity = MessageType.Error,
                            FixAction = () =>
                            {
                                collection.AddTable(table);
                                collection.AddSharedTableDataToAddressables();
                            }
                        });
                        continue;
                    }

                    groups.Add(entry.parentGroup);

                    // Group Name
                    var groupName = resolver.GetExpectedGroupName(new[] { table.LocaleIdentifier }, table, settings);
                    if (entry.parentGroup.Name != groupName)
                    {
                        Results.Add(new AnalyzeResultWithFixAction
                        {
                            resultName = $"{label}:Incorrect Group:Expected `{groupName}` but was `{entry.parentGroup.Name}`",
                            severity = MessageType.Warning,
                            FixAction = () => resolver.AddToGroup(table, new[] { table.LocaleIdentifier }, settings, false)
                        });
                    }

                    // Label
                    var expectedLabel = AddressHelper.FormatAssetLabel(table.LocaleIdentifier);
                    if (!entry.labels.Contains(expectedLabel))
                    {
                        Results.Add(new AnalyzeResultWithFixAction
                        {
                            resultName = $"{label}:Missing Locale label.",
                            severity = MessageType.Warning,
                            FixAction = () => entry.SetLabel(expectedLabel, true, true)
                        });
                    }

                    // Address
                    var expectedAddress = AddressHelper.GetTableAddress(table.TableCollectionName, table.LocaleIdentifier);
                    if (!entry.labels.Contains(expectedLabel))
                    {
                        Results.Add(new AnalyzeResultWithFixAction
                        {
                            resultName = $"{label}:Incorrect Address:Expected `{expectedAddress}` but was `{entry.address}`",
                            severity = MessageType.Error,
                            FixAction = () => entry.address = expectedAddress
                        });
                    }

                    // Shared Table Data
                    var sharedGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(table.SharedData));
                    var g = new Guid(sharedGuid);
                    if (table.SharedData.TableCollectionNameGuid != g)
                    {
                        Results.Add(new AnalyzeResultWithFixAction
                        {
                            resultName = $"{label}:Incorrect Shared Table Guid:Expected {g} but was {table.SharedData.TableCollectionNameGuid}",
                            severity = MessageType.Error,
                            FixAction = () =>
                            {
                                table.SharedData.TableCollectionNameGuid = g;
                                EditorUtility.SetDirty(table.SharedData);
                            }
                        });
                    }

                    var sharedEntry = settings.FindAssetEntry(sharedGuid);
                    if (sharedEntry == null)
                    {
                        Results.Add(new AnalyzeResultWithFixAction
                        {
                            resultName = $"{label}:Shared Table Not Marked as Addressable",
                            severity = MessageType.Warning,
                            FixAction = () => resolver.AddToGroup(table.SharedData, null, settings, false)
                        });
                        continue;
                    }

                    groups.Add(sharedEntry.parentGroup);

                    // Shared Group Name
                    var sharedGroupName = resolver.GetExpectedGroupName(null, table.SharedData, settings);
                    if (sharedEntry.parentGroup.Name != sharedGroupName)
                    {
                        Results.Add(new AnalyzeResultWithFixAction
                        {
                            resultName = $"{label}:Incorrect Shared Table Data Group:Expected `{sharedGroupName}` but was `{sharedEntry.parentGroup.Name}`",
                            severity = MessageType.Warning,
                            FixAction = () => resolver.AddToGroup(table.SharedData, null, settings, false)
                        });
                    }

                    var expectedSharedGroupName = resolver.GetExpectedGroupName(null, table.SharedData, settings);
                    if (sharedEntry.parentGroup.Name != expectedSharedGroupName)
                    {
                        Results.Add(new AnalyzeResultWithFixAction
                        {
                            resultName = $"{label}:Incorrect Group:Expected `{expectedSharedGroupName}` but was `{sharedEntry.parentGroup.Name}`",
                            severity = MessageType.Warning,
                            FixAction = () => resolver.AddToGroup(table.SharedData, null, settings, false)
                        });
                    }
                }

                if (groups.Count > 0)
                {
                    foreach (var g in groups)
                    {
                        if (g.Schemas.Count == 0 || g.Schemas.All(s => s == null))
                        {
                            Results.Add(new AnalyzeResultWithFixAction
                            {
                                resultName = $"{g.Name}:Addressables Group Contains No Schemas",
                                severity = MessageType.Error,
                                FixAction = () =>
                                {
                                    g.AddSchema<BundledAssetGroupSchema>();
                                    g.AddSchema<ContentUpdateGroupSchema>();
                                }
                            });
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        protected virtual void CheckContents(TTable table, string label, AddressableAssetSettings settings, LocalizationTableCollection collection) {}
    }
}

#endif
