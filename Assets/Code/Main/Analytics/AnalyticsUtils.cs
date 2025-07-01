#if !UNITY_GAMECORE && !UNITY_PS5
using System;
using System.Text.RegularExpressions;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Analytics {
    public static class AnalyticsUtils {
        [UnityEngine.Scripting.Preserve] static bool s_cheatsWasEnabledForHero;
        
        public static int HeroLevel => GetHeroLevel();
        public static float PlayTime => World.Only<GameRealTime>().PlayRealTime.TotalHours;
        
#if UNITY_EDITOR
        public static bool DisableDesignEvents => true;
        public static bool DisableProgressionEvents => true;
#else
        public static bool DisableDesignEvents => s_cheatsWasEnabledForHero;
        public static bool DisableProgressionEvents => false;
#endif
        
        public static string EventName(string name, int limit = 32) {
            if (string.IsNullOrWhiteSpace(name)) {
                name = "Invalid";
            }
            name = ShortenName(name, limit);
            return Regex.Replace(name, @"[^0-9a-zA-Z\._ ]", string.Empty);
        }

        public static string ShortenName(string name, int limit, string divider = "_") {
            if (name.Length <= 32) {
                return name;
            }
            
            string[] parts = name.Split(divider);

            string shortenedName = name;
            for (int i = 0; i < parts.Length - 1; i++) {
                shortenedName = shortenedName.Substring(parts[i].Length + 1);
                if (shortenedName.Length <= limit) {
                    break;
                }
            }

            if (shortenedName.Length > limit) {
                shortenedName = shortenedName.Substring(0, limit);
            }

            return shortenedName;
        }

        static int GetHeroLevel() {
            int lvl = Hero.Current.Level.ModifiedInt;
            if (lvl > 100) {
                lvl = 100;
            }
            return lvl;
        }

        public static void TrySendDesignEvent(string eventId, float value) {
            if (DisableDesignEvents) {
                return;
            }
            
            Log.Minor?.Info($"[DESIGN] {eventId} = {value}");
            SendDesignEvent(eventId, value);
        }
        
        public static void SendDesignEvent(string eventId, float value) {
            //GameAnalytics.NewDesignEvent(eventId, value);
        }
        
        public static void TrySendDesignEvent(string eventId) {
            if (DisableDesignEvents) {
                return;
            }
            Log.Minor?.Info($"[DESIGN] {eventId}");
            SendDesignEvent(eventId);
        }

        public static void SendDesignEvent(string eventId) {
            //GameAnalytics.NewDesignEvent(eventId);
        }

        public static void TrySendProgressionEvent(GAProgressionStatus progression, string id1, string id2, string id3, int value) {
            if (DisableProgressionEvents) {
                return;
            }
            Log.Minor?.Info($"[PROGRESS] {progression.ToString()}/{id1}/{id2}/{id3} = {value}");
            //GameAnalytics.NewProgressionEvent(progression, id1, id2, id3, value);
        }
        
        public static void CheatsEnabled(bool enabled) {
            s_cheatsWasEnabledForHero = enabled;
        }
    }
    
    public enum GAProgressionStatus
    {
        Undefined = 0,
        Start = 1,
        Complete = 2,
        Fail = 3
    }
}
#endif