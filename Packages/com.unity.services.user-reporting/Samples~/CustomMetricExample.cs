using Unity.Services.UserReporting;
using UnityEngine;

/// <summary>
/// This example shows how to use Cloud Diagnostics User Reporting to add custom metric data to a report.
/// </summary>
public class CustomMetricExample : MonoBehaviour
{
    int m_CubeCount;

    [Tooltip("Material used by spawned cubes.")]
    public Material CubeMaterial;

    [Tooltip("Source camera used for the screenshots added to the User Report.")]
    public Camera ScreenshotSource;

    /// <summary>
    /// Spawn a cube and sample the number of cubes in the scene as a custom metric for the report.
    /// </summary>
    public void SpawnCube()
    {
        // Spawn the cube.
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = new Vector3(-5, 5, 0);
        cube.transform.parent = transform;
        var cubeRenderer = cube.GetComponent<Renderer>();
        cubeRenderer.material = CubeMaterial;
        var rigidBody = cube.AddComponent<Rigidbody>();
        rigidBody.velocity = new Vector3(0, 5, 0);
        Debug.Log("Cube spawned.");

        // Update the custom metric.
        m_CubeCount++;
        // Sample the custom metric.
        UserReportingService.Instance.SampleMetric("CubeCount", m_CubeCount);
    }

    /// <summary>
    /// Take a screenshot for the report.
    /// </summary>
    public void TakeScreenshot()
    {
        UserReportingService.Instance.TakeScreenshot(1920, 1080, ScreenshotSource);
        Debug.Log("Screenshot taken.");
    }
}
