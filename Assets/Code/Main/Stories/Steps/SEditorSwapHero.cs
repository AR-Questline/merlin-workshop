using System.Linq;
using Awaken.TG.Main.AI.Utils;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterCreators;
using Awaken.TG.Main.Heroes.CharacterCreators.Data;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Hero: Swap Visuals and Logic"), NodeSupportsOdin]
    public class SEditorSwapHero : EditorStep {
        public bool cleanCurrentInventory = true;
        public CharacterPresetData visualPresetData;
        public AliveAudioContainerWrapper aliveAudioContainer; 
        
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SSwapHero() {
                cleanCurrentInventory = cleanCurrentInventory,
                visualPresetData = visualPresetData,
                aliveAudioContainer = aliveAudioContainer?.Data,
            };
        }
    }

    public partial class SSwapHero : StoryStep {
        public bool cleanCurrentInventory;
        public CharacterPresetData visualPresetData;
        public AliveAudioContainer aliveAudioContainer; 

        public override StepResult Execute(Story story) {
            var result = new StepResult();
            SetUpNew(story.Hero, result).Forget();
            return result;
        }

        async UniTaskVoid SetUpNew(Hero hero, StepResult result) {
            AdvancedNotificationBuffer.AllNotificationsSuspended = true;
            
            CachedHeroData heroData = World.Any<CachedHeroData>();
            bool firstHeroData = heroData == null;
            if (firstHeroData) {
                // Only hero data creates here (first one) is used later.
                heroData = World.Add(new CachedHeroData());
                heroData.StashVisuals(hero);
                heroData.StashDevelopment(hero);
            }

            var bodyFeatures = hero.BodyFeatures();
            bodyFeatures.MoveFrom(GetSetupBodyFeatures(visualPresetData));
            if (!await hero.View<VHeroController>().TryReloadBodyWithEquips()) {
                return;
            }
            bodyFeatures.Reload();
            if (!await TryCleanInventory(hero, firstHeroData ? heroData : null)) {
                return;
            }

            if (aliveAudioContainer != null) {
                hero.AliveAudio?.Discard();
                hero.AddElement(new HeroAliveAudio(aliveAudioContainer));
                Hero.UnloadGenderSoundBanks();
                Hero.LoadGenderSoundBanks(bodyFeatures.Gender);
            }
            
            SummonUtils.DestroyHeroSummonsExceptPets();
            
            AdvancedNotificationBuffer.AllNotificationsSuspended = false;
            result.Complete();
        }
        
        BodyFeatures GetSetupBodyFeatures(CharacterPresetData visualPresetData) {
            var features = new BodyFeatures();
            var template = World.Services.Get<TemplatesProvider>().GetAllOfType<CharacterCreatorTemplate>().First();
            
            // Gender
            features.Gender = CharacterCreatorTemplate.GenderOfIndex(visualPresetData.gender);
            // Shapes
            var shapes = new BlendShape[CharacterCreator.BlendShapesCount];
            shapes[0] = visualPresetData.Gender(template);
            visualPresetData.HeadPreset(template).FillShapesContinuously(shapes, 4);
            features.ShapesFeature = new BlendShapesFeature(shapes);

            features.FaceSkin = new FaceSkinTexturesFeature(visualPresetData.FaceSkin(template));
            var hairAsset = visualPresetData.Hair(template).Asset;
            features.Hair = hairAsset != null ? new MeshFeature(hairAsset) : null;
            var beardAsset = visualPresetData.Beard(template).Asset;
            features.Beard = beardAsset != null ? new MeshFeature(beardAsset) : null;

            features.SkinColor = new SkinColorFeature(visualPresetData.SkinColor(template).tint);
            features.ChangeHairColor(visualPresetData.HairColor(template).config);
            features.ChangeBeardColor(visualPresetData.BeardColor(template).config);
            features.Normals = new BodyNormalFeature(visualPresetData.BodyNormal(template));
            features.Eyebrows = new EyebrowFeature(visualPresetData.Eyebrow(template).Asset);
            features.Eyes = new EyeColorFeature(visualPresetData.EyeColor(template).tint);
            
            var bodyTattoo = visualPresetData.BodyTattoo(template);
            var tattooColor = visualPresetData.BodyTattooColor(template);

            if (bodyTattoo.data != null) {
                var config = new TattooConfig(bodyTattoo.data, tattooColor.tint);
                features.BodyTattoo = new BodyTattooFeature(config);
            } else {
                features.BodyTattoo = null;
            }
            
            var faceTattoo = visualPresetData.FaceTattoo(template);
            tattooColor = visualPresetData.FaceTattooColor(template);

            if (faceTattoo.data != null) {
                var config = new TattooConfig(faceTattoo.data, tattooColor.tint);
                features.FaceTattoo = new FaceTattooFeature(config);
            } else {
                features.FaceTattoo = null;
            }

            return features;
        }
        
        async UniTask<bool> TryCleanInventory(Hero hero, CachedHeroData cachedData = null) {
            if (cleanCurrentInventory) {
                return await CleanInventory(hero, cachedData);
            }
            return true;
        }
        
        public static async UniTask<bool> CleanInventory(Hero hero, CachedHeroData cachedData = null) {
            bool equipSoundsWereMuted = hero.MuteEquips;
            hero.MuteEquips = true;
            if (cachedData != null) {
                cachedData.StashItems(hero);
            } else {
                foreach (var slot in EquipmentSlotType.All) {
                    hero.Inventory.Unequip(slot);
                }
                foreach (var item in hero.Inventory.Items.ToArray()) {
                    if (item is { IsFists: false }) {
                        item.Discard();
                    }
                }
            }
            hero.MuteEquips = equipSoundsWereMuted;

            if (!await AsyncUtil.DelayFrame(hero, 5)) {
                return false;
            }
            return true;
        }
    }
}