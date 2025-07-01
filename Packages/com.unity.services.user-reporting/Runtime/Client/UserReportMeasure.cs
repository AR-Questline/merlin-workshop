using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Unity.Services.UserReporting.Client
{
    struct UserReportMeasure : IEquatable<UserReportMeasure>
    {
        public bool Equals(UserReportMeasure other)
        {
            return EndFrameNumber == other.EndFrameNumber && Metadata.SequenceEqual(other.Metadata)
                && Metrics.SequenceEqual(other.Metrics) && StartFrameNumber == other.StartFrameNumber;
        }

        [JsonProperty]
        internal int EndFrameNumber { get; set; }

        [JsonProperty]
        internal List<UserReportNamedValue> Metadata { get; set; }

        [JsonProperty]
        internal List<UserReportMetric> Metrics { get; set; }

        [JsonProperty]
        internal int StartFrameNumber { get; set; }
    }
}
