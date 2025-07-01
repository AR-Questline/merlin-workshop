using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Awaken.Utility.LowLevel {
    public class PlayerLoopBasedLifetime {
        Queue<ScheduledAction> _scheduledActions = new Queue<ScheduledAction>(32);
        bool _immediateMode;

        bool IsImmediateMode {
            [UnityEngine.Scripting.Preserve] get => _immediateMode;
            set {
                if (value) {
                    Run();
                }
                _immediateMode = value;
            }
        }

        // === Initialization
        public static PlayerLoopBasedLifetime Instance {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get;
            private set;
        }

        public static void Init() {
            if (Instance != null) return;
            Instance = new PlayerLoopBasedLifetime();
            PlayerLoopUtils.RegisterToPlayerLoopBegin<PlayerLoopBasedLifetime, Initialization>(Instance.Run);
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += static () => Instance.IsImmediateMode = true;
            UnityEditor.AssemblyReloadEvents.afterAssemblyReload += static () => Instance.IsImmediateMode = false;
            UnityEditor.EditorApplication.playModeStateChanged += static _ => {
                Instance._scheduledActions.Clear();
            };
#endif
        }

        public void ScheduleEnable(IWithPlayerLoopEnable enable) {
            if (_immediateMode) {
                enable.Enable();
                return;
            }
            _scheduledActions.Enqueue(new ScheduledAction(enable));
        }

        public void ScheduleDisable(IWithPlayerLoopDisable disable) {
            if (_immediateMode) {
                disable.Disable();
                return;
            }
            _scheduledActions.Enqueue(new ScheduledAction(disable));
        }

        void Run() {
            while (_scheduledActions.TryDequeue(out var action)) {
                if (action.actionType == ActionType.Enable) {
                    action.enable.Enable();
                } else if (action.actionType == ActionType.Disable) {
                    action.disable.Disable();
                }
            }
        }

        public interface IWithPlayerLoopEnable {
            public void Enable();
        }

        public interface IWithPlayerLoopDisable {
            public void Disable();
        }

        enum ActionType : byte {
            Enable,
            Disable,
        }

        [StructLayout(LayoutKind.Explicit)]
        readonly struct ScheduledAction {
            [FieldOffset(0)] public readonly ActionType actionType;
            [FieldOffset(8)] public readonly IWithPlayerLoopEnable enable;
            [FieldOffset(8)] public readonly IWithPlayerLoopDisable disable;

            public ScheduledAction(IWithPlayerLoopEnable enable) {
                disable = null;
                this.enable = enable;
                actionType = ActionType.Enable;
            }

            public ScheduledAction(IWithPlayerLoopDisable disable) {
                enable = null;
                this.disable = disable;
                actionType = ActionType.Disable;
            }
        }
    }
}
