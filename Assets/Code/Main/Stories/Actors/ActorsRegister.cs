using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Awaken.Utility.Enums;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Sessions;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Actors {
    public class ActorsRegister : MonoBehaviour, IService {
        public const string Path = "Assets/Data/Settings/Actors.prefab";
        const string CommonLocIDPrefix = "Template/displayName_";
        const string CommonLocIDSuffix = "_7909eee15dd99b04f8e84647e3c04a73";
        
#if UNITY_EDITOR
        static readonly Dictionary<string, ActorSpec> ActorSpecCache = new();
        static readonly Dictionary<string, string> SpecPathCache = new();
        static readonly HashSet<string> GuidsErrorLogged = new();
#endif
        
        static ActorsRegister s_instance;

        public static ActorsRegister Get {
            get {
                if (s_instance == null) {
#if UNITY_EDITOR
                    s_instance = UnityEditor.AssetDatabase.LoadAssetAtPath<ActorsRegister>(Path);
#else
                    s_instance = World.Services?.TryGet<ActorsRegister>();
#endif
                }

                return s_instance;
            }
        }

        public IEnumerable<ActorSpec> AllActors => GetComponentsInChildren<ActorSpec>();
        static Cached<Dictionary<string, Actor>> s_actorCache = new(() => new Dictionary<string, Actor>());

        public static void ClearCache() => s_actorCache.Get().Clear();
        
        public static string StateOf(string actorGuid) =>
            World.Services.Get<GameplayMemory>().Context("actors").Get<string>(actorGuid);

        public void SetState(string actorGuid, string state) {
            World.Services.Get<GameplayMemory>().Context("actors").Set(actorGuid, state);
            s_actorCache.Get().Remove(actorGuid);
        }

        public Actor GetActor(string actorGuid) {
            if (string.IsNullOrWhiteSpace(actorGuid) || actorGuid == "None") {
                return default;
            }

            var cache = s_actorCache.Get();
            if (!cache.TryGetValue(actorGuid, out var actor)) {
                cache[actorGuid] = actor = Create(actorGuid);
            }

            return actor;

            Actor Create(string actorGuid) {
                var definedActor = RichEnum.AllValuesOfType<DefinedActor>().FirstOrDefault(da => da.ActorGuid == actorGuid);
                if (definedActor != null) {
                    return definedActor.Retrieve();
                }

                var spec = AllActors.FirstOrDefault(a => a.Guid == actorGuid);
                if (spec == null) {
                    Log.Important?.Error($"Actor with guid {actorGuid} has not been found in Actors Registry.");
                    return default;
                }

                return spec.Create(StateOf(actorGuid));
            }
        }

        public string Editor_GetActorName(string actorGuid) {
#if UNITY_EDITOR
            string path = Editor_GetPathFromGUID(actorGuid);
            return path.Split('/').Last();
#else
            Log.Important?.Error($"Tried to use the 'Editor_GetActorName' - an Editor-only method at runtime for actor guid '{actorGuid}'. This is not allowed.");
            return string.Empty;
#endif
        }

#if UNITY_EDITOR
        public ActorRef Editor_AddActorToTheRegistry(string actorName) {

            ActorRef resultActorRef = new();
            UnityEditor.Undo.RecordObject(this, "Add actor to the registry");
            GameObject thisSceneObject = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(this.gameObject);
            var newActor = new GameObject(actorName).AddComponent<ActorSpec>();
            newActor.transform.parent = thisSceneObject.transform;
            UnityEditor.PrefabUtility.ApplyPrefabInstance(thisSceneObject, UnityEditor.InteractionMode.AutomatedAction);
            string localGuid = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(newActor).targetObjectId.ToString();
            newActor.displayName = new LocString();
            newActor.displayName.SetFallback(newActor.displayName);
            newActor.displayName.ID = CommonLocIDPrefix + localGuid + CommonLocIDSuffix;
            resultActorRef.guid = newActor.Guid;
            UnityEditor.PrefabUtility.ApplyPrefabInstance(thisSceneObject, UnityEditor.InteractionMode.AutomatedAction);
            DestroyImmediate(thisSceneObject);
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            return resultActorRef;
        }

        public string Editor_GetGuidFromActorPath(string actorPath) {
            var definedActors = RichEnum.AllValuesOfType<DefinedActor>();
            var definedActor = definedActors.FirstOrDefault(da => da.ActorPath == actorPath);
            if (definedActor != null) {
                return definedActor.ActorGuid;
            }

            var spec = GameObjects.TryGrabChild<ActorSpec>(gameObject, actorPath.Split('/'));
            if (spec == null) {
                Log.Important?.Error($"Actor with path {actorPath} has not been found in Actors Registry. Please fix it.");
                return string.Empty;
            }
            return spec.Guid;
        }

        public string Editor_GetPathFromGUID(string actorGuid) {
            if (string.IsNullOrEmpty(actorGuid)) {
                return DefinedActor.None.ActorPath;
            }

            var definedActors = RichEnum.AllValuesOfType<DefinedActor>();
            if (definedActors.Any(da => da.ActorGuid == actorGuid)) {
                return actorGuid;
            }

            if (SpecPathCache.TryGetValue(actorGuid, out string cachedPath)) {
                return cachedPath;
            }
            
            if (Editor_TryGetSpecFromCache(actorGuid, out ActorSpec spec)) {
                var path = spec.GetPath();
                SpecPathCache[actorGuid]  = path;
                return spec.GetPath();
            }
            
            return string.Empty;
        }
        
        static bool Editor_TryGetSpecFromCache(string actorGuid, out ActorSpec actorSpec) {
            if (ActorSpecCache.Count == 0) {
                Editor_RefreshActorSpecCache();
            }

            if (ActorSpecCache.TryGetValue(actorGuid, out actorSpec)) {
                return true;
            }

            if (!GuidsErrorLogged.Contains(actorGuid)) {
                Log.Important?.Error($"Actor with guid {actorGuid} has not been found in Actors Registry.");
                GuidsErrorLogged.Add(actorGuid);
            }
            
            return false;
        }
        
        [UnityEditor.MenuItem("TG/Actors/Refresh Actors Cache")]
        public static void Editor_RefreshActorSpecCache() {
            SpecPathCache.Clear();
            ActorSpecCache.Clear();
            foreach (var actor in Get.AllActors) {
                ActorSpecCache[actor.Guid] = actor;
            }
        }
#endif
    }
}