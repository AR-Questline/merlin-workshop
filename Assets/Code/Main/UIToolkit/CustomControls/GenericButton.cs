using System;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit.CustomControls {
    [UxmlElement]
    public partial class GenericButton : VisualElement {
        const string Path = "Assets/UI/UTK/Controls/GenericButton/GenericButton.uxml";
        public event Action HoverAction;
        public event Action ClickAction {
            add => Button.clicked += value;
            remove => Button.clicked -= value;
        }

        Button _button;
        Button Button => _button ??= this.Query<Button>();

        [UxmlAttribute] public string label;

        public GenericButton() {
            var op = Addressables.LoadAssetAsync<VisualTreeAsset>(Path);
            var uxml = op.WaitForCompletion();
            uxml.CloneTree(this);

            RegisterCallback<AttachToPanelEvent>(e => { Button.text = label; });
            
            if (Application.isPlaying) {
                Button.clicked += OnClick;
                Button.RegisterCallback<MouseEnterEvent>(_ => OnHoverChanged(true));
                Button.RegisterCallback<MouseOutEvent>(_ => OnHoverChanged(false));
            }
        }

        static void OnClick() {
            FMODManager.PlayOneShot(CommonReferences.Get.AudioConfig.ButtonApplySound);
        }
        
        void OnHoverChanged(bool isMouseOver) {
            if (isMouseOver) {
                FMODManager.PlayOneShot(CommonReferences.Get.AudioConfig.ButtonSelectedSound);
                HoverAction?.Invoke();
            }
        }
    }
}