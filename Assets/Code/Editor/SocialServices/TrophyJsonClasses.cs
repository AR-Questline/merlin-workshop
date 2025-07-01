using System.Collections.Generic;
using Newtonsoft.Json;

namespace Awaken.TG.Editor.SocialServices {
    public class Description {
        [JsonProperty("en-US")] public string enUS { get; set; }
        [JsonProperty("ja-JP")] public string jaJP { get; set; }
        [JsonProperty("de-DE")] public string deDE { get; set; }
        [JsonProperty("fr-FR")] public string frFR { get; set; }
        [JsonProperty("es-ES")] public string esES { get; set; }
        [JsonProperty("it-IT")] public string itIT { get; set; }
        [JsonProperty("pl-PL")] public string plPL { get; set; }
        [JsonProperty("pt-BR")] public string ptBR { get; set; }
        [JsonProperty("ru-RU")] public string ruRU { get; set; }
        [JsonProperty("zh-Hans")] public string zhHans { get; set; }
        [JsonProperty("zh-Hant")] public string zhHant { get; set; }
        [JsonProperty("cs-CZ")] public string csCZ { get; set; }
    }

    public class Entities {
        public List<TrophySet> trophySet { get; set; }
        public List<TrophyGroup> trophyGroups { get; set; }
        public List<Trophy> trophies { get; set; }
    }

    public class EnUS {
        public string type { get; set; }
        public string url { get; set; }
    }

    public class EsES {
        public string type { get; set; }
        public string url { get; set; }
    }

    public class FrFR {
        public string type { get; set; }
        public string url { get; set; }
    }

    public class Image {
        [JsonProperty("de-DE")] public LocalizedImage deDE { get; set; }
        [JsonProperty("zh-Hant")] public LocalizedImage zhHant { get; set; }
        [JsonProperty("en-US")] public LocalizedImage enUS { get; set; }
        [JsonProperty("it-IT")] public LocalizedImage itIT { get; set; }
        [JsonProperty("pl-PL")] public LocalizedImage plPL { get; set; }
        [JsonProperty("ru-RU")] public LocalizedImage ruRU { get; set; }
        [JsonProperty("zh-Hans")] public LocalizedImage zhHans { get; set; }
        [JsonProperty("pt-BR")] public LocalizedImage ptBR { get; set; }
        [JsonProperty("es-ES")] public LocalizedImage esES { get; set; }
        [JsonProperty("ja-JP")] public LocalizedImage jaJP { get; set; }
        [JsonProperty("cs-CZ")] public LocalizedImage csCZ { get; set; }
        [JsonProperty("fr-FR")] public LocalizedImage frFR { get; set; }
        public string type { get; set; }
        public string url { get; set; }
    }
    
    public class LocalizedImage {
        public string type { get; set; }
        public string url { get; set; }
    }

    public class Links {
        public List<TrophyGroupLink> trophyGroups { get; set; }
        public List<TrophySetLink> trophySet { get; set; }
        public List<TrophyLink> trophies { get; set; }
        public List<ParentTrophyGroup> parentTrophyGroup { get; set; }
    }

    public class Metadata {
        public string trophyDefRevision { get; set; }
        public string trophySetVersion { get; set; }
        public string trophySchemaVersion { get; set; }
        public string defaultLanguage { get; set; }
        public Name name { get; set; }
        public List<Image> images { get; set; }
        public List<string> platform { get; set; }
        public string trophyGroupId { get; set; }
        public string sortKey { get; set; }
        public bool isBaseGameGroup { get; set; }
        public string trophyId { get; set; }
        public bool hidden { get; set; }
        public string grade { get; set; }
        public bool hasReward { get; set; }
        public string trophyGroupObjectId { get; set; }
        public Description description { get; set; }
        public Reward reward { get; set; }
        public UnlockCondition unlockCondition { get; set; }
        public string platinumTrophyObjectId { get; set; }
    }

    public class TrophyGroupMetadata {
        public string trophyGroupId { get; set; }
        public string sortKey { get; set; }
        public Name name { get; set; }
        public bool isBaseGameGroup { get; set; }
    }
    
    public class TrophyMetadata {
        public string trophyId { get; set; }
        public string sortKey { get; set; }
        public bool hidden { get; set; }
        public string grade { get; set; }
        public List<Image> images { get; set; }
        public bool hasReward { get; set; }
        public UnlockCondition unlockCondition { get; set; }
        public string trophyGroupObjectId { get; set; }
        public string platinumTrophyObjectId { get; set; }
        public Name name { get; set; }
        public Description description { get; set; }
        public Reward reward { get; set; }
    }

    public class TrophySetMetadata {
        public string trophyDefRevision { get; set; }
        public string trophySetVersion { get; set; }
        public string trophySchemaVersion { get; set; }
        public string defaultLanguage { get; set; }
        public Name name { get; set; }
        public List<Image> images { get; set; }
        public List<string> platform { get; set; }
    }

    public class Name {
        [JsonProperty("cs-CZ")] public string csCZ { get; set; }
        [JsonProperty("de-DE")] public string deDE { get; set; }
        [JsonProperty("en-US")] public string enUS { get; set; }
        [JsonProperty("es-ES")] public string esES { get; set; }
        [JsonProperty("fr-FR")] public string frFR { get; set; }
        [JsonProperty("it-IT")] public string itIT { get; set; }
        [JsonProperty("ja-JP")] public string jaJP { get; set; }
        [JsonProperty("pl-PL")] public string plPL { get; set; }
        [JsonProperty("pt-BR")] public string ptBR { get; set; }
        [JsonProperty("ru-RU")] public string ruRU { get; set; }
        [JsonProperty("zh-Hans")] public string zhHans { get; set; }
        [JsonProperty("zh-Hant")] public string zhHant { get; set; }
    }

    public class ParentTrophyGroup {
        public TrophyGroup @object { get; set; }
    }

    public class Reward {
        public Name name { get; set; }
    }

    public class Root {
        public string schemaVersion { get; set; }
        public string contextType { get; set; }
        public string contextId { get; set; }
        public Entities entities { get; set; }
    }

    public class Trophy {
        public string objectId { get; set; }
        public TrophyMetadata metadata { get; set; }
        public Links links { get; set; }
    }

    public class TrophyLink {
        public int position { get; set; }
        public Trophy @object { get; set; }
    }

    public class TrophyGroup {
        public string objectId { get; set; }
        public TrophyGroupMetadata metadata { get; set; }
        public Links links { get; set; }
    }

    public class TrophyGroupLink {
        public int position { get; set; }
        public TrophyGroup @object { get; set; }
    }

    public class TrophySet {
        public string objectId { get; set; }
        public TrophySetMetadata metadata { get; set; }
        public Links links { get; set; }
    }

    public class TrophySetLink {
        public TrophySet @object { get; set; }
    }

    public class UnlockCondition {
        public string udsStatName { get; set; }
        public string comparator { get; set; }
        public string targetValue { get; set; }
        public bool isProgressive { get; set; }
    }
}