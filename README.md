# Instructions
> Not all combinations of Inputs are currently supported. It is planned to add more combinations in the future. For now an OptiTrack system is required to merge coordinate systems.

## General Use
### Installation
1. Open the VinteR app in Visual Studio (`vinter\VinteR.sln`)
	1. Tested with VS 2022 Community
	2. App is updated to .NET 4.8
2. Install the NuGet packages
	1. Right click on the VinteR C# project in the Solution Explorer and select "Manage NuGet Packages..."
	2. Install/Update the packages needed to run VinteR via the NuGet Manager. If there is an action that allows you to restore the needed packages just click on "restore" to retrieve packages automatically.
3. Opening "References" in the Solution Explorer fixes most "type or namespace name could not be found"-Errors
4. Press Start in Visual Studio to start VinteR

### Adding Functionality
To add different functionalities to VinteR like OptiTrack, LeapMotion or Kinect, the corresponding Flag has to be defined as a preprocessor directive.

1. Open the Properties of the VinteR C# project in the Solution Explorer
	1.	To see most of the Options, the .NET desktop development feature has to be added. This can be done by launching the Visual Studio Installer.
2.	In the "Build"-Tab is a field to enter own flags.

The next sections will describe requirements for each functionality and what Flag to define to activate it.

## OptiTrack

__NatNet SDK__
1. Download the NatNet SDK from the OptiTrack Website
2. Copy the "lib" folder into "vinter/"
3. (Re-Add the Reference in Visual Studio if necessary)

> It is assumed that an OptiTrack System and Software like Motive as a NatNet Server is present and configured to stream data to NatNet.

__Activation__
Make sure the compiler Flag "OPTITRACK" is set in the Build-Tab.

## LeapMotion
__Orion 3.2 + SDK__
1. Download Legacy API for Leap Motion Orion 3.2 from Ultraleap
2. Install the Orion Software and configure it
3. Copy the LeapCSharp.NET4.5.dll and the content of "lib/x64" into the project's "vinter/lib/x64"
3. (Re-Add the Reference in Visual Studio if necessary)
4. The Leap Motion is able to automatically rotate its coordinate system depending on the position and rotation of the hands. This should be disabled in the Orion software's settings to ensure a stable coordinate system. This can be done in the general settings (`Automatically align tracking`).

__Activation__
Make sure the compiler Flag "LEAP" is set in the Build-Tab.

## Kinect
__Currently not working__

## Peer & HoloRoom

__Currently not working__

### Adding Receivers
1. Open `vinter.config.json` in `vinter`
2. Add a Receiver by adding it to `udp.receivers` such as:
```
    {
      "ip": "127.0.0.1",
      "port": 6040,
      "hrri": "localhost"
    },
```
3. The `hrri` is used as a unique endpoint identifier

## Adding Unity Receiver

1. Import the Assets from the "Unity Files" Folder into the project.
2. Add the "Vinter Receiver" Script to a Game Object that is visible to any tracked object.
	1. Only one receiver is necessary per Scene.
	2. Make sure the Port is configured as a receiver in the vinter config
3. Add the "Tracker" Script to a Game Object that should be moved.
	1. Enter the Name of the Motive RigidBody under "Motive Name"
	2. (Optional) Configure a static position offset under "Offset"
	3. (Optional) If you track a HMD, the Rotation should be provided by the HMD's tracking for a smoother experience. Therefore the Tracker needs to provide "Position Only" and the XR System needs to provide "Rotation Only". The first Frame will sync both rotations.
	4. (Optional) if the Tracked Object is Leap Motion Hands, check the checkbox "Is Leap Hands"
	5. (Optional) If the Motion should be smoothed, a Dampening can be configured under "Dampening"
	6. (Optional) If the Game World should be initialized on a specific real world position, configure a Tracker as Init Point and add the Parent Game Object of the Map as "Map"


# Legacy Readme

## GitLab Runner

On the GitLab is a [.NET project](https://git.uni-due.de/VinteR/TheApplication) created which has CI configurations. Unfortunately, CI jobs cannot run on the GitLab because an installed Visual Studio 2017 Enterprise is required. Currently, the jobs are running on different developer machines. A GitLab runner has been installed and registered for this purpose. This was done according to the following instructions:

1. [Installation](https://docs.gitlab.com/runner/install/windows.html)
2. [Registration](https://docs.gitlab.com/runner/register/index.html#windows)

Registration requires a token to authorize the Runner to send results back to GitLab. In the[CI Settings](https://git.uni-due.de/VinteR/TheApplication/settings/ci_cd) -> Runner Settings -> Specific Runners -> Setup a specific Runner manually you can see an installation manual containing the token.

If the Runner is successfully installed and registered, the configuration file `config.toml` must be added to the gitlab runner exe. If necessary, this is already available. For the Runner to run from the Powershell, the file must be modified:

```ini
executor = "shell"
shell = "powershell"
```

To execute the runner locally execute the following command in a powershell instance.

```console
"C:\GitLab-Runner\gitlab-runner.exe" exec shell build --shell=powershell
```

This command must be executed inside the folder that contains the solution (`VinteR.sln`).

## Logging Output
NLogger is used for logging. This component can log different levels (e.g. INFO or DEBUG) for the whole project or for single namespaces. These settings are placed in a file named `NLog.config`. It can be defined, how the information is displayed and multiple rules can be created. The output can be logged to the console or to a file.

## Writing tests

Unit and integration tests can be written with the use of [NUnit](https://github.com/nunit/docs/wiki/). Write your tests inside the `VinteR.Tests` project. All test classes must be annotated with the `[TestFixture]` attribute. Test methods must also be annotated with the `[Test]` attribute. All tests are executed on the continuous integration system. If you want to execute your tests on your local machine open a terminal window and navigate to the `TheApplication` folder. To run all tests there are two options. There is a `run_unit_tests.ps1` file inside the root of the application. Inside a powershell instance you can simply run the script. The execution policy of your computer may have to be updated:

1. run a powershell instance as administrator
2. type: `Set-ExecutionPolicy "RemoteSigned"`
3. approve the question

Otherwise you have to make a copy of the `vinter.config.json` and `vinter.config.schema.json` to the root directory. Otherwise the application is not able to load the config.

```console
# copy configuration
xcopy VinteR\vinter.config.json .
xcopy VinteR\vinter.config.schema.json .

# run the tests
VinteR\packages\NUnit.ConsoleRunner.3.8.0\tools\nunit3-console.exe .\VinteR.Tests\bin\x64\Debug\VinteR.Tests.dll
```


# License
MIT
