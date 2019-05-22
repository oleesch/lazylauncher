# lazylauncher
Lazylauncher is a small executable that can be included in MSIX packages to perform certain operations on launch of the application.

Currently the following operations are supported:
- Adding registry values (first app launch only)
- Copying folders (first app launch only)
- Passing command line arguments to the target process (every app launch)

At the time of writing there is no easy way to include application configuration in a MSIX package that depends on the users `%appdata%` folder or `HKEY_CURRENT_USER` registry hive. Lazylauncher was created to remedy this shortcoming until Microsoft offers a first party solution.

## Download
If you're unable to build from source you can download a zip file containing the binaries on the [github release page](https://github.com/oleesch/lazylauncher/releases).

## Prerequisites
You need the following executables which are contained in the App Certification Kit of the [Windows 10 SDK](https://developer.microsoft.com/en-us/windows/downloads/windows-10-sdk) in order to edit your MSIX file:

- `makeappx.exe`
- `signtool.exe`

# Usage
## Add to MSIX
In order to add Lazylauncher to your MSIX package, perform the following steps:
1. Unpack the MSIX you need to edit:
```Batchfile
> makeappx unpack /p App.msix /d AppFolder
```
2. Copy and paste the files `lazylauncher.exe`,`llconfig.json` and `Newtonsoft.Json.dll` into the `AppFolder` directory
3. Edit the file `AppFolder\llconfig.json` to include the correct executable paths and operations
4. Edit the file `AppFolder\AppManifest.xml` and replace the "Executable" attribute to point to `lazylauncher.exe`, i.e.:
```XML
<Application Id="YourApplication" Executable="lazylauncher.exe" EntryPoint="Windows.FullTrustApplication">
```
5. Pack the MSIX:
```Batchfile
> makeappx pack /d AppFolder /p App.msix
```
6. Sign the MSIX:
```Batchfile
> signtool sign /a /v /fd sha256 /f Cert.pfx App.msix
```

Here is an unpacked [example MSIX application](https://github.com/oleesch/lazylauncher/tree/master/lazylauncher-example-msix) containing all the necessary edits.

## Configuration
All configuration is done in the `llconfig.json` file.

```jsonc
{
    // Set an application ID (does not have to match the MSIX AppID)
    "ID": "ApplicationID",

    // Provide the full path of the executable that should be launched by Lazylauncher
    // If the executable is in the package root you only need to provide the name
    "ExecutablePath": "C:\\Program Files\\lazylauncher\\lazylauncher-verify.exe",

    // Specify the working directory for the executable, will default to the package root if empty
    "WorkingDirPath": "C:\\Program Files\\lazylauncher",

    // Specify command line arguments that should be passed to the process
    "Arguments": "%APPDATA%\\lazylauncher",

    // An array of copy operations that should be performed on first launch, can be empty
    "CopyOperations": [
        {
            // The origin and destination directory paths
            // (Lazylauncher currently does not support single file copy)
            "OriginPath": "C:\\Program Files\\lazylauncher\\initialconfig",
            "DestinationPath": "%APPDATA%\\lazylauncher"
        }
    ],

    // An array of registry operations that should be performed on first launch, can be empty
    "RegistryOperations": [
        {
            // The target registry key, hives need to be specified using the full name (e.g. HKEY_CURRENT_USER, not HKCU)
            "KeyName": "HKEY_CURRENT_USER\\SOFTWARE\\lazylauncher-verify",

            // The target value name
            "ValueName": "UpdateCheck",

            // The target value
            "Value": "0",

            // The target value kind, has to be one of the following: String, ExpandString, Binary, DWord, QWord
            // Multistring is not supported yet
            "ValueKind": "DWord"
        }
    ]
}
```

## Logging
Lazylauncher provides a logile in `%temp%\lazylauncher.log`. Note that `%temp%` isn't affected by copy on write and can be read from outside the app container.
