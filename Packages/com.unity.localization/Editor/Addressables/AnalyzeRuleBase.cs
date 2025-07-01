#if ADDRESSABLES_V1

using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Settings;

namespace UnityEditor.Localization.Addressables
{
    abstract class AnalyzeRuleBase : AnalyzeRule
    {
        public const string NoIssuesFoundMessage = "No issues found";

        protected internal class AnalyzeResultWithFixAction : AnalyzeResult
        {
            public Action FixAction { get; set; }
        }

        internal List<AnalyzeResult> Results { get; } = new List<AnalyzeResult>();

        public override bool CanFix => true;

        public override List<AnalyzeResult> RefreshAnalysis(AddressableAssetSettings settings)
        {
            Results.Clear();
            PerformAnalysis(settings);
            if (Results.Count == 0)
                Results.Add(new AnalyzeResult { resultName  = NoIssuesFoundMessage });

            return Results;
        }

        protected internal abstract void PerformAnalysis(AddressableAssetSettings settings);

        public override void ClearAnalysis()
        {
            Results.Clear();
        }

        public override void FixIssues(AddressableAssetSettings settings)
        {
            foreach (var analyzeResult in Results)
            {
                if (analyzeResult is AnalyzeResultWithFixAction fix)
                    fix.FixAction?.Invoke();
            }
        }
    }
}

#endif
