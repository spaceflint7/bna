
using System;
using Microsoft.Xna.Framework;

namespace System.Windows.Forms
{

    public enum DialogResult
    {
        None, OK, Cancel, Abort, Retry, Ignore, Yes, No
    }

    public class MessageBox
    {

        public static volatile bool Showing;
        public static volatile bool Disable;

        public static DialogResult Show(string text)
        {
            if (! Disable)
            {
                var gameRunner = GameRunner.Singleton;
                var activity = gameRunner.Activity;
                if (    gameRunner.InModal || activity == null
                     || android.os.Looper.getMainLooper().getThread()
                                            == java.lang.Thread.currentThread())
                {
                    GameRunner.Log(text);
                }
                else
                {
                    gameRunner.PauseGame(true);

                    var waitObj = new android.os.ConditionVariable();
                    Show(activity, text, (_) => waitObj.open());
                    waitObj.block();

                    gameRunner.ResumeGame(true);

                }
            }
            return DialogResult.OK;
        }

        static void Show(android.app.Activity activity, string text,
                         System.Action<DialogResult> onClick)
        {
            activity.runOnUiThread(((java.lang.Runnable.Delegate) ( () => {

                var dlg = new android.app.AlertDialog.Builder(activity);
                dlg.setPositiveButton((java.lang.CharSequence) (object) "Close",
                        ((android.content.DialogInterface.OnClickListener.Delegate)
                        ((dialog, which) =>
                                { Showing = false; onClick(DialogResult.Yes); }
                        )).AsInterface());
                dlg.setOnDismissListener(
                        ((android.content.DialogInterface.OnDismissListener.Delegate)
                        ((dialog) =>
                                { Showing = false; onClick(DialogResult.Cancel); }
                        )).AsInterface());
                dlg.create();
                dlg.setMessage((java.lang.CharSequence) (object) text);

                Showing = true;
                dlg.show();

            })).AsInterface());
        }

    }

}
