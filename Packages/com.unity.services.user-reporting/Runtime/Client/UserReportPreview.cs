using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Unity.Services.UserReporting.Client
{
    /// <summary>
    ///     Represents a user report preview or the fly weight version of a user report.
    /// </summary>
    class UserReportPreview
    {
        internal UserReportPreview()
        {
            Dimensions = new List<UserReportNamedValue>();
        }

        [JsonProperty]
        internal List<UserReportMetric> AggregateMetrics { get; set; }

        public long ContentLength { get; set; }

        [JsonProperty]
        internal List<UserReportNamedValue> Dimensions { get; set; }

        [JsonProperty]
        internal DateTime ExpiresOn { get; set; }

        [JsonProperty]
        internal string Identifier { get; set; }

        [JsonProperty]
        internal bool IsHiddenWithoutDimension { get; set; }

        [JsonProperty]
        internal string ProjectIdentifier { get; set; }

        [JsonProperty]
        internal DateTime ReceivedOn { get; set; }

        [JsonProperty]
        internal string Summary { get; set; }

        [JsonProperty]
        internal UserReportScreenshot Thumbnail { get; set; }
    }
}
