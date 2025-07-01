using System.Collections;
using System.Text;
using Unity.Services.UserReporting;
using Unity.Services.UserReporting.Client;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UserReportExample : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The button used to show the User Report submission form.")]
    [SerializeField] Button m_userReportButton;

    [Tooltip("The UI for the User Report submission form.")]
    [SerializeField] GameObject m_userReportForm;

    [Tooltip("The thumbnail viewer for the User Report submission form.")]
    [SerializeField] Image m_thumbnailViewer;

    [Tooltip("The category dropdown for the User Report submission form.")]
    [SerializeField] Dropdown m_categoryDropdown;

    [Tooltip("The input for the summary of the User Report.")]
    [SerializeField] InputField m_summaryInput;

    [Tooltip("The input for the description of the User Report.")]
    [SerializeField] InputField m_descriptionInput;

    [Tooltip("The UI shown while the User Report is submitted.")]
    [SerializeField] GameObject m_submittingPopup;

    [Tooltip("The text for the User Report submission progress display.")]
    [SerializeField] Text m_progressText;

    [Tooltip("The UI shown when there's an error during User Report submission.")]
    [SerializeField] GameObject m_errorPopup;

    [Tooltip("The event raised when a User Report is submitting.")]
    [SerializeField] UnityEvent m_userReportSubmitting;

    [Header("User Reporting Configuration")]
    [Tooltip("Indicates whether each User Report shall include metrics about User Reporting itself.")]
    [SerializeField] bool m_SendInternalMetrics;

    bool m_IsCreatingUserReport;
    bool m_IsShowingError;
    bool m_IsSubmitting;

    async void Start()
    {
        // Initialize all Unity Services, including Cloud Diagnostics
        await UnityServices.InitializeAsync();

        // User Reporting is configured to default values when the Cloud Diagnostics service is initialized, but may be
        // reconfigured at any point during runtime
        // Bear in mind that any unsent report data has already been added to an ongoing report, including screenshots
        // and custom attachments, they will be lost when User Reporting is re-configured
        var customConfig = new UserReportingClientConfiguration(5, 5, 100, 100, MetricsGatheringMode.Automatic);
        UserReportingService.Instance.Configure(customConfig);

        // Set up an EventSystem in the scene, if one doesn't exist already, for UI-triggered events
        if (Application.isPlaying)
        {
            EventSystem sceneEventSystem = FindObjectOfType<EventSystem>();
            if (sceneEventSystem == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }
        }
        // This is the event used to trigger a User Report submission in the UI example
        m_userReportSubmitting = new UnityEvent();
    }

    void Update()
    {
        // For this example the internal metric sampling is configurable in the inspector while the Scene is playing
        // in-editor, so we update each value every frame
        UserReportingService.Instance.SendInternalMetrics = m_SendInternalMetrics;

        // Update the states of all UI elements
        if (m_userReportButton != null)
        {
            m_userReportButton.interactable = State == UserReportingState.Idle;
        }
        if (m_userReportForm != null)
        {
            m_userReportForm.SetActive(State == UserReportingState.ShowingForm);
        }
        if (m_submittingPopup != null)
        {
            m_submittingPopup.SetActive(State == UserReportingState.SubmittingForm);
        }
        if (m_errorPopup != null)
        {
            m_errorPopup.SetActive(m_IsShowingError);
        }
    }

    /// <summary>
    /// Create a new User Report from the contents of the submission form, along with some example custom attachments
    /// </summary>
    public void CreateUserReport()
    {
        // This check ensures we don't override a User Report that is already in mid-creation
        if (m_IsCreatingUserReport)
        {
            return;
        }
        m_IsCreatingUserReport = true;

        // This example adds a custom attachment, an empty JSON file, to the ongoing report
        // Note that we must provide the appropriate IANA media type for the JSON file: "application/json"
        string content = "{}";
        UserReportingService.Instance.AddAttachmentToReport("Sample Attachment JSON", "SampleAttachment.json",
            Encoding.UTF8.GetBytes(content), "application/json");

        // The thumbnail image in the GUI simply uses the latest screenshot available, if any
        SetThumbnail(UserReportingService.Instance.GetLatestScreenshot());

        // Now we create the report using the contents of the submission form along with the attachment and thumnail
        UserReportingService.Instance.CreateNewUserReport();

        // The report has been created, so we may now clear the flag
        m_IsCreatingUserReport = false;
    }

    /// <summary>
    /// Attempt to submit the ongoing User Report
    /// </summary>
    public void SubmitUserReport()
    {
        // This flag ensures we don't attempt multiple submission attempts at once
        if (m_IsSubmitting)
        {
            return;
        }
        m_IsSubmitting = true;

        // The summary of the User Report is used as the report's label when browsing reports on the dashboard
        if (m_summaryInput != null)
        {
            UserReportingService.Instance.SetReportSummary(m_summaryInput.text);
        }

        // User Reports can be sorted on the dashboard via Dimensions and Dimension Values
        // Consider our example dropdown: a "Category" dimension which uses values set in the form's dropdown menu
        if (m_categoryDropdown != null)
        {
            Dropdown.OptionData dropdownValue = m_categoryDropdown.options[m_categoryDropdown.value];
            UserReportingService.Instance.AddDimensionValue("Category", dropdownValue.text);
        }

        // The description of the report is shown when inspecting individual User Reports on the dashboard
        if (m_descriptionInput != null)
        {
            UserReportingService.Instance.SetReportDescription(m_descriptionInput.text);
        }

        // An event to show the submission progress UI
        if (m_userReportSubmitting is not null)
        {
            m_userReportSubmitting.Invoke();
        }

        // Sending a report offers two optional callbacks: one for submission progress updates, and another for when
        // the report submission attempt ends
        UserReportingService.Instance.SendUserReport((uploadProgress) =>
        {
            // The progressUpdate Action uses the uploadProgress float to indicate progress from 0.0 -> 1.0
            if (m_progressText != null)
            {
                m_progressText.text = $"{uploadProgress:P}";
            }
        }, (success) =>
            {
                // The result Action uses the success bool to indicate whether or not the submission succeeded
                if (!success)
                {
                    // The submission attempt was unsuccessful, so show the error display
                    StartCoroutine(ShowError());
                }
                ClearReport();
                m_IsSubmitting = false;
            });
    }

    /// <summary>
    /// Discard the User Report and clear the submission form content
    /// </summary>
    public void ClearReport()
    {
        UserReportingService.Instance.ClearOngoingReport();
        ClearForm();
    }

    /// <summary>
    /// Set the thumbnail preview in the User Report submission form to the given Texture2D
    /// </summary>
    /// <param name="thumbnail">The <see cref="Texture2D"/> to be used as the thumbnail.</param>
    void SetThumbnail(Texture2D thumbnail)
    {
        if (m_thumbnailViewer != null && thumbnail is not null)
        {
            m_thumbnailViewer.sprite = Sprite.Create(thumbnail, new Rect(0, 0, thumbnail.width, thumbnail.height),
                new Vector2(0.5F, 0.5F));
            m_thumbnailViewer.preserveAspect = true;
        }
    }

    // State representing waiting for user action, creating a report, showing the report form, or submitting a report
    enum UserReportingState
    {
        Idle = 0,
        CreatingUserReport = 1,
        ShowingForm = 2,
        SubmittingForm = 3
    }

    // Provide the current UserReportingState of the User Reporting example system
    UserReportingState State
    {
        get
        {
            if (m_IsCreatingUserReport)
            {
                return UserReportingState.CreatingUserReport;
            }
            if (UserReportingService.Instance.HasOngoingReport)
            {
                if (m_IsSubmitting)
                {
                    return UserReportingState.SubmittingForm;
                }
                else
                {
                    return UserReportingState.ShowingForm;
                }
            }

            return UserReportingState.Idle;
        }
    }

    // Coroutine to show the error popup, count to 10 seconds, and then hide the error popup
    IEnumerator ShowError()
    {
        m_IsShowingError = true;
        yield return new WaitForSeconds(10);
        m_IsShowingError = false;
    }

    // Clear the submission form contents and returns it to it's default values
    void ClearForm()
    {
        m_summaryInput.text = null;
        m_descriptionInput.text = null;
        m_categoryDropdown.value = 0;
    }
}
