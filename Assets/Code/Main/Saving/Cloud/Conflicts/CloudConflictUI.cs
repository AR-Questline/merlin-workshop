using System;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.Utility;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Awaken.TG.Main.Saving.Cloud.Conflicts {
    public class CloudConflictUI : MonoBehaviour {
        [Title("Popup")]
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text content;
        
        [Title("Choose local")]
        [SerializeField] Button chooseLocal;
        [SerializeField] TMP_Text chooseLocalText;
        [SerializeField] KeyIcon localIcon;
        
        [Title("Choose cloud")]
        [SerializeField] Button chooseCloud;
        [SerializeField] TMP_Text chooseCloudText;
        [SerializeField] KeyIcon cloudIcon;
        
        [Title("Key mapping")]
        [SerializeField] UIKeyMapping keyMapping;
        
        float _localHoldPercent;
        float _cloudHoldPercent;
        bool _interactable;
        bool _isGamepad;
        Action _chooseLocal;
        Action _chooseCloud;
        Button _currentHoveredButton;
        bool _holdingPointerDown;
        
        static KeyBindings ChoseLocal => KeyBindings.UI.CloudConflict.ChoseLocal;
        static KeyBindings ChoseCloud => KeyBindings.UI.CloudConflict.ChoseCloud;
        
        bool HoldingPointerOnLocal => _holdingPointerDown && _currentHoveredButton == chooseLocal;
        bool HoldingPointerOnCloud => _holdingPointerDown && _currentHoveredButton == chooseCloud;
        
        float CloudHoldPercent {
            get => _cloudHoldPercent;
            set {
                _cloudHoldPercent = value;
                cloudIcon.SetHoldPercent(value);
            }
        }
        
        float LocalHoldPercent {
            get => _localHoldPercent;
            set {
                _localHoldPercent = value;
                localIcon.SetHoldPercent(value);
            }
        }

        public bool IsGamepad {
            [UnityEngine.Scripting.Preserve] get => _isGamepad;
            set {
                if (_isGamepad != value) {
                    _isGamepad = value;
                    RefreshIcons();
                }
            }
        }
        
        public void Init(DateTime localTimeStamp, DateTime cloudTimeStamp, Action choseLocal, Action choseCloud) {
            _chooseLocal = choseLocal;
            _chooseCloud = choseCloud;
            
            title.SetText(LocTerms.CloudSyncConflictTitle.Translate());
            
            var formatProvider = LocalizationSettings.SelectedLocale.Formatter;
            var localTimeStampText =  localTimeStamp.ToUniversalTime() != default ? localTimeStamp.ToString("g", formatProvider) : LocTerms.CloudSyncFileDateNA.Translate();
            var cloudTimeStampText = cloudTimeStamp.ToUniversalTime() != default ? cloudTimeStamp.ToString("g", formatProvider) : LocTerms.CloudSyncFileDateNA.Translate();
            content.SetText(LocTerms.CloudSyncConflict.Translate(localTimeStampText, cloudTimeStampText));
            
            chooseLocalText.SetText(LocTerms.CloudSyncChooseLocal.Translate());
            chooseCloudText.SetText(LocTerms.CloudSyncChooseCloud.Translate());

            keyMapping.RefreshCache();
            cloudIcon.SetupWithoutMVC(new KeyIcon.Data(ChoseCloud, true), keyMapping);
            localIcon.SetupWithoutMVC(new KeyIcon.Data(ChoseLocal, true), keyMapping);
            
            // ReInput.ControllerConnectedEvent -= RefreshIcons;
            // ReInput.ControllerConnectedEvent += RefreshIcons;
            // ReInput.ControllerDisconnectedEvent -= RefreshIcons;
            // ReInput.ControllerDisconnectedEvent += RefreshIcons;
            _interactable = true;
        }

        public void SetPointerHoveredButton(Button button) {
            _currentHoveredButton = button;
        }
        
        public void SetHoldingPointerDown(bool holdingPointerDown) {
            _holdingPointerDown = holdingPointerDown;
        }
        
        void Update() {
            if (_interactable == false) return;
            UpdateControls();
            UpdateControllerState();
        }
        
        // void RefreshIcons(ControllerStatusChangedEventArgs args) {
        //     keyMapping.RefreshCache();
        //     RefreshIcons();
        // }
        
        void RefreshIcons() {
            localIcon.RefreshIcons();
            cloudIcon.RefreshIcons();
        }
        
        void UpdateControls() {
            // if (RewiredHelper.Player.GetButton(ChoseLocal) || HoldingPointerOnLocal) {
            //     LocalHoldPercent += Time.unscaledDeltaTime;
            //     if (LocalHoldPercent >= 1) {
            //         _chooseLocal?.Invoke();
            //         _interactable = false;
            //     } 
            // } else {
            //     LocalHoldPercent = 0f;
            // }
            //
            // if (RewiredHelper.Player.GetButton(ChoseCloud) || HoldingPointerOnCloud) {
            //     CloudHoldPercent += Time.unscaledDeltaTime;
            //     if (CloudHoldPercent >= 1) {
            //         _chooseCloud?.Invoke();
            //         _interactable = false;
            //     }
            // } else {
            //     CloudHoldPercent = 0f;
            // }
        }
        
        void UpdateControllerState() {
            IsGamepad = RewiredHelper.IsGamepad;
        }
    }
}