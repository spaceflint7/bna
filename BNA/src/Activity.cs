
namespace Microsoft.Xna.Framework
{

    public class Activity : android.app.Activity
    {

        //
        // Android onCreate
        //

        protected override void onCreate(android.os.Bundle savedInstanceState)
        {
            // on some devices, this should be before call to base.onCreate
            // requestWindowFeature(android.view.Window.FEATURE_NO_TITLE);

            logTag = GetMetaAttr_Str("log.tag", "BNA_Game");

            backKeyCode = GetMetaAttr_Int("back.key");

            if (android.os.Build.VERSION.SDK_INT >= 19)
            {
                immersiveMode = GetMetaAttr_Int("immersive.mode") != 0;

                if (immersiveMode && android.os.Build.VERSION.SDK_INT >= 28)
                {
                    var layoutParams = getWindow().getAttributes();
                    layoutParams.layoutInDisplayCutoutMode =
                        android.view.WindowManager.LayoutParams.LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES;
                    getWindow().setAttributes(layoutParams);
                }
            }

            if (GetMetaAttr_Int("keep.screen.on") != 0)
            {
                getWindow().addFlags(
                    android.view.WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
            }

            /*
            int flags = android.view.WindowManager.LayoutParams.FLAG_FULLSCREEN
                      | android.view.WindowManager.LayoutParams.FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS
                      | android.view.WindowManager.LayoutParams.FLAG_LAYOUT_IN_SCREEN
                      | android.view.WindowManager.LayoutParams.FLAG_LAYOUT_NO_LIMITS;
            if (GetMetaAttr_Int("keep.screen.on") != 0)
                flags |= android.view.WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON;
            getWindow().setFlags(flags
                    | android.view.WindowManager.LayoutParams.FLAG_FORCE_NOT_FULLSCREEN,
                                flags);
            */

            base.onCreate(savedInstanceState);
        }

        //
        // FinishAndRestart
        //

        public void FinishAndRestart(bool restart)
        {
            if (! (isFinishing() || isChangingConfigurations()))
            {
                runOnUiThread(((java.lang.Runnable.Delegate) (() =>
                {
                    finish();
                    restartActivity = restart;
                })).AsInterface());
            }
        }

        //
        // Android events forwarded to GameRunner:
        // onPause, onWindowFocusChanged, onDestroy, onTouchEvent, onBackPressed
        //

        protected override void onPause()
        {
            gameRunner?.ActivityPause();
            base.onPause();
        }

        public override void onWindowFocusChanged(bool hasFocus)
        {
            base.onWindowFocusChanged(hasFocus);

            if (hasFocus)
            {
                if (immersiveMode)
                {
                    getWindow().getDecorView().setSystemUiVisibility(
                              android.view.View.SYSTEM_UI_FLAG_LAYOUT_STABLE
                            | android.view.View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
                            | android.view.View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN
                            | android.view.View.SYSTEM_UI_FLAG_HIDE_NAVIGATION
                            | android.view.View.SYSTEM_UI_FLAG_FULLSCREEN
                            | android.view.View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY);
                }

                if (object.ReferenceEquals(gameRunner, null))
                {
                    new java.lang.Thread(gameRunner = new GameRunner(this)).start();
                }
                else
                {
                    gameRunner.ActivityResume();
                }
            }
        }

        /*protected override void onResume()
        {
            gameRunner?.ActivityResume();
            base.onResume();
        }*/

        protected override void onDestroy()
        {
            gameRunner?.ActivityDestroy();
            base.onDestroy();

            if (restartActivity)
            {
                // note, do not use activity.recreate() here, as it occasionally
                // keeps the old surface locked for a few seconds
                startActivity(getIntent()
                    .addFlags(android.content.Intent.FLAG_ACTIVITY_NO_ANIMATION)
                    .addFlags(android.content.Intent.FLAG_ACTIVITY_TASK_ON_HOME)
                    .addFlags(android.content.Intent.FLAG_ACTIVITY_NEW_TASK)
                );
            }

            // we have to destroy the activity process to get rid of leaking
            // static references that can never be garbage collected

            java.lang.System.exit(0);
        }

        public override bool onTouchEvent(android.view.MotionEvent motionEvent)
        {
            gameRunner?.ActivityTouch(motionEvent);
            return true;
        }

        public override void onBackPressed()
        {
            if (backKeyCode != 0)
                gameRunner?.ActivityKey(backKeyCode);
            else
                base.onBackPressed();
        }

        //
        // GetMetaAttr_Str, GetMetaAttr_Int
        //

        public string GetMetaAttr_Str(string name, string def)
        {
            name = "BNA." + name;
            var str = GetMetaData()?.getString(name);
            if (string.IsNullOrEmpty(str))
            {
                Activity.Log($"missing metadata attribute '{name}'");
                str = def;
            }
            return str;
        }

        public int GetMetaAttr_Int(string name)
            => GetMetaData()?.getInt("BNA." + name) ?? 0;

        private android.os.Bundle GetMetaData()
            => getPackageManager().getActivityInfo(
                            getComponentName(),
                            android.content.pm.PackageManager.GET_ACTIVITIES
                          | android.content.pm.PackageManager.GET_META_DATA)
                    ?.metaData;

        //
        // Log
        //

        public static void Log(string s) => android.util.Log.i(logTag, s);

        private static string logTag;

        //
        // data
        //

        private GameRunner gameRunner;
        private bool restartActivity;
        private bool immersiveMode;
        private int backKeyCode;

    }

}
