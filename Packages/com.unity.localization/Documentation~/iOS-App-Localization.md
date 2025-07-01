# iOS App Localization

The Localization package provides support for localizing values within the iOS `Info.plist` information property list file. When building the iOS player, the Localization values are applied to the project during the post build step.
The projects' known regions are populated with the project Locales and an [Info.Plist](https://developer.apple.com/library/archive/documentation/General/Reference/InfoPlistKeyReference/Introduction/Introduction.html) variant is added for each Locale.

To configure the App localization, add the **iOS/App** Info **Metadata** to the Localization Settings **Metadata**.

![To configure the App localization, add the iOS/App Info Metadata to the Localization Settings Metadata.](images/iOSAppInfoMetadata.png)

The following fields can be localized in the App:

Values that are not set are left unlocalized and use their default values.

| **Name**                          | **Info.Plist Key** | **Description** |
| --------------------------------- | ------------------ | --------------- |
| **Short Name**                    | [CFBundleName](https://developer.apple.com/documentation/bundleresources/information_property_list/cfbundlename) | The user-visible name for the bundle, used by Siri and visible on the iOS Home screen. This name can contain up to 15 characters.
| **Display Name**                  | [CFBundleDisplayName](https://developer.apple.com/documentation/bundleresources/information_property_list/cfbundledisplayname) | The user-visible name for the bundle, used by Siri and visible on the iOS Home screen. Use this key if you want a product name that's longer than Bundle Name.
| **Camera Usage Description**      | [NSCameraUsageDescription](https://developer.apple.com/documentation/bundleresources/information_property_list/nscamerausagedescription) | A message that tells the user why the app is requesting access to the device’s camera. This replaces the value in the Project Settings.
| **Microphone Usage Description**  | [NSMicrophoneUsageDescription](https://developer.apple.com/documentation/bundleresources/information_property_list/nsmicrophoneusagedescription) | A message that tells the user why the app is requesting access to the device’s microphone. This replaces the value in the Project Settings.
| **Location Usage Description**    | [NSLocationWhenInUseUsageDescription](https://developer.apple.com/documentation/bundleresources/information_property_list/nslocationwheninuseusagedescription) | A message that tells the user why the app is requesting access to the user’s location information while the app is running in the foreground. This replaces the value in the Project Settings.
| **User Tracking Usage Description** | [NSUserTrackingUsageDescription](https://developer.apple.com/documentation/bundleresources/information_property_list/nsusertrackingusagedescription) | A message that informs the user why an app is requesting permission to use data for tracking the user or the device. |

## Configure through script

The App Info can also be configure via script. Please note that this configuration must be completed prior to the application build, otherwise, the changes will not be applied.

[!code-cs[](../DocCodeSamples.Tests/IosAppInfoExample.cs)]
