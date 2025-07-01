using System;
using System.Globalization;
using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Saving.SaveSlots {
    public static class UnsavedSavesInfo {
        public static string GetUnsavedGameWarning() {
            SaveSlot active = SaveSlot.LastSaveSlotOfCurrentHero;

            if (active is {LastSavedTime: { }}) {
                return $"{LocTerms.PopupNotSavedWarning.Translate()} {GetLastSaveInfo(active.LastSavedTime.Value)}";
            }

            return LocTerms.PopupNotSavedWarning.Translate();
        }

        static string GetLastSaveInfo(DateTime lastSavedTime) {
            TimeSpan timeFromSave = DateTime.Now - lastSavedTime;
            return LocTerms.PopupLastSaveTime.Translate(timeFromSave.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture));
        }
    }
}