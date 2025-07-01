using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Templates.Attachments;
using Sirenix.OdinInspector;
using LogType = Awaken.Utility.Debugging.LogType;
#if UNITY_EDITOR  
using System.Linq;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
#endif

namespace Awaken.TG.Main.Fights.NPCs {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Common, "Used by unique NPCs, supports multiple presences.")]
    public class UniqueNpcAttachment : NpcAttachment, ISelfValidator {
        public override bool IsUnique => true;
        
        // --- Validation
#if UNITY_EDITOR  
        static UniqueNpcAttachment[] s_allUniqueNpcs;
        static double s_lastNpcsRefreshTime = -999;

        public static bool ValidateUniqueActor(UniqueNpcAttachment uniqueNpc, out string error) {
            error = "";
            if (uniqueNpc == null) {
                return true;
            }
            var actor = uniqueNpc.GetActor();
            if (actor.IsEmpty || actor.guid == "None") {
                return true;
            }

            if (s_lastNpcsRefreshTime + 60f < System.DateTime.Now.TimeOfDay.TotalSeconds) {
                s_allUniqueNpcs = GetAllUniqueNpcs();
                s_lastNpcsRefreshTime = System.DateTime.Now.TimeOfDay.TotalSeconds;
            }

            bool result = s_allUniqueNpcs.Count(n => n.GetActor() == actor) == 1;
            if (!result) {
                error = $"Actor {actor.guid} is used in more than 1 Unique NPC! {string.Join(", ", s_allUniqueNpcs.Where(n => n.GetActor() == actor))}";
            }
            return result;
        }
            
        static UniqueNpcAttachment[] GetAllUniqueNpcs() {
            var prefabs = AssetDatabase.FindAssets("t:prefab", new string[] { "Assets/Data/LocationSpecs/AI/" })
                .Select(s => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(s)));
            return prefabs.Where(p => p.TryGetComponent<UniqueNpcAttachment>(out _)).Select(p => p.GetComponent<UniqueNpcAttachment>()).ToArray();
        }
#endif
        
        public void Validate(SelfValidationResult result) {
#if UNITY_EDITOR
            if (!ValidateUniqueActor(this, out var error)) {
                Log.Important?.Error(error, this);
                result.AddError(error);
            }
#endif
        }
    }
}