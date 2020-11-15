
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
        private int clientWidth, clientHeight;
        private Rectangle clientBounds;
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

        //
        // constructor
        //

        public GameRunner(Activity activity)
        {
            this.activity = activity;

            UpdateConfiguration(false);

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
                System.Windows.Forms.MessageBox.Show(e.ToString());
            }


            Type GetMainClass()
            {
                Type clsType = null;

                var clsName = activity.GetMetaAttr("main.class", true);
                if (clsName != null)
                {
                    if (clsName[0] == '.')
                        clsName = activity.getPackageName() + clsName;

                    clsType = System.Type.GetType(clsName, false, true);
                }

                if (clsType == null)
                {
                    throw new Exception($"main class '{clsName}' not found");
                }

                return clsType;
            }


            void CallMainMethod(Type mainClass)
            {
                var method = mainClass.GetMethod("Main");
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
                        UpdateConfiguration(true);

                    if ((eventBits & TOUCH_EVENT) != 0)
                    {
                        Microsoft.Xna.Framework.Input.Mouse
                                .HandleEvents(clientWidth, clientHeight);
                    }
                }

                //
                // run one game frame
                //

                game.Tick();
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
            if (! InModal)
            {
                shouldPause.incrementAndGet();
                waitForPause.block();
                if (shouldExit.get() == 0)
                    waitForPause.close();
            }
        }

        //
        // ActivityResume
        //

        public void ActivityResume()
        {
            if (shouldResume.compareAndSet(1, 0))
                waitForResume.open();
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

        void UpdateConfiguration(bool withCallback)
        {
            var metrics = new android.util.DisplayMetrics();
            activity.getWindowManager().getDefaultDisplay().getRealMetrics(metrics);

            clientWidth  = metrics.widthPixels;
            clientHeight = metrics.heightPixels;
            clientBounds = new Rectangle(0, 0, clientWidth, clientHeight);

            if (dict == null)
                dict = new System.Collections.Hashtable();

            // int dpi - pixels per inch
            dict["dpi"] = (int) ((metrics.xdpi + metrics.ydpi) * 0.5f);

            if (withCallback)
            {
                OnClientSizeChanged();
                OnOrientationChanged();
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
        // GameWindow interface
        //

        public override Rectangle ClientBounds => clientBounds;

        public override string ScreenDeviceName => "Android";

        public override bool AllowUserResizing { get => false; set { } }

        public override void SetSupportedOrientations(DisplayOrientation orientations)
            => CurrentOrientation = orientations;

        public override DisplayOrientation CurrentOrientation
        {
            get => (clientWidth < clientHeight) ? DisplayOrientation.Portrait
                                                : DisplayOrientation.LandscapeLeft;
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
                    r = android.content.pm.ActivityInfo.SCREEN_ORIENTATION_FULL_USER;
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
