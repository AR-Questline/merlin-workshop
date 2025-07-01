using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.TG.Main.SocialServices.DebugServices;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Enums;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Debug = UnityEngine.Debug;

namespace Awaken.TG.Main.SocialServices {
    public abstract class SocialService : IService {
        public bool AllowUploads { get; set; } = true;
        
        public static SocialService Get { get; private set; }
        
        public static void EDITOR_RuntimeReset() {
            Get = null;
        }
        
        public static SocialService CreateSocialService() {
            try {
                if (Get == null) {
                    Get = new DebugSocialService();
                }
                return Get;
            } catch (Exception e) {
                Debug.LogException(e);
                return null;
            }
        }

        /// <summary>
        /// This method happens before GameLanguage setting gets created, so we need to use indirect communication (Prefs).
        /// </summary>
        public static void SetCurrentGameLanguage() {

        }

        [UnityEngine.Scripting.Preserve]
        static bool TrySetSelectedLocale(string currentLang) {
            if (string.IsNullOrWhiteSpace(currentLang)) {
                return false;
            }
            
            Locale languageSet = LocalizationSettings.AvailableLocales.Locales.FirstOrDefault(l => l.LocaleName.Contains(currentLang, StringComparison.OrdinalIgnoreCase));
            if (languageSet != null) {
                LocalizationSettings.SelectedLocale = languageSet;
                return true;
            }

            return false;
        }

        public abstract void SetAchievement(string id, Action onSuccess = null);
        public abstract void SetAchievementProgress(string id, int value);
        public abstract ILeaderboard GetLeaderboard(string id);
        public abstract void LeaderboardAddToScore(string id, int value);
        [UnityEngine.Scripting.Preserve] public abstract void LeaderboardUpdateScore(string id, int value);
        [UnityEngine.Scripting.Preserve] public abstract void GetLeaderboardScore(string id, Action<int> callback, Action onFailure = null);
        protected string AchievementProgressId(string achievementID) => achievementID + "_stat";
        protected abstract bool HasDlc_Internal(DlcId dlcId);

        public bool HasDlc(DlcId dlcId) {
            return HasDlc_Internal(dlcId);
        }
    }

    public class LeaderboardLibrary : RichEnum {
        public string LeaderboardName { get; }
        public bool ShowRuneSelection { [UnityEngine.Scripting.Preserve] get; }
        public Func<string> LeaderboardId { get; }
        public SortingType SortingType { get; }
        public DisplayType DisplayType { get; }
        
        protected LeaderboardLibrary(string enumName, string leaderboardName, bool showRuneSelection = true, Func<string> leaderboardId = null, SortingType sortingType = SortingType.Descending, DisplayType displayType = DisplayType.Numeric) : base(enumName) {
            LeaderboardName = leaderboardName;
            ShowRuneSelection = showRuneSelection;
            LeaderboardId = leaderboardId;
            SortingType = sortingType;
            DisplayType = displayType;
        }

        public static readonly LeaderboardLibrary
            GamesWon = new(nameof(GamesWon), "GamesWon");

        public static IEnumerable<string> GetAllStaticLeaderboards() {
            yield return GamesWon.LeaderboardName;
        }
    }

    public static class LeaderboardUtils {
        [UnityEngine.Scripting.Preserve]
        public static string ToString(this LeaderboardLibrary leaderboard) {
            return leaderboard.LeaderboardId == null
                ? leaderboard.LeaderboardName
                : leaderboard.LeaderboardId();
        }
    }
}