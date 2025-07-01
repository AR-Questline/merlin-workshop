using System;

namespace Unity.Services.UserReporting.Client
{
    /// <summary>
    /// Represents configuration for the User Reporting client.
    /// </summary>
    public class UserReportingClientConfiguration
    {
        /// <summary>
        /// Creates a new instance of <see cref="UserReportingClientConfiguration" />.
        /// </summary>
        /// <param name="maximumEventCount">The maximum count of events tracked, in a rolling window.</param>
        /// <param name="maximumMeasureCount">The maximum count of measures tracked, in a rolling window.</param>
        /// <param name="framesPerMeasure">The number of frames in a measure. A user report is only created on the
        /// boundary between measures. A large number of frames per measure will increase user report creation time by
        /// this number of frames in worst case.
        /// </param>
        /// <param name="maximumScreenshotCount">The maximum count of screenshots tracked, in a rolling window.</param>
        /// <param name="metricsGatheringMode">The metrics gathering mode. Automatic will sample many useful metrics
        /// automatically for your reports, whereas Manual only accepts your own custom metrics. Disable if you wish to
        /// prevent any metrics from being added to your reports.</param>
        public UserReportingClientConfiguration(int maximumEventCount = 100, int maximumMeasureCount = 300,
                                                int framesPerMeasure = 60, int maximumScreenshotCount = 10,
                                                MetricsGatheringMode metricsGatheringMode = MetricsGatheringMode.Automatic)
        {
            MaximumEventCount = maximumEventCount;
            MetricsGatheringMode = metricsGatheringMode;
            MaximumMeasureCount = maximumMeasureCount;
            FramesPerMeasure = framesPerMeasure;
            MaximumScreenshotCount = maximumScreenshotCount;

            // Configuration Clean Up
            FramesPerMeasure = FramesPerMeasure > 0 ? FramesPerMeasure : 1;
            MaximumEventCount = MaximumEventCount > 0 ? MaximumEventCount : 1;
            MaximumMeasureCount =
                MaximumMeasureCount > 0 ? MaximumMeasureCount : 1;
        }

        internal int FramesPerMeasure { get; }

        internal int MaximumEventCount { get; }

        internal int MaximumMeasureCount { get; }

        internal MetricsGatheringMode MetricsGatheringMode { get; }

        internal int MaximumScreenshotCount { get; }
    }
}
