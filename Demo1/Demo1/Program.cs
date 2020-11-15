using Microsoft.Xna.Framework;
using System;
using System.Security.Principal;

namespace com.spaceflint.bluebonnet.xnademo1
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            using (var game = new Demo1.Game1())
            {
                try
                {
                    game.Run();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    System.Windows.Forms.MessageBox.Show(e.ToString());
                }
            }
        }
    }
#endif
}

