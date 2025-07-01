using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;

#region locale-identifier-1

public class LocaleIdentifierExample1 : MonoBehaviour
{
    void Start()
    {
        // Create a locale identifier to represent English
        var localeEnglishSystemLanguage = new LocaleIdentifier(SystemLanguage.English);
        var localeEnglishCode = new LocaleIdentifier("en");
        var localeEnglishCi = new LocaleIdentifier(CultureInfo.GetCultureInfo("en"));

        Debug.Log(localeEnglishSystemLanguage);
        Debug.Log(localeEnglishCode);
        Debug.Log(localeEnglishCi);
    }
}
#endregion

#region locale-identifier-2

public class LocaleIdentifierExample2 : MonoBehaviour
{
    void Start()
    {
        // Create a Locale to represent English.
        var localeId = new LocaleIdentifier(SystemLanguage.English);
        var locale = Locale.CreateLocale(localeId);
        Debug.Log("English locale: " + locale);

        // Create a regional Locale to represent English UK.
        var regionalLocaleId = new LocaleIdentifier("en-GB");
        var regionalLocale = Locale.CreateLocale(regionalLocaleId);
        Debug.Log("English(en-GB) locale: " + regionalLocale);
    }
}
#endregion

#region culture-info

public class CultureInfoExample : MonoBehaviour
{
    void Start()
    {
        var localeIdentifier = new LocaleIdentifier("en");
        Debug.Log("Code 'en' maps to the CultureInfo: " + localeIdentifier.CultureInfo.NativeName);
    }
}
#endregion
