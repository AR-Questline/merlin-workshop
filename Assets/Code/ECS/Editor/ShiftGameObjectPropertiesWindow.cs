using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.ECS.DrakeRenderer.Authoring;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Awaken.ECS.Editor {
    public class ShiftGameObjectPropertiesWindow : OdinEditorWindow {
        [MenuItem("TG/Assets/Gameobject Properties Shifter", priority = 100)]
        static void OpenWindow() {
            var window = GetWindow<ShiftGameObjectPropertiesWindow>();
            window.Show();
        }
        
        [SerializeField, OnValueChanged(nameof(OnFromChange))]
        GameObject from;
        [SerializeField]
        GameObject to;
        
        const string PropertiesToShiftGroup = "Properties to Shift";
        const string TransformShiftGroup = PropertiesToShiftGroup + "/" + nameof(DisableTransformShiftToggles) + "/" + nameof(shiftTransform);
        
        [LabelText("Transform"), SerializeField, Space(10)]
        [BoxGroup(PropertiesToShiftGroup)]
        [HideIfGroup(PropertiesToShiftGroup + "/" + nameof(DisableTransformShiftToggles))]
        [ToggleGroup(TransformShiftGroup, 3, "Transform")]
        bool shiftTransform = true;
        [ToggleGroup(TransformShiftGroup), SerializeField]
        bool shiftPosition = true;
        [ToggleGroup(TransformShiftGroup), SerializeField]
        bool shiftRotation = true;
        [ToggleGroup(TransformShiftGroup), SerializeField, PropertySpace(0, spaceAfter: 10)]
        bool shiftScale = true;
        
        [BoxGroup("Properties to Shift"), SerializeField, ListDrawerSettings(IsReadOnly = true)]
        List<ComponentsToShift> componentsToShift = new();

        protected override void Initialize() {
            base.Initialize();
            OnFromChange();
        }

        void OnFromChange() {
            if (from == null) return;

            List<ComponentsToShift> newComponentsToShift = new List<ComponentsToShift>(componentsToShift.Count);
            foreach (Component component in from.GetComponents<Component>()) {
                if (component is Transform) continue;

                ComponentsToShift firstOrDefault = componentsToShift.FirstOrDefault(cts => cts.type == component.GetType());
                if (firstOrDefault == null) {
                    // We want the same settings to apply to the same component type, even if it's a different instance
                    ComponentsToShift firstOrDefaultNew = newComponentsToShift.FirstOrDefault(cts => cts.type == component.GetType());
                    if (firstOrDefaultNew == null) {
                        newComponentsToShift.Add(new ComponentsToShift() {
                            type = component.GetType(),
                            shouldShift = true
                        });
                    }
                } else {
                    newComponentsToShift.Add(firstOrDefault);
                }
            }

            componentsToShift = newComponentsToShift;
        }

        [Button(ButtonSizes.Medium, ButtonStyle.CompactBox), EnableIf(nameof(CanShift))]
        void ApplyShift() {
            Transform target = to.transform;
            
            if (DisableTransformShiftToggles) {
                shiftTransform = true;
                shiftPosition = true;
                shiftRotation = true;
                shiftScale = true;
            }
            
            if (shiftTransform) {
                if (shiftPosition) {
                    target.position = from.transform.position;
                    from.transform.localPosition = Vector3.zero;
                }
                if (shiftRotation) {
                    target.rotation = from.transform.rotation;
                    from.transform.localRotation = Quaternion.identity;
                }
                if (shiftScale) {
                    target.localScale = from.transform.localScale;
                    from.transform.localScale = Vector3.one;
                }
            }
            List<Component> components = new List<Component>();
            
            foreach (ComponentsToShift toShift in componentsToShift) {
                if (toShift.shouldShift) {
                    from.GetComponents(toShift.type, components);
                    foreach (Component c in components) {
                        ComponentUtility.CopyComponent(c);
                        ComponentUtility.PasteComponentAsNew(to);
                        DestroyImmediate(c);
                    }
                    components.Clear();
                }
            }

            FixDrakeRenderers(target);
        }

        static void FixDrakeRenderers(Transform transform) {
            DrakeLodGroup lodGroup = transform.GetComponent<DrakeLodGroup>();
            if (lodGroup != null) {
                foreach (var drakeMeshRenderer in transform.GetComponents<DrakeMeshRenderer>()) {
                    drakeMeshRenderer.EDITOR_AssignParent(lodGroup);
                }
            }
        }

        bool CanShift => to != null && from != null;
        bool DisableTransformShiftToggles => componentsToShift.Any(c => c.type == typeof(DrakeLodGroup) || c.type == typeof(DrakeMeshRenderer));

        [Serializable]
        class ComponentsToShift {
            [ReadOnly, ShowInInspector]
            public Type type;
            public bool shouldShift;
        }
    }
}