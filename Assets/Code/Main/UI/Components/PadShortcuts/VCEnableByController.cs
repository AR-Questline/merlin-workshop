using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Rewired;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.UI.Components.PadShortcuts {
    public class VCEnableByController : ViewComponent<Model> {
        [SerializeField] List<GameObject> byJoystick;
        [SerializeField] List<GameObject> byKeyboard;
        [SerializeField] bool checkIfOnlyOneActive = true;
        [SerializeField, ShowIf(nameof(checkIfOnlyOneActive)), CanBeNull] Transform checkParent;
        
        ControllerType _previousControllerType;
        bool _refreshed;

        void Awake() {
            ForceReset();
        }

        protected override void OnAttach() {
            World.EventSystem.ListenTo(EventSelector.AnySource, Focus.Events.ControllerChanged, this, RefreshActivity);
            Refresh().Forget();
        }
        
        void OnEnable() {
            Refresh().Forget();
        }

        void OnDisable() {
            ForceReset();
            _refreshed = false;
        }

        async UniTaskVoid Refresh() {
            if (await AsyncUtil.DelayFrame(Target)) {
                RefreshActivity(RewiredHelper.IsGamepad ? ControllerType.Joystick : ControllerType.Keyboard);
            }
        }
        
        void RefreshActivity(ControllerType controllerType) {
            if (_previousControllerType == controllerType && _refreshed) return;
            
            if (CanRefresh()) {
                byJoystick.ForEach(go => go.SetActiveOptimized(controllerType == ControllerType.Joystick));
                byKeyboard.ForEach(go => go.SetActiveOptimized(controllerType == ControllerType.Keyboard));
                
                _refreshed = true;
                _previousControllerType = controllerType;
            }
        }
        
        void ForceReset() {
            byJoystick.ForEach(go => go.SetActiveOptimized(false));
            byKeyboard.ForEach(go => go.SetActiveOptimized(false));
        }
        
        bool CanRefresh() {
            if (checkIfOnlyOneActive == false) return true;
            
            checkParent = checkParent ? checkParent : transform;
            int childActiveCount = checkParent.Cast<Transform>().Count(child => child.gameObject.activeInHierarchy);
            return childActiveCount > 1;
        }
    }
}