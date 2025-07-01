using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Pathfinding;
using Sirenix.OdinInspector;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Debugging {
    public class AIDebug : MonoBehaviour {
        public bool draw = true;

        public bool drawViewCone = true;
        public static bool DrawViewCone { get; private set; }
        
        void OnDrawGizmos() {
        #if UNITY_EDITOR
            if (Application.isPlaying) {
                DrawViewCone = drawViewCone;
                
                if (draw) {
                    var color = UnityEditor.Handles.color;
                    var zTest = UnityEditor.Handles.zTest;
                    UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                
                    foreach (var ai in World.All<NpcAI>()) {
                        try { 
                            ai.Behaviour.OnDrawGizmos(new Data(ai.ParentModel));
                        } catch {
                            // ignored
                        }
                    }

                    UnityEditor.Handles.zTest = zTest;
                    UnityEditor.Handles.color = color;
                }
            } else {
                ResetFlags();
            }
        #endif
        }

        void OnDestroy() {
            ResetFlags();
        }

        public void ResetFlags() {
            DrawViewCone = false;
        }

        public class Data {
            public Vector3 elevatedPosition;
            public Vector3 headForward;
            public float visionMultiplier;

            public Data(NpcElement npc) {
                elevatedPosition = npc.Coords + new Vector3(0, 0.05f, 0);
                headForward = Perception.GetLookForward(npc, out visionMultiplier);
            }
        }

        [Button]
        void SetActiveAllVisuals(bool value) {
            foreach (var npc in FindObjectsByType<NpcController>(FindObjectsSortMode.None)) {
                npc.transform.Find("Visuals").gameObject.SetActive(value);
            }
        }

        [Button]
        void SetActiveAllMovement(bool value) {
            foreach (var npc in FindObjectsByType<NpcController>(FindObjectsSortMode.None)) {
                npc.enabled = value;
                npc.GetComponent<Seeker>().enabled = value;
                npc.GetComponent<FunnelModifier>().enabled = value;
            }
        }

        [Button]
        void PrintStates() {
            int working = 0;
            int notWorking = 0;
            foreach (var npc in World.All<NpcAI>()) {
                if (npc.Working) {
                    working++;
                } else {
                    notWorking++;
                }
            }
            
            Log.Important?.Info($"NpcWorking: {working}");
            Log.Important?.Info($"NpcNotWorking: {notWorking}");
        }
    }
}