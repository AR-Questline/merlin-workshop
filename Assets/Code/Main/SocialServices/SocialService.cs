using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.PackageUtilities.CommonInterfaces;
using Awaken.TG.Main.SocialServices.DebugServices;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Enums;
using Awaken.Utility.Extensions;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Debug = UnityEngine.Debug;
#if !DEBUG
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Memories;
#endif

#if MICROSOFT_GAME_CORE || UNITY_GAMECORE
using Awaken.TG.Main.General;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.SocialServices.MicrosoftServices;
#endif

#if UNITY_PS5
using Awaken.TG.Main.SocialServices.PlayStationServices;
#endif

// #if !UNITY_GAMECORE && !UNITY_PS5 && !MICROSOFT_GAME_CORE
// using Awaken.TG.Main.SocialServices.GOGServices;
// using Awaken.TG.Main.SocialServices.SteamServices;
// #endif

// #if !DEBUG && !UNITY_GAMECORE && !UNITY_PS5 && !MICROSOFT_GAME_CORE
// using Galaxy.Api;
// using Steamworks;
// #endif


namespace Awaken.TG.Main.SocialServices {
    public abstract class SocialService : IService {
#if UNITY_EDITOR || AR_DEBUG || DLC_DEBUG
        public static DlcCategoryFlags debugDlcEnabled = DlcCategoryFlags.All;
#endif
        public bool AllowUploads { get; set; } = true;

        public static SocialService Get { get; private set; }

        public static void EDITOR_RuntimeReset() {
            Get = null;
        }

        public static SocialService CreateSocialService() {
            try {
                if (Application.version.Contains("b")) {
                    Get = new DebugSocialService();
                    return Get;
                }
// #if MICROSOFT_GAME_CORE || UNITY_GAMECORE
//                 Get = new MicrosoftSocialService();
// #elif UNITY_PS5
//                 Get = new PlayStationSocialService();
// #else
//                 if (PlatformUtils.IsSteamInitialized) {
//                     Get = new SteamSocialService();
//                 } else if (GogGalaxyManager.IsInitialized()) {
//                     Get = new GOGSocialService();
//                 }
// #endif
#if UNITY_EDITOR
                if (Get == null) {
                    Get = new DebugSocialService();
                }
#endif
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
#if !DEBUG
// #if !UNITY_STANDALONE_WIN || LOCALIZATION_TESTS
//             string chosenLanguage = PrefMemory.GetString(GameLanguage.ChosenLanguageKey);
//             if (TrySetSelectedLocale(chosenLanguage)) {
//                 return;
//             }
// #endif
// #if MICROSOFT_GAME_CORE || UNITY_GAMECORE
//             string currentLang = MicrosoftManager.Instance.GetGameLanguage().ToLower();
//             TrySetSelectedLocale(currentLang);
// #elif UNITY_PS5
//             string currentLang = PlayStationUtils.GetGameLanguage();
//             TrySetSelectedLocale(currentLang);
// #else
//             if (Application.version.Contains("b")) {
//                 return;
//             }
//
//             try {
//                 if (PlatformUtils.IsSteamInitialized) {
//                     string currentLang = SteamApps.GetCurrentGameLanguage();
//                     if (currentLang.ToLower() == "schinese") {
//                         currentLang = "Chinese (Simplified)";
//                     } else if (currentLang.ToLower() == "tchinese") {
//                         currentLang = "Chinese (Traditional)";
//                     } else if (currentLang.ToLower() == "brazilian") {
//                         currentLang = "portuguese";
//                     }
//                     TrySetSelectedLocale(currentLang);
//                 } else if (GogGalaxyManager.IsInitialized()) {
//                     string currentLang = GalaxyInstance.Apps().GetCurrentGameLanguage().ToLower();
//                     if (currentLang.ToLower() == "schinese") {
//                         currentLang = "Chinese (Simplified)";
//                     } else if (currentLang.ToLower() == "tchinese") {
//                         currentLang = "Chinese (Traditional)";
//                     } else if (currentLang.ToLower().Contains("brazilian")) {
//                         currentLang = "portuguese";
//                     }
//                     TrySetSelectedLocale(currentLang);
//                 }
//             } catch (Exception e) {
//                 Debug.LogException(e);
//                 if (LocalizationSettings.SelectedLocale != null) {
//                     ILocalizationManager.Current.SwitchLanguage(LocalizationSettings.SelectedLocale.Identifier);
//                 }
//             }
// #endif
// #else
//             ILocalizationManager.Current.SwitchLanguage(LocalizationSettings.SelectedLocale.Identifier);
#endif
        }

        [UnityEngine.Scripting.Preserve]
        static bool TrySetSelectedLocale(string currentLang) {
            if (string.IsNullOrWhiteSpace(currentLang)) {
                return false;
            }

            Locale languageSet = LocalizationSettings.AvailableLocales.Locales.FirstOrDefault(l => l.LocaleName.Contains(currentLang, StringComparison.OrdinalIgnoreCase));
            if (languageSet != null) {
                LocalizationSettings.SelectedLocale = languageSet;
                // ILocalizationManager.Current.SwitchLanguage(languageSet.Identifier);
                return true;
            } else {
                Debug.LogError($"Language not found: {currentLang}");
                return false;
            }
        }

        public abstract void SetAchievement(string id, Action onSuccess = null);
        public abstract void SetAchievementProgress(string id, int value);
        public abstract ILeaderboard GetLeaderboard(string id);
        public abstract void LeaderboardAddToScore(string id, int value);

        [UnityEngine.Scripting.Preserve]
        public abstract void LeaderboardUpdateScore(string id, int value);

        [UnityEngine.Scripting.Preserve]
        public abstract void GetLeaderboardScore(string id, Action<int> callback, Action onFailure = null);

        protected string AchievementProgressId(string achievementID) => achievementID + "_stat";
        public abstract UniTask ShowStorePage(DlcId dlcId);
        public abstract UniTask<PurchaseResult> ShowPurchaseDialog(DlcId dlcId);
        public virtual UniTask RecollectAddOns() => UniTask.CompletedTask;
        protected abstract bool HasDlc_Internal(DlcId dlcId);

        public bool HasDlc(DlcCategory category) {
#if UNITY_EDITOR || AR_DEBUG || DLC_DEBUG
            return debugDlcEnabled.HasFlagFast(category.ToFlags());
#else
            DlcId? dlcId = DlcId.GetDlcId(category);
            return dlcId != null && HasDlc_Internal(dlcId.Value);
#endif
        }

        public bool HasDlc(DlcCategoryFlags categories) {
#if UNITY_EDITOR || AR_DEBUG || DLC_DEBUG
            return debugDlcEnabled.HasCommonBitsFast(categories);
#else
            List<DlcId?> dlcId = DlcId.GetDlcId(categories);
            bool hasDlc = false;
            foreach (var id in dlcId) {
                if (id.HasValue && HasDlc_Internal(id.Value)) {
                    hasDlc = true;
                    break;
                }
            }
            return hasDlc;

#endif
        }
    }

    public class LeaderboardLibrary : RichEnum {
        public string LeaderboardName { get; }
        public bool ShowRuneSelection { [UnityEngine.Scripting.Preserve] get; }
        public Func<string> LeaderboardId { get; }
        public SortingType SortingType { get; }
        public DisplayType DisplayType { get; }

        protected LeaderboardLibrary(string enumName, string leaderboardName, bool showRuneSelection = true,
            Func<string> leaderboardId = null, SortingType sortingType = SortingType.Descending,
            DisplayType displayType = DisplayType.Numeric) : base(enumName) {
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