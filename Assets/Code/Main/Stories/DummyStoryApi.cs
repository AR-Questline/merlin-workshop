using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.UI.Popup.PopupContents;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Stories {
    public partial class DummyStoryApi : Model {
        public override ushort TypeForSerialization => SavedModels.DummyStoryApi;

        public override Domain DefaultDomain => Domain.Gameplay;

        public ICollection<string> Tags { get; }
        public IEnumerable<VariableDefine> Variables { get; }
        public IEnumerable<VariableReferenceDefine> VariableReferences { get; }
        public bool IsSharedBetweenMultipleNPCs => false;
        public Location OwnerLocation { get; }
        public Location FocusedLocation { get; }
        public IEnumerable<WeakModelRef<Location>> Locations => Enumerable.Empty<WeakModelRef<Location>>();

        public Hero Hero {
            get => Hero.Current;
            set => throw new System.NotImplementedException();
        }
        public bool InvolveHero => false;
        public Item Item { get; }
        public IMemory Memory => Services.Get<GameplayMemory>();
        public IMemory ShortMemory { get; } = new PrivateMemory();
        public bool StoryEndRequiresInteraction => false;
        public bool WasVOSuccessfullyDelayed { get; set; }
        
        public void Clear() {}
        public void RemoveView() {}
        public void SetArt(SpriteReference art) {}
        public void SetTitle(string title) {}
        public void ShowText(TextConfig textConfig) {}
        public void ShowLastChoice(string textToDisplay, string iconName) {}
        public void ShowChange(Stat stat, int change) {}
        public void OfferChoice(ChoiceConfig choiceConfig) {}
        public void ChangeFocusedLocation(Location location) {}
        public UniTask SetupLocation(Location location, bool invulnerability, bool involve, bool rotReturnToInteraction, bool rotToHero, bool forceExitInteraction = false) => UniTask.CompletedTask;
        public UniTask SetupNpc(NpcElement npc, bool invulnerability, bool involve, bool rotReturnToInteraction, bool rotToHero, bool forceExitInteraction = false) => UniTask.CompletedTask;

        public void ToggleBg(bool enabled) {}
        public void ToggleViewBackground(bool enabled) {}
        public Transform LastChoicesGroup() { return null; }
        public Transform StatsPreviewGroup() { return null; }
        public void JumpTo(IEditorChapter chapter) {}
        public void JumpToDifferentGraph(StoryBookmark bookmark) { }
        public void FinishStory(bool wasInterrupted = false) {}
        public void SpawnContent(DynamicContent contentElement) { }
        public void LockChoiceAssetGate() { }
        public void UnlockChoiceAssetGate() { }
    }
}