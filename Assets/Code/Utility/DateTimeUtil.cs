using System;
using System.Globalization;
using System.Linq;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Utility {
    public static class DateTimeUtil {
        public static string DateTimeToString(DateTime dateTime) {
            return dateTime.ToString(CultureInfo.InvariantCulture).Replace(" ", "_").Replace(".", "_").Replace(":", "_").Replace("/", "_");
        }

        public static DateTime? StringToDateTime(string text, bool logError = true) {
            try {
                int[] elements = text.Split('_').Take(6).Select(int.Parse).ToArray();
                DateTime date = new DateTime(elements[2], elements[0], elements[1], elements[3], elements[4], elements[5]);
                return date;
            } catch (Exception e) {
                if (logError) {
                    Log.Important?.Error($"Failed to parse dateTime: {text}\n{e.Message}");
                }
            }

            return null;
        }
    }
}