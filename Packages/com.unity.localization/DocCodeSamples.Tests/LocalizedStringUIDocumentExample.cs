#if MODULE_UITK && UNITY_2023_3_OR_NEWER

#region example
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

public class LocalizedStringUIDocumentExample : MonoBehaviour
{
    void Start()
    {
        var document = GetComponent<UIDocument>();

        var label = new Label();
        label.text = "Default Text";
        document.rootVisualElement.Add(label);

        // Add binding to the text property of the label.
        var localizedString = new LocalizedString("My table", "My Entry");
        label.SetBinding("text", localizedString);
    }
}
#endregion

#endif
