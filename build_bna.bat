@echo off
if "%ANDROID_JAR%" == "" (
    echo Missing environment variable ANDROID_JAR.
    echo It should specify the full path to an Android.jar file in the platforms directory of the Android SDK.
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

echo ========================================
echo Building BNA.  Command:
echo MSBuild BNA -p:Configuration=Release
echo ========================================
MSBuild BNA -p:Configuration=Release

:EOF
