namespace UnityEditor.Localization.Reporting
{
    /// <summary>
    /// Interface for reporting the status of a process.
    /// </summary>
    public interface ITaskReporter
    {
        /// <summary>
        /// The current progress in the range 0-1.
        /// </summary>
        float CurrentProgress { get; }

        /// <summary>
        /// Has the process started?
        /// </summary>
        bool Started { get; }

        /// <summary>
        /// Start the process.
        /// </summary>
        /// <param name="name">The name of the process.</param>
        /// <param name="description">An optional description of the process.</param>
        void Start(string name, string description);

        /// <summary>
        /// Report the current progress.
        /// </summary>
        /// <param name="description">Description of the current state.</param>
        /// <param name="progress">The progress value in the range 0-1.</param>
        void ReportProgress(string description, float progress);

        /// <summary>
        /// Report the process is completed successfully.
        /// </summary>
        /// <param name="message">Completion message.</param>
        void Completed(string message);

        /// <summary>
        /// Report the process failed.
        /// </summary>
        /// <param name="message">Error message.</param>
        void Fail(string message);
    }
}
