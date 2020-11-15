@echo off
if "%ANDROID_JAR%" == "" (
    echo Missing environment variable ANDROID_JAR.
    echo It should specify the full path to an Android.jar file in the platforms directory of the Android SDK.
    goto :EOF
)
if "%ANDROID_BUILD%" == "" (
    echo Missing environment variable ANDROID_BUILD.
    echo It should specify the full path to a build-tools directory in the Android SDK.
    goto :EOF
)
if "%FNA_DLL%" == "" (
    echo Missing environment variable FNA_DLL.
    echo It should specify the full path to the FNA.DLL file.
    goto :EOF
)
if "%BLUEBONNET_EXE%" == "" (
    echo Missing environment variable BLUEBONNET_EXE.
    echo It should specify the full path to the Bluebonnet executable.
    goto :EOF
)
if "%BLUEBONNET_LIB%" == "" (
    echo Missing environment variable BLUEBONNET_LIB.
    echo It should specify the full path to the Bluebonnet Baselib.jar file.
    goto :EOF
)

echo ========================================
echo Building BNA.  Command:
echo MSBuild BNA -p:Configuration=Release
echo ========================================
MSBuild BNA -p:Configuration=Release
pause

echo ========================================
echo Building Demo1.  Command:
echo nuget restore Demo1
echo msbuild Demo1 -p:Configuration=Release -p:Platform="x86"
echo ========================================
nuget restore Demo1
msbuild Demo1 -p:Configuration=Release -p:Platform="x86"
pause

echo ========================================
echo Converting Demo1 to APK.  Command:
echo MSBuild MakeAPK.project -p:INPUT_DLL=.obj\Demo1\Release\Demo1.exe -p:INPUT_DLL_2=.obj\Demo1\Release\Demo1FSharp.dll -p:INPUT_DLL_3=.obj\Demo1\Release\FSharp.Core.dll -p:CONTENT_DIR=.obj\Demo1\Release\Content -p:ICON_PNG=Demo1\Demo1\GameThumbnail.png -p:ANDROID_MANIFEST=Demo1\AndroidManifest.xml -p:KEYSTORE_FILE=.\my.keystore -p:KEYSTORE_PWD=123456 -p:APK_OUTPUT=.obj\Demo1.apk -p:APK_TEMP_DIR=.obj\Demo1\Release\TempApk -p:EXTRA_JAR_1=.obj\BNA.jar -p:EXTRA_JAR_2=%BLUEBONNET_LIB%
echo ========================================
MSBuild MakeAPK.project -p:INPUT_DLL=.obj\Demo1\Release\Demo1.exe -p:INPUT_DLL_2=.obj\Demo1\Release\Demo1FSharp.dll -p:INPUT_DLL_3=.obj\Demo1\Release\FSharp.Core.dll -p:CONTENT_DIR=.obj\Demo1\Release\Content -p:ICON_PNG=Demo1\Demo1\GameThumbnail.png -p:ANDROID_MANIFEST=Demo1\AndroidManifest.xml -p:KEYSTORE_FILE=.\my.keystore -p:KEYSTORE_PWD=123456 -p:APK_OUTPUT=.obj\Demo1.apk -p:APK_TEMP_DIR=.obj\Demo1\Release\TempApk -p:EXTRA_JAR_1=.obj\BNA.jar -p:EXTRA_JAR_2=%BLUEBONNET_LIB%

echo ========================================
echo All done
echo ========================================

:EOF
