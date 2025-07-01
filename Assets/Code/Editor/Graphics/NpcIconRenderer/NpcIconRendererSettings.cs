using System;
using System.Collections.Generic;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Graphics.NpcIconRenderer {
    [InlineEditor(InlineEditorObjectFieldModes.Hidden)]
    public class NpcIconRendererSettings : ScriptableSingleton<NpcIconRendererSettings> {
        [OnValueChanged(nameof(TryUpdateBackgroundColor))]
        public Color backgroundColor = new (1, 1, 1, 0);
        public List<Entry> entries = new();
        
        void TryUpdateBackgroundColor() {
            NpcIconRenderingUtils.TryUpdateBackgroundColor(backgroundColor);
        }
    }

    [Serializable]
    public class Entry {
        [SerializeField, TemplateType(typeof(LocationTemplate)),OnValueChanged(nameof(TryUpdatePreview))] TemplateReference location;
        [SerializeField, Range(0, 360), OnValueChanged(nameof(TryUpdateRotation))] float rotY;
        [SerializeField, Range(0, 1), OnValueChanged(nameof(TryUpdateAnimDeltaTime))] float animDeltaTime;
        [SerializeField, OnValueChanged(nameof(TryUpdateCamera))] Vector3 cameraOffset;
        [SerializeField, OnValueChanged(nameof(TryUpdateState))] NpcStateType stateType;
        [SerializeField, OnValueChanged(nameof(TryUpdateWeaponRenderers))] bool renderWeapons = true;
        
        public Vector3 CameraOffset => cameraOffset;
        public float AnimDeltaTime => animDeltaTime;
        public NpcStateType StateType => stateType;
        public bool RenderWeapons => renderWeapons;
        
        public Quaternion GetRotation() => Quaternion.Euler(0, -rotY, 0);
        public LocationTemplate GetLocationTemplate() => location.Get<LocationTemplate>();

        [Button]
        public void Preview() {
            NpcIconRenderingUtils.TryUpdatePreview(this);
        }
        
        [Button]
        public void RenderIcon() {
            NpcIconRenderingUtils.RenderAndAssignIcon(GetLocationTemplate());
        }
        
        void TryUpdatePreview() {
            NpcIconRenderingUtils.TryUpdatePreview(this);
        }
        
        void TryUpdateRotation() {
            NpcIconRenderingUtils.TryUpdateRotation();
        }
        
        void TryUpdateAnimDeltaTime() {
            NpcIconRenderingUtils.TryUpdateAnimDelta();
        }

        void TryUpdateCamera() {
            NpcIconRenderingUtils.TryUpdateCamera();
        }

        void TryUpdateState() {
            NpcIconRenderingUtils.TryUpdateState();
        }

        void TryUpdateWeaponRenderers() {
            NpcIconRenderingUtils.TryUpdateWeaponRenderers();
        }
    }
}