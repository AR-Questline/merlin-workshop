using Awaken.TG.Main.AI.Idle;
using Awaken.TG.Main.AI.Idle.Data.Attachment;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Editor.Main.AI {
    
    [CustomEditor(typeof(IdleDataAttachment))]
    public class IdleDataAttachmentEditor : OdinEditor {
        IdleDataAttachment Attachment => target as IdleDataAttachment;

        void OnSceneGUI() {
            IdleDataAttachment attachment = Attachment;
            if (Selection.activeObject != attachment.gameObject) {
                return;
            }

            if (attachment.IsEmbed) {
                if (attachment.EmbedData.behaviours != null) {
                    for (int i = 0; i < attachment.EmbedData.behaviours.Length; i++) {
                        InteractionIntervalData.EDITOR_Accessor accessor;
                        ref var interval = ref attachment.EmbedData.behaviours[i];
                        ref var data = ref accessor.Data(ref interval);
                        interval.CacheData();
                        var actionName = $"{interval.name}\n[{data.type}]\n{accessor.StartHours(ref interval):00}:{accessor.StartMinutes(ref interval):00}";
                        OnSceneGUI(attachment, actionName, ref data, out bool changed);
                        if (changed) {
                            interval.FlushData();
                        }
                    }
                }
                if (attachment.EmbedData.customActions != null) {
                    for (int i = 0; i < attachment.EmbedData.customActions.Length; i++) {
                        ref var customAction = ref attachment.EmbedData.customActions[i];
                        ref var data = ref customAction.action;
                        var actionName = $"{customAction.name}\nCustom: [{data.type}]";
                        OnSceneGUI(attachment, actionName, ref data, out _);
                    }
                }
            }
        }

        void OnSceneGUI(IdleDataAttachment attachment, string actionName, ref InteractionData data, out bool changed) {
            changed = false;
            var worldPosition = GetWorldPosition(attachment, data.position);
            
            if (data.HasPosition) {
                EditorGUI.BeginChangeCheck();
                Vector3 newWorldPosition = Handles.PositionHandle(worldPosition, Quaternion.identity);
                if (EditorGUI.EndChangeCheck() && !Application.isPlaying) {
                    newWorldPosition = Ground.SnapNpcToGround(newWorldPosition);
                    Undo.RecordObject(attachment, "Change idleBehaviour position");
                    SetLocalPosition(attachment, ref data.position, newWorldPosition);
                    changed = true;
                }
            }
            
            if (data.HasRange) {
                EditorGUI.BeginChangeCheck();
                
                var previousZTest = Handles.zTest;
                var previousColor = Handles.color;

                float newRange = data.range;
                DrawRangeHandle(worldPosition, ref newRange, CompareFunction.Greater, Color.gray);
                DrawRangeHandle(worldPosition, ref newRange, CompareFunction.Less, Color.white);

                Handles.zTest = previousZTest;
                Handles.color = previousColor;
                
                if (EditorGUI.EndChangeCheck() && !Application.isPlaying) {
                    Undo.RecordObject(attachment, "Change idleBehaviour range");
                    data.range = newRange;
                    changed = true;
                }
            }

            Handles.Label(worldPosition, actionName);

        }

        void DrawRangeHandle(Vector3 position, ref float range, CompareFunction zTest, Color color) {
            Handles.zTest = zTest;
            Handles.color = color;
            range = Handles.RadiusHandle(Quaternion.identity, position, range);
        }
        
        
        // === Debug/Editor
        Vector3 GetWorldPosition(IdleDataAttachment attachment, in IdlePosition position) {
            if (Application.isPlaying) {
                var location = RetrieveLocation(attachment);
                var data = location.Element<IdleDataElement>();
                var npcLocation = location.TryGetElement(out NpcPresence presence)
                    ? presence.AliveNpc?.ParentModel ?? location
                    : location;
                return position.WorldPosition(npcLocation, data);
            } else {
                return position.IdleSpace == IdlePosition.Space.World ? position.position : attachment.transform.position + position.position;
            }
        }

        void SetLocalPosition(IdleDataAttachment attachment, ref IdlePosition position, Vector3 worldPosition) {
            if (position.IdleSpace == IdlePosition.Space.World) {
                position.position = worldPosition;
            } else {
                position.position = worldPosition - attachment.transform.position;
            }
        }

        static Location RetrieveLocation(IdleDataAttachment attachment) {
            string id = attachment.GetComponentInParent<LocationSpec>().GetLocationId();
            return World.ByID<Location>(id);
        }
    }
}