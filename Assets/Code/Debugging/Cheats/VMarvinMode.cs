using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.TG.Debugging.ModelsDebugs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.UIToolkit.MarvinUTK;
using Awaken.TG.Main.UIToolkit.Utils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Debugging;
using Awaken.Utility.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;
using StringExtensions = Awaken.Utility.Extensions.StringExtensions;

namespace Awaken.TG.Debugging.Cheats {
    [UsesPrefab("Debug/" + nameof(VMarvinMode))]
    public class VMarvinMode : View<MarvinMode> {
        const int WindowWidth = 404;
        const int WindowHeight = 600;

        MethodData[] _staticMethods = Array.Empty<MethodData>();
        MethodData[] _methods = Array.Empty<MethodData>();
        readonly List<MethodData> _allMethods = new();
        readonly List<MethodData> _shownMethods = new();

        DebugWindowUI _debugWindowUI;
        ListView _methodList;
        TextField _searchField;
        string _searchContext = string.Empty;

        const string ParametersParentId = "parametersParent";
        const string ParametersToggleId = "parametersToggle";
        const string CallButtonId = "callButton";
        const string MarvinButtonToggledClass = "marvin-button-toggled";
        const string ParametersButtonText = ">";
        const string ParametersButtonToggledText = "^";
        const string SearchPlaceholderText = "Search methods...";
        
        protected override void OnInitialize() {
            _methods = Target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttribute<MarvinButtonAttribute>() != null)
                .Select(m => {
                   var attribute = m.GetCustomAttribute<MarvinButtonAttribute>();
                   return new MethodData {
                       memberListItem = new MethodMemberListItem(m),
                       isVisible = GetMethod(attribute.Visible),
                       getState = GetMethod(attribute.State)
                   };
                })
                .ToArray();
            
            AnchoredRect position = new(20, WindowHeight / 2f, WindowWidth, new StyleLength(StyleKeyword.Auto), AnchoredPoint.CenterRight);
            var document = World.Services.Get<UIDocumentProvider>().TryGetDocument(UIDocumentType.Marvin);
            _debugWindowUI = new DebugWindowUI(document, position, "Marvin mode", InitUI, () => Target.ToggleView());
            return;

