
using System;
using Microsoft.Xna.Framework.Graphics;
using GL10 = javax.microedition.khronos.opengles.GL10;
using EGLConfig = javax.microedition.khronos.egl.EGLConfig;
#pragma warning disable 0436

namespace Microsoft.Xna.Framework
{

    public class GameRunner : GameWindow, IServiceProvider, java.lang.Runnable

    {

        private Activity activity;
        private System.Collections.Hashtable dict;
        private bool recreateActivity;

        private java.util.concurrent.atomic.AtomicInteger inModal;
        private java.util.concurrent.atomic.AtomicInteger shouldPause;
        private java.util.concurrent.atomic.AtomicInteger shouldResume;
        private java.util.concurrent.atomic.AtomicInteger shouldExit;
        private java.util.concurrent.atomic.AtomicInteger shouldEvents;
        private android.os.ConditionVariable waitForPause;
        private android.os.ConditionVariable waitForResume;

        private static readonly java.lang.ThreadLocal selfTls =
                                        new java.lang.ThreadLocal();

        private const int CONFIG_EVENT = 1;
        private const int TOUCH_EVENT  = 2;
        private const int KEY_EVENT    = 4;

        private int eventKeyCode;

        //
        // constructor
        //

        public GameRunner(Activity activity)
        {
            this.activity = activity;

            // in Bluebonnet, the following method is used to specify the
            // size that Marshal.SizeOf should return for non-primitive types.
            // this is used to enable Texture2D.GetData/SetData to accept
            // Color[] arrays.  see also SetTextureData in FNA3D_Tex
            System.Runtime.InteropServices.Marshal.SetComObjectData(
                typeof(System.Runtime.InteropServices.Marshal),
                typeof(Color), -1);

            UpdateConfiguration();

            inModal       = new java.util.concurrent.atomic.AtomicInteger();
            shouldPause   = new java.util.concurrent.atomic.AtomicInteger();
            shouldResume  = new java.util.concurrent.atomic.AtomicInteger();
            shouldExit    = new java.util.concurrent.atomic.AtomicInteger();
            shouldEvents  = new java.util.concurrent.atomic.AtomicInteger();
            waitForPause  = new android.os.ConditionVariable();
            waitForResume = new android.os.ConditionVariable();
        }

        //
        // Singleton and Activity properties
        //

        public static GameRunner Singleton
            => (GameRunner) selfTls.get()
                    ?? throw new System.InvalidOperationException("not main thread");

        public android.app.Activity Activity => activity;

        //
        // InModal
        //

        public bool InModal
        {
            get => inModal.get() != 0 ? true : false;
            set => inModal.set(value ? 1 : 0);
        }

        //
        // CheckGlErrors
        //

        public bool CheckGlErrors() => activity.GetMetaAttr_Int("check.gl.errors") != 0;

        //
        // Thread run() method
        //

        [java.attr.RetainName]
        public void run()
        {
            selfTls.set(this);

            RunMainMethod();

            shouldExit.set(1);
            waitForPause.open();

            activity.FinishAndRestart(recreateActivity);
        }

        //
        // RunMainMethod
        //

        private void RunMainMethod()
        {
            try
            {
                CallMainMethod(GetMainClass());
            }
            catch (Exception e)
            {
                GameRunner.Log("========================================");
                GameRunner.Log(e.ToString());
                GameRunner.Log("========================================");
                if (! object.ReferenceEquals(e.InnerException, null))
                    e = e.InnerException;
                System.Windows.Forms.MessageBox.Show(e.ToString());
            }


            Type GetMainClass()
            {
                Type clsType = null;

                var clsName = activity.GetMetaAttr_Str("main.class", ".Program");
                if (clsName[0] == '.')
                    clsName = activity.getPackageName() + clsName;
                clsType = System.Type.GetType(clsName, false, true);

                if (clsType == null)
                    throw new Exception($"main class '{clsName}' not found");

                return clsType;
            }


            void CallMainMethod(Type mainClass)
            {
                var method = mainClass.GetMethod("Main") ?? mainClass.GetMethod("main");
                if (method.IsStatic)
                {
                    method.Invoke(null, new object[method.GetParameters().Length]);
                }
                else
                {
                    throw new Exception($"missing or invalid method 'Main' in type '{mainClass}'");
                }
            }
        }

        //
        // MainLoop
        //

