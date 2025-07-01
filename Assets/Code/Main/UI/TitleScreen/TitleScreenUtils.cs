using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterCreators;
using Awaken.TG.Main.Heroes.CharacterCreators.Data;
using Awaken.TG.Main.Heroes.CharacterCreators.PresetSelection;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Settings.FirstTime;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.UI.RoguePreloader;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Automation;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen {
    public static class TitleScreenUtils {
        public const string IntendedScene = "PlayModeEnter: IntendedScene";
        public const string FirstTimeSettingPrefKey = "First_Time_Settings";
        static PopupUI s_popup;
        
        static bool s_needToSetInitialSettings;
        static bool s_needToCreateHero;
        static bool s_needTransitionToCamera;
        static bool s_needToSetCharacterCreatorRandomPreset;
        static CharacterBuildPreset s_preset;
        static SceneReference s_intendedScene;
        
        public static void LoadGame(SaveSlot slot) {
            if (slot.CanLoad()) {
                ScenePreloader.Load(slot, "TitleScreen");
            }
        }

        public static void StartNewGame(StartGameData data) {
            ClosePopup();

            s_needToSetInitialSettings = true;
            s_needToCreateHero = data.withHeroCreation;
            s_needTransitionToCamera = data.withTransitionToCamera;
            s_intendedScene = data.sceneReference;
            s_preset = data.characterPresetData;

            Log.Marking?.Warning($"Starting new game: {s_intendedScene.Name}");
            
            data.onStart?.Invoke();
            ScenePreloader.StartNewGame(data.sceneReference);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ForceRandomCharacterCreatorPreset() {
#if UNITY_EDITOR
            s_needToSetCharacterCreatorRandomPreset = true;
#endif
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PreventRandomCharacterCreatorPreset() {
#if UNITY_EDITOR
            s_needToSetCharacterCreatorRandomPreset = false;
#endif
        }
        
        public static UniTask RunFirstSettings() {
            if (PrefMemory.GetBool(FirstTimeSettingPrefKey) || Automations.HasAutomation) {
                return UniTask.CompletedTask;
            }

            if (s_needToSetInitialSettings) {
                Log.Marking?.Warning("Showing initial settings");
                s_needToSetInitialSettings = false;

                var firstTimeSettings = World.Add(new FirstTimeSettings());
                return AsyncUtil.WaitForDiscard(firstTimeSettings);
            }

            return UniTask.CompletedTask;
        }

        public static UniTask RunHeroCharacterCreatorIfNeeded(MapScene requester) {
            if (s_needToCreateHero) {
                s_needToCreateHero = false;
                s_needToSetCharacterCreatorRandomPreset = false;
                var creator = World.Add(new CharacterCreator(Hero.Current.BodyFeatures(), requester, s_preset, null, s_needTransitionToCamera));
                s_needTransitionToCamera = false;
                return AsyncUtil.WaitForDiscard(creator);
            } else if (s_needToSetCharacterCreatorRandomPreset) {
                s_needToSetCharacterCreatorRandomPreset = false;
                SetRandomCharacterCreatorPreset();
            }

            return UniTask.CompletedTask;
        }

        public static void FastHeroCharacterCreatorIfNeeded() {
            if (s_needToCreateHero) {
                s_needToCreateHero = false;
                s_needToSetCharacterCreatorRandomPreset = false;
                var features = Hero.Current.BodyFeatures();

                features.Gender = Gender.Male;
                features.Reload();
                Hero.LoadGenderSoundBanks(features.Gender);
                
                World.EventSystem.LimitedListenTo(
                    EventSelector.AnySource,
                    SceneLifetimeEvents.Events.AfterSceneStoriesExecuted,
                    Hero.Current,
                    _ => {
                        s_preset.Apply();
                        if (LoadSave.Get.CanAutoSave()) {
                            LoadSave.Get.Save(SaveSlot.GetAutoSave());
                        }
                    },
                    1);
            } else if (s_needToSetCharacterCreatorRandomPreset) {
                s_needToSetCharacterCreatorRandomPreset = false;
                SetRandomCharacterCreatorPreset();
            }
        }

        static void SetRandomCharacterCreatorPreset() {
            var template = World.Services.Get<TemplatesProvider>().GetAllOfType<CharacterCreatorTemplate>().First();
#if UNITY_EDITOR
            int presetIndex = UnityEditor.EditorPrefs.GetInt("debug.cc-preset", -1);
            if (presetIndex == -1) {
                presetIndex = Random.Range(0, template.PresetsCount);
            }
#else
            int presetIndex = 0;            
#endif
            ref readonly var preset = ref template.Preset(presetIndex).data;

            var blendshapes = new BlendShape[CharacterCreator.BlendShapesCount];
            var hairAsset = preset.Hair(template).Asset;
            var beardAsset = preset.Beard(template).Asset;

            blendshapes[0] = preset.Gender(template);
            preset.HeadPreset(template).FillShapesContinuously(blendshapes, 4);
            
            var features = Hero.Current.BodyFeatures();
            features.Gender = CharacterCreatorTemplate.GenderOfIndex(preset.gender);
            Hero.LoadGenderSoundBanks(features.Gender);
            features.ShapesFeature = new BlendShapesFeature(blendshapes);
            features.SkinColor = new SkinColorFeature(preset.SkinColor(template).tint);
            features.Hair = hairAsset != null ? new MeshFeature(hairAsset) : null;
            features.Beard = beardAsset != null ? new MeshFeature(beardAsset) : null;
            features.ChangeHairColor(preset.HairColor(template).config);
            features.ChangeBeardColor(preset.HairColor(template).config);
            features.Normals = new BodyNormalFeature(preset.BodyNormal(template));
            features.Reload();
            CommonReferences.RefreshLocsGender(features.Gender);
            Hero.Current.VHeroController.ChangeHeroPerspective(Hero.TppActive).Forget();
        }

        static void ClosePopup() {
            s_popup?.Discard();
            s_popup = null;
        }
    }
}