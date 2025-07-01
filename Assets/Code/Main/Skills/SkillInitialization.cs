using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.MVC;
using Unity.Collections;

namespace Awaken.TG.Main.Skills {
    public static class SkillInitialization {
        static readonly List<SkillReference> ReusableReferences = new();
        static readonly List<Skill> ReusableSkillsToDelete = new();
        
        public static void Initialize(ISkillOwner owner, IEnumerable<SkillReference> data, SkillState state) {
            foreach (var reference in data) {
                CreateSkill(owner, reference, state);
            }
        }
        
        public static void Initialize(ISkillOwner owner, IEnumerable<(SkillReference, SkillVariablesOverride)> data, SkillState state) {
            foreach (var (reference, variableOverride) in data) {
                CreateSkill(owner, reference, variableOverride, state);
            }
        }
        
        public static void CustomRestore(ISkillOwner owner, IEnumerable<SkillReference> data, SkillState state) {
            MarkForManualCustomRestore(owner);
            owner.ListenToLimited(Model.Events.BeforeFullyInitialized, _ => {
                ManualCustomRestore(owner, data, state);
            }, owner);
        }
        
        public static void MarkForManualCustomRestore(ISkillOwner owner) {
            foreach (var skill in owner.Elements<Skill>()) {
                skill.MarkForCustomRestore();
            }
        }
        
        public static void ManualCustomRestore(ISkillOwner owner, IEnumerable<SkillReference> references, in SkillState state) {
            ReusableReferences.Clear();
            ReusableSkillsToDelete.Clear();
            
            ReusableReferences.AddRange(references);
            var toDelete = ReusableSkillsToDelete;

            foreach (var skill in owner.Elements<Skill>()) {
                if (TryPopReferenceFor(ReusableReferences, skill, out var reference)) {
                    skill.CustomRestore(reference);
                } else {
                    toDelete.Add(skill);
                }
            }

            foreach (var skill in toDelete) {
                skill.Discard();
            }

            foreach (var reference in ReusableReferences) {
                CreateSkill(owner, reference, state);
            }

            ReusableReferences.Clear();
            ReusableSkillsToDelete.Clear();
        }

        static bool TryPopReferenceFor(List<SkillReference> references, Skill skill, out SkillReference reference) {
            for (int i = 0; i < references.Count; i++) {
                if (references[i].SkillGraph(null) == skill.Graph) {
                    reference = references[i];
                    references.RemoveAtSwapBack(i);
                    return true;
                }
            }
            reference = null;
            return false;
        }

        static void CreateSkill(ISkillOwner owner, SkillReference reference, SkillState state) {
            var skill = reference.CreateSkill();
            owner.AddElement(skill);
            owner.AfterFullyInitialized(() => state.Apply(skill), owner);
        }

        static void CreateSkill(ISkillOwner owner, SkillReference reference, SkillVariablesOverride variablesOverride, SkillState state) {
            var skill = reference.CreateSkill();
            variablesOverride?.Apply(skill);
            owner.AddElement(skill);
            owner.AfterFullyInitialized(() => state.Apply(skill), owner);
        }
    }
}