        public void MainLoop(Game game)
        {
            int pauseCount = 0;
            bool clearKeys = false;

            while (game.RunApplication)
            {

                //
                // pause game if required
                //

                if (shouldPause.get() != pauseCount)
                {
                    pauseCount = shouldPause.incrementAndGet();

                    // FNA.Game calls game.OnDeactivated()
                    game.IsActive = false;

                    if (shouldExit.get() != 0)
                        break;

                    PauseGame(false);

                    shouldResume.incrementAndGet();
                    waitForPause.open();
                    waitForResume.block();
                    waitForResume.close();

                    if (! ResumeGame(false))
                        break;

                    // on resume from pause, reset input state
                    Microsoft.Xna.Framework.Input.Mouse.WindowHandle =
                        Microsoft.Xna.Framework.Input.Mouse.WindowHandle;

                    // FNA.Game calls game.OnActivated()
                    game.IsActive = true;
                }

                //
                // handle various events as indicated
                //

                int eventBits = shouldEvents.get();
                if (eventBits != 0)
                {
                    while (! shouldEvents.compareAndSet(eventBits, 0))
                        eventBits = shouldEvents.get();

                    if ((eventBits & CONFIG_EVENT) != 0)
                        UpdateConfiguration();

                    if ((eventBits & TOUCH_EVENT) != 0)
                    {
                        Microsoft.Xna.Framework.Input.Mouse
                            .HandleEvents((int) dict["width"], (int) dict["height"]);
                    }

                    if ((eventBits & KEY_EVENT) != 0)
                    {
                        Microsoft.Xna.Framework.Input.Keyboard.keys.Add(
                                (Microsoft.Xna.Framework.Input.Keys) eventKeyCode);
                        clearKeys = true;
                    }
                }

                //
                // run one game frame
                //

                game.Tick();

                //
                // a simulated key is signalled during a single frame
                //

                if (clearKeys)
                {
                    clearKeys = false;
                    Microsoft.Xna.Framework.Input.Keyboard.keys.Clear();
                }
            }

            InModal = true;
            game.RunApplication = false;
        }

        //
        // PauseGame
        //

        public void PauseGame(bool enterModal)
        {
            Renderer.Pause(activity);
            if (enterModal)
                InModal = true;
        }

        //
        // ResumeGame
        //

        public bool ResumeGame(bool leaveModal)
        {
            if (leaveModal)
                InModal = false;

            if (shouldExit.get() != 0)
                return false;

            if (! Renderer.CanResume(activity))
            {
                // restart because we lost the GL context and state
                recreateActivity = true;

                // in case we are called from MessageBox, make sure
                // the main loop sees that we need to exit.
                shouldExit.set(1);
                shouldPause.incrementAndGet();

                return false;
            }

            return true;
        }

        //
        // Callbacks from Android activity UI thread:
        // onPause, onResume, onDestroy, onTouchEvent
        //

        public void ActivityPause()
        {
            if (shouldResume.get() == 0)
            {
                Microsoft.Xna.Framework.Media.MediaPlayer.ActivityPauseOrResume(true);
                Microsoft.Xna.Framework.Audio.SoundEffect.ActivityPauseOrResume(true);

                if (! InModal)
                {
                    shouldPause.incrementAndGet();
                    waitForPause.block();
                    if (shouldExit.get() == 0)
                        waitForPause.close();
                }
            }
        }

        //
        // ActivityResume
        //

        public void ActivityResume()
        {
            if (shouldResume.compareAndSet(1, 0))
            {
                // force a call to UpdateConfiguration after main loop wakes up.
                // this will not actually invoke callbacks if nothing has changed.
                OnSurfaceChanged();

                waitForResume.open();

                Microsoft.Xna.Framework.Media.MediaPlayer.ActivityPauseOrResume(false);
                Microsoft.Xna.Framework.Audio.SoundEffect.ActivityPauseOrResume(false);
            }
        }

        //
        // ActivityDestroy
        //

        public void ActivityDestroy()
        {
            shouldExit.set(1);
            ActivityResume();
            ActivityPause();
        }

        //
        // ActivityTouch
        //

        public void ActivityTouch(android.view.MotionEvent motionEvent)
        {
            Microsoft.Xna.Framework.Input.Mouse.QueueEvent(motionEvent);
            for (;;)
            {
                int v = shouldEvents.get();
                if (shouldEvents.compareAndSet(v, v | TOUCH_EVENT))
                    break;
            }
        }

        //
        // ActivityKey
        //

        public void ActivityKey(int keyCode)
        {
            for (;;)
            {
                int v = shouldEvents.get();
                if (shouldEvents.compareAndSet(v, v | KEY_EVENT))
                {
                    eventKeyCode = keyCode;
                    break;
                }
            }
        }

