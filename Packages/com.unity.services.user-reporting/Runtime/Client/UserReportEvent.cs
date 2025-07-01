using System;
using Newtonsoft.Json;

namespace Unity.Services.UserReporting.Client
{
    struct UserReportEvent : IEquatable<UserReportEvent>
    {
        internal UserReportEvent(UserReportEventLevel level, string message, int frameNumber, string stackTrace,
            DateTime timestamp)
            : this()
        {
            Level = level;
            FrameNumber = frameNumber;
            Message = message;
            StackTrace = stackTrace;
            Timestamp = timestamp;
        }

        public bool Equals(UserReportEvent other)
        {
            return Equals(Exception, other.Exception) && FrameNumber == other.FrameNumber && Level == other.Level
                && Message == other.Message && StackTrace == other.StackTrace && Timestamp.Equals(other.Timestamp);
        }

        [JsonProperty]
        internal SerializableException Exception { get; set; }

        [JsonProperty]
        internal int FrameNumber { get; set; }

        [JsonProperty]
        internal UserReportEventLevel Level { get; set; }

        [JsonProperty]
        internal string Message { get; set; }

        [JsonProperty]
        internal string StackTrace { get; set; }

        [JsonProperty]
        internal DateTime Timestamp { get; set; }
    }
}
