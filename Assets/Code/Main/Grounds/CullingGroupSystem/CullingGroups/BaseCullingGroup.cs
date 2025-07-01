using System;
using System.Runtime.CompilerServices;
using System.Linq;
using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.Utility.Threads;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups {
    public abstract class BaseCullingGroup : IDisposable {
        const int DefaultRegistrarSize = 10000;

        //Resources
        CullingGroup _generatedGroup;
        Registrar _registrar;

        //Config
        float[] OriginalBands { get; set; }
        float[] Bands { get; set; }
        bool WithHysteresis { get; set; }
        protected float HysteresisPercent { get; private set; }
        [UnityEngine.Scripting.Preserve] public bool IsAnyPaused => _registrar.IsAnyPaused;

        //State

        // === Group construction

        protected BaseCullingGroup(float[] distanceBands, float hysteresisPercent, int registrarInitialSize = DefaultRegistrarSize) {
            OriginalBands = distanceBands;
            SetDistanceBands(distanceBands, hysteresisPercent);
            Initialize(registrarInitialSize);
        }

        void SetDistanceBands(float[] distanceBands, float hysteresisPercent) {
            WithHysteresis = hysteresisPercent > 0;
            HysteresisPercent = hysteresisPercent;
            if (WithHysteresis) {
                Bands = new float[distanceBands.Length * 2];
                for (int i = 0; i < Bands.Length; i += 2) {
                    Bands[i] = distanceBands[i / 2];
                    Bands[i + 1] = distanceBands[i / 2] * (1 + hysteresisPercent);
                }
            } else {
                Bands = distanceBands;
            }
        }

        void Initialize(int registrarInitialSize) {
            if (_registrar != null) return;

            _generatedGroup = new();

            CameraStateStack.TryGetCamera(out Camera cam);
            if (cam == null) {
                Log.Critical?.Error("No camera found, CullingGroup will not work");
                return;
            }

            World.Services.Get<UnityUpdateProvider>().RegisterCullingGroup(this);

            _generatedGroup.targetCamera = cam;

            _generatedGroup.SetBoundingDistances(Bands);
            _generatedGroup.SetDistanceReferencePoint(cam.transform);

            // Initialization finalized
            _registrar = new(_generatedGroup, registrarInitialSize);
        }

        // === Public
        protected void RefreshDistanceBands(float[] distanceBands, float hysteresisPercent) {
            if (_generatedGroup == null) {
                return;
            }

            SetDistanceBands(distanceBands, hysteresisPercent);
            _generatedGroup.SetBoundingDistances(Bands);
        }

        protected float[] GetOriginalBands(float multiplier = 1) => OriginalBands.Select(band => band * multiplier).ToArray();

        public void Register(Registree registree) {
            if (_registrar.NoElements) {
                _generatedGroup.onStateChanged -= StateChangedMethod;
                _generatedGroup.onStateChanged += StateChangedMethod;
            }

            _registrar.AddRegistrySphereRef(registree);
        }

        public void Unregister(Registree registree) {
            if (_registrar.TryRemoveRegistrySphereRef(registree) && _registrar.NoElements) {
                _generatedGroup.onStateChanged -= StateChangedMethod;
            }
        }

        public void ScheduleUpdateAll() => _registrar.ScheduleUpdateAll(WithHysteresis);

        public void UnityUpdate() => _registrar.RunScheduledUpdates();

        public void UpdatePosition(Registree owner, Vector3 newPosition) {
            _registrar.UpdatePosition(owner, newPosition);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PauseCurrentElements() => _registrar.PauseAllUnpausedElements();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnpauseAllElements() => _registrar.UnpauseAllPausedElements();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnpauseElement(ICullingSystemRegistree element) => _registrar.UnpauseElement(element);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PauseElement(ICullingSystemRegistree element) => _registrar.PauseElement(element);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetElementPausedStatus(ICullingSystemRegistree element, out bool isPaused) => _registrar.TryGetElementPausedStatus(element, out isPaused);
        // === Group effects
        void StateChangedMethod(CullingGroupEvent ev) {
            Registree registree = _registrar.GetRegistreeFromIndex(ev.index);
            if (registree == null) {
                Log.Important?.Error("StructuralError");
                return;
            }

            int currentDistanceBand = ev.currentDistance;

            if (WithHysteresis) {
                currentDistanceBand = HysteresisToBand(currentDistanceBand, registree.CurrentDistanceBand);
            }

            // Unity can send 0->0 event, but our logic distinguishes between 0 band and 0 from default
            // So we don't care about Unity oldDistance, we only care if "our logic" values change
            _registrar.ScheduleUpdate(registree, currentDistanceBand);
        }

        public static int HysteresisToBand(int currentDistanceBand, int previousDistanceBand) {
            // Hysteresis bands are always odd
            if (currentDistanceBand % 2 == 1) {
                if (currentDistanceBand > previousDistanceBand * 2) {
                    currentDistanceBand -= 1;
                } else {
                    currentDistanceBand += 1;
                }
            }

            currentDistanceBand /= 2;
            return currentDistanceBand;
        }

        // === Disposal
        public virtual void Dispose() {
            ThreadSafeUtils.AssertMainThread();
            if (_registrar != null) {
                _registrar.Dispose();
            }
            if (_generatedGroup != null) {
                _generatedGroup.onStateChanged -= StateChangedMethod;
                _generatedGroup.Dispose();
                _generatedGroup = null;
                World.Services.Get<UnityUpdateProvider>().UnregisterCullingGroup(this);
            }
        }
    }
}