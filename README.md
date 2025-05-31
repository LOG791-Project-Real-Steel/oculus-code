# oculus-code
Repository for the code that runs on the Oculus quest 2.

## Getting started

### Setup Unity
These steps are needed to run the uity project with all the necessary dependencies.

1. Start by installing unity hub [here](https://unity.com/download).
2. Clone this repository somewhere on your computer.
3. Go on the unity hub app and go to the projects tab (usually the tab that's already openned when openning the app).
4. Click on `Add > Add project from disk` and select the folder where you cloned this project.
5. **!! Might need to add some more instructions here since when importing a project, unity hub asks for installation steps.**
6. Go to the `Installs` tab on the left of the app.
7. Click on the Parameter icon button on the installation that is present and then click on `Add modules` (6.1 should be the one that is compatible with the project).
8. Select the following options and click on the `Install` button.
    - Android Build Support
    - OpenJDK
    - Android SDK & NDK Tools
9. Once all of this is installed, you can go back to the `Projects` tab and launch the project.

### Setup ADB
ADB (Android Debug Bridge) is a tool which alows us to take the build from unity and upload it to the oculus quest 2 headset.

1. Start by downloading ADB [here](https://developer.android.com/tools/releases/platform-tools).
2. Then, unzip the downloaded zip file somewhere like your `C:` drive as it will be added to your environment variables.
3. To make sure it works, open a command prompt, also called cmd, (not powershell as it won't work) and open the `platform-tools` folder in the folder you just unzipped.
4. Run the following command in that folder (there should be a program called `adb.exe`).
```shell
adb devices
```
5. You should see a list with the quest 2 if it's connected to your PC.\
\
**Important note:** if it shows as `Unauthorized`, you will need to allow the USB connection in the Quest 2 headset. If there are no pop-up showing up in the headset, unplug and replug the usb-c cable in the computer. The pop-up should now show up. You should accept it. the device should now show as `device`.\
\
What you should see:
```shell
List of devices attached
1XXXXXXXXXXX	device
```

---

**This part needs to be completed**
## Build the unity project
4. check the lines to write in the cmd to build, install and run the app on the oculus 

## Deploy to the Oculus Quest 2
Install brand new version onto the adb (oculus)
adb install oculus-app-0.1.0.apk

Update an already installed version
adb install -r oculus-app-0.1.0.apk

Send app to oculus
adb shell am start -n com.realsteelproject.oculusapp/com.unity3d.player.UnityPlayerGameActivity

