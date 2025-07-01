using System;

namespace Awaken.Utility.Times {
    public static class GameTimeUtil {
        /// <summary>
        /// Make human readable time description from game time
        /// </summary>
        /// <param name="gameTime">Time in game (eg. 1 - day, 0.5 - 12h)</param>
        /// <returns>Human readable time like: if time bigger than 1h -> 2 days, 1 hour, otherwise -> under hour</returns>
        [UnityEngine.Scripting.Preserve]
        public static string ToReadableString(float gameTime)
        {
            if (float.IsNaN(gameTime) || float.IsInfinity(gameTime)) {
                throw new ArgumentException("Game time is NaN or Infinity");
            }
            return ToReadableString(TimeSpan.FromDays(gameTime));
        }

        /// <summary>
        /// Make human readable time description from game time
        /// </summary>
        /// <param name="gameTimeSpan">Time in game converted to TimeSpan</param>
        /// <returns>Human readable time like: if time bigger than 1h -> 2 days, 1 hour, otherwise -> under hour</returns>
        public static string ToReadableString(TimeSpan gameTimeSpan)
        {
            string formatted = string.Format("{0}{1}",
                gameTimeSpan.Duration().Days > 0 ? $"{gameTimeSpan.Days:0} day{(gameTimeSpan.Days == 1 ? string.Empty : "s")}, " : string.Empty,
                gameTimeSpan.Duration().Hours > 0 ? $"{gameTimeSpan.Hours:0} hour{(gameTimeSpan.Hours == 1 ? string.Empty : "s")}, " : string.Empty);

            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

            if (string.IsNullOrEmpty(formatted)) formatted = "under hour";

            return formatted;
        }

        public static int DayOfTheWeek(int day) {
            return 1 + (day - 1) % 7;
        }

        public static int Week(int day) {
            return 1 + (day - 1) / 7;
        }

        public static int Month(int turn) {
            return 1 + (turn - 1) / 30;
        }

        /// <summary>
        /// Given turn A and turn B, this checks if a given period of time passed between them.
        /// For example when we check with Week span -> given turn 8 (week 2) and since 6 (week 1), this returns true
        /// </summary>
        public static bool HasTimeSpanChanged(int turn, int since, TimeSpans span) {
            if (since == 0 || span == TimeSpans.None) {
                return true;
            } else {
                return TimeSpansSince(turn, since, span) > 0;
            }
        }
        
        /// <summary>
        /// How many weeks/months/days has passed between 2 given turns.
        /// Note that between turn 7 and turn 8 there is 1 week difference! 
        /// </summary>
        public static int TimeSpansSince(int turn, int since, TimeSpans span) {
            switch (span) {
                case TimeSpans.Day:
                    return turn - since;
                case TimeSpans.Week: {
                    int currentWeek = GameTimeUtil.Week(turn);
                    int lastTakenWeek = GameTimeUtil.Week(since);
                    return currentWeek - lastTakenWeek;
                }
                case TimeSpans.Month: {
                    int currentMonth = GameTimeUtil.Month(turn);
                    int lastTakenMonth = GameTimeUtil.Month(since);
                    return currentMonth - lastTakenMonth;
                }
                case TimeSpans.Ever:
                case TimeSpans.Dialogue:
                    return since <= 0 ? 1 : 0;
                default:
                    return 0;
            }
        }
    }
}
