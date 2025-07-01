using System;
using UnityEngine;

namespace Unity.Services.UserReporting
{
    class UserReportingConfigureOnly : MonoBehaviour
    {
        void Start()
        {
            if (UserReportingManager.CurrentClient == null)
            {
                UserReportingManager.Configure();
            }
        }
    }
}
