
using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input.Touch;
#pragma warning disable 0436

namespace Microsoft.Xna.Framework.Input
{

    public static class Mouse
    {

        private static MouseState state;

        private static Queue motionEvents = new Queue();

        private static List<(int id, float x, float y)> fingerXYs =
                                    new List<(int id, float x, float y)>();

        public static java.util.concurrent.atomic.AtomicInteger NumTouchFingers =
                                    new java.util.concurrent.atomic.AtomicInteger();



        public static IntPtr WindowHandle
        {
            get => IntPtr.Zero;
            set
            {
                state = default(MouseState);
                lock (motionEvents)
                {
                    motionEvents.Clear();
                }
                fingerXYs.Clear();
                NumTouchFingers.set(0);
            }
        }
        public static int INTERNAL_BackBufferWidth;
        public static int INTERNAL_BackBufferHeight;



        public static void QueueEvent(android.view.MotionEvent motionEvent)
        {
            lock (motionEvents)
            {
                motionEvents.Enqueue(
                    android.view.MotionEvent.obtainNoHistory(motionEvent));
            }
        }

        public static void HandleEvents(int clientWidth, int clientHeight)
        {
            for (;;)
            {
                android.view.MotionEvent motionEvent;
                lock (motionEvents)
                {
                    if (motionEvents.Count == 0)
                        motionEvent = null;
                    else
                    {
                        motionEvent =
                            (android.view.MotionEvent) motionEvents.Dequeue();
                    }
                }

                if (motionEvent == null)
                    return;

                HandleOneEvent(motionEvent, clientWidth, clientHeight);
            }
        }



        private static void HandleOneEvent(android.view.MotionEvent motionEvent,
                                           int clientWidth, int clientHeight)
        {
            int action = motionEvent.getActionMasked();

            if (     action == android.view.MotionEvent.ACTION_DOWN
                  || action == android.view.MotionEvent.ACTION_MOVE
                  || action == android.view.MotionEvent.ACTION_POINTER_DOWN)
            {
                state.LeftButton = ButtonState.Pressed;
                state.X = (int) Clamp(motionEvent.getX(),
                                      clientWidth, INTERNAL_BackBufferWidth);
                state.Y = (int) Clamp(motionEvent.getY(),
                                      clientHeight, INTERNAL_BackBufferHeight);

                var which = (action == android.view.MotionEvent.ACTION_DOWN)
                          ? TouchLocationState.Pressed
                          : TouchLocationState.Moved;

                SendTouchEvents(motionEvent, which, clientWidth, clientHeight);
            }

            else if (    action == android.view.MotionEvent.ACTION_UP
                      || action == android.view.MotionEvent.ACTION_POINTER_UP
                      || action == android.view.MotionEvent.ACTION_CANCEL)
            {
                state.LeftButton = ButtonState.Released;

                SendTouchEvents(motionEvent, TouchLocationState.Released,
                                clientWidth, clientHeight);
            }
        }

        private static void SendTouchEvents(android.view.MotionEvent motionEvent,
                                            TouchLocationState whichTouchEvent,
                                            int clientWidth, int clientHeight)
        {
            int pointerCount = motionEvent.getPointerCount();
            var eventArray =
                new (int id, float x, float y, float dx, float dy)[pointerCount];

            for (int pointerIndex = 0; pointerIndex < pointerCount; pointerIndex++)
            {
                int id = motionEvent.getPointerId(pointerIndex);
                var x = Clamp(motionEvent.getX(pointerIndex), clientWidth, 1);
                var y = Clamp(motionEvent.getY(pointerIndex), clientHeight, 1);
                float dx, dy;

                int fingerCount = fingerXYs.Count;
                int fingerIndex = 0;
                while (fingerIndex < fingerCount)
                {
                    if (fingerXYs[fingerIndex].id == id)
                        break;
                    fingerIndex++;
                }

                if (fingerIndex == fingerCount)
                {
                    dx = dy = 0f;
                    if (whichTouchEvent != TouchLocationState.Released)
                    {
                        fingerXYs.Add((id: id, x: x, y: y));
                    }
                }
                else
                {
                    dx = x - fingerXYs[fingerIndex].x;
                    dy = y - fingerXYs[fingerIndex].y;

                    if (whichTouchEvent != TouchLocationState.Released)
                    {
                        fingerXYs[fingerIndex] = (id: id, x: x, y: y);
                    }
                    else
                    {
                        fingerXYs.RemoveAt(fingerIndex);
                    }
                }

                eventArray[pointerIndex] = (id: id, x: x, y: y, dx: dx, dy: dy);
            }

            NumTouchFingers.set(fingerXYs.Count);

            for (int eventIndex = 0; eventIndex < pointerCount; eventIndex++)
            {
                var e = eventArray[eventIndex];
                TouchPanel.INTERNAL_onTouchEvent(e.id, whichTouchEvent, e.x, e.y, e.dx, e.dy);
            }
        }

        private static float Clamp(float v, int clientSize, int backbufferSize)
        {
            if (v < 0)
                v = 0;
            if (v > clientSize)
                v = clientSize;
            return v * backbufferSize / clientSize;
        }



        public static MouseState GetState() => state;

        public static void SetPosition(int x, int y) { }

    }

}