        //
        // OnSurfaceChanged
        //

        public void OnSurfaceChanged()
        {
            for (;;)
            {
                int v = shouldEvents.get();
                if (shouldEvents.compareAndSet(v, v | CONFIG_EVENT))
                    break;
            }
        }

        //
        // UpdateConfiguration
        //

        void UpdateConfiguration()
        {
            var metrics = GetDisplayMetrics(activity);
            int width = metrics.widthPixels;
            int height = metrics.heightPixels;

            var dict = new System.Collections.Hashtable();

            dict["width"]  = width;
            dict["height"] = height;
            dict["dpi"]    = (int) ((metrics.xdpi + metrics.ydpi) * 0.5f);

            if (object.ReferenceEquals(this.dict, null))
            {
                // on first call, set the dict without invoking callbacks
                SetExtra(dict, width, height);
                this.dict = dict;
            }
            else if (! DictsEqual(dict, this.dict))
            {
                SetExtra(dict, width, height);
                this.dict = dict;

                OnClientSizeChanged();
                OnOrientationChanged();
            }


            android.util.DisplayMetrics GetDisplayMetrics(android.app.Activity activity)
            {
                bool isMultiWindow = false;
                if (android.os.Build.VERSION.SDK_INT >= 24)
                    isMultiWindow = activity.isInMultiWindowMode();

                var metrics = new android.util.DisplayMetrics();
                var display = activity.getWindowManager().getDefaultDisplay();
                if (! isMultiWindow)
                {
                    // getRealMetrics gets real size of the entire screen.
                    // this is what we need in full screen immersive mode.
                    display.getRealMetrics(metrics);
                }
                else
                {
                    // getMetrics gets the size of the split window, minus
                    // window frames.  this is useful in multi-window mode.
                    display.getMetrics(metrics);
                }

                return metrics;
            }


            bool DictsEqual(System.Collections.Hashtable dict1,
                            System.Collections.Hashtable dict2)
            {
                foreach (var key in dict1.Keys)
                {
                    if (! dict1[key].Equals(dict2[key]))
                        return false;
                }
                return true;
            }


            void SetExtra(System.Collections.Hashtable dict, int width, int height)
            {
                dict["bounds"] = new Rectangle(0, 0, width, height);
                dict["openUri"] = (Action<string>) OpenUri;
            }
        }

        //
        // GetService
        //

        public object GetService(Type type)
        {
            if (object.ReferenceEquals(type, typeof(System.Collections.IDictionary)))
                return dict.Clone();
            return null;
        }

        //
        // OpenUri
        //

        private void OpenUri(string uri)
        {
            activity.runOnUiThread(((java.lang.Runnable.Delegate) (() =>
            {
                try
                {
                    activity.startActivity(
                        new android.content.Intent(android.content.Intent.ACTION_VIEW,
                            android.net.Uri.parse(uri)));
                }
                catch (Exception e)
                {
                    GameRunner.Log(e.ToString());
                }
            })).AsInterface());
        }

        //
        // GameWindow interface
        //

        public override Rectangle ClientBounds => (Rectangle) dict["bounds"];

        public override string ScreenDeviceName => "Android";

        public override bool AllowUserResizing { get => false; set { } }

        public override void SetSupportedOrientations(DisplayOrientation orientations)
            => CurrentOrientation = orientations;

        public override DisplayOrientation CurrentOrientation
        {
            get => ((int) dict["width"] < (int) dict["height"])
                        ? DisplayOrientation.Portrait : DisplayOrientation.LandscapeLeft;
            set
            {
                bool portrait  = 0 != (value & DisplayOrientation.Portrait);
                bool landscape = 0 != (value & (   DisplayOrientation.LandscapeLeft
                                                 | DisplayOrientation.LandscapeRight));
                int r;
                if (portrait && (! landscape))
                    r = android.content.pm.ActivityInfo.SCREEN_ORIENTATION_USER_PORTRAIT;
                else if (landscape && (! portrait))
                    r = android.content.pm.ActivityInfo.SCREEN_ORIENTATION_USER_LANDSCAPE;
                else
                    return;
                activity.setRequestedOrientation(r);
            }
        }

        public static void Log(string s) => Microsoft.Xna.Framework.Activity.Log(s);

        //
        // not implemented
        //

        public override IntPtr Handle => IntPtr.Zero;

        public override void SetTitle(string title) { }

        public override void BeginScreenDeviceChange(bool willBeFullScreen) { }

        public override void EndScreenDeviceChange(string screenDeviceName,
                                                   int clientWidth, int clientHeight) { }


    }

}
