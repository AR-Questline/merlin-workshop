using Unity.Services.UserReporting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This example shows how to configure Cloud Diagnostics User Reporting to use a given Unity Project ID
/// Consider the possibility different versions of your game have their own Unity Project IDs, but by using a specific
/// Unity Project ID all reports can be accessed from a single Unity Project dashboard of your choosing
/// </summary>
public class CustomProjectIdentifierExample : MonoBehaviour
{
    [SerializeField]
    InputField m_ProjectIdentifierInput;

    /// <summary>
    /// Configure to use custom input as the Unity Project ID when submitting User Reports
    /// </summary>
    public void Configure()
    {
        if (m_ProjectIdentifierInput is not null)
        {
            UserReportingService.Instance.SetProjectIdentifier(m_ProjectIdentifierInput.text);
        }
        else
        {
            Debug.Log("CustomConfigurationExample is missing project identifier InputField.");
        }
    }
}
