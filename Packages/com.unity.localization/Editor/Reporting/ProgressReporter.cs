#if UNITY_2020_1_OR_NEWER || PACKAGE_DOCS_GENERATION

using UnityEngine;

namespace UnityEditor.Localization.Reporting
{
    public class ProgressReporter : TaskTimerReporter
    {
        /// <summary>
        /// Should the summary be sent to the console using Debug.Log/Debug.LogError?
        /// </summary>
        public bool ReportTaskSummaryInConsole { get; set; } = true;

        /// <summary>
        /// UnityEditor.Progress Id.
        /// </summary>
        public int ProgressId { get; set; }

        /// <summary>
        /// Options for the Progress.
        /// </summary>
        public Progress.Options Options { get; set; }

        /// <inheritdoc/>
        public override void Start(string title, string description)
        {
            base.Start(title, description);
            ProgressId = Progress.Start(title, description, Options);
        }

        /// <inheritdoc/>
        protected override void PrintStatus(string title, string description, float progress)
        {
            Progress.Report(ProgressId, progress, description);
        }

        /// <inheritdoc/>
        protected override void PrintSummary(string summary, bool fail)
        {
            Progress.Finish(ProgressId, fail ? Progress.Status.Failed : Progress.Status.Succeeded);
            ProgressId = 0;
            if (!ReportTaskSummaryInConsole)
                return;

            if (fail)
                Debug.LogError(summary);
            else
                Debug.Log(summary);
        }
    }
}
#endif
