using Awaken.ECS.Editor.DrakeRenderer;
using Awaken.Kandra;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Editor.Validation;
using Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.Main.Locations.Regrowables.Moths;
using Awaken.TG.Main.Locations.Setup.Editor;
using Awaken.TG.Main.Locations.Spawners.Critters;
using Awaken.TG.Main.Memories.FilePrefs;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.Cloud;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Tutorials;
using Awaken.TG.Main.UI.RoguePreloader;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.WorkflowTools {
    public static class EditorRuntimeResetManager {
        [InitializeOnLoadMethod]
        static void Init() {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void SubsystemRegistrationOperations() {
            ScenePreloader.EDITOR_RuntimeReset();
            World.EDITOR_RuntimeReset();
            View.EDITOR_RuntimeReset();
            LoadSave.EDITOR_RuntimeReset();
            ApplicationScene.EDITOR_RuntimeReset();
            TitleScreen.EDITOR_RuntimeReset();
            
            SceneService.EDITOR_RuntimeReset();
            UnityUpdateProvider.EDITOR_RuntimeReset();
            InteractionProvider.EDITOR_RuntimeReset();
            CameraStateStack.EDITOR_RuntimeReset();
            UIKeyMapping.EDITOR_RuntimeReset();
            RewiredHelper.EDITOR_RuntimeReset();
            TutorialSequence.EDITOR_RuntimeReset();
            
            CrimeReactionUtils.EDITOR_RuntimeReset();
            VfxMothUpdateHandler.EDITOR_RuntimeReset();
            CritterPools.EDITOR_RuntimeReset();
            
            DrakeRendererManagerEditor.EDITOR_RuntimeReset();
            DrakeRendererAuthoringHackManagerEditor.EDITOR_RuntimeReset();
            
            FileBasedPrefs.EDITOR_RuntimeReset();
            SocialService.EDITOR_RuntimeReset();
            CloudService.EDITOR_RuntimeReset();
            SteamCloudOrigin.EDITOR_RuntimeReset();
#if !UNITY_GAMECORE && !UNITY_PS5
            Awaken.TG.Main.RemoteEvents.RemoteConfig.EDITOR_RuntimeReset();
#endif

            LocationSpecPreviewManager.EDITOR_RuntimeReset();
            TemplatesSearcher.EDITOR_RuntimeReset();
            InvalidSetupFinders.EDITOR_RuntimeReset();
            
            GuardIntervention.EDITOR_RuntimeReset();
            QuestUtils.EDITOR_RuntimeReset();
        }

        static void PlayModeStateChanged(PlayModeStateChange stateChange) {
            if (stateChange == PlayModeStateChange.ExitingPlayMode) {
                OnExitPlayMode();
            }
        }

        static void OnExitPlayMode() {
            if (World.Services != null) {
                World.DropDomain(Domain.Main);
            }
            BrokenKandraMessage.EDITOR_RuntimeReset();
            Log.Utils.EDITOR_RuntimeReset();
        }
    }
}
