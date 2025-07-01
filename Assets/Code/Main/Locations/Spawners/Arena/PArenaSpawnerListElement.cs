using System;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.Locations.Spawners.Arena {
    public class PArenaSpawnerListElement : Presenter<ArenaSpawnerUI> {
        const string MarkFavouriteClassName = "favourite-button-marked";

        Label _name;
        Label _difficulty;
        Label _faction;
        VisualElement _clickTarget;
        Button _favourite;
        VisualTreeAsset _template;
        Action _onFavouriteClick;

        public IArenaSpawnerEntry Entry { get; private set; }
        public Action<PArenaSpawnerListElement> Clicked { get; set; }
        public PArenaSpawnerListElement(VisualElement parent) : base(parent) { }

        protected override void CacheVisualElements(VisualElement contentRoot) {
            _name = contentRoot.Q<Label>("name");
            _difficulty = contentRoot.Q<Label>("difficulty");
            _faction = contentRoot.Q<Label>("faction");
            _favourite = contentRoot.Q<Button>("favourite-button");
            _clickTarget = contentRoot.Q<VisualElement>("click-target");
        }

        public void Init(IArenaSpawnerEntry entry, bool showDifficulty, bool showFaction, bool isFavourite, Action onFavouriteClick) {
            Entry = entry;
            _name.text = entry.Label;
            _difficulty.text = showDifficulty ? entry.ThreatLevel.ToString() : string.Empty;
            _faction.text = showFaction ? entry.FactionName : string.Empty;
            _onFavouriteClick += onFavouriteClick;
            _favourite.clickable.clicked += _onFavouriteClick;
            if (isFavourite) {
                _favourite.AddToClassList(MarkFavouriteClassName);
            } else {
                _favourite.RemoveFromClassList(MarkFavouriteClassName);
            }

            _clickTarget.RegisterCallback<ClickEvent>(evt => OnClick());
            _clickTarget.RegisterCallback<MouseOverEvent>(evt => OnHover());
        }

        public void Dispose() {
            _favourite.clickable.clicked -= _onFavouriteClick;
            _onFavouriteClick = null;
            _clickTarget.UnregisterCallback<ClickEvent>(evt => OnClick());
            _clickTarget.UnregisterCallback<MouseOverEvent>(evt => OnHover());
        }

        void OnClick() {
            FMODManager.PlayOneShot(CommonReferences.Get.AudioConfig.ButtonApplySound);
            Clicked?.Invoke(this);
        }

        void OnHover() {
            FMODManager.PlayOneShot(CommonReferences.Get.AudioConfig.ButtonSelectedSound);
        }
    }
}