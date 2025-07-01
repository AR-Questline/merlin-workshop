using System;
using System.Diagnostics;
using Unity.Services.UserReporting.Client;
using UnityEngine;

namespace Unity.Services.UserReporting
{
    class ScreenshotOperation
    {
        internal ScreenshotOperation(UserReportingClient client, int frameNumber, int maximumWidth, int maximumHeight, object source,
            Action<int, byte[]> callback)
        {
            m_Client = client;
            Callback = callback;
            FrameNumber = frameNumber;
            MaximumHeight = maximumHeight;
            MaximumWidth = maximumWidth;
            Source = source;
            m_Stopwatch = new Stopwatch();
        }

        readonly UserReportingClient m_Client;

        Stopwatch m_Stopwatch;

        Action<int, byte[]> Callback { get; set; }

        int FrameNumber { get; set; }

        int MaximumHeight { get; set; }

        int MaximumWidth { get; set; }

        byte[] PngData { get; set; }

        object Source { get; set; }

        internal ScreenshotStage Stage { get; set; }

        Texture2D Texture { get; set; }

        Texture2D TextureResized { get; set; }

        internal void Update()
        {
            switch (Stage)
            {
                case ScreenshotStage.Render:
                    RenderScreenshot();
                    break;
                case ScreenshotStage.ReadPixels:
                    ReadPixels();
                    break;
                case ScreenshotStage.ResizeTexture:
                    ResizeTexture();
                    break;
                case ScreenshotStage.EncodeToPNG:
                    EncodeToPNG();
                    break;
            }
        }

        void ReadPixels()
        {
            m_Stopwatch.Reset();
            m_Stopwatch.Start();
            if (Source is RenderTexture renderTextureSource)
            {
                RenderTexture originalActiveTexture = RenderTexture.active;
                RenderTexture.active = renderTextureSource;
                Texture = new Texture2D(renderTextureSource.width,
                    renderTextureSource.height, TextureFormat.ARGB32, true);
                Texture.ReadPixels(new Rect(0, 0, renderTextureSource.width,
                    renderTextureSource.height), 0, 0);
                Texture.Apply();
                RenderTexture.active = originalActiveTexture;
            }
            else
            {
                Texture = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32,
                    true);
                Texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height),
                    0, 0);
                Texture.Apply();
            }

            m_Stopwatch.Stop();
            m_Client.SampleClientMetric("Screenshot.ReadPixels", m_Stopwatch.ElapsedMilliseconds);
            Stage = ScreenshotStage.ResizeTexture;
        }

        void RenderScreenshot()
        {
            if (Source is not Camera cameraSource)
            {
                Stage = ScreenshotStage.ReadPixels;
            }
            else
            {
                m_Stopwatch.Reset();
                m_Stopwatch.Start();
                RenderTexture renderTexture = new RenderTexture(MaximumWidth, MaximumHeight, 24);
                RenderTexture originalTargetTexture = cameraSource.targetTexture;
                cameraSource.targetTexture = renderTexture;
                cameraSource.Render();
                cameraSource.targetTexture = originalTargetTexture;
                m_Stopwatch.Stop();
                m_Client.SampleClientMetric("Screenshot.Render", m_Stopwatch.ElapsedMilliseconds);
                Source = renderTexture;
                Stage = ScreenshotStage.ReadPixels;
            }
        }

        void ResizeTexture()
        {
            m_Stopwatch.Reset();
            m_Stopwatch.Start();
            int maximumWidth = MaximumWidth > 32 ? MaximumWidth : 32;
            int maximumHeight = MaximumHeight > 32 ? MaximumHeight : 32;
            int width = Texture.width;
            int height = Texture.height;
            int mipLevel = 0;
            while (width > maximumWidth || height > maximumHeight)
            {
                width /= 2;
                height /= 2;
                mipLevel++;
            }

            TextureResized = new Texture2D(width, height);
            TextureResized.SetPixels(Texture.GetPixels(mipLevel));
            TextureResized.Apply();
            m_Stopwatch.Stop();
            m_Client.SampleClientMetric("Screenshot.GetPixels", m_Stopwatch.ElapsedMilliseconds);
            Stage = ScreenshotStage.EncodeToPNG;
        }

        void EncodeToPNG()
        {
            m_Stopwatch.Reset();
            m_Stopwatch.Start();
            PngData = TextureResized.EncodeToPNG();
            m_Stopwatch.Stop();
            m_Client.SampleClientMetric("Screenshot.EncodeToPNG", m_Stopwatch.ElapsedMilliseconds);
            Callback(FrameNumber, PngData);
            UnityEngine.Object.Destroy(Texture);
            UnityEngine.Object.Destroy(TextureResized);
            Stage = ScreenshotStage.Done;
        }
    }
}