            static MethodInfo GetMethod(string name) {
                return StringExtensions.IsNullOrWhitespace(name) ? null : typeof(MarvinMode).GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        protected override void OnFullyInitialized() {
            DelayFocus().Forget();
        }
        
        async UniTaskVoid DelayFocus() {
            if (await AsyncUtil.DelayFrame(Target)) {
                _searchField.Focus();
            }
        }

        VisualElement InitUI() {
            if (_methods.Length <= 0) {
                return new VisualElement();
            }
            
            _allMethods.AddRange(_methods.Where(method => method.isVisible?.Invoke(Target, Array.Empty<object>()) is not false));
            _shownMethods.AddRange(_allMethods);
            
            UTKLayoutBuilder layoutBuilder = new();
            _searchField = new UTKSearchFieldFactory(SearchPlaceholderText, Filter).Create();
            layoutBuilder.AddElement(_searchField);
            
            _methodList = new ListView();
            _methodList.makeItem = CreateItem;
            _methodList.bindItem = (element, i) => Bind(element, (MethodData)_methodList.itemsSource[i]);
            _methodList.unbindItem = (element, _) => Unbind(element);
            _methodList.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            _methodList.itemsSource = _shownMethods;
            layoutBuilder.AddElement(_methodList);

            ShowAdditionalMethods();
            return layoutBuilder.Build();
        }

        void Filter(ChangeEvent<string> evt) {
            _searchContext = evt.newValue == SearchPlaceholderText ? string.Empty : evt.newValue;
            _shownMethods.Clear();

            if (string.IsNullOrWhiteSpace(_searchContext)) {
                _shownMethods.AddRange(_allMethods);
                _methodList.RefreshItems();
                return;
            }

            _shownMethods.AddRange(_allMethods.Where(m => m.memberListItem.Name.ToLower().Contains(_searchContext.ToLower())));
            _methodList.RefreshItems();
        }
        
        void ShowAdditionalMethods() {
            CollectStaticMethods();
            _allMethods.AddRange(_staticMethods);
            
            if (string.IsNullOrWhiteSpace(_searchContext)) {
                _shownMethods.AddRange(_staticMethods);
                _methodList.RefreshItems();
            }
        }

        static VisualElement CreateItem() {
            UTKLayoutBuilder layoutBuilder = new(customUssClasses: new[] { "full-grow" });
            layoutBuilder.AddVerticalContainer(customUssClasses: new[] { "full-grow", "horizontal-layout" })
                .AddButton(string.Empty, null, CallButtonId, customUssClasses: new[] { "full-grow" })
                .AddButton(string.Empty, null, ParametersToggleId);
            layoutBuilder.AddVerticalContainer(ParametersParentId);
            return layoutBuilder.Build();
        }
        
        void Bind(VisualElement element, MethodData methodData) {
            MethodMemberListItem method = methodData.memberListItem;
            string methodName = method.Name.Normalize();
            methodName = methodName[(methodName.IndexOf('.') + 1)..];
            var button = element.Q<Button>(CallButtonId);
            Toggle(button, true);
            button.text = StringUtil.NicifyName(methodName);
            button.clickable = new Clickable(() => CallButton(button, methodData, method));

            if (methodData.getState != null) {
                bool toggled = (bool)methodData.getState.Invoke(Target, Array.Empty<object>());
                Toggle(button, !toggled);
            }
            
            bool hasParameters = (method.Parameters?.Length ?? 0) > 0;
            var parametersButton = element.Q<Button>(ParametersToggleId);
            parametersButton.text = ParametersButtonText;
            parametersButton.SetActiveOptimized(hasParameters);

            if (hasParameters == false) {
                return;
            }
            
            var parametersParent = element.Q<VisualElement>(ParametersParentId);
            parametersParent.SetActiveOptimized(methodData.foldoutState);
            parametersButton.clickable = new Clickable(() => Foldout(parametersParent, parametersButton, methodData));
            parametersParent.Clear();

            foreach (Parameter parameter in method.Parameters) {
                if (!parameter.IsPrimitive && parameter.IsString == false) {
                    continue;
                }
                
                parametersParent.Add(CreateParameter(method, parameter));
            }
        }

        static void Unbind(VisualElement element) {
            var parametersParent = element.Q<VisualElement>(ParametersParentId);
            if (parametersParent.style.display == DisplayStyle.None) {
                return;
            }
            
            parametersParent.SetActiveOptimized(false);
            var parametersButton = element.Q<Button>(ParametersToggleId);
            parametersButton.text = ParametersButtonText;
        }

        void CallButton(Button button, MethodData methodData, MethodMemberListItem method) {
            if (methodData.getState != null) {
                bool toggled = (bool)methodData.getState.Invoke(Target, Array.Empty<object>());
                Toggle(button, toggled);
            }

            method.TryCall(Target);
            RefreshList();
        }
        
        void RefreshList() {
            _allMethods.Clear();
            _shownMethods.Clear();

            foreach (var data in _methods.Where(data => data.isVisible?.Invoke(Target, Array.Empty<object>()) is not false)) {
                _allMethods.Add(data);
            }
            
            _allMethods.AddRange(_staticMethods);

            _shownMethods.AddRange(string.IsNullOrWhiteSpace(_searchContext)
                ? _allMethods
                : _allMethods.Where(m => m.memberListItem.Name.ToLower().Contains(_searchContext.ToLower())));

            _methodList.RefreshItems();
        }

        static void Toggle(Button button, bool toggledState) {
            if (toggledState) {
                button.RemoveFromClassList(MarvinButtonToggledClass);
            } else {
                button.AddToClassList(MarvinButtonToggledClass);
            }
        }

        static void Foldout(VisualElement parent, Button button, MethodData methodData) {
            bool isActive = !parent.IsActive();
            parent.SetActiveOptimized(isActive);
            methodData.foldoutState = isActive;
            button.text = isActive ? ParametersButtonToggledText : ParametersButtonText;
        }

        static VisualElement CreateParameter(MethodMemberListItem method, Parameter parameter) {
            VisualElement parameterElement = parameter.ParameterType switch {
                { } type when type == typeof(bool) => RegisterParameter(new Toggle(), parameter),
                { } type when type == typeof(float) => RegisterParameter(new FloatField(), parameter),
                { } type when type == typeof(int) => RegisterParameter(new IntegerField(), parameter),
                { } type when type == typeof(string) => RegisterParameter(new TextField(), parameter),
                _ => null
            };
            
            if (parameterElement == null) {
                Log.Important?.Error($"Unsupported parameter type: {parameter.ParameterType} for method {method.Name} at Marvin. Skipping parameter.");
                return new VisualElement();
            }
             
            parameterElement.style.marginLeft = 20;
            return parameterElement;
        }
        
        static VisualElement RegisterParameter<T>(BaseField<T> field, Parameter parameter) {
            field.label = parameter.Name;
            field.value = (T)parameter.Value;
            field.RegisterValueChangedCallback(changeEvent => parameter.Value = changeEvent.newValue);
            return field;
        }

        void CollectStaticMethods() {
            _staticMethods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => (t.IsNotPublic & t.IsSealed & t.IsAbstract) || t.IsSubclassOf(typeof(UGUIWindowDisplay))) // IsSealed && IsAbstract == static class
                .SelectMany(t => t.GetMethods(BindingFlags.NonPublic |
                                              BindingFlags.Public |
                                              BindingFlags.Static |
                                              BindingFlags.FlattenHierarchy))
                .Where(m => m.GetCustomAttribute<StaticMarvinButtonAttribute>() != null)
                .Select(m => {
                    var attribute = m.GetCustomAttribute<StaticMarvinButtonAttribute>();
                    return new MethodData {
                        memberListItem = new MethodMemberListItem(m),
                        isVisible = GetMethod(m, attribute.Visible),
                        getState = GetMethod(m, attribute.State)
                    };
                }).ToArray();
            return;

            static MethodInfo GetMethod(MethodInfo mainMethod, string name) {
                return StringExtensions.IsNullOrWhitespace(name) ?
                    null :
                    mainMethod.DeclaringType?.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
        }
        
        protected override IBackgroundTask OnDiscard() {
            _debugWindowUI?.Hide();
            _debugWindowUI = null;
            return base.OnDiscard();
        }

        class MethodData {
            public MethodMemberListItem memberListItem;
            public MethodInfo isVisible;
            public MethodInfo getState;
            public bool foldoutState;
        }
    }
}