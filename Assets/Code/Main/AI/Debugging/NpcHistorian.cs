// Attention! That is a performance critical operation!!!
// Comment or uncomment the lines below to enable NpcHistorian to collect data.
//#define NPC_HISTORIAN
//#define NPC_HISTORIAN_GLOBAL
//#define NPC_HISTORIAN_LOCAL
//#define FORCE_SPAWN_NPC_HISTORIAN

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.Utility;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;


namespace Awaken.TG.Main.AI.Debugging {
    public class NpcHistorian : MonoBehaviour {
        
        public static bool Enabled { get; set; }

        [TableList(IsReadOnly = true, ShowPaging = true)] public List<Notification> all = new();
        [TableList(IsReadOnly = true, ShowPaging = true), Indent(1)] public List<Notification> locomotion = new();
        [TableList(IsReadOnly = true, ShowPaging = true), Indent(2)] public List<Notification> movement = new();
        [TableList(IsReadOnly = true, ShowPaging = true), Indent(2)] public List<Notification> rotation = new();
        [TableList(IsReadOnly = true, ShowPaging = true), Indent(1)] public List<Notification> states = new();
        [TableList(IsReadOnly = true, ShowPaging = true), Indent(1)] public List<Notification> interactions = new();
        [TableList(IsReadOnly = true, ShowPaging = true), Indent(1)] public List<Notification> animations = new();
        [TableList(IsReadOnly = true, ShowPaging = true), Indent(2)] public List<Notification> animationsGeneral = new();
        [TableList(IsReadOnly = true, ShowPaging = true), Indent(2)] public List<Notification> animationsAdditive = new();
        [TableList(IsReadOnly = true, ShowPaging = true), Indent(2)] public List<Notification> animationsCustom = new();
        [TableList(IsReadOnly = true, ShowPaging = true), Indent(2)] public List<Notification> animationsTopBody = new();

        void AppendAll(Notification notification) {
            all.Add(notification);
        }
        
        void AppendLocomotion(Notification notification) {
            locomotion.Add(notification);
            AppendAll(notification);
        }

        [UnityEngine.Scripting.Preserve]
        void AppendMovement(Notification notification) {
            movement.Add(notification);
            AppendLocomotion(notification);
        }
        
        [UnityEngine.Scripting.Preserve]
        void AppendRotation(Notification notification) {
            rotation.Add(notification);
            AppendLocomotion(notification);
        }

        [UnityEngine.Scripting.Preserve]
        void AppendStates(Notification notification) {
            states.Add(notification);
            AppendAll(notification);
        }

        [UnityEngine.Scripting.Preserve]
        void AppendInteractions(Notification notification) {
            interactions.Add(notification);
            AppendAll(notification);
        }
        
        void AppendAnimations(Notification notification) {
            animations.Add(notification);
            AppendAll(notification);
        }
        
        [UnityEngine.Scripting.Preserve]
        void AppendAnimationsGeneral(Notification notification) {
            animationsGeneral.Add(notification);
            AppendAnimations(notification);
        }
        
        [UnityEngine.Scripting.Preserve]
        void AppendAnimationsAdditive(Notification notification) {
            animationsAdditive.Add(notification);
            AppendAnimations(notification);
        }
        
        [UnityEngine.Scripting.Preserve]
        void AppendAnimationsCustom(Notification notification) {
            animationsCustom.Add(notification);
            AppendAnimations(notification);
        }
        
        [UnityEngine.Scripting.Preserve]
        void AppendAnimationsTopBody(Notification notification) {
            animationsTopBody.Add(notification);
            AppendAnimations(notification);
        }
        
        #if !NPC_HISTORIAN_GLOBAL
        [Conditional("NPC_HISTORIAN_LOCAL")] [UnityEngine.Scripting.Preserve]
        #endif
        public static void Notify(NpcElement npc, string message) {
            #if NPC_HISTORIAN
            HistorianToWrite(npc)?.AppendAll(new Notification(message));
            #endif
        }

        #if !NPC_HISTORIAN_GLOBAL
        [Conditional("NPC_HISTORIAN_LOCAL")] [UnityEngine.Scripting.Preserve]
        #endif
        public static void NotifyLocomotion(NpcElement npc, string message) {
            #if NPC_HISTORIAN
            HistorianToWrite(npc)?.AppendLocomotion(new Notification(message));
            #endif
        }
        
