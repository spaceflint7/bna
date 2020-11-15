
# Bluebonnet BNA

This is a port of [FNA](https://fna-xna.github.io/) for use with [Bluebonnet](https://github.com/spaceflint7/bluebonnet) to build games for Android using the [XNA 4.0](https://en.wikipedia.org/wiki/Microsoft_XNA) libraries.

**Bluebonnet** is an Android-compatible implementation of the .NET platform on top of the Java Virtual Machine.  **Bluebonnet BNA** makes it possible to compile XNA games written in C# or F# to Android Java without any dependencies on native code libraries.

## Building

- Download and build the `Bluebonnet` compiler and its runtime library, `Baselib.jar`.  For instructions, see [Bluebonnet README](https://github.com/spaceflint7/bluebonnet/blob/master/README.md).

- Download the [FNA](https://github.com/FNA-XNA/FNA/archive/master.zip) source code.  Build by typing the following command in the FNA root directory:

    - `MSBuild FNA.csproj -p:Configuration=Release`

    - If the build is successful, the file `FNA.DLL` will be generated in the `bin/Release` sub-directory of the FNA root directory.

- Download this `BNA` project and build it by typing the following command in the BNA root directory:

    - `MSBuild BNA -p:Configuration=Release -p:ANDROID_JAR=/path/to/Android.jar -p:BLUEBONNET_EXE=/path/to/Bluebonnet/executable -p:FNA_DLL=/path/to/FNA.DLL`

    - The `ANDROID_JAR` property specifies the full path to an `Android.jar` file from the Android SDK distribution.  `BNA` requires Android SDK version 18 or later.

    - The `BLUEBONNET_EXE` property specifies the full path to the Bluebonnet compiler that you built in an earlier step.

    - The `FNA_DLL` property specifies the full path to the FNA.DLL that you built in an earlier step.  As noted earlier, this path should be `(FNA_DIR)/bin/Release/FNA.dll`.

    - If the build is successful, the file `BNA.jar` will be generated in the `.obj` sub-directory of the repository root directory.

## Building the Demo

An example application `Demo1` is provided, which demonstrates some XNA functionality in C# and F#.  It can be built using Visual Studio (solution file `Demo1.sln`), or from the command line:

- Type `nuget restore Demo1` to restore packages using [nuget](https://www.nuget.org/downloads).

- Type `msbuild Demo1 -p:Configuration=Release -p:Platform="x86"`

- Test the program: `.obj\Demo1\Release\Demo1.exe`.  (Note that this will create the directory `SavedGames\Demo1` in the `Documents` directory.)

- To convert the built application to an Android APK, type the following command:  (Note that this is a single-line command; line breaks were added for clarity.)

    - <code>MSBuild MakeAPK.project<br>
    -p:INPUT_DLL=.obj\Demo1\Release\Demo1.exe<br>
    -p:INPUT_DLL_2=.obj\Demo1\Release\Demo1FSharp.dll<br>
    -p:INPUT_DLL_3=.obj\Demo1\Release\FSharp.Core.dll<br>
    -p:CONTENT_DIR=.obj\Demo1\Release\Content<br>
    -p:ICON_PNG=Demo1\Demo1\GameThumbnail.png<br>
    -p:ANDROID_MANIFEST=Demo1\AndroidManifest.xml<br>
    -p:KEYSTORE_FILE=.\my.keystore<br>
    -p:KEYSTORE_PWD=123456<br>
    -p:APK_OUTPUT=.obj\Demo1.apk<br>
    -p:APK_TEMP_DIR=.obj\Demo1\Release\TempApk<br>
    -p:EXTRA_JAR_1=.obj\BNA.jar<br>
    -p:EXTRA_JAR_2=\path\to\Bluebonnet\Baselib.jar<br>
    -p:BLUEBONNET_EXE=\path\to\Bluebonnet.exe<br>
    -p:ANDROID_JAR=\path\to\Android\platforms\android-XX\android.jar<br>
    -p:ANDROID_BUILD=\path\to\Android\build-tools\30.0.2</code>

    - Make sure to specify the right paths for the Bluebonnet compiler (via the `BLUEBONNET_EXE` property), the Baselib support library (via the `EXTRA_JAR_2` property), the Android.jar file (via the `ANDROID_JAR` property) and the Android build-tools directory (via the `ANDROID_BUILD` property).

    - The parameters are detailed at the top of the [MakeAPK.project](MakeAPK.project) file.  See also the comments in the [AndroidManifest.xml](Demo1/AndroidManifest.xml) file, and comments throughout the `Demo1` source files.

    - If the build is successful, the file `Demo1.apk` will be generated in the `.obj` sub-directory of the repository root directory.

- The batch file `build_demo.bat` runs the steps discussed in this "Building the Demo" section.

- Install the built APK to an Android device:

    - `\path\to\Android\platform-tools\adb install -r .obj\Demo1.apk`
