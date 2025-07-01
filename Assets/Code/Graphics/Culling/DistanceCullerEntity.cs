using System.Diagnostics;
using Awaken.TG.Main.Cameras;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Graphics.Culling {
    public abstract class DistanceCullerEntity : MonoBehaviour {
        protected const string DebugFoldout = "Debug";
        
        public int id;

        // === Debug
#if DEBUG
        [FoldoutGroup(DebugFoldout), ShowInInspector] int _rangeGroup;
        [FoldoutGroup(DebugFoldout), ShowInInspector] float _rangeDistance;
        [FoldoutGroup(DebugFoldout), ShowInInspector] float _initVolume;

        DistanceCuller _distanceCuller;

        [FoldoutGroup(DebugFoldout), ShowInInspector] DistanceCullerData State => _distanceCuller?.State(id, this) ?? default;
        [FoldoutGroup(DebugFoldout), ShowInInspector] bool IsVisible => State.IsVisible();
        [FoldoutGroup(DebugFoldout), ShowInInspector] bool HasChange => State.HasChange();
        [FoldoutGroup(DebugFoldout), ShowInInspector] protected abstract bool RenderingState { get; }
        [FoldoutGroup(DebugFoldout), ShowInInspector] protected BoundsCorners Corners => _distanceCuller?.Corners(id, this) ?? default;

        [FoldoutGroup(DebugFoldout), ShowInInspector] protected abstract Bounds CurrentBounds { get; }
        [FoldoutGroup(DebugFoldout), ShowInInspector] float CurrentVolume => CurrentBounds.VolumeOfAverage();
        [FoldoutGroup(DebugFoldout), ShowInInspector, ReadOnly] int LastChangeFrame { get; set; }
        [FoldoutGroup(DebugFoldout), ShowInInspector, ReadOnly] bool LastChangeVisible { get; set; } = true;
        [FoldoutGroup(DebugFoldout), ShowInInspector] int CurrentFrame => Time.frameCount;
        [FoldoutGroup(DebugFoldout), ShowInInspector] public bool IsInValidState => LastChangeVisible == IsVisible && IsVisible == RenderingState;

        Camera MainCamera => World.Any<GameCamera>()?.MainCamera;
        [FoldoutGroup(DebugFoldout), ShowInInspector] float Distance => MainCamera ? Vector3.Distance(MainCamera.transform.position, transform.position) : float.NaN;
        [FoldoutGroup(DebugFoldout), ShowInInspector] float Dot => MainCamera ? Vector3.Dot(MainCamera.transform.forward, transform.position - MainCamera.transform.position) : float.NaN;
#endif

        [Conditional("DEBUG")]
        public void SetDebugData(
            DistanceCuller distanceCuller, int rangeGroup, float rangeDistanceSq, float initVolume) {
#if DEBUG
            _distanceCuller = distanceCuller;
            _rangeGroup = rangeGroup;
            _rangeDistance = rangeDistanceSq;
            _initVolume = initVolume;
#endif
        }
        
        [Conditional("DEBUG")]
        public void DebugChangePerformed(bool destination) {
#if DEBUG
            if (LastChangeVisible == destination) {
                Log.Important?.Error($"Changing state from {LastChangeVisible} to same", this);
            }
            LastChangeFrame = Time.frameCount;
            LastChangeVisible = destination;
#endif
        }

        void OnDrawGizmosSelected() {
#if DEBUG
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(CurrentBounds.center, CurrentBounds.size);

            Gizmos.color = Color.yellow;
            var cornersBounds = Corners.GetBounds();
            Gizmos.DrawWireCube(cornersBounds.center, cornersBounds.size);
#endif
        }
    }
}
