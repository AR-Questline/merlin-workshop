using System;
using Newtonsoft.Json;

namespace Unity.Services.UserReporting.Client
{
    struct UserReportAttachment : IEquatable<UserReportAttachment>
    {
        public bool Equals(UserReportAttachment other)
        {
            return ContentType == other.ContentType && DataBase64 == other.DataBase64
                && DataIdentifier == other.DataIdentifier && FileName == other.FileName && Name == other.Name;
        }

        internal UserReportAttachment(string name, string fileName, string contentType, byte[] data)
        {
            Name = name;
            FileName = fileName;
            ContentType = contentType;
            DataBase64 = Convert.ToBase64String(data);
            DataIdentifier = null;
        }

        [JsonProperty]
        internal string ContentType { get; set; }

        [JsonProperty]
        internal string DataBase64 { get; set; }

        [JsonProperty]
        internal string DataIdentifier { get; set; }

        [JsonProperty]
        internal string FileName { get; set; }

        [JsonProperty]
        internal string Name { get; set; }
    }
}
