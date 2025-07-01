using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Unity.Services.UserReporting.Client
{
    class UserReport : UserReportPreview, IEquatable<UserReport>
    {
        public bool Equals(UserReport other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null)
            {
                return false;
            }

            return Attachments.SequenceEqual(other.Attachments) && ClientMetrics.SequenceEqual(other.ClientMetrics)
                && DeviceMetadata.SequenceEqual(other.DeviceMetadata) && Events.SequenceEqual(other.Events)
                && Fields.SequenceEqual(other.Fields) && Measures.SequenceEqual(other.Measures)
                && Screenshots.SequenceEqual(other.Screenshots);
        }

        class UserReportMetricSorter : IComparer<UserReportMetric>
        {
            public int Compare(UserReportMetric x, UserReportMetric y)
            {
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            }
        }

        internal UserReport()
        {
            AggregateMetrics = new List<UserReportMetric>();
            Attachments = new List<UserReportAttachment>();
            ClientMetrics = new List<UserReportMetric>();
            DeviceMetadata = new List<UserReportNamedValue>();
            Events = new List<UserReportEvent>();
            Fields = new List<UserReportNamedValue>();
            Measures = new List<UserReportMeasure>();
        }

        [JsonProperty]
        internal List<UserReportAttachment> Attachments { get; set; }

        [JsonProperty]
        internal List<UserReportMetric> ClientMetrics { get; set; }

        [JsonProperty]
        internal List<UserReportNamedValue> DeviceMetadata { get; set; }

        [JsonProperty]
        internal List<UserReportEvent> Events { get; set; }

        [JsonProperty]
        internal List<UserReportNamedValue> Fields { get; set; }

        [JsonProperty]
        internal List<UserReportMeasure> Measures { get; set; }

        [JsonProperty]
        internal List<UserReportScreenshot> Screenshots { get; set; }

        /// <summary>
        /// Completes the user report. Called by the client, and only when constructing a user report manually.
        /// </summary>
        internal void Complete()
        {
            // Add Thumbnail if a screenshot exists.
            if (Screenshots.Count > 0)
            {
                Thumbnail = Screenshots.Last();
            }

            // Aggregate Metrics
            Dictionary<string, UserReportMetric> aggregateMetrics = new Dictionary<string, UserReportMetric>();
            foreach (var measure in Measures)
            {
                if (measure.Metrics is null)
                {
                    break;
                }
                foreach (var metric in measure.Metrics)
                {
                    if (!aggregateMetrics.ContainsKey(metric.Name))
                    {
                        UserReportMetric userReportMetric = new UserReportMetric();
                        userReportMetric.Name = metric.Name;
                        aggregateMetrics.Add(metric.Name, userReportMetric);
                    }

                    UserReportMetric aggregateMetric = aggregateMetrics[metric.Name];
                    aggregateMetric.Sample(metric.Average);
                    aggregateMetrics[metric.Name] = aggregateMetric;
                }
            }

            if (AggregateMetrics == null)
            {
                AggregateMetrics = new List<UserReportMetric>();
            }

            foreach (var kvp in aggregateMetrics)
            {
                AggregateMetrics.Add(kvp.Value);
            }

            AggregateMetrics.Sort(new UserReportMetricSorter());
        }
    }
}
