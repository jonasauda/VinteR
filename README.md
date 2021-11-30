# VinteR


## Deployment:

1. Deploy the broker (server.py) on a server. The server must be publicly available.

2. Adjust the SERVER_ADDRESS (IP address and port): SERVER_ADDRESS = ("46.101.139.210", 43720) in server.py (Line 5).

3. Start the script.

4. Open the VinteR app in Visual Studio.

5. Install the NuGet packages. Right click on the VinteR C# project in Visual Studio and select "Manage NuGet Packages...".

6. Install/Update the packages needed to run VinteR via the NuGet Manager.


## Configure VinteR

1. Enter the broker IP address/url and port in the "vinter.config.json" at "broker.address".

## Run VinteR

1. Start the project in Visual Studio (Start button).
