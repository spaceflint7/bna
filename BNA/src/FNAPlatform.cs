
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
#pragma warning disable 0436

namespace Microsoft.Xna.Framework
{

    public static class FNAPlatform
    {
        //
        // CreateWindow
        //

        private static GameWindow CreateWindowImpl() => GameRunner.Singleton;

        public delegate GameWindow CreateWindowFunc();
        public static readonly CreateWindowFunc CreateWindow = CreateWindowImpl;

        //
        // DisposeWindow
        //

        private static void DisposeWindowImpl(GameWindow window) { }

        public delegate void DisposeWindowFunc(GameWindow window);
        public static readonly DisposeWindowFunc DisposeWindow = DisposeWindowImpl;

        //
        // SupportsOrientationChanges
        //

        private static bool SupportsOrientationChangesImpl() => true;

        public delegate bool SupportsOrientationChangesFunc();
        public static readonly SupportsOrientationChangesFunc SupportsOrientationChanges = SupportsOrientationChangesImpl;

        //
        // GetGraphicsAdapters
        //

        private static GraphicsAdapter[] GetGraphicsAdaptersImpl()
        {
            var bounds = GameRunner.Singleton.ClientBounds;
            var modesList = new List<DisplayMode>();
            var theMode = new DisplayMode(bounds.Width, bounds.Height, SurfaceFormat.Color);
            modesList.Add(theMode);
            var modesCollection = new DisplayModeCollection(modesList);
            var name = "Android Surface";
            var theAdapter = new GraphicsAdapter(modesCollection, name, name);
            return new GraphicsAdapter[] { (GraphicsAdapter) (object) theAdapter };
        }

        public delegate GraphicsAdapter[] GetGraphicsAdaptersFunc();
        public static readonly GetGraphicsAdaptersFunc GetGraphicsAdapters = GetGraphicsAdaptersImpl;

        //
        // RegisterGame
        //

        public static GraphicsAdapter RegisterGameImpl(Game game) => GraphicsAdapter.Adapters[0];

        public delegate GraphicsAdapter RegisterGameFunc(Game game);
        public static readonly RegisterGameFunc RegisterGame = RegisterGameImpl;

        //
        // GetTouchCapabilities
        //

        public static TouchPanelCapabilities GetTouchCapabilitiesImpl()
            => (TouchPanelCapabilities) (object) new TouchPanelCapabilities(true, 4);

        public delegate TouchPanelCapabilities GetTouchCapabilitiesFunc();
        public static readonly GetTouchCapabilitiesFunc GetTouchCapabilities = GetTouchCapabilitiesImpl;

        //
        // NeedsPlatformMainLoop
        //

        public static bool NeedsPlatformMainLoopImpl() => true;

        public delegate bool NeedsPlatformMainLoopFunc();
        public static readonly NeedsPlatformMainLoopFunc NeedsPlatformMainLoop = NeedsPlatformMainLoopImpl;

        //
        //
        //

        public static void RunPlatformMainLoopImpl(Game game)
            => GameRunner.Singleton.MainLoop(game);

        public delegate void RunPlatformMainLoopFunc(Game game);
        public static readonly RunPlatformMainLoopFunc RunPlatformMainLoop = RunPlatformMainLoopImpl;

        //
        // UnregisterGame
        //

        public static void UnregisterGameImpl(Game game) { }

        public delegate void UnregisterGameFunc(Game game);
        public static readonly UnregisterGameFunc UnregisterGame = UnregisterGameImpl;

        //
        // OnIsMouseVisibleChanged
        //

        public static void OnIsMouseVisibleChangedImpl(bool visible) { }

        public delegate void OnIsMouseVisibleChangedFunc(bool visible);
        public static readonly OnIsMouseVisibleChangedFunc OnIsMouseVisibleChanged = OnIsMouseVisibleChangedImpl;

        //
        // GetNumTouchFingers
        //

        public static int GetNumTouchFingersImpl() => Mouse.NumTouchFingers.get();

        public delegate int GetNumTouchFingersFunc();
        public static readonly GetNumTouchFingersFunc GetNumTouchFingers = GetNumTouchFingersImpl;

        //
        // GetStorageRoot
        //

        public static string GetStorageRootImpl()
        {
            var s = GameRunner.Singleton.Activity.getFilesDir();
            return s.getAbsolutePath();
        }

        public delegate string GetStorageRootFunc();
        public static readonly GetStorageRootFunc GetStorageRoot = GetStorageRootImpl;

        //
        // GetDriveInfo
        //

        public static System.IO.DriveInfo GetDriveInfoImpl(string storageRoot) => null;

        public delegate System.IO.DriveInfo GetDriveInfoFunc(string storageRoot);
        public static readonly GetDriveInfoFunc GetDriveInfo = GetDriveInfoImpl;

        //
        // GetMouseState
        //

        /*public static void GetMouseStateImpl(IntPtr window, out int x, out int y, out ButtonState left, out ButtonState middle, out ButtonState right, out ButtonState x1, out ButtonState x2)
        {
            x = 0;
            y = 0;
            left = ButtonState.Released;
            middle = ButtonState.Released;
            right = ButtonState.Released;
            x1 = ButtonState.Released;
            x2 = ButtonState.Released;
        }

        public delegate void GetMouseStateFunc(IntPtr window, out int x, out int y, out ButtonState left, out ButtonState middle, out ButtonState right, out ButtonState x1, out ButtonState x2);
        public static readonly GetMouseStateFunc GetMouseState = GetMouseStateImpl;*/

        //
        // TextInputCharacters
        //

        public static readonly char[] TextInputCharacters = new char[0];
    }

}
