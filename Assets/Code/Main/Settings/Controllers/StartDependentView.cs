using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Controllers {
    [NoPrefab]
    public abstract class StartDependentView<T> : View<T> where T : Model {
        protected override bool CanNestInside(View view) => false;

        void Awake() {
            AsyncAwake().Forget();
        }

        async UniTaskVoid AsyncAwake() {
            OnAwake();
            if (!Application.isPlaying) {
                return;
            }
            await AsyncUtil.CheckAndWaitUntil(() => World.EventSystem != null);
            ModelUtils.DoForFirstModelOfType<T>(m => World.BindView(m, this, removeAutomatically: true), this);
        }

        protected virtual void OnAwake() {}

        protected virtual void OnDestroy() {
            if (Application.isPlaying && GenericTarget != null) {
                Discard();
            }
        }
    }
}