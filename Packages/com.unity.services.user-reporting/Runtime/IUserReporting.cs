using System;
using Unity.Services.UserReporting.Client;
using UnityEngine;

namespace Unity.Services.UserReporting
{
    /// <summary>
    /// Provides the access point for User Reporting functionality.
    /// </summary>
    public interface IUserReporting
    {
        /// <summary>
        /// Configures User Reporting to default values, or using optional parameters. Any unsent User Reporting data
        /// will be lost when reconfiguring.
        /// </summary>
        /// <param name="configuration">Option to use a given UserReportingClientConfiguration</param>
        /// <param name="projectIdentifier">Option to send reports to the given Unity Project ID dashboard</param>
        void Configure(UserReportingClientConfiguration configuration = null, string projectIdentifier = null);

        /// <summary>
        /// Selects to send reports to the current project's Unity Project ID dashboard, or to the optional Unity
        /// Project ID parameter.
        /// </summary>
        /// <param name="projectIdentifier">Option to send reports to given Unity Project ID dashboard</param>
        void SetProjectIdentifier(string projectIdentifier = null);

        /// <summary>
        /// Whether or not User Reporting should track metrics about itself and include them in reports.
        /// </summary>
        bool SendInternalMetrics { get; set; }

        /// <summary>
        /// Whether or not User Reporting should produce an event in Unity Analytics when new reports are
        /// submitted.
        /// </summary>
        [Obsolete("SendEventsToAnalytics is deprecated, please use the UGS Analytics SDK for your needs.")]
        bool SendEventsToAnalytics { get; set; }

        /// <summary>
        /// Checks whether or not User Reporting possesses a current User Report that has not been submitted.
        /// </summary>
        bool HasOngoingReport { get; }

        /// <summary>
        /// Takes a screenshot of the given maximum dimensions from the screen view by default, or optionally from a
        /// given source object such as a Camera, for use in User Reports.
        /// </summary>
        /// <param name="maximumWidth">The maximum height of the screenshot that will be taken.</param>
        /// <param name="maximumHeight">The maximum width of the screenshot that will be taken.</param>
        /// <param name="source">The optional source of the screenshot, such as a Camera or RenderTexture.</param>
        void TakeScreenshot(int maximumWidth, int maximumHeight, object source = null);

        /// <summary>
        /// Creates a new User Report, which will clear the previous current User Report if one exists, and populates it
        /// with the latest screenshots, attachments, and metrics collected by User Reporting.
        /// </summary>
        /// <param name="callback">Optional Action which is invoked after the report is created.</param>
        void CreateNewUserReport(Action callback = null);

        /// <summary>
        /// Discards the current User Report.
        /// </summary>
        void ClearOngoingReport();

        /// <summary>
        /// Adds an attachment to User Reporting. For the attachment to work correctly when accessing them in reports on
        /// the dashboard, please ensure you provide the correct IANA media type for your file.
        /// </summary>
        /// <param name="title">The title of the attachment.</param>
        /// <param name="filename">The filename of the attachment, which appears when inspecting User Report attachments
        /// within the dashboard.</param>
        /// <param name="data">The data of the attachment as a byte array.</param>
        /// <param name="mediaType">The IANA media type of the attachment. For example: "attachment/json"</param>
        void AddAttachmentToReport(string title, string filename, byte[] data, string mediaType = "");

        /// <summary>
        /// The latest screenshot taken by User Reporting.
        /// </summary>
        /// <returns>Returns the latest screenshot taken by User Reporting if one exists, otherwise null.</returns>
        Texture2D GetLatestScreenshot();

        /// <summary>
        /// Sets the summary of the current User Report.
        /// </summary>
        /// <param name="summaryInputText">The text used as the summary.</param>
        void SetReportSummary(string summaryInputText);

        /// <summary>
        /// Adds a piece of metadata to the current User Report, which will appear in reports alongsde the
        /// device metadata included in reports by default.
        /// </summary>
        /// <param name="name">The name the value will be titled with in the report.</param>
        /// <param name="value">The value that will appear under the given name in the report.</param>
        void AddMetadata(string name, string value);

        /// <summary>
        /// Adds a Dimension Value to the current User Report, and creates a new Dimension for the report if it doesn't
        /// already exist.
        /// </summary>
        /// <param name="dimension">The Dimension the value will be added to.</param>
        /// <param name="value">The value that will be added to the dimension.</param>
        void AddDimensionValue(string dimension, string value);

        /// <summary>
        /// Sets the description of the current User Report.
        /// </summary>
        /// <param name="description">The text that will be used as the description.</param>
        void SetReportDescription(string description);

        /// <summary>
        /// Sends the current User Report, and provides two optional Actions to track the progress of the report
        /// submission and handle the completion of the submission respectively.
        /// </summary>
        /// <param name="progressUpdate">Action to track the submission progress via float values 0.0 to 1.0.</param>
        /// <param name="result">Action called when the submission attempt is done, with a bool indicating
        /// whether it was successful or not.</param>
        void SendUserReport(Action<float> progressUpdate, Action<bool> result);

        /// <summary>
        /// Creates a sample a metric with the given value, creating a new metric if it doesn't already exist.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <param name="value">The value of the metric used in this sample.</param>
        void SampleMetric(string name, double value);

        /// <summary>
        /// Sets User Reporting to submit User Reports to a custom endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to be used.</param>
        void SetEndpoint(string endpoint);

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="level">The level to categorize this event in.</param>
        /// <param name="message">The message to accompany the event.</param>
        void LogEvent(UserReportEventLevel level, string message);
    }
}
