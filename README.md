# VinteR


# Deployment and Installation:

As this project makes heavy use of OptiTrack, Leap Motion and Microsoft Kinect in specific versions, you have to install some software to get things working.

The leap motion requires the .NET Framework 4.5.x! 4.6 and higher might work but are not tested. For Windows 10 users the kinect sdk has to be installed in windows 7 compatibility mode! Otherwise runtime errors are going to occure. To get the frames from the Leap Motion, you need to install the Leap Motion Orion Software on your machine, for building or running the solution it is not necessary. If you want use a gitlab runner instance on your machine to execute continuous integration Visual Studio 2017 Enterprise is required. Take a look at the `.gitlab-ci.yml`. There is an absolute path to the `MSBuild.exe`.

## Requirements

- .NET Framework 4.5.x
- Microsoft Kinect SDK 1.8
- Leap Motion Developer Kit (Orion) 3.2.1
- Microsoft Visual Studio 2017 Enterprise (only for gitlab runner)

## Installation

1. Deploy the broker (server.py) on a server. The server must be publicly available.

2. Adjust the SERVER_ADDRESS (IP address and port): SERVER_ADDRESS = ("46.101.139.210", 43720) in server.py (Line 5).

3. Start the script.

4. Open the VinteR app in Visual Studio (VinteR\vinter\VinteR\VinteR.sln).

5. Install the NuGet packages. Right click on the VinteR C# project in the Solution Explorer and select "Manage NuGet Packages...".

6. Install/Update the packages needed to run VinteR via the NuGet Manager. If there is an action that allows you to restore the needed packages just click on "restore" to retreive packages automatically.

## Configure VinteR

1. Enter the broker IP address/url and port in the "vinter.config.json" at "broker.address".
2. in "udp.receivers" the locations are definded. For location "LOCATION-1" enter your IP address and port. For the remote location enter the remote IP address and port.
3. You may need to configure port forwarding on your router.

## Run VinteR

1. Start the project in Visual Studio (Start button).
