using System;
using Newtonsoft.Json;

namespace Unity.Services.UserReporting.Client
{
    struct UserReportNamedValue : IEquatable<UserReportNamedValue>
    {
        public bool Equals(UserReportNamedValue other)
        {
            return Name == other.Name && Value == other.Value;
        }

        public UserReportNamedValue(string name, string value)
        {
            Name = name;
            Value = value;
        }

        [JsonProperty]
        internal string Name { get; set; }
        [JsonProperty]
        internal string Value { get; set; }
    }
}
