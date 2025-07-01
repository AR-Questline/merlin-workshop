#if MODULE_UITK && UNITY_2023_3_OR_NEWER

#region example
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class LocalizedStringVariablesUIDocumentExample : MonoBehaviour
{
    IntVariable m_Counter;

    void Start()
    {
        var document = GetComponent<UIDocument>();

        var label = new Label();
        document.rootVisualElement.Add(label);

        // Add binding to the text property of the label.
        // Example Smart string "The current count is {counter}."
        m_Counter = new IntVariable { Value = 1 };
        var localizedString = new LocalizedString("My Table", "My Entry");

        // Add the variable
        localizedString.Add("counter", m_Counter);

        label.SetBinding("text", localizedString);

        // Localize the button labels
        var buttonIncrement = new Button();
        var buttonDecrement = new Button();
        buttonIncrement.SetBinding("text", new LocalizedString("My Table", "Increment Button Label"));
        buttonDecrement.SetBinding("text", new LocalizedString("My Table", "Decrement Button Label"));
        buttonIncrement.clicked += () => m_Counter.Value++;
        buttonDecrement.clicked += () => m_Counter.Value--;

        document.rootVisualElement.Add(buttonIncrement);
        document.rootVisualElement.Add(buttonDecrement);
        document.rootVisualElement.Add(label);
    }
}
#endregion

#endif
