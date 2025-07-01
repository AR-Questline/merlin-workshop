namespace Awaken.TG.Main.Stories.Execution {
    /// <summary>
    /// Returned by step executions - allows a step to delay further Chapter execution if it needs
    /// to wait for eg. a user interaction. The Chapter won't go forward until IsDone becomes true
    /// on the StepCompletion returned by Step.Execute().
    /// </summary>
    public class StepResult {
        /// <summary>
        /// A special StepCompletion instance that can be returned by steps that complete immediately.
        /// </summary>
        public static readonly StepResult Immediate = new StepResult {IsDone = true};

        public bool IsDone { get; private set; }

        public void Complete() {
            IsDone = true;
        }
    }
}