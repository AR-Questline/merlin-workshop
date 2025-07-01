#if PACKAGE_UGUI

using System.Collections;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace UnityEngine.Localization.Samples
{
    /// <summary>
    /// This example shows how a loading screen can be displayed while Localization Initialization/Preloading is being performed.
    /// </summary>
    public class PreloadingScreen : MonoBehaviour
    {
        public GameObject root;
        public Image background;
        public Text progressText;
        public float crossFadeTime = 0.5f;

        WaitForSecondsRealtime waitForSecondsRealtime;

        void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += SelectedLocaleChanged;

            if (waitForSecondsRealtime == null)
                waitForSecondsRealtime = new WaitForSecondsRealtime(crossFadeTime);

            if (!LocalizationSettings.InitializationOperation.IsDone)
                StartCoroutine(Preload(null));
        }

        void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= SelectedLocaleChanged;
        }

        void SelectedLocaleChanged(Locale locale)
        {
            StartCoroutine(Preload(locale));
        }

        IEnumerator Preload(Locale locale)
        {
            root.SetActive(true);
            background.CrossFadeAlpha(1, crossFadeTime, true);
            progressText.CrossFadeAlpha(1, crossFadeTime, true);

            var operation = LocalizationSettings.InitializationOperation;

            do
            {
                // When we first initialize the Selected Locale will not be available however
                // it is the first thing to be initialized and will be available before the InitializationOperation is finished.
                if (locale == null)
                    locale = LocalizationSettings.SelectedLocaleAsync.Result;

                progressText.text = $"{locale?.Identifier.CultureInfo.NativeName} {operation.PercentComplete * 100}%";
                yield return null;
            }
            while (!operation.IsDone);

            if (operation.Status == ResourceManagement.AsyncOperations.AsyncOperationStatus.Failed)
            {
                progressText.text = operation.OperationException.ToString();
                progressText.color = Color.red;
            }
            else
            {
                background.CrossFadeAlpha(0, crossFadeTime, true);
                progressText.CrossFadeAlpha(0, crossFadeTime, true);

                waitForSecondsRealtime.Reset();
                yield return waitForSecondsRealtime;
                root.SetActive(false);
            }
        }
    }
}

#endif
