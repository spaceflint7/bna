
namespace Microsoft.Xna.Framework
{

    public class Activity : android.app.Activity
    {

        //
        // Android onCreate
        //

        protected override void onCreate(android.os.Bundle savedInstanceState)
        {
            base.onCreate(savedInstanceState);

            _LogTag = GetMetaAttr("log.tag") ?? _LogTag;

            new java.lang.Thread(gameRunner = new GameRunner(this)).start();
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
        // onPause, onResume, onDestroy, onTouchEvent
        //

        protected override void onPause()
        {
            gameRunner?.ActivityPause();
            base.onPause();
        }

        protected override void onResume()
        {
            gameRunner?.ActivityResume();
            base.onResume();
        }

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

        //
        // GetMetaAttr
        //

        public string GetMetaAttr(string name, bool warn = false)
        {
            var info = getPackageManager().getActivityInfo(getComponentName(),
                                    android.content.pm.PackageManager.GET_ACTIVITIES
                                  | android.content.pm.PackageManager.GET_META_DATA);
            name = "microsoft.xna.framework." + name;
            var str = info?.metaData?.getString(name);
            if (string.IsNullOrEmpty(str))
            {
                if (warn)
                    Activity.Log($"missing metadata attribute '{name}'");
                str = null;
            }
            return str;
        }

        //
        // Log
        //

        public static void Log(string s) => android.util.Log.i(_LogTag, s);

        private static string _LogTag = "BNA_Game";

        //
        // data
        //

        private GameRunner gameRunner;
        private bool restartActivity;

    }

}
