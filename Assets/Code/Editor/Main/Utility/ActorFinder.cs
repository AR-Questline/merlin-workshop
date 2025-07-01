using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.Main.Stories.Actors;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;
using UnityEditor;

namespace Awaken.TG.Editor.Main.Utility {
    public static class ActorFinder {
        /// <summary>
        /// Tries to find ActorRef that matches given actor name.
        /// Returns DefinedActor.None.ActorRef if unsuccessful.
        /// </summary>
        public static bool TryGetActorRef(string actorName, out ActorRef actorRef) {
            string actorNameWithoutWhiteSpaces = Regex.Replace(actorName, @"\s+", "");
            if (string.IsNullOrWhiteSpace(actorNameWithoutWhiteSpaces)) {
                actorRef = DefinedActor.None.ActorRef;
                return false;
            }
            
            Regex regex = new(actorNameWithoutWhiteSpaces, RegexOptions.IgnoreCase);
            foreach (var definedActor in RichEnum.AllValuesOfType<DefinedActor>()) {
                if (regex.IsMatch(definedActor.ActorName)) {
                    actorRef = definedActor.ActorRef;
                    return true;
                }
            }
            
            if (ActorsRegister.Get.AllActors.TryGetFirst(a => ActorsRegister.Get.Editor_GetActorName(a.Guid) == actorName, out ActorSpec actorSpec)) {
                actorRef = new ActorRef() { guid = actorSpec.Guid };
                return true;
            }

            actorRef = DefinedActor.None.ActorRef;
            return false;
        }
        
        public static bool TryGetActorWithFix(string actorName, out ActorRef actorRef) {
            if (ActorFinder.TryGetActorRef(actorName, out actorRef)) {
                return true;
            }

            if (DisplayUndefinedActorDialog(actorName)) {
                actorRef = ActorsRegister.Get.Editor_AddActorToTheRegistry(actorName);
                return true;
            }

            return false;

            bool DisplayUndefinedActorDialog(string actorName) {
                return EditorUtility.DisplayDialog($"Add {actorName} to the registry?", 
                    "It looks like there in is an undefined actor in the provided input." +
                    $" Do you want to add said actor\n - {actorName} \n to the Actors Register?",
                    "Add", "Skip");
            }
        }
    }
}