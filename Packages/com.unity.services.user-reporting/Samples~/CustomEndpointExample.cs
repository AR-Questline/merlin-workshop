using Unity.Services.UserReporting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This example shows how to configure Cloud Diagnostics User Reporting to use a given endpoint
/// This feature is useless without an endpoint set up with a means of handling the reports, and will only cause your
/// reports to effectively be discarded if you lack an endpoint that meets such requirements
/// </summary>
public class CustomEndpointExample : MonoBehaviour
{
    [SerializeField]
    InputField m_EndpointInput;

    /// <summary>
    /// Configure to use custom input as the endpoint when submitting User Reports
    /// </summary>
    public void Configure()
    {
        if (m_EndpointInput is not null)
        {
            UserReportingService.Instance.SetEndpoint(m_EndpointInput.text);
        }
        else
        {
            Debug.Log("CustomEndpointExample is missing endpoint InputField.");
        }
    }
}
