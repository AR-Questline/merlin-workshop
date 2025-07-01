using System;
using System.Collections;
using UnityEngine;

namespace Unity.Services.UserReporting
{
    class UserReportingSceneHelper : MonoBehaviour
    {
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            StartCoroutine(UpdateClientByFrame());
        }

        public static void Initialize()
        {
            UserReportingSceneHelper[] matches = FindObjectsOfType<UserReportingSceneHelper>();
            if (matches.Length < 1)
            {
                var cloudDiagnosticsUserReporter = new GameObject("CloudDiagnosticsUserReportingSceneHelper");
                cloudDiagnosticsUserReporter.AddComponent<UserReportingSceneHelper>();
            }
        }

        IEnumerator UpdateClientByFrame()
        {
            while (enabled)
            {
                UserReportingManager.CurrentClient.Update();
                yield return new WaitForEndOfFrame();
                UserReportingManager.CurrentClient.UpdateOnEndOfFrame();
                yield return null;
            }
        }
    }
}