        #if !NPC_HISTORIAN_GLOBAL
        [Conditional("NPC_HISTORIAN_LOCAL")]
        #endif
        public static void NotifyMovement(NpcElement npc, string message) {
            #if NPC_HISTORIAN
            HistorianToWrite(npc)?.AppendMovement(new Notification(message));
            #endif
        }
        
        #if !NPC_HISTORIAN_GLOBAL
        [Conditional("NPC_HISTORIAN_LOCAL")]
        #endif
        public static void NotifyRotation(NpcElement npc, string message) {
            #if NPC_HISTORIAN
            HistorianToWrite(npc)?.AppendRotation(new Notification(message));
            #endif
        }

        #if !NPC_HISTORIAN_GLOBAL
        [Conditional("NPC_HISTORIAN_LOCAL")]
        #endif
        public static void NotifyStates(NpcElement npc, string message) {
            #if NPC_HISTORIAN
            HistorianToWrite(npc)?.AppendStates(new Notification(message));
            #endif
        }

        #if !NPC_HISTORIAN_GLOBAL
        [Conditional("NPC_HISTORIAN_LOCAL")]
        #endif
        public static void NotifyInteractions(NpcElement npc, string message) {
            #if NPC_HISTORIAN
            HistorianToWrite(npc)?.AppendInteractions(new Notification(message));
            #endif
        }
        
        #if !NPC_HISTORIAN_GLOBAL
        [Conditional("NPC_HISTORIAN_LOCAL")]
        #endif
        public static void NotifyAnimations(NpcAnimatorSubstateMachine machine, string message) {
            #if NPC_HISTORIAN
            var historian = HistorianToWrite(machine.ParentModel);
            if (machine is NpcGeneralFSM) {
                historian?.AppendAnimationsGeneral(new Notification(message));
            } else if (machine is NpcAdditiveFSM) {
                historian?.AppendAnimationsAdditive(new Notification(message));
            } else if (machine is NpcCustomActionsFSM) {
                historian?.AppendAnimationsCustom(new Notification(message));
            } else if (machine is NpcTopBodyFSM) {
                historian?.AppendAnimationsTopBody(new Notification(message));
            }
            #endif
        }

        [UnityEngine.Scripting.Preserve]
        static NpcHistorian HistorianToWrite(NpcElement npc) => Enabled ? GetHistorian(npc) : null;
        
        public static NpcHistorian GetHistorian(NpcElement npc) {
            var viewParent = npc?.ParentModel?.ViewParent;
            if (viewParent == null) {
                return null;
            }
            
            var historian = viewParent.GetComponent<NpcHistorian>();
#if FORCE_SPAWN_NPC_HISTORIAN
            historian ??= viewParent.AddComponent<NpcHistorian>();
#endif
            return historian;
        } 
        public static void Create(NpcElement npc) {
            var viewParent = npc?.ParentModel?.ViewParent;
            if (viewParent != null) {
                viewParent.AddComponent<NpcHistorian>();
            }
        }
        
        [Serializable]
        public struct Notification {
            const int WidthUnit = 40;
            [VerticalGroup("when"), TableColumnWidth(WidthUnit), DisplayAsString(false)] public float time;
            [VerticalGroup("when"), TableColumnWidth(WidthUnit), DisplayAsString(false)] public int frames;
            [VerticalGroup("what"), TableColumnWidth(5 * WidthUnit), HideLabel, DisplayAsString(false)] public string message;
            [VerticalGroup("where"), HideLabel, ListDrawerSettings(IsReadOnly = true, ShowPaging = false)] [UnityEngine.Scripting.Preserve] public List<FilePtr> stack;
            [HideInInspector] public string stackTrace;

            public Notification(string message) : this() {
                time = Time.time;
                frames = Time.frameCount;
                this.message = message;
                stackTrace = StackTraceUtility.ExtractStackTrace();
                stack = StackTraceUtils.StackTraceToFilePointers(stackTrace)
                    .Skip(2)
                    .ToList();
            }
        }
    }
}