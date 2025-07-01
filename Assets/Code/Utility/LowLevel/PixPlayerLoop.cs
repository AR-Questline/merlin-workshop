using System.Collections.Generic;
using Awaken.Utility.Collections;
using Pix;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;

namespace Awaken.Utility.LowLevel {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_GAMECORE) && (DEBUG || AR_DEBUG) && !DISABLE_NATIVE_PROFILING
    public class PixPlayerLoop {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize() {
            s_instance = new PixPlayerLoop();
        }

        static PixPlayerLoop s_instance;
        int _startedFrame = -1;
        bool _started = false;

        LogPoint<Initialization> _initialization;
        LogPoint<EarlyUpdate> _earlyUpdate;
        LogPoint<FixedUpdate> _fixedUpdate;
        LogPoint<PreUpdate> _preUpdate;
        LogPoint<Update> _update;
        LogPoint<PreLateUpdate> _preLateUpdate;
        LogPoint<PostLateUpdate> _postLateUpdate;

        PixPlayerLoop() {
            if (Configuration.GetBool("pix_full_player_loop")) {
                CreateFull();
            } else {
                CreateEssential();
            }
            CreateCommon();
        }

        void CreateFull() {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            playerLoop = ProcessPlayerLoopSystem(playerLoop);
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        static PlayerLoopSystem ProcessPlayerLoopSystem(PlayerLoopSystem playerLoopSystem) {
            var systems = playerLoopSystem.subSystemList;
            if (systems.IsNullOrEmpty()) {
                return playerLoopSystem;
            }
            var newSystems = new PlayerLoopSystem[systems.Length * 3];
            for (int i = 0; i < systems.Length; i++) {
                var system = systems[i];
                var name = system.type?.Name ?? "Unknown";

                var beginType = typeof(BeginPixLoopSample);
                var endType = typeof(EndPixLoopSample);
#if UNITY_EDITOR
                if (system.type != null) {
                    beginType = typeof(BeginPixLoopSample<>).MakeGenericType(system.type);
                    endType = typeof(EndPixLoopSample<>).MakeGenericType(system.type);
                }
#endif

                newSystems[i * 3 + 0] = new PlayerLoopSystem {
                    type = beginType,
                    updateDelegate = () => PixWrapper.StartEvent(Color.magenta, name),
                };
                newSystems[i * 3 + 1] = ProcessPlayerLoopSystem(systems[i]);
                newSystems[i * 3 + 2] = new PlayerLoopSystem {
                    type = endType,
                    updateDelegate = () => PixWrapper.EndEvent(),
                };
            }
            playerLoopSystem.subSystemList = newSystems;
            return playerLoopSystem;
        }

        void CreateEssential() {
            _initialization = new LogPoint<Initialization>(Color.blue);
            _earlyUpdate = new LogPoint<EarlyUpdate>(new Color(0.2f, 0.7f, 0.69f));
            _fixedUpdate = new LogPoint<FixedUpdate>(new Color(0.7f, 0.7f, 0.7f));
            _preUpdate = new LogPoint<PreUpdate>(new Color(1f, 0, 1f));
            _update = new LogPoint<Update>(Color.grey);
            _preLateUpdate = new LogPoint<PreLateUpdate>(Color.red);
            _postLateUpdate = new LogPoint<PostLateUpdate>(new Color(0.2f, 0.3f, 0.4f));
        }

        void CreateCommon() {
            RenderPipelineManager.beginContextRendering -= BeginContextRendering;
            RenderPipelineManager.beginContextRendering += BeginContextRendering;
            RenderPipelineManager.endContextRendering -= EndContextRendering;
            RenderPipelineManager.endContextRendering += EndContextRendering;

            PlayerLoopUtils.RemoveFromPlayerLoop<PixPlayerLoop, Initialization>();
            PlayerLoopUtils.RegisterToPlayerLoopBegin<PixPlayerLoop, Initialization>(InitializationLoop);
        }

        void BeginContextRendering(ScriptableRenderContext arg1, List<Camera> arg2) {
            PixWrapper.StartEvent(Color.yellow, "Rendering");
            _startedFrame = Time.frameCount;
        }

        void EndContextRendering(ScriptableRenderContext arg1, List<Camera> arg2) {
            if (_startedFrame == Time.frameCount) {
                PixWrapper.EndEvent();
            }
            _startedFrame = -1;
        }

        void InitializationLoop() {
            if (_started) {
                PixWrapper.EndEvent();
            }
            PixWrapper.StartEvent(Color.black, "FullFrame");
            _started = true;
        }

        class LogPoint<T> where T : struct {
            Color _color;
            string _name = typeof(T).Name;

            public LogPoint(Color color) {
                _color = color;
                PlayerLoopUtils.RemoveFromPlayerLoop<Begin, T>();
                PlayerLoopUtils.RegisterToPlayerLoopBegin<Begin, T>(PointBegin);
                PlayerLoopUtils.RemoveFromPlayerLoop<End, T>();
                PlayerLoopUtils.RegisterToPlayerLoopEnd<End, T>(PointEnd);
            }

            void PointBegin() {
                PixWrapper.StartEvent(_color, _name);
            }

            void PointEnd() {
                PixWrapper.EndEvent();
            }

            struct Begin {}

            struct End {}
        }

#if UNITY_EDITOR
        struct BeginPixLoopSample<T> {}
        struct EndPixLoopSample<T> {}
#endif
        struct BeginPixLoopSample {}
        struct EndPixLoopSample {}
    }
#endif
}
