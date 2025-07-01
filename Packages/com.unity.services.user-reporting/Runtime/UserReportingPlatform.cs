using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using Unity.Services.UserReporting.Client;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Unity.Services.UserReporting
{
    class UserReportingPlatform
    {
        struct LogMessage
        {
            internal string LogString;

            internal LogType LogType;

            internal string StackTrace;
        }

        class PostOperation
        {
            public PostOperation(string endpoint = null, string contentType = null, byte[] content = null,
                                 Action<bool, byte[]> callback = null, Action<float, float> progressCallback = null)
            {
                Endpoint = endpoint;
                ContentType = contentType;
                Content = content;
                Callback = callback;
                ProgressCallback = progressCallback;
            }

            internal Action<bool, byte[]> Callback { get; set; }

            internal Action<float, float> ProgressCallback { get; set; }

            internal string Endpoint;
            internal string ContentType;
            internal string Error;

            internal long ResponseCode;

            internal byte[] Content;
            internal byte[] Data;

            internal bool WebRequestStarted;

            internal IEnumerator WebRequestUpdate;
        }

        class ProfilerSampler
        {
            internal string Name;

            internal Recorder Recorder;

            internal double GetValue()
            {
                if (Recorder == null)
                {
                    return 0;
                }

                return Recorder.elapsedNanoseconds / 1000000.0;
            }
        }

        internal UserReportingPlatform()
        {
            m_LogMessages = new List<LogMessage>();
            m_PostOperations = new List<PostOperation>();
            m_ScreenshotOperations = new List<ScreenshotOperation>();

            // Recorders
            m_ProfilerSamplers = new ConcurrentBag<ProfilerSampler>();
            Dictionary<string, string> samplerNames = GetSamplerNames();
            foreach (var kvp in samplerNames)
            {
                Sampler sampler = Sampler.Get(kvp.Key);
                if (sampler.isValid)
                {
                    Recorder recorder = sampler.GetRecorder();
                    recorder.enabled = true;
                    ProfilerSampler profilerSampler = new ProfilerSampler();
                    profilerSampler.Name = kvp.Value;
                    profilerSampler.Recorder = recorder;
                    m_ProfilerSamplers.Add(profilerSampler);
                }
            }

            // Log Messages
            LogDispatcher.Register(this);
        }

        List<LogMessage> m_LogMessages;

        List<PostOperation> m_PostOperations;

        ConcurrentBag<ProfilerSampler> m_ProfilerSamplers;

        List<ScreenshotOperation> m_ScreenshotOperations;

        List<PostOperation> m_TaskOperations;

        public void OnEndOfFrame(UserReportingClient client)
        {
            lock (m_ScreenshotOperations)
            {
                foreach (var screenshotOperation in m_ScreenshotOperations)
                {
                    screenshotOperation.Update();
                }

                m_ScreenshotOperations.RemoveAll(operation => operation.Stage == ScreenshotStage.Done);
            }
        }

        public void Post(string endpoint, string contentType, byte[] content, Action<float, float> progressCallback,
            Action<bool, byte[]> callback)
        {
            PostOperation postOperation = new PostOperation(endpoint, contentType, content, callback, progressCallback);
            lock (m_PostOperations)
            {
                m_PostOperations.Add(postOperation);
            }
        }

        public void ReceiveLogMessage(string logString, string stackTrace, LogType logType)
        {
            lock (m_LogMessages)
            {
                LogMessage logMessage = new LogMessage();
                logMessage.LogString = logString;
                logMessage.StackTrace = stackTrace;
                logMessage.LogType = logType;
                m_LogMessages.Add(logMessage);
            }
        }

        public void RunTask(Func<object> task, Action<object> callback)
        {
            callback(task());
        }

        public void TakeScreenshot(UserReportingClient client, int frameNumber, int maximumWidth, int maximumHeight, object source,
            Action<int, byte[]> callback)
        {
            ScreenshotOperation screenshotOperation = new ScreenshotOperation(client, frameNumber, maximumWidth,
                maximumHeight, source, callback);
            lock (m_ScreenshotOperations)
            {
                m_ScreenshotOperations.Add(screenshotOperation);
            }
        }

        public void Update(UserReportingClient client)
        {
            // Log Messages
            lock (m_LogMessages)
            {
                foreach (var logMessage in m_LogMessages)
                {
                    UserReportEventLevel eventLevel = UserReportEventLevel.Info;
                    if (logMessage.LogType == LogType.Warning)
                    {
                        eventLevel = UserReportEventLevel.Warning;
                    }
                    else if (logMessage.LogType == LogType.Error)
                    {
                        eventLevel = UserReportEventLevel.Error;
                    }
                    else if (logMessage.LogType == LogType.Exception)
                    {
                        eventLevel = UserReportEventLevel.Error;
                    }
                    else if (logMessage.LogType == LogType.Assert)
                    {
                        eventLevel = UserReportEventLevel.Error;
                    }

                    if (client.IsConnectedToLogger)
                    {
                        client.LogEvent(eventLevel, logMessage.LogString, logMessage.StackTrace);
                    }
                }

                m_LogMessages.Clear();
            }

            // Metrics
            if (client.Configuration.MetricsGatheringMode == MetricsGatheringMode.Automatic)
            {
                // Sample Automatic Metrics
                SampleAutomaticMetrics(client);

                // Profiler Samplers
                lock (m_ProfilerSamplers)
                {
                    foreach (var profilerSampler in m_ProfilerSamplers)
                    {
                        client.SampleMetric(profilerSampler.Name, profilerSampler.GetValue());
                    }
                }
            }

            // Post Operations.
            int postOperationIndex = 0;
            lock (m_PostOperations)
            {
                while (postOperationIndex < m_PostOperations.Count)
                {
                    PostOperation postOperation = m_PostOperations[postOperationIndex];
                    if (!postOperation.WebRequestStarted)
                    {
                        postOperation.WebRequestUpdate = DoWebRequest(postOperationIndex);
                    }

                    if (postOperation.WebRequestUpdate.MoveNext())
                    {
                        postOperationIndex++;
                    }
                    else
                    {
                        // The operation is done.
                        bool isError = postOperation.Error != null || postOperation.ResponseCode >= 300;
                        if (isError)
                        {
                            string errorMessage = $"UserReportingPlatform.Post: {postOperation.ResponseCode} {postOperation.Error}";
                            Debug.Log(errorMessage);
                            client.LogEvent(UserReportEventLevel.Error, errorMessage);
                        }

                        postOperation.ProgressCallback(1, 1);
                        postOperation.Callback(!isError, postOperation.Data);
                        m_PostOperations.RemoveAt(postOperationIndex);
                    }
                }
            }
        }

        IEnumerator DoWebRequest(int postOperationIndex)
        {
            lock (m_PostOperations)
            {
                m_PostOperations[postOperationIndex].WebRequestStarted = true;

                using (var webRequest = new UnityWebRequest(m_PostOperations[postOperationIndex].Endpoint, "POST"))
                {
                    webRequest.disposeUploadHandlerOnDispose = true;
                    webRequest.disposeDownloadHandlerOnDispose = true;
                    webRequest.uploadHandler = new UploadHandlerRaw(m_PostOperations[postOperationIndex].Content);
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    webRequest.SetRequestHeader("Content-Type", m_PostOperations[postOperationIndex].ContentType);
                    webRequest.SendWebRequest();

                    while (webRequest.result == UnityWebRequest.Result.InProgress)
                    {
                        m_PostOperations[postOperationIndex].ProgressCallback(webRequest.uploadProgress,
                            webRequest.downloadProgress);
                        yield return true;
                    }

                    m_PostOperations[postOperationIndex].Error = webRequest.error;
                    m_PostOperations[postOperationIndex].ResponseCode = webRequest.responseCode;
                    m_PostOperations[postOperationIndex].Data = webRequest.downloadHandler.data;
                }
            }
        }

        public IDictionary<string, string> GetDeviceMetadata()
        {
            Dictionary<string, string> deviceMetadata = new Dictionary<string, string>();

            // Unity
            deviceMetadata.Add("BuildGUID", Application.buildGUID);
            deviceMetadata.Add("DeviceModel", SystemInfo.deviceModel);
            deviceMetadata.Add("DeviceType", SystemInfo.deviceType.ToString());
            deviceMetadata.Add("DPI", Screen.dpi.ToString(CultureInfo.InvariantCulture));
            deviceMetadata.Add("GraphicsDeviceName", SystemInfo.graphicsDeviceName);
            deviceMetadata.Add("GraphicsDeviceType", SystemInfo.graphicsDeviceType.ToString());
            deviceMetadata.Add("GraphicsDeviceVendor", SystemInfo.graphicsDeviceVendor);
            deviceMetadata.Add("GraphicsDeviceVersion", SystemInfo.graphicsDeviceVersion);
            deviceMetadata.Add("GraphicsMemorySize", SystemInfo.graphicsMemorySize.ToString());
            deviceMetadata.Add("InstallerName", Application.installerName);
            deviceMetadata.Add("InstallMode", Application.installMode.ToString());
            deviceMetadata.Add("IsEditor", Application.isEditor.ToString());
            deviceMetadata.Add("IsFullScreen", Screen.fullScreen.ToString());
            deviceMetadata.Add("OperatingSystem", SystemInfo.operatingSystem);
            deviceMetadata.Add("OperatingSystemFamily", SystemInfo.operatingSystemFamily.ToString());
            deviceMetadata.Add("Orientation", Screen.orientation.ToString());
            deviceMetadata.Add("Platform", Application.platform.ToString());
            deviceMetadata.Add("QualityLevel", QualitySettings.names[QualitySettings.GetQualityLevel()]);
            deviceMetadata.Add("ResolutionWidth", Screen.currentResolution.width.ToString());
            deviceMetadata.Add("ResolutionHeight", Screen.currentResolution.height.ToString());
#if UNITY_2022_2_OR_NEWER
            deviceMetadata.Add("ResolutionRefreshRate", Screen.currentResolution.refreshRateRatio.ToString());
#else
            deviceMetadata.Add("ResolutionRefreshRate", Screen.currentResolution.refreshRate.ToString());
#endif
            deviceMetadata.Add("SystemLanguage", Application.systemLanguage.ToString());
            deviceMetadata.Add("SystemMemorySize", SystemInfo.systemMemorySize.ToString());
            deviceMetadata.Add("TargetFrameRate", Application.targetFrameRate.ToString());
            deviceMetadata.Add("UnityVersion", Application.unityVersion);
            deviceMetadata.Add("Version", Application.version);

            // Other
            deviceMetadata.Add("Source", "Unity");

            // Type
            Type type = GetType();
            deviceMetadata.Add("IUserReportingPlatform", type.Name);

            // Cloud Diagnostics Package
            if (UserReportingService.serviceInternalUserReportingServiceInstance != null)
            {
                deviceMetadata.Add("PackageVersion", UserReportingService.serviceInternalUserReportingServiceInstance.Version);
                deviceMetadata.Add("InstallationIdentifier",
                    UserReportingService.serviceInternalUserReportingServiceInstance.InstallationIdentifier);
            }

            // Return
            return deviceMetadata;
        }

        static Dictionary<string, string> GetSamplerNames()
        {
            Dictionary<string, string> samplerNames = new Dictionary<string, string>();
            samplerNames.Add("AudioManager.FixedUpdate", "AudioManager.FixedUpdateInMilliseconds");
            samplerNames.Add("AudioManager.Update", "AudioManager.UpdateInMilliseconds");
            samplerNames.Add("LateBehaviourUpdate", "Behaviors.LateUpdateInMilliseconds");
            samplerNames.Add("BehaviourUpdate", "Behaviors.UpdateInMilliseconds");
            samplerNames.Add("Camera.Render", "Camera.RenderInMilliseconds");
            samplerNames.Add("Overhead", "Engine.OverheadInMilliseconds");
            samplerNames.Add("WaitForRenderJobs", "Engine.WaitForRenderJobsInMilliseconds");
            samplerNames.Add("WaitForTargetFPS", "Engine.WaitForTargetFPSInMilliseconds");
            samplerNames.Add("GUI.Repaint", "GUI.RepaintInMilliseconds");
            samplerNames.Add("Network.Update", "Network.UpdateInMilliseconds");
            samplerNames.Add("ParticleSystem.EndUpdateAll", "ParticleSystem.EndUpdateAllInMilliseconds");
            samplerNames.Add("ParticleSystem.Update", "ParticleSystem.UpdateInMilliseconds");
            samplerNames.Add("Physics.FetchResults", "Physics.FetchResultsInMilliseconds");
            samplerNames.Add("Physics.Processing", "Physics.ProcessingInMilliseconds");
            samplerNames.Add("Physics.ProcessReports", "Physics.ProcessReportsInMilliseconds");
            samplerNames.Add("Physics.Simulate", "Physics.SimulateInMilliseconds");
            samplerNames.Add("Physics.UpdateBodies", "Physics.UpdateBodiesInMilliseconds");
            samplerNames.Add("Physics.Interpolation", "Physics.InterpolationInMilliseconds");
            samplerNames.Add("Physics2D.DynamicUpdate", "Physics2D.DynamicUpdateInMilliseconds");
            samplerNames.Add("Physics2D.FixedUpdate", "Physics2D.FixedUpdateInMilliseconds");
            return samplerNames;
        }

        public void AddInSceneData(UserReport userReport)
        {
            // Active Scene
            Scene activeScene = SceneManager.GetActiveScene();
            userReport.DeviceMetadata.Add(new UserReportNamedValue("ActiveSceneName", activeScene.name));

            // Main Camera
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                userReport.DeviceMetadata.Add(new UserReportNamedValue("MainCameraName", mainCamera.name));
                userReport.DeviceMetadata.Add(new UserReportNamedValue("MainCameraPosition", mainCamera.transform.position.ToString()));
                userReport.DeviceMetadata.Add(new UserReportNamedValue("MainCameraForward", mainCamera.transform.forward.ToString()));

                // Looking At
                if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out var hit))
                {
                    GameObject lookingAt = hit.transform.gameObject;
                    userReport.DeviceMetadata.Add(new UserReportNamedValue("LookingAt", hit.point.ToString()));
                    userReport.DeviceMetadata.Add(new UserReportNamedValue("LookingAtGameObject", lookingAt.name));
                    userReport.DeviceMetadata.Add(new UserReportNamedValue("LookingAtGameObjectPosition",
                        lookingAt.transform.position.ToString()));
                }
            }
        }

        static void SampleAutomaticMetrics(UserReportingClient client)
        {
            // Graphics
            client.SampleMetric("Graphics.FramesPerSecond", 1.0f / Time.deltaTime);

            // Memory
            client.SampleMetric("Memory.MonoUsedSizeInBytes", Profiler.GetMonoUsedSizeLong());
            client.SampleMetric("Memory.TotalAllocatedMemoryInBytes", Profiler.GetTotalAllocatedMemoryLong());
            client.SampleMetric("Memory.TotalReservedMemoryInBytes", Profiler.GetTotalReservedMemoryLong());
            client.SampleMetric("Memory.TotalUnusedReservedMemoryInBytes", Profiler.GetTotalUnusedReservedMemoryLong());

            // Battery
            client.SampleMetric("Battery.BatteryLevelInPercent", SystemInfo.batteryLevel);
        }
    }

    enum ScreenshotStage
    {
        Render = 0,
        ReadPixels = 1,
        ResizeTexture = 2,
        EncodeToPNG = 3,
        Done = 4
    }
}
