using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Crafting.HandCrafting;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Crafting.Slots;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Maths;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Crafting {
    public class VCItemLevelOutcomesInfo : ViewComponent<IRecipeCrafting> {
        const float RoundFactor = 0.5f;
        [SerializeField] TextMeshProUGUI exceptedOutcomeText;
        [SerializeField] TextMeshProUGUI chancesText;
        
        public int CurrentDrawnItemLevel { get; set; }
        public CraftingResultQuality CurrentDrawnItemLevelQuality { get; set; }
        
        protected override void OnAttach() {
            SetupDefaultTitle();
            gameObject.SetActive(false);
            Target.ListenTo(IRecipeCrafting.Events.OnRecipeChanged, OnRecipeChanged, this);
            Target.ListenTo(Crafting.Events.OnRecipeCrafted, OnRecipeChanged, this);
            Target.ListenTo(Model.Events.AfterElementsCollectionModified, OnWorkbenchSlotAdded, this);
        }

        public void RefreshCurrentItemLevel() {
            UpdateLevelChances();
        }

        void OnWorkbenchSlotAdded(Element element) {
            if (element is EditableWorkbenchSlot {HasBeenDiscarded: false} workbenchSlot) {
                workbenchSlot.ListenTo(EditableWorkbenchSlot.Events.OnIngredientQuantityChanged, UpdateLevelChances, this);
            }
        }

        void OnRecipeChanged(IRecipe recipe) {
            gameObject.SetActive(recipe.CanHaveItemLevel);
            CurrentDrawnItemLevel = 0;
            CurrentDrawnItemLevelQuality = CraftingResultQuality.Regular;
            Target.RefreshTooltipDescriptor(0);

            if (recipe.CanHaveItemLevel) {
                UpdateLevelChances();
            }
        }

        void UpdateLevelChances() {
            bool validRecipe = Target.CurrentRecipe is { CanHaveItemLevel: true };
            
            if (!validRecipe || !CraftingUtils.IsRecipeCraftable(Target.CurrentRecipe, Target)) {
                SetupDefaultTitle();
                return;
            }

            float mainMean = CraftingUtils.GetItemLevelForCrafted(Target.CurrentRecipe);
            float hiddenMean = mainMean + CraftingUtils.CalculateBonusLevelFromIngredients(Target.CurrentRecipe, Target.WorkbenchCraftingItems);
            int minLevel = math.max((int)mainMean - 2, BaseRecipe.GarbageItemThreshold);
            int maxLevel = minLevel + 4;
            float sigma = 2f / 3f; // <- from 3 sigma rule when mean=0
            var chanceLevels = new List<(float chance, int level)>();

            int level0index = -1;
            for (int itemLevel = minLevel; itemLevel <= maxLevel; itemLevel++) {
                float chance;
                if (itemLevel == minLevel) {
                    chance = (float) (NormalDistribution.CDF(hiddenMean, sigma, itemLevel + RoundFactor) - NormalDistribution.CDF(hiddenMean, sigma, float.NegativeInfinity));
                } else if (itemLevel == maxLevel) {
                    chance = (float) (NormalDistribution.CDF(hiddenMean, sigma, float.PositiveInfinity) - NormalDistribution.CDF(hiddenMean, sigma, itemLevel - RoundFactor));
                } else {
                    chance = (float) (NormalDistribution.CDF(hiddenMean, sigma, itemLevel + RoundFactor) - NormalDistribution.CDF(hiddenMean, sigma, itemLevel - RoundFactor));
                }
                
                chance *= 100f;
                if (itemLevel <= 0 && Hero.Current.Development.RemoveNegativeLevelsForCraftingItems) {
                    if (level0index >= 0) {
                        chanceLevels[level0index] = (chanceLevels[level0index].chance + chance, 0);
                    } else {
                        chanceLevels.Add((chance, 0));
                        level0index = chanceLevels.Count - 1;
                    }
                    continue;
                }
                chanceLevels.Add((chance, itemLevel));
            }
            
            var maxChanceLevel = chanceLevels.MaxBy(c => c.chance);
            var lowerLevelChance = chanceLevels.Where(c => c.level < maxChanceLevel.level).Sum(c => c.chance);
            var betterLevelChance = chanceLevels.Where(c => c.level > maxChanceLevel.level).Sum(c => c.chance);
            
            string outcomeText = string.Empty;
            if (lowerLevelChance > 0f) {
                outcomeText += $"{LocTerms.ExceptedCraftingChanceForWorse.Translate()}: {lowerLevelChance:F1}%\n";
            }
            
            if (betterLevelChance > 0f) {
                outcomeText += $"{LocTerms.ExceptedCraftingChanceForBetter.Translate()}: {betterLevelChance:F1}%\n";
            }
            
            float invCdf = RandomUtil.NormalDistribution(hiddenMean, sigma, false);
            int roundInvCdf = Mathf.Clamp(Mathf.RoundToInt(invCdf), minLevel, maxLevel);
            CurrentDrawnItemLevel = roundInvCdf <= 0 && Hero.Current.Development.RemoveNegativeLevelsForCraftingItems 
                ? 0
                : roundInvCdf;
            
            int expectedLevel = maxChanceLevel.level;
            if(CurrentDrawnItemLevel == BaseRecipe.GarbageItemThreshold || CurrentDrawnItemLevel < expectedLevel) {
                CurrentDrawnItemLevelQuality = CraftingResultQuality.Poor;
            } else if (CurrentDrawnItemLevel > expectedLevel) {
                CurrentDrawnItemLevelQuality = CraftingResultQuality.Great;
            } else {
                CurrentDrawnItemLevelQuality = CraftingResultQuality.Regular;
            }
            
            chancesText.SetText(outcomeText);

            string outcomeChance = $"{maxChanceLevel.chance:F1}%".ColoredText(ARColor.MainAccent);
            exceptedOutcomeText.SetText(maxChanceLevel.level == BaseRecipe.GarbageItemThreshold
                ? $"{LocTerms.ExceptedCraftingOutcome.Translate()}: {LocTerms.ItemTypeGarbage.Translate()} {outcomeChance}"
                : $"{LocTerms.ExceptedCraftingOutcome.Translate()}: {outcomeChance}");

            Target.RefreshTooltipDescriptor(maxChanceLevel.level);
        }
        
        void SetupDefaultTitle() {
            exceptedOutcomeText.SetText(LocTerms.ExceptedCraftingOutcome.Translate());
            chancesText.SetText(string.Empty);
        }
    }
}