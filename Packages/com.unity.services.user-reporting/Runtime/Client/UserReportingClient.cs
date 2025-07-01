using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Unity.Services.UserReporting.Client
{
    class UserReportingClient
    {
        /// <summary>
        ///     Creates a new instance of the <see cref="UserReportingClient" /> class.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="projectIdentifier">The project identifier.</param>
        /// <param name="configuration">The configuration.</param>
        internal UserReportingClient(string endpoint, string projectIdentifier,
                                     UserReportingClientConfiguration configuration)
        {
            // Arguments
            Endpoint = endpoint;
            ProjectIdentifier = projectIdentifier;
            Platform = new UserReportingPlatform();
            Configuration = configuration;

            // Lists
            Attachments = new ConcurrentBag<UserReportAttachment>();
            ClientMetrics = new ConcurrentDictionary<string, UserReportMetric>();
            CurrentMetrics = new ConcurrentDictionary<string, UserReportMetric>();
            Events = new ConcurrentCycle<UserReportEvent>(configuration.MaximumEventCount);
            Measures = new ConcurrentCycle<UserReportMeasure>(configuration.MaximumMeasureCount);
            Screenshots = new ConcurrentCycle<UserReportScreenshot>(configuration.MaximumScreenshotCount);

            // Device Metadata
            DeviceMetadata = new ConcurrentBag<UserReportNamedValue>();
            foreach (var kvp in Platform.GetDeviceMetadata())
            {
                AddDeviceMetadata(kvp.Key, kvp.Value);
            }

            // Is Connected to Logger
            IsConnectedToLogger = true;
        }

        internal ConcurrentDictionary<string, UserReportMetric> ClientMetrics;
        internal ConcurrentDictionary<string, UserReportMetric> CurrentMetrics;

        internal ConcurrentBag<UserReportNamedValue> DeviceMetadata;
        internal ConcurrentBag<UserReportAttachment> Attachments;

        internal ConcurrentCycle<UserReportEvent> Events;
        internal ConcurrentCycle<UserReportMeasure> Measures;
        internal ConcurrentCycle<UserReportScreenshot> Screenshots;

        internal int FrameNumber;
        int m_MeasureFrames;

        internal UserReportingClientConfiguration Configuration { get; }

        internal string Endpoint { get; set; }

        internal bool IsConnectedToLogger { get; set; }

        internal bool SendInternalMetrics { get; set; }

        UserReportingPlatform Platform { get; }

        internal string ProjectIdentifier { get; set; }

        internal void AddDeviceMetadata(string name, string value)
        {
            DeviceMetadata.Add(new UserReportNamedValue(name, value));
        }

        internal void CreateUserReport(Action<UserReport> callback)
        {
            LogEvent(UserReportEventLevel.Info, "Creating user report.");

            Platform.RunTask(() =>
            {
                // Start Stopwatch
                Stopwatch stopwatch = Stopwatch.StartNew();

                // Copy Data
                UserReport userReport = new UserReport();
                userReport.ProjectIdentifier = ProjectIdentifier;

                // Device Metadata
                userReport.DeviceMetadata = DeviceMetadata.ToList();

                // Events
                userReport.Events = Events.ToList();

                // Measures
                userReport.Measures = Measures.ToList();

                // Screenshots
                userReport.Screenshots = Screenshots.ToList();

                // Attachments
                userReport.Attachments = Attachments.ToList();

                // Complete
                userReport.Complete();

                // Modify
                Platform.AddInSceneData(userReport);

                // Stop Stopwatch
                stopwatch.Stop();

                // Sample Client Metric
                SampleClientMetric("UserReportingClient.CreateUserReport.Task", stopwatch.ElapsedMilliseconds);

                // Copy Client Metrics
                foreach (var pair in ClientMetrics)
                {
                    userReport.ClientMetrics.Add(pair.Value);
                }

                // Return
                return userReport;
            }, result => { callback(result as UserReport); });
        }

        internal void Post(string endpoint, string contentType, byte[] content, Action<float, float> progressCallback,
            Action<bool, byte[]> callback)
        {
            Platform.Post(endpoint, contentType, content, progressCallback, callback);
        }

        string GetEndpoint()
        {
            if (Endpoint is null)
            {
                return "https://localhost";
            }

            return Endpoint.Trim();
        }

        internal void LogEvent(UserReportEventLevel level, string message)
        {
            LogEvent(level, message, null, null);
        }

        internal void LogEvent(UserReportEventLevel level, string message, string stackTrace)
        {
            LogEvent(level, message, stackTrace, null);
        }

        void LogEvent(UserReportEventLevel level, string message, string stackTrace, Exception exception)
        {
            UserReportEvent userReportEvent = new UserReportEvent(level, message, FrameNumber, stackTrace,
                DateTime.UtcNow);
            if (exception is not null)
            {
                userReportEvent.Exception = new SerializableException(exception);
            }

            Events.Add(userReportEvent);
        }

        internal void SampleClientMetric(string name, double value)
        {
            if (double.IsInfinity(value) || double.IsNaN(value))
            {
                return;
            }

            ClientMetrics.TryAdd(name, new UserReportMetric(name));

            UserReportMetric userReportMetric = ClientMetrics[name];
            userReportMetric.Sample(value);
            ClientMetrics[name] = userReportMetric;

            // Self Reporting
            if (SendInternalMetrics)
            {
                SampleMetric(name, value);
            }
        }

        internal void SampleMetric(string name, double value)
        {
            if (Configuration.MetricsGatheringMode == MetricsGatheringMode.Disabled)
            {
                return;
            }

            if (double.IsInfinity(value) || double.IsNaN(value))
            {
                return;
            }

            CurrentMetrics.TryAdd(name, new UserReportMetric(name));

            UserReportMetric userReportMetric = CurrentMetrics[name];
            userReportMetric.Sample(value);
            CurrentMetrics[name] = userReportMetric;
        }

        internal void SendUserReport(UserReport userReport, Action<bool> sendCallback, Action<UserReport> replyCallback)
        {
            SendUserReport(userReport, null, null, sendCallback, replyCallback);
        }

        internal void SendUserReport(UserReport userReport, Action<float> sendProgressCallback,
            Action<float> replyProgressCallback,
            Action<bool> sendSuccessCallback, Action<UserReport> replySuccessCallback)
        {
            try
            {
                if (userReport is null)
                {
                    return;
                }

                if (ProjectIdentifier is null || ProjectIdentifier == "")
                {
                    LogEvent(UserReportEventLevel.Warning,
                        "The project identifier is invalid. This report will be abandoned.");
                    throw new Exception("The project identifier is invalid. This report will be abandoned.");
                }

                if (userReport.Identifier is not null)
                {
                    LogEvent(UserReportEventLevel.Warning,
                        "Identifier cannot be set on the client side. The value provided was discarded.");
                    return;
                }

                if (userReport.ContentLength != 0)
                {
                    LogEvent(UserReportEventLevel.Warning,
                        "ContentLength cannot be set on the client side. The value provided was discarded.");
                    return;
                }

                if (userReport.ReceivedOn != default)
                {
                    LogEvent(UserReportEventLevel.Warning,
                        "ReceivedOn cannot be set on the client side. The value provided was discarded.");
                    return;
                }

                if (userReport.ExpiresOn != default)
                {
                    LogEvent(UserReportEventLevel.Warning,
                        "ExpiresOn cannot be set on the client side. The value provided was discarded.");
                    return;
                }

                LogEvent(UserReportEventLevel.Info, "Sending user report.");
                string json = JsonConvert.SerializeObject(userReport);
                byte[] jsonData = Encoding.UTF8.GetBytes(json);
                string endpoint = GetEndpoint();
                string url = $"{endpoint}/api/userreporting";
                Platform.Post(url, "application/json", jsonData, (uploadProgress, downloadProgress) =>
                {
                    if (sendProgressCallback is not null)
                    {
                        sendProgressCallback(uploadProgress);
                    }
                    if (replyProgressCallback is not null)
                    {
                        replyProgressCallback(downloadProgress);
                    }
                }, (success, result) =>
                    {
                        UserReportingService.serviceInternalUserReportingServiceInstance.Scheduler.ScheduleAction(()  =>
                        {
                            if (!success)
                            {
                                LogEvent(UserReportEventLevel.Error, "Sending user report failed.");
                                sendSuccessCallback?.Invoke(false);

                                return;
                            }

                            try
                            {
                                string jsonResult = Encoding.UTF8.GetString(result);
                                UserReport userReportResult = JsonConvert.DeserializeObject<UserReport>(jsonResult);
                                if (userReportResult is not null)
                                {
                                    sendSuccessCallback?.Invoke(true);
                                    replySuccessCallback?.Invoke(userReportResult);
                                }
                                else
                                {
                                    sendSuccessCallback?.Invoke(false);
                                }
                            }
                            catch (Exception ex)
                            {
                                LogEvent(UserReportEventLevel.Error, $"Sending user report failed: {ex}");
                                sendSuccessCallback?.Invoke(false);
                            }
                        });
                    });
            }
            catch (Exception ex)
            {
                LogEvent(UserReportEventLevel.Error, $"Sending user report failed: {ex}");
                sendSuccessCallback?.Invoke(false);
            }
        }

        internal void TakeScreenshot(int maximumWidth, int maximumHeight, object source = null)
        {
            LogEvent(UserReportEventLevel.Info, "Taking screenshot.");
            Platform.TakeScreenshot(this, FrameNumber, maximumWidth, maximumHeight, source, (passedFrameNumber, data) =>
            {
                if (UserReportingService.serviceInternalUserReportingServiceInstance is null)
                {
                    UserReportingManager.WarnConfigurationFailure();
                    return;
                }
                UserReportingService.serviceInternalUserReportingServiceInstance.Scheduler.ScheduleAction(() =>
                {
                    UserReportScreenshot userReportScreenshot = new UserReportScreenshot();
                    userReportScreenshot.FrameNumber = passedFrameNumber;
                    userReportScreenshot.DataBase64 = Convert.ToBase64String(data);
                    Screenshots.Add(userReportScreenshot);
                });
            });
        }

        internal void Update()
        {
            // Stopwatch
            var updateStopwatch = new Stopwatch();
            updateStopwatch.Start();

            // Update Platform
            Platform.Update(this);

            // Measures
            if (Configuration.MetricsGatheringMode != MetricsGatheringMode.Disabled)
            {
                int framesPerMeasure = Configuration.FramesPerMeasure;
                if (m_MeasureFrames >= framesPerMeasure)
                {
                    UserReportMeasure userReportMeasure = new UserReportMeasure();
                    userReportMeasure.StartFrameNumber = FrameNumber - framesPerMeasure;
                    userReportMeasure.EndFrameNumber = FrameNumber - 1;
                    userReportMeasure.Metadata = new List<UserReportNamedValue>();
                    userReportMeasure.Metrics = new List<UserReportMetric>();
                    foreach (var kvp in CurrentMetrics)
                    {
                        userReportMeasure.Metrics.Add(kvp.Value);
                    }

                    CurrentMetrics.Clear();
                    Measures.Add(userReportMeasure);
                    m_MeasureFrames = 0;
                }

                m_MeasureFrames++;
            }

            // Frame Number
            FrameNumber++;

            // Stopwatch
            updateStopwatch.Stop();
            SampleClientMetric("UserReportingClient.Update", updateStopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        ///     Updates the user reporting client at the end of the frame, which updates networking communication,
        ///     and metrics gathering.
        /// </summary>
        public void UpdateOnEndOfFrame()
        {
            var updateStopwatch = new Stopwatch();
            updateStopwatch.Start();

            lock (Platform)
            {
                Platform.OnEndOfFrame(this);
            }

            updateStopwatch.Stop();
            SampleClientMetric("UserReportingClient.UpdateOnEndOfFrame", updateStopwatch.ElapsedMilliseconds);
        }
    }
}
