using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers;
using Awaken.Utility;
using QFSW.QC;
using UnityEngine;

namespace Awaken.TG.Debugging.Cheats {
    public partial class CheatController : Element<GameUI>, ISmartHandler {
        public const string CheatsMemoryLabel = "CheatsEnabled";

        public sealed override bool IsNotSaved => true;
        
        string _cheatCode;
        int _cheatIndex = -1;

        bool CheatsOn { get; set; } = false;

        public static bool CheatsEnabled() {
#if UNITY_EDITOR
            return true;
#else
            return World.Any<CheatController>()?.CheatsOn ?? false;
#endif
        }

        public static bool CheatShortcutsEnabled { get; set; } = true;

        protected override void OnInitialize() {
            var constants = Services.Get<DebugReferences>();
            _cheatCode = constants.cheatCode.ToLower();
            
            World.EventSystem.ListenTo(EventSelector.AnySource, Hero.Events.MainViewInitialized, this, SaveCheatsWasEnabled);
            var cheatsFromConfig = Configuration.GetBool("cheatme_on");

            if (CheatsOn || cheatsFromConfig) {
                TurnCheatsOn();
            } else if (Application.isEditor) {
                InitQuantumConsole();
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            // var quantumConsole = QuantumConsole.Instance;
            // if (quantumConsole != null && quantumConsole.IsInitialized) {
            //     quantumConsole.OnActivate -= QuantumConsoleOpened;
            //     quantumConsole.OnDeactivate -= QuantumConsoleClosed;
            // }
        }

        void TurnCheatsOn() {
            CheatsOn = true;
            InitQuantumConsole();

            Services.Get<SocialService>().AllowUploads = false;
            World.SpawnView<VCheatMessage>(this);
            SaveCheatsWasEnabled();
            TriggerChange();
        }

        void InitQuantumConsole() {
            // var quantumConsole = QuantumConsole.Instance;
            // if (!quantumConsole.IsInitialized) {
            //     quantumConsole.Initialize();
            //     quantumConsole.OnActivate += QuantumConsoleOpened;
            //     quantumConsole.OnDeactivate += QuantumConsoleClosed;
            // }
        }
        
        void QuantumConsoleOpened() {
            this.AddMarkerElement<GivePlayerCursorElement>();
        }
        
        void QuantumConsoleClosed() {
            RemoveElementsOfType<GivePlayerCursorElement>();
        }

        void SaveCheatsWasEnabled() {
            var facts = Services.TryGet<GameplayMemory>()?.Context();
            if (facts == null || facts.Get<bool>(CheatsMemoryLabel)) {
                return;
            }
            
            facts.Set(CheatsMemoryLabel, CheatsOn);
        }

        public static bool CheatsWasEnabledForHero() {
            return Services.TryGet<GameplayMemory>()?.Context()?.Get<bool>(CheatsMemoryLabel) ?? false;
        }

        // === Input Events Handling
        public UIResult BeforeDelivery(UIEvent evt) {
            if (!CheatsOn && evt is UIEKeyDown e && _cheatCode.Length > 2) {
                if (e.Key == KeyCode.Home || e.Key == KeyCode.BackQuote) {
                    _cheatIndex = 0;
                    return UIResult.Accept;
                } else if (_cheatIndex >= 0) {
                    var character = e.Key.ToString().ToLower()[0];
                    return TryCheatChar(character);
                }
            }

            return UIResult.Ignore;
        }

        UIResult TryCheatChar(char character) {
            if (character == _cheatCode[_cheatIndex]) {
                _cheatIndex++;
                if (_cheatIndex >= _cheatCode.Length) {
                    // cheat code correct
                    _cheatIndex = -1;
                    TurnCheatsOn();
                }
                return UIResult.Accept;
            } else {
                _cheatIndex = -1;
                return UIResult.Ignore;
            }
        }

        public UIResult BeforeHandlingBy(IUIAware handler, UIEvent evt) {
            return UIResult.Ignore;
        }

        public UIResult AfterHandlingBy(IUIAware handler, UIEvent evt) {
            return UIResult.Ignore;
        }

        public UIEventDelivery AfterDelivery(UIEventDelivery delivery) {
            return delivery;
        }
    }
}