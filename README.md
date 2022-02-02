# Instructions
> Not all combinations of Inputs are currently supported. It is planned to add more combinations in the future. For now an OptiTrack system is required to merge coordinate systems.

## General Use
### Installation
1. Open the VinteR app in Visual Studio (`VinteR\vinter\VinteR.sln`)
2. Install the NuGet packages
	1. Right click on the VinteR C# project in the Solution Explorer and select "Manage NuGet Packages..."
	2. Install/Update the packages needed to run VinteR via the NuGet Manager. If there is an action that allows you to restore the needed packages just click on "restore" to retrieve packages automatically.
3. Opening "References" in the Solution Explorer fixes most "type or namespace name could not be found"-Errors
4. Press Start in Visual Studio to start VinteR

### Adding Functionality
To add different functionalities to VinteR like OptiTrack, LeapMotion or Kinect, the corresponding Flag has to be defined as a preprocessor directive.

1. Open the Properties of the VinteR C# project in the Solution Explorer
	1.	To see most of the Options, the .NET desktop development feature has to be added to Visual Studio 2019 Community. This can be done by launching the Visual Studio Installer.
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
1. Open `vinter.config.json` in `VinteR\vinter`
2. Add a Receiver by adding it to `udp.receivers` such as:
```
    {
      "ip": "127.0.0.1",
      "port": 6040,
      "hrri": "localhost"
    },
```
3. The `hrri` is used as a unique endpoint identifier

# License
MIT
