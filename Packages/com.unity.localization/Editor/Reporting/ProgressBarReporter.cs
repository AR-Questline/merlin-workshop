using UnityEngine;

namespace UnityEditor.Localization.Reporting
{
    /// <summary>
    /// Reports the progress using [EditorUtility.DisplayProgressBar](https://docs.unity3d.com/ScriptReference/EditorUtility.DisplayProgressBar.html).
    /// </summary>
    public class ProgressBarReporter : TaskTimerReporter
    {
        /// <summary>
        /// Should a summary of the process be printed to the console after the task is completed?
        /// </summary>
        public bool ReportTaskSummaryInConsole { get; set; } = true;

        /// <inheritdoc/>
        protected override void PrintStatus(string title, string description, float progress)
        {
            EditorUtility.DisplayProgressBar(title, description, progress);
        }

        /// <inheritdoc/>
        protected override void PrintSummary(string summary, bool fail)
        {
            EditorUtility.ClearProgressBar();

            if (!ReportTaskSummaryInConsole)
                return;

            if (fail)
                Debug.LogError(summary);
            else
                Debug.Log(summary);
        }
    }
}
