# HedgeModManager UWP

An unofficial Xbox Developer Mode patch for HedgeModManager UWP. It manages mods for Unleashed Recompiled through a controller-friendly interface.

[Releases](https://github.com/mateusmol-bruh/HedgeModManager-UWP/releases) · [Installation](#install-on-xbox) · [Build the patch](#build-the-patch)

## Features

- Enable, disable, reorder, and configure individual mods.
- Edit options provided by a mod's configuration schema, including supported settings stored in `mod.ini`.
- Install mods through GameBanana links and check for updates from supported mod sources.
- Choose English (U.S., default), Spanish, Brazilian Portuguese, Italian, or French. The app remembers your selection.

## Install on Xbox

You'll need an x64 Xbox console in **Developer Mode**, a PC on the same local network, and an existing Unleashed Recompiled installation. The public release provides the patch source. To install this version, use an authorized test package or build and sign one from an authorized copy of the base UWP package.

### Installation files

| File | Purpose |
| --- | --- |
| `HedgeModManager.Uwp_0.1.7.0_x64_Improved.msix` | Signed app package to upload to your test console. |
| `Microsoft.VCLibs.x64.Debug.14.00.appx` | x64 Visual C++ Debug framework, version **14.0.33519.0** or later. |
| `Microsoft.NET.CoreRuntime.2.2.appx` | x64 .NET Core Runtime framework, version **2.2.31331.1** or later. |
| `Microsoft.NET.CoreFramework.Debug.2.2.appx` | x64 .NET Core Debug framework, version **2.2.31327.1** or later. |

Obtain the exact x64 framework packages from your authorized development setup. The app manifest requests the Debug identities listed above. Microsoft's redistribution list identifies the retail Visual C++ package separately. [Visual Studio 2022 redistribution list](https://learn.microsoft.com/en-us/visualstudio/releases/2022/redistribution) and [Microsoft guidance on deploying Debug builds](https://learn.microsoft.com/en-us/cpp/windows/preparing-a-test-machine-to-run-a-debug-executable?view=msvc-170).

### Deploy with Xbox Device Portal

1. Boot your Xbox into Developer Mode and open **Dev Home**.
2. Open **Remote Access Settings**, enable **Xbox Device Portal**, and set a username and password.
3. From the PC, open the Device Portal address shown in Dev Home and sign in.
4. On the portal's **Home** page, open the app installer and select the signed `.msix`.
5. Add the three x64 `.appx` files as dependencies if the console does not already have compatible versions installed. Start the deployment and wait for it to finish.
6. Open **HedgeModManager UWP** from Dev Home. In **My mods**, select the mods folder used by your Unleashed Recompiled installation. The app also tries `E:\Unleashed\mods` at startup.
7. Enable and reorder the mods you want, select **Save changes**, and restart the game to load the new order.

See [Microsoft's Xbox Device Portal guide](https://learn.microsoft.com/en-us/previous-versions/windows/uwp/xbox-apps/device-portal-xbox).

## Controller shortcuts

| Control | Action |
| --- | --- |
| **A** on a mod row | Enable or disable that mod |
| **X** | Configure the selected mod |
| **Y** | Enable or disable the selected mod |
| **LT / RT** | Move the selected mod up / down by one position |
| **LB / RB** | Switch tabs |

The top of the list has the highest priority. Save changes to apply activation and priority. Use the language selector in the upper-right corner to change the interface language.

## Build the patch

The patch requires an authorized copy of the original `HedgeModManager.Uwp_0.1.1.0_x64_Debug.msix`. The repository contains the patch code and build script.

1. Install PowerShell 7, the .NET 6 SDK, and Windows SDK **10.0.19041.0** with the packaging tools.
2. From the repository root, run:

   ```powershell
   pwsh -File .\src\Build.ps1 -OriginalMsix 'C:\path\to\HedgeModManager.Uwp_0.1.1.0_x64_Debug.msix'
   ```

3. Find the resulting MSIX in `src\build\`. Sign the package with your development certificate before deploying it. The certificate subject must match the manifest's publisher, `CN=HedgeModManagerUwp`. [Microsoft's package-signing guide](https://learn.microsoft.com/en-us/windows/msix/package/sign-app-package-using-signtool).

To run the automated checks:

```powershell
dotnet run --project .\src\Tests\Tests.csproj --configuration Release
```

## Status and credits

Version 0.1.7.0 adds language selection and changes how LT and RT are handled to prevent the list from jumping a page during reordering. The build and automated checks passed. The updated trigger behavior still needs verification with a physical Xbox controller.

This independent patch builds on HedgeModManager UWP. See the [Hedge Mod Manager desktop project](https://github.com/hedge-dev/HedgeModManager) and [Unleashed Recompiled](https://github.com/hedge-dev/UnleashedRecomp) for upstream projects and support.
