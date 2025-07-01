using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories.Journal.Conditions.Models;
using Awaken.TG.Main.Memories.Journal.ReadonlySerialized;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Memories.Journal.Conditions {
    [Serializable]
    public class KillCountCondition : Condition {
        [SerializeField, TemplateType(typeof(NpcTemplate))]
        TemplateReference[] targetsOrAbstracts = Array.Empty<TemplateReference>();

        [LabelWidth(150)]
        [SerializeField, InlineProperty] 
        SerializedReadonlyInt killCount;

        public int KillCount => killCount.Value;

        public override void Initialize(Model owner) {
            if (IsMet()) return;
            targetsOrAbstracts = targetsOrAbstracts.Where(t => t != null && t.IsSet).ToArray();
            if (InvalidSetup()) {
                Log.Important?.Info("Invalid setup for KillCountCondition");
                return;
            }
            
            if (!owner.TryGetElement(out KillCountRuntime dataModel)) {
                dataModel = owner.AddElement<KillCountRuntime>();
            }
            dataModel.RegisterCondition(this);
        }
        
        public override bool InvalidSetup() => killCount.Value <= 0 || targetsOrAbstracts == null || targetsOrAbstracts.All(t => t == null || !t.IsSet);

        public bool AppliesTo(NpcElement npc) {
            foreach (TemplateReference target in targetsOrAbstracts) {
                var targetTemplate = target.Get<NpcTemplate>();
                
                if (targetTemplate.IsAbstract) {
                    if (npc.Template.InheritsFrom(targetTemplate)) {
                        return true;
                    }
                } else if (npc.Template == targetTemplate) {
                    return true;
                }
            }

            return false;
        }
        
        public void OnKill(int kills, KillCountRuntime dataModel) {
            if (kills >= killCount.Value) {
                ConditionsMet();
                dataModel.UnregisterCondition(this);
            }
        }
        
#if UNITY_EDITOR
        public override string EDITOR_PreviewInfo() {
            if (InvalidSetup()) return "!!! Invalid setup !!!: " + base.EDITOR_PreviewInfo();
            string targetNames = string.Join(", ", targetsOrAbstracts.Select(t => t.TryGet<NpcTemplate>()?.name.Replace("NPCTemplate_", "")).WhereNotNull());
            return $"Kill {killCount.ToString()} of {targetNames}";
        }
        
        public List<NpcTemplate> EDITOR_ValidTargets => targetsOrAbstracts.Where(t => t != null && t.IsSet).Select(t => t.Get<NpcTemplate>()).WhereNotUnityNull().ToList();
        public bool EDITOR_HasValidTargets => EDITOR_ValidTargets.Count > 0;
#endif
    }
}
