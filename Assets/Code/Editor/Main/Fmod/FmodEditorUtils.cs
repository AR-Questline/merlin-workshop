using System.Collections.Generic;
using System.Linq;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using Unity.Collections;
using Debug = UnityEngine.Debug;
using GUID = FMOD.GUID;

namespace Awaken.TG.Editor.Main.Fmod {
    public static class FmodEditorUtils {
        
        public static void UnloadAllBanks() {
            // EditorUtils.System.unloadAll();
        }

        // public static void LoadAllBanks(out List<(Bank bank, string bankName)> banksDatas) {
        //     EventManager.ClearCache();
        //     EventManager.RefreshBanks();
        //
        //     banksDatas = new List<(Bank bank, string bankName)>(EventManager.Banks.Count);
        //     foreach (var bankRef in EventManager.Banks) {
        //         var status = FMODUnity.EditorUtils.System.loadBankFile(bankRef.Path, FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out var bank);
        //         if (status != RESULT.OK) {
        //             Debug.LogError($"Could not load bank {bankRef.Name}. Result: {status}");
        //             continue;
        //         }
        //
        //         banksDatas.Add((bank, bankRef.Name));
        //     }
        // }

        // public static void GetEventGuidToBankNameMap(List<(Bank bank, string bankName)> banksDatas, out Dictionary<GUID, List<string>> eventGuidToBanksNamesMap) {
        //     eventGuidToBanksNamesMap = new Dictionary<GUID, List<string>>();
        //
        //     foreach (var (bank, bankName) in banksDatas) {
        //         if (bank.getEventList(out var eventsDescriptions) != RESULT.OK) {
        //             continue;
        //         }
        //
        //         var eventsGuids = eventsDescriptions.Select(x => x.getID(out var guid) == RESULT.OK ? guid : default);
        //         foreach (GUID eventGuid in eventsGuids) {
        //             if (eventGuid == default) {
        //                 continue;
        //             }
        //
        //             if (eventGuidToBanksNamesMap.TryGetValue(eventGuid, out var eventBanksNames) == false) {
        //                 eventBanksNames = new List<string>(1);
        //                 eventGuidToBanksNamesMap.Add(eventGuid, eventBanksNames);
        //             }
        //             eventBanksNames.Add(bankName);
        //         }
        //     }
        // }

        // public static void GetEventGuidToPathMap(List<(Bank bank, string bankName)> banksDatas, out Dictionary<GUID, string> eventGuidToPathMap) {
        //     eventGuidToPathMap = new Dictionary<GUID, string>();
        //
        //     foreach (var (bank, _) in banksDatas) {
        //         if (bank.getEventList(out var eventsDescriptions) != RESULT.OK) {
        //             continue;
        //         }
        //
        //         foreach (var eventDescription in eventsDescriptions) {
        //             if (eventDescription.getID(out var guid) != RESULT.OK || guid == default || eventDescription.getPath(out var path) != RESULT.OK) {
        //                 continue;
        //             }
        //
        //             eventGuidToPathMap[guid] = path;
        //         }
        //     }
        // }

        // public static Bank GetBankWithName(List<(Bank bank, string bankName)> banksDatas, string name) {
        //     for (int i = 0; i < banksDatas.Count; i++) {
        //         var (bank, bankName) = banksDatas[i];
        //         if (bankName == name) {
        //             return bank;
        //         }
        //     }
        //     return default;
        // }

        // public static void GetBankEventsGuids(Bank bank, NativeHashSet<GUID> eventsGuids) {
        //     if (bank.getEventList(out var eventsDescriptions) != RESULT.OK) {
        //         return;
        //     }
        //
        //     foreach (var eventDescription in eventsDescriptions) {
        //         if (eventDescription.getID(out var guid) != RESULT.OK || guid == default) {
        //             continue;
        //         }
        //
        //         eventsGuids.Add(guid);
        //     }
        // }
    }
}