using System;
using Newtonsoft.Json;

namespace Unity.Services.UserReporting.Client
{
    struct UserReportScreenshot : IEquatable<UserReportScreenshot>
    {
        public bool Equals(UserReportScreenshot other)
        {
            return string.Equals(DataBase64, other.DataBase64) && FrameNumber == other.FrameNumber;
        }

        [JsonProperty]
        internal string DataBase64 { get; set; }

        [JsonProperty]
        internal int FrameNumber { get; set; }
    }
}
