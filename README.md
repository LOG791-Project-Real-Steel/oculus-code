# Oculus Code
Repository for the code that runs on the Oculus quest 2.

## Getting started

### Setup Unity
These steps are needed to run the uity project with all the necessary dependencies.

1. Start by installing unity hub [here](https://unity.com/download).

2. Clone this repository somewhere on your computer.

3. Go on the unity hub app and go to the projects tab (usually the tab that's already openned when openning the app).

4. Click on `Add > Add project from disk` and select the folder where you cloned this project.

5. You should now see the `oculus-code` project. On that project, you should see a yellow triangle (warning sign). Click on it.
If you don't see the warning sign, continue to step 10.

6. In the new window that just appeared, simply click on the `Install Version 6000.1.3f1` button without changing anything.

7. Now, you should see modules that you can download. You should select the following modules:
    - Android Build Support
    - OpenJDK
    - Android SDK & NDK Tools

    Once that is done, click on the `Continue` button.

8. You can now check the checkbox to say that you have read and agree with the terms and conditions. After that, click on the `Continue` button.

9. And again, check the checkbox to say that you have read and agree with the terms and conditions. After that, click on the `Install` button.

    This process can take a while depending on the PC and internet connection speed that you have. Just be patient. You can go ahead and [setup the ADB](#setup-adb) in the meantime if you want and come back when unity is done installing stuff.

    You can now skip to step 13 since you have already installed the Android dependencies (just launch the project).

10. Go to the `Installs` tab on the left of the app.

11. Click on the Parameter icon button on the installation that is present and then click on `Add modules` (6.1 should be the one that is compatible with the project).

12. Select the following options and click on the `Install` button.\
If they are already selected, simply go to step 13.
    - Android Build Support
    - OpenJDK
    - Android SDK & NDK Tools

13. Once all of this is installed, you can go back to the `Projects` tab and launch the project.

### Setup ADB
ADB (Android Debug Bridge) is a tool which alows us to take the build from unity and upload it to the oculus quest 2 headset.

1. Start by downloading ADB [here](https://developer.android.com/tools/releases/platform-tools).

2. Then, unzip the downloaded zip file somewhere like your `C:` drive as it will be added to your environment variables.

3. To make sure it works, open a command prompt, also called cmd, (not powershell as it won't work) and open the `platform-tools` folder in the folder you just unzipped.

4. Run the following command in that folder (there should be a program called `adb.exe`).

    ```shell
    adb devices
    ```

5. You should see a list with the quest 2 if it's connected to your PC.

    **Important note:** if it shows as `Unauthorized`, you will need to allow the USB connection in the Quest 2 headset. If there are no pop-up showing up in the headset, unplug and replug the usb-c cable in the computer. The pop-up should now show up. You should accept it. the device should now show as `device`.

    What you should see:
    ```shell
    List of devices attached
    1XXXXXXXXXXX	device
    ```

6. Add ADB to your PATH variables.\
In order to make sure ADB is avaible everywhere on your PC, you will want to follow the following steps:

    1. Click on the windows icon in the taskbar of your PC and search for `Edit the system environment variables` then open it.

    2. In the window that just opened, click on the `Environment Variables...` button.

    3. In System variables, select `Path` and click on the `Edit...` button.

    4. Click on the `New` button then on the `Browse...` button. You'll want to go to the `platform-tools` folder that you jsut downloaded and then click on `Ok`.

    5. To make sure that the changes are persisted through all your command prompts, close the ones that are opened and run `adb devices` anywhere on your PC to make sure that it still works.\
    If it doesn't work, make sure you put the right file path and that `adb.exe` is not included in the path that you jsut added.

## Build the unity project
This section will show you how to build the unity project as an android application that is runnable on the oculus quest 2.

1. On the toolbar at the top of the unity application, click on `File > Build Profiles`.

2. It should bring you to the Android platform where you can see `Build` and `Build aqnd Run` buttons. (Check that you are on the Adnroid platform by looking in the left menu and Android should be selected).

3. When you are reaqdy to build the project, click on the `Build` button on the botton left of this window.

4. Next, create a folder somewhere on your PC (not in the repository) where you'll want to store your builds. You can call it `OculusBuilds` for example.

    Then, you'll want to name the build file in this format `oculus-app-X.X.X.apk` with the letters X representing the numbers of the current build version. For example, the first build version is 0.1.0 so the name of the build was `oculus-app-0.1.0.apk`.

    **Important note:** The first build you ever do on your machine can take anywhere from 20 to 30 minutes so, be patient. The following build should be faster depending on what you change in-between builds. 

5. Once the build is finished, you can now deploy it by following the [deploy instructions](#deploy-to-the-oculus-quest-2).

## Deploy to the Oculus Quest 2
This section will walk you through the steps that you need to do to deploy this application on the oculus quest 2.

1. First, open a command prompt (or cmd) and go to the folder where you created the builds for Android that will be uploaded to the oculus quest 2.\
If you followed the build walkthrought, that folder should be called `OculusBuilds`.

2. Now, we'll install the build onto the oculus quest 2 by entering the following command(s) in the command prompt **(Take the time to read the section below to know which commands to run)**:

    If the application has already been installed on the oculus quest 2 from another PC, enter the following command:

    ```shell
    adb uninstall com.realsteelproject.oculusapp
    ```

    Then you should run this command to install the application onto the headset **(Replace the letters X with the version you are currently installing)**:

    ```shell
    adb install oculus-app-X.X.X.apk
    ```

    Lastly, if you are the last person to have intalled a buid onto the headset, you can simply run this command **(Replace the letters X with the version you are currently installing)**:

    ```shell
    adb install -r oculus-app-0.1.0.apk
    ```

3. You just need to send this command and the app should be available as `oculusApp` on the oculus quest 2:

    ```shell
    adb shell am start -n com.realsteelproject.oculusapp/com.unity3d.player.UnityPlayerGameActivity
    ```
