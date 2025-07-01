using Awaken.TG.Main.UI.Components.PadShortcuts;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Sources;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.UI.ButtonSystem {
    /// <summary>
    /// see: https://www.notion.so/awaken/Prompts-40b77a36ea764751abd136d7b82243c2
    /// </summary>
    public partial class Prompts : Element, IShortcut, IUIAware {
        public sealed override bool IsNotSaved => true;

        readonly IPromptHost _host;
        readonly ButtonsHandler _handler = new();

        public Prompts(IPromptHost host) {
            _host = host;
        }
        
        protected override void OnInitialize() {
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.All, this, this, -1));
        }
        
        /// <summary>
        /// Add new Prompt without spawning its view
        /// </summary>
        public TPrompt AddPrompt<TPrompt>(TPrompt prompt, IModel owner, IPromptListener listener, bool active = true, bool visible = true, PromptAudio audio = null, bool useDefaultAudio = false) 
            where TPrompt : Prompt {
            InitPrompt(prompt, owner);
            prompt.AddListener(listener);
            prompt.SetupState(visible, active);
            prompt.RefreshState();
            return prompt;
        }

        /// <summary> Add new Prompt and spawn its view </summary>
        public TPrompt AddPrompt<TPrompt, TPromptView>(TPrompt prompt, IModel owner, bool active = true, bool visible = true, PromptAudio audio = null, bool useDefaultAudio = false) 
            where TPrompt : Prompt where TPromptView : View, IPromptListener {
            InitPrompt(prompt, owner);
            var view = World.SpawnView<TPromptView>(prompt, true, true, _host.PromptsHost);
            prompt.AddListener(view);
            prompt.SetupState(visible, active);
            RefreshPromptsPositions();
            prompt.RefreshState();
            return prompt;
        }
        
        /// <summary> Add new Prompt and spawn its view </summary>
        public Prompt AddPrompt<TPromptView>(Prompt prompt, IModel owner, bool active = true, bool visible = true) 
            where TPromptView : View, IPromptListener {
            return AddPrompt<Prompt, TPromptView>(prompt, owner, active, visible);
        }
        /// <summary> Add new Prompt and spawn its view </summary>
        public Prompt AddPrompt(Prompt prompt, IModel owner, bool active = true, bool visible = true) {
            return AddPrompt<Prompt, VGenericPromptUI>(prompt, owner, active, visible);
        }

        public void RemovePrompt(ref Prompt prompt) {
            if (prompt == null) {
                return;
            }
            prompt.Discard();
            prompt = null;
        }
        
        /// <summary> Add new Prompt and bind it with already spawned View </summary>
        public TPrompt BindPrompt<TPrompt, TPromptView>(TPrompt prompt, IModel owner, TPromptView view, bool active = true, bool visible = true) 
            where TPrompt : Prompt where TPromptView : View, IPromptListener {
            if (prompt == null) {
                Log.Important?.Error("Provided view must not be null");
                return null;
            }
            InitPrompt(prompt, owner);
            World.BindView(prompt, view, true, true);
            prompt.AddListener(view);
            prompt.SetupState(visible, active);
            prompt.RefreshState();
            return prompt;
        }

        void InitPrompt<TPrompt>(TPrompt prompt, IModel owner) where TPrompt : Prompt {
            var promptType = prompt.GetType();
            int nextId = World.Services.Get<IdStorage>().NextIdFor(prompt, promptType, true);
            prompt.AssignID($"{owner.ID}::{TypeNameCache.Name(promptType)}({prompt.ActionName}):{nextId}");
            AddElement(prompt);
            owner.ListenTo(Events.AfterDiscarded, prompt.Discard, prompt);
        }

        void RefreshPromptsPositions() {
            int firstsCount = 0;
            foreach (var prompt in Elements<Prompt>()) {
                prompt.RefreshPosition(ref firstsCount);
            }
        }

        public UIResult HandleMouse(Prompt prompt, UIMouseButtonEvent mouseButtonEvent) {
            return _handler.HandleMouse(prompt, mouseButtonEvent);
        }

        public UIResult Handle(UIEvent evt) {
            if (this.IsActive() && GenericParentModel != null && evt is UIKeyAction action) {
                return _handler.Handle(Elements<Prompt>(), action);
            }

            return UIResult.Ignore;
        }
    }
}