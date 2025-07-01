using System.Diagnostics;
using System.Text;

namespace UnityEditor.Localization.Reporting
{
    static class TaskReporter
    {
        public static ITaskReporter CreateDefaultReporter()
        {
            #if UNITY_2020_1_OR_NEWER
            return new ProgressReporter();
            #else
            return new ProgressBarReporter();
            #endif
        }
    }

    /// <summary>
    /// Base class responsible for recording the time between each <see cref="ReportProgress"/>.
    /// </summary>
    public abstract class TaskTimerReporter : ITaskReporter
    {
        StringBuilder m_Summary = new StringBuilder();
        Stopwatch m_Timer;
        string m_Title;
        string m_CurrentDescription;
        double m_TotalTimeInSeconds;

        /// <inheritdoc/>
        public float CurrentProgress { get; private set; }

        /// <inheritdoc/>
        public bool Started { get; private set; }

        /// <inheritdoc/>
        public virtual void Start(string title, string description)
        {
            if (Started)
                return;

            Started = true;
            m_Title = title;
            m_CurrentDescription = description;
            m_Summary.Clear();
            m_Timer = Stopwatch.StartNew();
            m_TotalTimeInSeconds = 0;
        }

        /// <inheritdoc/>
        public void ReportProgress(string description, float progress)
        {
            if (!Started)
            {
                Start(description, description);
            }

            CurrentProgress = progress;
            LogStep();
            PrintStatus(m_Title, description, progress);
            m_CurrentDescription = description;
            m_Timer.Restart();
        }

        /// <inheritdoc/>
        public void Completed(string message)
        {
            if (!Started)
                return;

            Started = false;
            CurrentProgress = 1;
            LogStep();
            m_Summary.AppendFormat("Total time: {0:0.##}s\n{1}", m_TotalTimeInSeconds, message);
            m_Summary.Insert(0, $"{m_Title}\n");
            PrintSummary(m_Summary.ToString(), false);
        }

        /// <inheritdoc/>
        public void Fail(string message)
        {
            if (!Started)
                return;

            Started = false;
            LogStep();
            m_Summary.AppendFormat("Total time: {0:0.##}s", m_TotalTimeInSeconds);
            m_Summary.Insert(0, $"{m_Title}: Failed\n{message}\n");
            PrintSummary(m_Summary.ToString(), true);
        }

        void LogStep()
        {
            double secs = m_Timer.Elapsed.TotalSeconds;
            m_TotalTimeInSeconds += secs;
            m_Summary.AppendFormat("{0}: {1:0.##}s\n", m_CurrentDescription, secs);
        }

        /// <summary>
        /// Called after <see cref="ReportProgress"/> to print a summary of the progress.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="progress"></param>
        protected abstract void PrintStatus(string title, string description, float progress);

        /// <summary>
        /// Called after <see cref="Completed"/> or <see cref="Fail"/> to optionally print a summary of the whole task.
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="fail"></param>
        protected abstract void PrintSummary(string summary, bool fail);
    }
}
