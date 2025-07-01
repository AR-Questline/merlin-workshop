#if MODULE_UITK && UNITY_2023_3_OR_NEWER

using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.Core.Extensions;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UIElements;

public class UITKDataBindingExamples : MonoBehaviour
{
    void AddStringBinding()
    {
        #region set-string-binding
        var label = new Label();
        label.text = "Default Text";

        // Add binding to the text property of the label.
        var localizedString = new LocalizedString("My table", "My Entry");
        label.SetBinding("text", localizedString);
        #endregion
    }

    void AddTextureBinding()
    {
        #region set-texture-binding
        var element = new VisualElement();

        // Add binding to the text property of the label.
        var localizedTexture = new LocalizedTexture { TableReference = "My assets", TableEntryReference = "My texture" };
        element.SetBinding("style.backgroundImage", localizedTexture);
        #endregion
    }

    void GetStringBinding()
    {
        #region get-string-binding
        var document = GetComponent<UIDocument>();
        var label = document.rootVisualElement.Q<Label>("my label");

        // Get the binding from the label
        var binding = label.GetBinding("text") as LocalizedString;

        // Update the local variable.
        // Changing the value will trigger an automatic update of the label.
        var score = binding["score"] as IntVariable;
        score.Value++;
        #endregion
    }

    void GetSpriteBinding()
    {
        #region get-sprite-binding
        var document = GetComponent<UIDocument>();
        var label = document.rootVisualElement.Q<Button>("my button");

        // Get the binding from the button
        var binding = label.GetBinding("iconImage.sprite") as LocalizedSprite;

        // Change the sprite, this will trigger an automatic update of the button image.
        binding.TableEntryReference = "A different sprite";
        #endregion
    }

    void CustomBinding()
    {
        #region custom-element-binding
        var element = new MyElement();

        // Add binding to the MyString property
        var localizedString = new LocalizedString { TableReference = "My strings", TableEntryReference = "My string entry" };
        element.SetBinding(nameof(MyElement.MyString), localizedString);

        // Add binding to the MyPrefab property
        var localizedPrefab = new LocalizedGameObject { TableReference = "My assets", TableEntryReference = "My prefab entry" };
        element.SetBinding(nameof(MyElement.MyPrefab), localizedPrefab);
        #endregion
    }

    void CustomAssetBinding()
    {
        #region custom-asset-binding-code
        var element = new MyElementWithAsset();
        var localizedAsset = new LocalizedMyAsset { TableReference = "My table", TableEntryReference = "My entry" };
        element.SetBinding(nameof(MyElementWithAsset.MyAsset), localizedAsset);
        #endregion
    }
}

#region custom-element
[UxmlElement]
public partial class MyElement : VisualElement
{
    [UxmlAttribute, CreateProperty]
    public string MyString { get; set; }

    [UxmlAttribute, CreateProperty]
    public GameObject MyPrefab { get; set; }
}
#endregion

#region custom-asset
[CreateAssetMenu]
public class MyAsset : ScriptableObject
{
    public string someStringValue;
    public int someIntValue;
}

public partial class MyElementWithAsset : VisualElement
{
    [UxmlAttribute, CreateProperty]
    public MyAsset MyAsset { get; set; }
}
#endregion

#region custom-asset-binding
[UxmlObject]
public partial class LocalizedMyAsset : LocalizedAsset<MyAsset>
{
}
#endregion

#region custom-variable
[UxmlObject, Serializable]
public partial class MyDateTimeVariable : IVariable
{
    [UxmlAttribute, Range(1900, 2050)] public int year;
    [UxmlAttribute, Range(0, 12)] public int month;
    [UxmlAttribute, Range(0, 31)] public int day;
    [UxmlAttribute, Range(0, 24)] public int hour;
    [UxmlAttribute, Range(0, 60)] public int min;
    [UxmlAttribute, Range(0, 60)] public int sec;

    public object GetSourceValue(ISelectorInfo _)
    {
        try
        {
            return new DateTime(year, month, day, hour, min, sec);
        }
        catch
        {
            // Ignore issues about incorrect values.
        }
        return new DateTime();
    }
}
#endregion

#endif

