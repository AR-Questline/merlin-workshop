using System;
using Newtonsoft.Json;

namespace Unity.Services.UserReporting.Client
{
    struct UserReportMetric : IEquatable<UserReportMetric>
    {
        internal UserReportMetric(string name)
        {
            Name = name;
            Count = 0;
            Maximum = 0;
            Minimum = 0;
            Sum = 0;
        }

        public bool Equals(UserReportMetric other)
        {
            return Count == other.Count && Maximum.Equals(other.Maximum) && Minimum.Equals(other.Minimum)
                && Name == other.Name && Sum.Equals(other.Sum);
        }

        [JsonProperty]
        internal double Average => Sum / Count;

        [JsonProperty]
        int Count { get; set; }

        [JsonProperty]
        double Maximum { get; set; }

        [JsonProperty]
        double Minimum { get; set; }

        [JsonProperty]
        internal string Name { get; set; }

        [JsonProperty]
        double Sum { get; set; }

        internal void Sample(double value)
        {
            if (Count == 0)
            {
                Minimum = double.MaxValue;
                Maximum = double.MinValue;
            }

            Count++;
            Sum += value;
            Minimum = Math.Min(Minimum, value);
            Maximum = Math.Max(Maximum, value);
        }
    }
}
