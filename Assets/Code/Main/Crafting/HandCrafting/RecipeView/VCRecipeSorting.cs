using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.HandCrafting.RecipeView {
    public class VCRecipeSorting : ViewComponent<RecipeTabContents>, IPromptHost {
        [SerializeField] VGenericPromptUI sortPrompt;
        public Transform PromptsHost => transform;

        readonly RecipeSorting[] _allowedComparers = {
            RecipeSorting.AlphabeticallyAscending,
            RecipeSorting.AlphabeticallyDescending,
            RecipeSorting.ByPriceAscending,
            RecipeSorting.ByPriceDescending,
        };

        RecipeSorting[] _comparers;
        Prompt _sortPrompt;
        int _currentComparer;

        protected override void OnAttach() {
            UpdateComparersArray();
            var prompts = Target.AddElement(new Prompts(this));
            _sortPrompt = Prompt.Tap(KeyBindings.UI.Items.SortItems, LocTerms.UIItemsChangeSorting.Translate(), NextSorting).AddAudio();
            prompts.BindPrompt(_sortPrompt, Target, sortPrompt);
            _currentComparer = _comparers.IndexOf(Target.RecipeGridUI.CurrentSorting);
            if (_currentComparer < 0) _currentComparer = 0;
            Refresh().Forget();
        }

        void UpdateComparersArray() => _comparers = _allowedComparers.Where(c => c.IsAvailable).ToArray();

        public void NextSorting() {
            _currentComparer = (_currentComparer + 1) % _comparers.Length;
            Refresh().Forget();
        }

        async UniTaskVoid Refresh() {
            await UniTask.DelayFrame(1);
            var comparer = _comparers[_currentComparer];
            string comparerName = comparer.Name;
            RefreshPromptName(_sortPrompt, $"{LocTerms.UIItemsChangeSorting.Translate().ColoredText(ARColor.MainGrey).FontLight()} {comparerName.ColoredText(ARColor.MainWhite).FontSemiBold()}");
            Target.RecipeGridUI.ChangeItemsComparer(comparer);
        }
        
        void RefreshPromptName(Prompt prompt, string name) {
            prompt.ChangeName(name);
        }
    }
}