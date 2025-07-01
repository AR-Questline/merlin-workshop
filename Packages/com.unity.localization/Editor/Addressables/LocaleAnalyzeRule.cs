#if ADDRESSABLES_V1

using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace UnityEditor.Localization.Addressables
{
    [InitializeOnLoad]
    class LocaleAnalyzeRule : AnalyzeRuleBase
    {
        public override string ruleName => "Check Localization Locales";

        static LocaleAnalyzeRule() => AnalyzeSystem.RegisterNewRule<LocaleAnalyzeRule>();

        protected internal override void PerformAnalysis(AddressableAssetSettings settings)
        {
            Results.Clear();

            var locales = AssetDatabase.FindAssets("t:Locale");

            // Collate the groups so we can check them at the end.
            var groups = new HashSet<AddressableAssetGroup>();

            foreach (var guid in locales)
            {
                var entry = settings.FindAssetEntry(guid);
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var locale = AssetDatabase.LoadAssetAtPath<Locale>(path);

                if (entry == null)
                {
                    Results.Add(new AnalyzeResultWithFixAction
                    {
                        resultName = $"{locale.LocaleName} - {path}:Not Marked as Addressable",
                        severity = MessageType.Error,
                        FixAction = () => LocalizationEditorSettings.AddLocale(locale)
                    });
                    continue;
                }

                groups.Add(entry.parentGroup);

                var groupName = AddressableGroupRules.Instance.LocaleResolver.GetExpectedGroupName(new[] { locale.Identifier }, locale, settings);

                if (entry.parentGroup.Name != groupName)
                {
                    Results.Add(new AnalyzeResultWithFixAction
                    {
                        resultName = $"{locale.LocaleName} - {path}:Incorrect Group:Expected `{groupName}` but was `{entry.parentGroup.Name}`",
                        severity = MessageType.Warning,
                        FixAction = () => AddressableGroupRules.Instance.LocaleResolver.AddToGroup(locale, new[] { locale.Identifier }, settings, false)
                    });
                }

                if (!entry.labels.Contains(LocalizationSettings.LocaleLabel))
                {
                    Results.Add(new AnalyzeResultWithFixAction
                    {
                        resultName = $"{locale.LocaleName} - {path}:Missing Locale label",
                        severity = MessageType.Error,
                        FixAction = () =>
                        {
                            entry.SetLabel(LocalizationSettings.LocaleLabel, true, true);
                            LocalizationEditorSettings.EditorEvents.RaiseLocaleAdded(locale);
                        }
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
    }
}

#endif
