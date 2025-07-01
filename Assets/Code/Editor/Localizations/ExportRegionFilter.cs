using System;

namespace Awaken.TG.Editor.Localizations {
    [Flags]
    public enum ExportRegionFilter : byte {
        None = 0,
        Prologue = 1 << 0,
        HoS = 1 << 1,
        Cuanacht = 1 << 2,
        Forlorn = 1 << 3,
        Unknown = 1 << 7,
        All = Prologue | HoS | Cuanacht | Forlorn | Unknown
    }

    public static class RegionFilterUtil {
        public static ExportRegionFilter GetRegionFrom(string scene) {
            return scene switch {
                "Prologue_Jail" => ExportRegionFilter.Prologue,
                "CampaignMap_HOS" => ExportRegionFilter.HoS,
                "CampaignMap_Cuanacht" => ExportRegionFilter.Cuanacht,
                "CampaignMap_Forlorn" => ExportRegionFilter.Forlorn,
                _ => ExportRegionFilter.None
            };
        }
    }
}