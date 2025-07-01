using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Awaken.TG.Editor.SimpleTools;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Templates;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Design.Cooking {
    [CreateAssetMenu(menuName = "NpcData/CookingBaseIngredients")]
    public class CookingBaseIngredients : ScriptableObject {
        const string PathToGenericTemplatesFolder = "Assets/Data/Templates/Items/CraftingRecipes/CraftingRecipes/Cooking/Generic";
        public List<CookingIngredient> ingredients;
        public List<CookingResult> results;
        
        /// <summary>
        /// This list is to allow for adding multiple CookingIngredient at once.
        /// </summary>
        [ShowInInspector, HideInPlayMode, OnValueChanged(nameof(AddIngredientsFunction)), PropertyOrder(0), HideReferenceObjectPicker]
        List<Template> _ingredientsToAdd = new();

        void AddIngredientsFunction() {
            IEnumerable<CookingIngredient> toAdd = _ingredientsToAdd.Select(c => {
                var templateRef = new TemplateReference(GetAssetGUID(c));
                string iName = c.name.Split('_').LastOrDefault() ?? "";
                return new CookingIngredient(iName, templateRef);
            }).WhereNotNull().Where(c => ingredients.All(i => i.templateReference != c.templateReference));

            ingredients.AddRange(toAdd);
            _ingredientsToAdd.Clear();
        }

        /// <summary>
        /// This list is to allow for adding multiple CookingResult at once.
        /// </summary>
        [ShowInInspector, HideInPlayMode, OnValueChanged(nameof(AddResultsFunction)), PropertyOrder(0), HideReferenceObjectPicker]
        List<Template> _resultsToAdd = new();
        readonly string[] _keyWords = {"Good", "Weak"};

        void AddResultsFunction() {
            IEnumerable<CookingResult> toAdd = _resultsToAdd.Select(c => {
                var templateRef = new TemplateReference(GetAssetGUID(c));
                string[] nameSplit = c.name.Split('_');
                string iName = nameSplit.LastOrDefault() ?? "";
                if (_keyWords.Contains(iName, StringComparer.InvariantCultureIgnoreCase) && nameSplit.Length > 2) {
                    iName = nameSplit[^2] + nameSplit[^1];
                }
                return new CookingResult(iName, templateRef);
            }).WhereNotNull().Where(c => ingredients.All(i => i.templateReference != c.templateReference));

            results.AddRange(toAdd);
            _resultsToAdd.Clear();
        }

        static string GetAssetGUID(Object template) {
            string result = "";
            string assetPath = AssetDatabase.GetAssetPath(template);
            if (!string.IsNullOrEmpty(assetPath)) {
                result = AssetDatabase.AssetPathToGUID(assetPath);
            } else {
                Log.Important?.Error($"Could not find guid for: '{template.name.ColoredText(Color.red)}' ");
            }

            return result;
        }

        
        // /// <summary>
        // /// Almost identical to DataToExportMap, yet we need other class for importing because Source isn't stored,
        // /// and we need to check whether the Source changed since last export.
        // /// </summary>
        // [UsedImplicitly]
        // sealed class DataToImportMap : ClassMap<IngredientDataToImport> {
        //     public DataToImportMap() {
        //         Map(m => m.component1).Index(0).Name("Comp1");
        //         Map(m => m.component2).Index(1).Name("Comp2");
        //         Map(m => m.component3).Index(2).Name("Comp3");
        //         Map(m => m.result).Index(3).Name("Result");
        //         Map(m => m.quality).Index(4).Name("Quality");
        //     }
        // }
        
        [Button("Generate Generic Cooking Recipes")]
        void ImportFile() {
            ImportCsv(EditorUtility.OpenFilePanel("Select File To Import", "", "csv"));
        }
        
        void ImportCsv(string path) {
            if (!string.IsNullOrWhiteSpace(path) && Path.GetExtension(path) == ".csv") {
                using var stream = File.OpenRead(Path.Combine(path));
                using var writer = new StreamReader(stream);
                // using var csv = new CsvReader(writer, CultureInfo.InvariantCulture);
                // csv.Context.RegisterClassMap<DataToImportMap>();
                // var recipes = csv.GetRecords<IngredientDataToImport>().ToList();
                //
                // using var progressBar = ProgressBar.Create("Creating Recipes");
                // int i = 0;
                // int itemsCount = recipes.Count;
                //
                // AssetDatabase.StartAssetEditing();
                //
                // if (EditorUtility.DisplayDialog("Delete Old", "Delete all templates that are inside Generic folder?", "YES", "NO")) {
                //     var templates = AssetDatabase.FindAssets("t:GameObject", new[] {PathToGenericTemplatesFolder});
                //     foreach (var templateGuid in templates) {
                //         AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(templateGuid));
                //     }
                // }
                //
                // foreach (var recipe in recipes) {
                //     try {
                //         progressBar.DisplayCancellable(i / (float) itemsCount,
                //             $"Creating Recipe ({i}/{itemsCount}): {recipe.component1}/{recipe.component2}/{recipe.component3} - {recipe.result}{recipe.quality}");
                //
                //         List<CookingIngredient> matchingIngredients = new() {
                //             GetCookingIngredient(recipe.component1),
                //             GetCookingIngredient(recipe.component2),
                //             GetCookingIngredient(recipe.component3)
                //         };
                //         matchingIngredients = matchingIngredients.WhereNotNull().ToList();
                //         if (matchingIngredients.Count <= 0) {
                //             continue;
                //         }
                //
                //         string resultName = recipe.result;
                //         if (!recipe.quality.Contains("normal", StringComparison.InvariantCultureIgnoreCase)) {
                //             resultName += recipe.quality;
                //         }
                //
                //         CookingResult result = GetCookingResult(resultName.Replace(" ", ""));
                //         if (result == null) {
                //             Log.Important?.Error($"Failed to find result for {resultName}!");
                //             continue;
                //         }
                //
                //         GameObject gameObject = new(GetCookingResultName(result));
                //         CookingRecipe cookingRecipe = gameObject.AddComponent<CookingRecipe>();
                //
                //         Ingredient[] groupedIngredients = matchingIngredients.GroupBy(c => c.templateReference)
                //             .Select(r => new Ingredient {templateReference = r.Key, Count = r.Count()}).ToArray();
                //         cookingRecipe.Editor_SetIngredients(groupedIngredients);
                //
                //         cookingRecipe.outcome = result.templateReference;
                //
                //
                //         string filePath = GetAssetPath(gameObject.name);
                //         int index = 1;
                //         while (File.Exists(filePath)) {
                //             filePath = GetAssetPath(gameObject.name, index);
                //             index++;
                //         }
                //
                //         PrefabUtility.SaveAsPrefabAsset(gameObject, filePath);
                //
                //         i++;
                //     } catch (Exception e) {
                //         Debug.LogException(e);
                //     }
                // }
                //
                // AssetDatabase.StopAssetEditing();
                // AssetDatabase.Refresh();
            }
        }

        CookingIngredient GetCookingIngredient(string ingredientName) {
            return ingredients.FirstOrDefault(i => i.ingredientName.Equals(ingredientName, StringComparison.InvariantCultureIgnoreCase));
        }

        CookingResult GetCookingResult(string resultName) {
            return results.FirstOrDefault(r => r.resultName.Equals(resultName, StringComparison.InvariantCultureIgnoreCase));
        }

        string GetCookingResultName(CookingResult result) {
            return $"Recipe_Cooking_{result.resultName}";
        }

        string GetAssetPath(string resultName, int index = 0) {
            return $"{PathToGenericTemplatesFolder}/{resultName}_{index}.prefab";
        }
    }

    [Serializable]
    public class IngredientDataToImport {
        public string component1, component2, component3;
        public string result;
        public string quality;
    }

    [Serializable]
    public class CookingIngredient {
        public string ingredientName;
        public TemplateReference templateReference;

        public CookingIngredient(string ingredientName, TemplateReference templateReference) {
            this.ingredientName = ingredientName;
            this.templateReference = templateReference;
        }
    }

    [Serializable]
    public class CookingResult {
        public string resultName;
        public TemplateReference templateReference;

        public CookingResult(string resultName, TemplateReference templateReference) {
            this.resultName = resultName;
            this.templateReference = templateReference;
        }
    }
}
