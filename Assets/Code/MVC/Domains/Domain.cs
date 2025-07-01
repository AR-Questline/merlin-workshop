using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Awaken.TG.Assets;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.Main.Scenes;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;
using Unity.IL2CPP.CompilerServices;

namespace Awaken.TG.MVC.Domains {
    [Il2CppEagerStaticClassConstruction]
    public readonly struct Domain : IEquatable<Domain> {
#if UNITY_EDITOR || AR_DEBUG
        static Dictionary<int, string> s_fullNameByHash = new();
#endif
        
        // === Properties
        public string Name { get; }
        public string ParentName { get; }
        public string FullName { get; }
        public bool Modal { get; }
        public int Hash { get; }
        
        public string SaveName { get; }
        public string SavePath { get; }

        
        public bool IsMetaDataDomain => Name != null && Name.StartsWith("MetaData_");
        // === Domains
        
        // Tree Visualization:
        //                     Game
        //      ProfileGlobals   |  TitleScreen  |          {save_slot}
        //                       |               |  Gameplay | Scene1 | Scene2 | ... 
        
        // Alternative Tree Visualization:
        // Game -> ProfileGlobals, TitleScreen, SaveSlot
        // SaveSlot -> Gameplay, Scene1, Scene2, ...

        public static readonly Domain
            Main = new("Game", isModal: true),
            Globals = new("Globals", Main),
            TitleScreen = new ("TitleScreen", Main),
            SaveSlot = new("{save_slot}", Main, true),
            Gameplay = new("Gameplay", SaveSlot);

        // === Dynamic domain creators
        public static Domain SaveSlotMetaData(string saveSlotId) => new($"MetaData_{saveSlotId}", Main, false, "MetaData", $"{Main}.{saveSlotId}");
        public static Domain Scene(SceneReference sceneReference) => new(sceneReference?.DomainName, SaveSlot);

        // === Static methods
        public static Domain CurrentScene() {
            if (!SceneLifetimeEvents.Get.ValidMainSceneState) {
                Log.Important?.Error("Reading " + nameof(CurrentScene) + " while scene is loading. This will return invalid values!");
            }
            return World.Services.Get<SceneService>().ActiveDomain;
        }

        /// <summary>
        /// Will return the main scene even if the current scene is an additive scene.
        /// Will return old domain before MapScene is initialized and log error
        /// </summary>
        public static Domain CurrentMainScene() {
            if (!SceneLifetimeEvents.Get.ValidDomainState) {
                Log.Important?.Error("Reading " + nameof(CurrentMainScene) + " while scene is loading. This will return invalid values!");
            }
            return World.Services.Get<SceneService>().MainDomain;
        }
        
        public static Domain CurrentMainSceneOrPreviousMainSceneWhileDropping() {
            return World.Services.Get<SceneService>().MainDomain;
        }

        static readonly Regex RootNameRegex = new(@"(Game/?)(.*)", RegexOptions.Compiled);

        // === Constructor
        Domain(string name, string parentName = null, bool isModal = false, string saveName = null, string savePath = null) {
            Name = name;
            ParentName = parentName;
            FullName = ParentName != null ? $"{ParentName}.{Name}" : Name;
            Modal = isModal;
            Hash = FullName.GetHashCode();
#if UNITY_EDITOR || AR_DEBUG
            if (s_fullNameByHash.TryGetValue(Hash, out string existingName)) {
                if (existingName != FullName) {
                    Log.Critical?.Error($"Hash collision in {nameof(Domain)}: {existingName} and {FullName}");
                    DebugUtils.Crash();
                }
            } else {
                s_fullNameByHash.Add(Hash, FullName);
            }
#endif
            
            SaveName = saveName ?? Name;
            SavePath = savePath ?? ParentName;
            SavePath = SavePath?.Replace('.', '/');
        }

        // === Methods
        public string ConstructSavePath([CanBeNull] string saveSlot) {
            string saveParentName = SavePath ?? "";
            string path = RootNameRegex.Replace(saveParentName, "$2");
            path = path
                .Replace("{save_slot}", saveSlot);
            
            return Path.Combine(CloudService.SavedGamesDirectory, path);
        }
        
        public bool IsChildOf(Domain domain, bool includeSelf = false) {
            if (includeSelf && domain == this) {
                return true;
            }
            return ParentName?.StartsWith(domain.FullName) ?? false;
        }

        public bool IsDirectChildOf(Domain domain) {
            return ParentName == domain.FullName;
        }

        // === Operators
        public static bool operator ==(Domain a, Domain b) => a.Equals(b);
        public static bool operator !=(Domain a, Domain b) => !(a == b);
        
        public static implicit operator string(Domain domain) => domain.FullName;
        public override string ToString() => FullName;

        // === Equality members
        public bool Equals(Domain other) {
            return Hash == other.Hash;
        }

        public override bool Equals(object obj) {
            return obj is Domain other && Equals(other);
        }

        public override int GetHashCode() {
            return Hash;
        }
    }
}