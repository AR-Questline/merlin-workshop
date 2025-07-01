using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using UniversalProfiling;

namespace Awaken.TG.Main.Utility {
    public static class FmodRuntimeManagerUtils {
        static readonly UniversalProfilerMarker LoadSoundBanksMarker = new("FmodRuntimeManagerUtils.LoadUsedSoundBanks");
        static readonly UniversalProfilerMarker UnloadSoundBanksMarker = new("FmodRuntimeManagerUtils.UnloadUsedSoundBanks");

        // public static void LoadSoundBanks(IReadOnlyList<string> soundBanksNames, ref UnsafeList<Bank> loadingBanks, Allocator allocator) {
        //     LoadSoundBanksMarker.Begin();
        //     foreach (string soundBankName in soundBanksNames) {
        //         //LoadSoundBank(soundBankName, ref loadingBanks, allocator, soundBanksNames.Count);
        //     }
        //     LoadSoundBanksMarker.End();
        // }

        // public static void LoadSoundBank(string soundBankName, ref UnsafeList<Bank> loadingBanks, Allocator allocator, int capacity = 1) {
        //     var bank = RuntimeManager.LoadBank(soundBankName, loadBankFlags: LOAD_BANK_FLAGS.NONBLOCKING);
        //     if (bank.isValid() && bank.getLoadingState(out LOADING_STATE loadingState) == RESULT.OK && loadingState == LOADING_STATE.LOADING) {
        //         if (loadingBanks.IsCreated == false) {
        //             loadingBanks = new UnsafeList<Bank>(capacity, allocator);
        //         }
        //         loadingBanks.Add(bank);
        //     }
        // }
        
        public static void LoadSoundBanksAsyncAndForget(params string[] soundBanksNames) {
            LoadSoundBanksMarker.Begin();
            foreach (string soundBankName in soundBanksNames) {
                //RuntimeManager.LoadBank(soundBankName, loadBankFlags: LOAD_BANK_FLAGS.NONBLOCKING);
            }
            LoadSoundBanksMarker.End();
        }

        public static void UnloadSoundBanks(params string[] soundBanksNames) {
            UnloadSoundBanksMarker.Begin();
            foreach (string soundBankName in soundBanksNames) {
                //RuntimeManager.UnloadBank(soundBankName);
            }

            UnloadSoundBanksMarker.End();
        }
        
        public static void UnloadSoundBanks(IEnumerable<string> soundBanksNames) {
            UnloadSoundBanksMarker.Begin();
            foreach (string soundBankName in soundBanksNames) {
                //RuntimeManager.UnloadBank(soundBankName);
            }

            UnloadSoundBanksMarker.End();
        }
        
        // public static async UniTask<(int waitedFramesCount, bool loadedAll)> WaitForBanksFinishLoading(UnsafeList<Bank>.ReadOnly loadingBanks, int maxFramesToWaitBanksLoad) {
        //     if (loadingBanks.Length <= 0) {
        //         return (0, true);
        //     }
        //
        //     int waitedFramesCount = 0;
        //     bool loadedAll;
        //     do {
        //         loadedAll = true;
        //         for (int i = 0; i < loadingBanks.Length; i++) {
        //             // Bank bank;
        //             // unsafe {
        //             //     bank = loadingBanks.Ptr[i];
        //             // }
        //             // if (bank.getLoadingState(out var loadingState) == RESULT.OK && loadingState == LOADING_STATE.LOADING) {
        //             //     loadedAll = false;
        //             //     break;
        //             // }
        //         }
        //         
        //         if ((waitedFramesCount == maxFramesToWaitBanksLoad) | loadedAll) {
        //             break;
        //         }
        //         await UniTask.DelayFrame(1);
        //         waitedFramesCount++;
        //     } while (true);
        //
        //     return (waitedFramesCount, loadedAll);
        // }
    }
}