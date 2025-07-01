# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

### [2.0.11] - 2024-05-01

### Changed
- Upgraded package dependencies.

## [2.0.10] - 2024-04-25

### Added
- Added Apple Privacy Manifest.

## [2.0.9] - 2024-01-16

### Fixed
- Fixed an issue where some HTTP response codes caused silent failures.

## [2.0.8] - 2024-01-11

### Changed
- Updated package dependencies.

## [2.0.7] - 2024-01-09

### Changed
- Improved UX of the sample.

### Fixed
- Updated package dependencies to respect minimal dependency requirments.

## [2.0.6] - 2023-03-06

### Fixed
- Upgrade package dependencies and sample.

## [2.0.5] - 2023-01-16

### Removed
- Removed a deprecated UnityAnalytics event that could optionally be sent in conjunction with new user reports.

## [2.0.4] - 2022-11-30

### Added
- Internal quality management improvements.

## [2.0.1] - 2022-11-04

### Added
- Added missing dependencies for projects without default dependencies.

## [2.0.0] - 2022-07-19

### Added
- Added an importable package sample with a full test scene that uses the example prefab. To import the sample, visit the User Reporting package in the Package Manager and view the "Samples" tab.
- Added thread safety practices to the SDK, along with test practices to preserve it. Using the SDK off the main Unity thread will automatically start work in the main Unity thread when required.
- Added contemporary Unity package initialization flow and arrangement via the Package Manager to improve visibility and user experience when installing/removing, enabling/disabling, updating, and importing the package and it's new sample.
- Added checks against missing Unity Project ID, and appropriate warning logs when detected.

### Changed
- Changed entire API to use single point of access, `Unity.Services.UserReporting.Instance`, to reduce public API scope and prevent further public API churn.
- Changed the SDK to only manage one report at a time via single point of access, referred to as the ongoing report, and removed access to the User Report class.
- Changed example prefab to no longer rely on access to client and instead use single point of access.
- Changed plugin to use external dependencies instead of `SimpleJSON` parser.
- Changed `UnityUserReportingUpdater` to safely handle updates on the main Unity thread exclusively. In scenes using the SDK you will see a `DontDestroyOnLoad` GameObject named "UserReportingSceneHelper" which takes care of these duties.
- Changed previously public classes such as `CyclicalList` to be either internal or replaced with external dependencies.
- Changes prefab example `Canvas` settings to scale with display.
- Improved performance when taking screenshots by eliminating some periods of waiting.

### Removed
- Removed asynchronous screenshotting functionality.
- Removed `SimpleJSON` parser, `PngHelper`, and `AttachmentExtensions` in favor of external dependencies and to focus the public API of the SDK.

### Fixed
- Fixed a memory leak that could occur when sending reports by adding safety checks to our use of `UnityWebRequest`.

## [1.0.0] - 2019-04-05

### This is the first release of *Unity User Reporting*

### Added
- All source instead of relying on DLLs.

### Changed
- Upgraded from WWW to UnityWebRequest.
- For 2018.3 and above, added a new IUserReportingPlatform for asynchronous screen shots and report generation (DirectX only). To enable this feature, switch the UserReportingPlatform to Async on the UserReporting game object.

### Fixed
- Fixed an issue where successful user report submissions were reporting as an error.
- Fixed a possible memory leak when taking screenshots.
- Fixed various small bugs.

## [0.1.7-preview] - 2018-10-31

### This is the first release of *Unity User Reporting*
