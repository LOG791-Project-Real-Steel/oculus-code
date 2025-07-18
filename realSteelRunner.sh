#!/bin/bash

cd oculus-build && adb uninstall com.realsteelproject.oculusapp && adb install oculus-app-0.1.0.apk && adb shell am start -n com.realsteelproject.oculusapp/com.unity3d.player.UnityPlayerGameActivity -D
