using Awaken.TG.Main.AudioSystem.Notifications;
using Awaken.TG.Main.Crafting;
using Awaken.TG.Main.Heroes.Fishing;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Objective;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Quest;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.General.Configs
{
    
    public class AudioConfig : ScriptableObject
    {
        [FoldoutGroup("Gameplay defaults"), SerializeField] ItemAudioContainerWrapper defaultItemAudioContainer;
        [FoldoutGroup("Gameplay defaults"), SerializeField] AliveAudioContainerWrapper defaultAliveAudioContainer;
        [FoldoutGroup("Gameplay defaults"), SerializeField] AliveAudioContainerWrapper defaultWyrdAudioContainer;
        [FoldoutGroup("Gameplay defaults"), SerializeField] EventReference defaultEnemyFootStep;
        
        [FoldoutGroup("Hero"), SerializeField] EventReference heroHitBonusAudio;
        [FoldoutGroup("Hero"), SerializeField] EventReference heroLandedSound;
        [FoldoutGroup("Hero"), SerializeField] EventReference attackOutsideFOVWarningSound;
        
        [FoldoutGroup("Trespassing"), SerializeField] EventReference trespassingWarning;
        [FoldoutGroup("Trespassing"), SerializeField] EventReference trespassingDetection;
        
        [FoldoutGroup("UI"), SerializeField] EventReference startGameSound;
        [FoldoutGroup("UI"), SerializeField] EventReference lightNegativeFeedbackSound;
        [FoldoutGroup("UI"), SerializeField] EventReference strongNegativeFeedbackSound;
        [FoldoutGroup("UI"), SerializeField] EventReference tabSelectedSound;
        [FoldoutGroup("UI"), SerializeField] EventReference restoreDefaults;
        [Space]
        [FoldoutGroup("UI"), SerializeField] EventReference buttonSelectedSound;
        [FoldoutGroup("UI"), SerializeField] EventReference buttonClickedSound; 
        [FoldoutGroup("UI"), SerializeField] EventReference buttonAcceptSound; // for lighter feedback
        [FoldoutGroup("UI"), SerializeField] EventReference buttonApplySound; // for heavier feedback
        [FoldoutGroup("UI"), SerializeField] EventReference switchSlotSound ; // for switching between quick slots
        
        [Space]
        [FoldoutGroup("UI"), SerializeField] PromptAudio defaultPromptAudio;
        [FoldoutGroup("UI"), SerializeField] CraftingAudio craftingAudio;
        
        [FoldoutGroup("Notifications"), SerializeField] NotificationSoundEvent expAudio;
        [FoldoutGroup("Notifications"), SerializeField] NotificationSoundEvent itemAudio;
        [FoldoutGroup("Notifications"), SerializeField] NotificationSoundEvent locationAudio;
        [FoldoutGroup("Notifications"), SerializeField] NotificationSoundEvent specialItemAudio;
        [FoldoutGroup("Notifications"), SerializeField] NotificationSoundEvent proficiencyAudio;
        [FoldoutGroup("Notifications"), SerializeField] NotificationSoundEvent levelUpAudio;
        [FoldoutGroup("Notifications"), SerializeField] QuestAudio questAudio;
        [FoldoutGroup("Notifications"), SerializeField] ObjectiveAudio objectiveAudio;
        [FoldoutGroup("Notifications"), SerializeField] NotificationSoundEvent recipeAudio;
        [FoldoutGroup("Notifications"), SerializeField] NotificationSoundEvent journalAudio;
        [FoldoutGroup("Notifications"), SerializeField] NotificationSoundEvent wyrdInfoAudio;
        
        [FoldoutGroup("Rain"), Range(0, 1)] public float rainIntensityMultiplierWhenUnderRoof = 0.5f;
        [FoldoutGroup("Rain"), Range(0, 1)] public float rainIntensityMultiplierWhenUnderWater = 0.2f;
        
        [FoldoutGroup("Fishing"), SerializeField, HideLabel] FishingAudio fishingAudio;
        [FoldoutGroup("Other"), SerializeField] StatusAudioMap statusAudioMap;

        // === Defaults
        public ItemAudioContainer DefaultItemAudioContainer => defaultItemAudioContainer.Data;
        public AliveAudioContainer DefaultAliveAudioContainer => defaultAliveAudioContainer.Data;
        public AliveAudioContainer DefaultWyrdAudioContainer => defaultWyrdAudioContainer.Data;
        public EventReference DefaultEnemyFootStep => defaultEnemyFootStep;
        
        // === Hero
        public EventReference HeroHitBonusAudio => heroHitBonusAudio;
        public EventReference HeroLandedSound => heroLandedSound;
        public EventReference AttackOutsideFOVWarningSound => attackOutsideFOVWarningSound;
        
        // === Trespassing
        public EventReference TrespassingWarning => trespassingWarning;
        public EventReference TrespassingDetection => trespassingDetection;

        public EventReference LightNegativeFeedbackSound => lightNegativeFeedbackSound;
        public EventReference StrongNegativeFeedbackSound => strongNegativeFeedbackSound;
        
        public EventReference TabSelectedSound => tabSelectedSound;
        public EventReference RestoreDefaults => restoreDefaults;
        
        public EventReference ButtonSelectedSound => buttonSelectedSound;
        public EventReference ButtonClickedSound => buttonClickedSound;
        public EventReference ButtonAcceptSound => buttonAcceptSound;
        public EventReference ButtonApplySound => buttonApplySound;
        public EventReference SwitchSlotSound => switchSlotSound;
        
        public PromptAudio DefaultPromptAudio => defaultPromptAudio;
        public CraftingAudio CraftingAudio => craftingAudio;
        
        public NotificationSoundEvent ExpAudio => expAudio;
        public NotificationSoundEvent ItemAudio => itemAudio;
        public NotificationSoundEvent LocationAudio => locationAudio;
        public NotificationSoundEvent SpecialItemAudio => specialItemAudio;
        public NotificationSoundEvent ProficiencyAudio => proficiencyAudio;
        public NotificationSoundEvent LevelUpAudio => levelUpAudio;
        public ObjectiveAudio ObjectiveAudio => objectiveAudio;
        public QuestAudio QuestAudio => questAudio;
        public NotificationSoundEvent RecipeAudio => recipeAudio;
        public NotificationSoundEvent JournalAudio => journalAudio;
        public NotificationSoundEvent WyrdInfoAudio => wyrdInfoAudio;

        public ref readonly FishingAudio FishingAudio => ref fishingAudio;
        public StatusAudioMap StatusAudioMap => statusAudioMap;
        
        public EventReference StartGameSound => startGameSound;
    }
}
