using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit.MarvinUTK {
    public static class UTKLayoutBuilderExtensions {
        public static IUTKContainerBuilder AddVerticalContainer(this IUTKContainerBuilder builder, string name = null, IEnumerable<string> customUssClasses = null) {
            var container = new UTKLayoutBuilder(name, CustomUssClassesToArray(customUssClasses));
            builder.Add(container);
            return container;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static IUTKContainerBuilder AddHorizontalContainer(this IUTKContainerBuilder builder, string name = null, IEnumerable<string> customUssClasses = null) {
            customUssClasses = customUssClasses.Append("horizontal-layout").ToArray();
            var container = new UTKLayoutBuilder(name, CustomUssClassesToArray(customUssClasses));
            builder.Add(container);
            return container;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static IUTKContainerBuilder AddScrollContainer(this IUTKContainerBuilder builder, ScrollViewMode mode, string name = null, IEnumerable<string> customUssClasses = null) {
            var container = new UTKScrollContainerBuilder(mode, name, CustomUssClassesToArray(customUssClasses));
            builder.Add(container);
            return container;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static IUTKContainerBuilder AddLabel(this IUTKContainerBuilder builder, string text, string name = null, IEnumerable<string> customUssClasses = null) {
            var element = new UTKLabelFactory(text, name, CustomUssClassesToArray(customUssClasses));
            return builder.Add(element);
        }
        
        public static IUTKContainerBuilder AddButton(this IUTKContainerBuilder builder, string text, Clickable clickCallback, string name = null, IEnumerable<string> customUssClasses = null) {
            var element = new UTKButtonFactory(text, clickCallback, name, CustomUssClassesToArray(customUssClasses));
            return builder.Add(element);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static IUTKContainerBuilder AddSearchField(this IUTKContainerBuilder builder, string text, EventCallback<ChangeEvent<string>> searchCallback, string name = null, IEnumerable<string> customUssClasses = null) {
            var element = new UTKSearchFieldFactory(text, searchCallback, name, CustomUssClassesToArray(customUssClasses));
            return builder.Add(element);
        }
        
        public static IUTKContainerBuilder AddElement(this IUTKContainerBuilder builder, VisualElement element, string name = null, IEnumerable<string> customUssClasses = null) {
            return builder.Add(new UTKElementFactory(element, name, CustomUssClassesToArray(customUssClasses)));
        }
        
        static string[] CustomUssClassesToArray(IEnumerable<string> customUssClasses) {
            return customUssClasses as string[] ?? customUssClasses?.ToArray() ?? Array.Empty<string>();
        }
    }
}