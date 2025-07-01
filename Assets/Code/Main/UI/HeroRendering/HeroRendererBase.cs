using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.UI.HeroRendering {
    public abstract partial class HeroRendererBase : Element, ICustomClothesOwner {
        public sealed override bool IsNotSaved => true;
        
        // === Fields
        protected BodyFeatures _features;
        
        // === Properties
        public Hero Hero => Hero.Current;
        public Transform HeadSocket => View<VHeroRendererBase>().HeadSocket;
        public Transform MainHandSocket => View<VHeroRendererBase>().MainHandSocket;
        public Transform MainHandWristSocket => View<VHeroRendererBase>().MainHandWristSocket;
        public Transform OffHandSocket => View<VHeroRendererBase>().OffHandSocket;
        public Transform OffHandWristSocket => View<VHeroRendererBase>().OffHandWristSocket;
        public Transform HipsSocket => View<VHeroRendererBase>().RootSocket;
        public Transform RootSocket => View<VHeroRendererBase>().RootSocket;
        public bool IsLoading => View<VHeroRendererBase>().IsLoading;
        public bool UseLoadoutAnimations { get; }
        // === ICustomClothesOwner
        public IInventory Inventory => Hero.Inventory;
        public ICharacter Character => Hero;
        public IEquipTarget EquipTarget => this;
        public IBaseClothes<IItemOwner> Clothes => TryGetElement<CustomHeroClothes>();
        public IView BodyView => MainView;
        public abstract uint? LightRenderLayerMask { get; }
        public abstract int? WeaponLayer { get; }

        // === Initialization
        protected HeroRendererBase(bool useLoadoutAnimations) {
            UseLoadoutAnimations = useLoadoutAnimations;
        }
        
        protected override void OnFullyInitialized() {
            BodyFeatures bodyFeatures = AddElement(new BodyFeatures());
            bodyFeatures.CopyFrom(_features ?? Hero.BodyFeatures());
            bodyFeatures.RefreshDistanceBand(0);
            _features = null;
            
            InitBody().Forget();
        }

        async UniTaskVoid InitBody() {
            await RefreshGender();
            ShowBody();
        }
        
        // === Public API
        public UniTask RefreshGender() {
            return View<VHeroRendererBase>().ReloadBody();
        }
        
        public void HideBody() {
            View<VHeroRendererBase>().HideBody();
        }
        
        public void ShowBody() {
            View<VHeroRendererBase>().ShowBody();
        }

        public void RemoveClothesSpawnedFromPrefab() {
            View<VHeroRendererBase>().RemoveClothesSpawnedFromPrefab(Element<BodyFeatures>());
        }
    }
}