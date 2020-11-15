
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Demo1
{
    public class Touch : DrawableGameComponent
    {

        private int pressX, pressY;
        private int releaseX, releaseY;
        private static Touch instance;
        public static GestureSample LastGesture;


        public Touch(Game game) : base(game)
        {
            pressX   = int.MinValue;
            releaseX = int.MinValue;
            instance = this;

            // TouchPanel is functional when running on Android
            TouchPanel.EnabledGestures = GestureType.Tap | GestureType.FreeDrag;
        }


        protected override void Dispose(bool disposing)
        {
            instance = null;
            base.Dispose(disposing);
        }

        public override void Draw(GameTime gameTime)
        {
            // this code is in Draw because Update may be invoked multiple times
            // per frame, which might cause the loss of the occasional click

            // on Android, the Mouse class tracks single-finger taps, so it can
            // be used on both Windows and Android for simple input.  for more
            // advanced touch tracking, use TouchPanel and gestures.

            var state = Mouse.GetState();
            if (state.LeftButton == ButtonState.Pressed)
            {
                if (pressX == int.MinValue)
                {
                    pressX = state.X;
                    pressY = state.Y;
                }
            }
            else if (pressX != int.MinValue && releaseX == int.MinValue)
            {
                releaseX = state.X;
                releaseY = state.Y;
            }
            else
            {
                pressX   = int.MinValue;
                releaseX = int.MinValue;
            }

            if (TouchPanel.IsGestureAvailable)
            {
                LastGesture = TouchPanel.ReadGesture();
                Console.WriteLine($"Gesture {LastGesture.GestureType} at {LastGesture.Position}");
            }
        }


        public bool _Clicked(Rectangle rect)
            => rect.Contains(pressX, pressY) && rect.Contains(releaseX, releaseY);

        public static bool Clicked(Rectangle rect) => instance._Clicked(rect);

    }

}
