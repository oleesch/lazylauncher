# lazylauncher
msix launcher for repackaging applications that depend on file configurations in %appdata%

## Download
If you're unable to build from source you can download a zip file containing the binaries on the [github release page](https://github.com/oleesch/lazylauncher/releases).

## Prerequisites
You need the following executables which are be contained in the App Certification Kit of the [Windows 10 SDK](https://developer.microsoft.com/en-us/windows/downloads/windows-10-sdk) in order to edit your MSIX file:

- `makeappx.exe`
- `signtool.exe`

## Usage
1. Unpack the MSIX you need to edit:
```Batchfile
makeappx unpack /p App.msix /d AppFolder
```
2. Copy and paste the files `lazylauncher.exe`,`llconfig.json` and `Newtonsoft.Json.dll` into the `AppFolder` directory
3. Edit the file `AppFolder\llconfig.json` to include the correct executable paths and copy operations
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

Here is an [example MSIX application](https://github.com/oleesch/lazylauncher/tree/master/lazylauncher-example-msix) containing all the necessary edits.
