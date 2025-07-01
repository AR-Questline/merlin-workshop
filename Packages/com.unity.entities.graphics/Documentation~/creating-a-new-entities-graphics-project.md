# Install Entities Graphics

Entities Graphics collects the data necessary for rendering entities and sends this data to Unity's existing rendering architecture. Entities Graphics supports the [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html) (URP) and the [High Definition Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html) (HDRP). It doesn't support the [Built-in Render Pipeline](xref:built-in-render-pipeline).

## Create a new project with Entities Graphics

The simplest way to install Entities Graphics is to use Unity's project templates to create a new project. The templates set up the project with some necessary packages and with the correct configuration.

1. Open [Unity Hub](https://unity.com/unity-hub) and create a new project. Depending on which render pipeline you want to use with Entities Graphics, use one of the following templates:
	* For URP, use the **3D (URP)** template.
	* For HDRP, use the **3D (HDRP)** template.
2. Install the Entities Graphics package. Installing the Entities Graphics package also installs its dependencies including the ECS-related packages.
	* Open the Package Manager window (**Window > Package Manager**)
	* [Add the package by its name](xref:upm-ui-quick) (com.unity.entities.graphics)

## Install Entities Graphics into an existing project

If you want to install Entities Graphics into an existing project, there are manual configuration steps that you must complete.

1. Install either URP or HDRP. For more information, see [Installing the Universal Render Pipeline into an existing Project](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest?subfolder=/manual/InstallURPIntoAProject.html) or [Upgrading to HDRP from the built-in render pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/Upgrading-To-HDRP.html).
2. Install the Entities Graphics package. Installing the Entities Graphics package also installs its dependencies including the ECS-related packages.
	* Open the Package Manager window (**Window > Package Manager**)
	* [Add the package by its name](xref:upm-ui-quick) (com.unity.entities.graphics)
3. Make sure SRP Batcher is enabled in your project's URP or HDRP Assets.
   * **URP**: Select the [URP Asset](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest?subfolder=/manual/universalrp-asset.html) and view it in the Inspector, go to **Advanced** and make sure **SRP Batcher** is enabled.
   * **HDRP**: Select the [HDRP Asset](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/index.html) and view it in the Inspector, enter [Debug Mode ](https://docs.unity3d.com/Manual/InspectorOptions.html)for the Inspector, and make sure **SRP Batcher** is enabled.
4. Entities Graphics doesn't support gamma color space. To set your 
 project to use linear correct space:
   1. Go to **Edit** > **Project Settings** > **Player** > **Other Settings** and locate the **Color Space** property.
   2. Set **Color Space** to **Linear**.