using System;
using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.UI.TitleScreen.Expansion {
    public static class ExpansionUtils {
        public static int GetDaysLeft(DateTime releaseDate) {
            DateTime todayUtc = DateTime.UtcNow.Date;
            DateTime releaseDayOnly = releaseDate.Date;

            return (releaseDayOnly - todayUtc).Days;
        }

        public static string GetTimeToReleaseText(int daysLeft) {
            return daysLeft switch {
                > 1 => LocTerms.ExpansionDaysToRelease.Translate(daysLeft),
                1 => LocTerms.ExpansionTomorrowRelease.Translate(),
                0 => LocTerms.ExpansionTodayRelease.Translate(),
                _ => LocTerms.ExpansionAvailableNow.Translate()
            };
        }

        public static string GetTimeToReleaseText(DateTime releaseDate) {
            return GetTimeToReleaseText(GetDaysLeft(releaseDate));
        }

        public static string GetAvailabilityText(int daysLeft) {
            return daysLeft switch {
                > 1 => LocTerms.ExpansionAvailableSoon.Translate(),
                _ => LocTerms.ExpansionAvailableNow.Translate()
            };
        }

        public static string GetAvailabilityText(DateTime releaseDate) {
            return GetAvailabilityText(GetDaysLeft(releaseDate));
        }
    }
}