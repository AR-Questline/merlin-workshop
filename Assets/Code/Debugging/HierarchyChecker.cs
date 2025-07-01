using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Debugging {
    /// <summary>
    /// Utility component to check who and when makes transforms hierarchy dirty.
    /// It should be used only for debugging and WILL interfere with other scripts which use <see cref="Transform.hasChanged"/>
    /// </summary>
    [TypeInfoBox("Utility component to check who and when makes transforms hierarchy dirty." +
                 " It should be used only for debugging and WILL interfere with other scripts which use Transform.hasChanged")]
    public class HierarchyChecker : MonoBehaviour {
        [ShowInInspector] int[] _changes = new int[10];
        [ShowInInspector] int _changesHead;

        [ShowInInspector] int _lastChange;
        [ShowInInspector] Transform _lastChangeTransform;
        [ShowInInspector] int CurrentFrame => Time.frameCount;

        void Start() {
#if !UNITY_EDITOR
            Destroy(this);
#endif
        }

#if UNITY_EDITOR
        async void Update() {
            if (!CheckSingleTransform(transform)) {
                CheckTransform(transform);
            }
            await UniTask.DelayFrame(0, PlayerLoopTiming.LastUpdate);
            ClearTransformChanged(transform);
        }
#endif

        bool CheckTransform(Transform transformToCheck) {
            for (var i = 0; i < transformToCheck.childCount; i++) {
                var child = transformToCheck.GetChild(i);
                if (CheckSingleTransform(child)) {
                    return true;
                }
            }

            for (var i = 0; i < transformToCheck.childCount; i++) {
                var child = transformToCheck.GetChild(i);
                if (CheckTransform(child)) {
                    return true;
                }
            }
            return false;
        }

        bool CheckSingleTransform(Transform transformToCheck) {
            if (!transformToCheck.hasChanged) {
                return false;
            }
            InsertChangeFrame(Time.frameCount, transformToCheck);
            return true;
        }

        void InsertChangeFrame(int frame, Transform subject) {
            _lastChange = frame;
            _lastChangeTransform = subject;
            if (_changes[_changesHead] == frame) {
                return;
            }
            _changesHead = (_changesHead + 1) % _changes.Length;
            _changes[_changesHead] = frame;
        }

        void ClearTransformChanged(Transform subject) {
            subject.hasChanged = false;
            for (var i = 0; i < subject.childCount; i++) {
                var child = subject.GetChild(i);
                ClearTransformChanged(child);
            }
        }
    }
}
