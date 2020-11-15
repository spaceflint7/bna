
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Demo1
{
    public static class Config
    {

        public static int ClientWidth;
        public static int ClientHeight;
        public static int PixelsPerInch;

        public static void InitGraphics(Game game)
        {
            // by default, if the field DisplayOrientation is left as default,
            // BNA will fit the game 'window' to the Android screen size.
            // this may or not be appropriate.  it may be preferrable to use
            // a PreparingDeviceSettings hook, as shown below, to explicitly
            // set the size and orientation.

            new GraphicsDeviceManager(game).PreparingDeviceSettings += ((sender, args) =>
            {
                var pp = args.GraphicsDeviceInformation.PresentationParameters;
                pp.BackBufferWidth = game.Window.ClientBounds.Width;
                pp.BackBufferHeight = game.Window.ClientBounds.Height;
                pp.DisplayOrientation = game.Window.CurrentOrientation;
                pp.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            });
        }

        public static void InitWindow(GameWindow window)
        {
            // request notification as the 'window' changes orientation.
            // if AndroidManifest.xml specifies a locked orientation,
            // this may not be needed.
            window.ClientSizeChanged += WindowResized;

            // initialize the window configuration
            WindowResized(window, null);
        }

        private static void WindowResized(object sender, EventArgs eventArgs)
        {
            if (sender is GameWindow window)
            {
                ClientWidth  = window.ClientBounds.Width;
                ClientHeight = window.ClientBounds.Height;

                // the BNA GameWindow object (the sender parameter) provides
                // an IDictionary object through an IServiceProvider interface.
                // the dictionary can be used to query information that is not
                // otherwise accessible via XNA interfaces.  at this time, only
                // the screen DPI (dots per inch) value is provided.

                PixelsPerInch = 144;
                if (((object) window) is IServiceProvider windowServiceProvider)
                {
                    var windowDict = (System.Collections.IDictionary)
                                            windowServiceProvider.GetService(
                                                    typeof(System.Collections.IDictionary));
                    if (windowDict != null)
                    {
                        PixelsPerInch = (int) windowDict["dpi"];
                    }
                }
            }
            Console.WriteLine($">>> WINDOW CONFIG {ClientWidth} x {ClientHeight} @ {PixelsPerInch} ppi");
        }

    }

}
