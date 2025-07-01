using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniversalProfiling;

namespace Awaken.TG.Main.Saving.Models {
    public partial class SavingWorldMarker : Model {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;

        List<UniTask> _savingTasks = new();

        SavingWorldMarker() {
            CloudService.Get.BeginSaveBatch();
            UniversalProfiler.SetMarker(LoadSave.LoadSaveProfilerColor, "Saving.StartWorldSave");
            WaitForTask().Forget();
        }

        public static void Add(UniTask task, bool withUI) {
            SavingWorldMarker marker = ModelUtils.GetSingletonModel(() => new SavingWorldMarker());
            if (withUI && marker.View<VSavingWorldMarker>() == null) {
                World.SpawnView<VSavingWorldMarker>(marker, true);
            }
            
            marker.AddTask(task);
        }

        void AddTask(UniTask task) {
            _savingTasks.Add(task);
        }

        async UniTaskVoid WaitForTask() {
            await UniTask.NextFrame();
            while (_savingTasks.Any()) {
                UniTask savingTask = _savingTasks[0];
                if (savingTask.Status == UniTaskStatus.Pending) {
                    await savingTask;
                }

                _savingTasks.RemoveAt(0);
            }

            _savingTasks.Clear();
            UniversalProfiler.SetMarker(LoadSave.LoadSaveProfilerColor, "Saving.EndWorldSave");
            CloudService.Get.EndSaveBatch();
            PrefMemory.Save();
            Discard();
        }
    }
}