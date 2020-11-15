
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using android.opengl;
using GL10 = javax.microedition.khronos.opengles.GL10;
using EGLConfig = javax.microedition.khronos.egl.EGLConfig;

namespace Microsoft.Xna.Framework.Graphics
{

    public class Renderer : android.opengl.GLSurfaceView.Renderer
    {

        //
        // renderer data
        //

        private android.opengl.GLSurfaceView surface;
        private android.os.ConditionVariable waitObject;
        private java.util.concurrent.atomic.AtomicInteger paused;
        private Action actionOnChanged;

        public object UserData;

        //
        // surface configuration
        //

        public int SurfaceWidth, SurfaceHeight;
        public DepthFormat SurfaceDepthFormat;
        public int TextureUnits;
        public int TextureSize;
        public int[] TextureFormats;

        //
        // constructor
        //

        private Renderer(android.app.Activity activity, Action onChanged,
                         int redSize, int greenSize, int blueSize,
                         int alphaSize, int depthSize, int stencilSize)
        {
            waitObject = new android.os.ConditionVariable();
            paused = new java.util.concurrent.atomic.AtomicInteger();
            actionOnChanged = onChanged;

            activity.runOnUiThread(((java.lang.Runnable.Delegate) (() =>
            {
                surface = new android.opengl.GLSurfaceView(activity);
                surface.setEGLContextClientVersion(3); // OpenGL ES 3.0
                surface.setEGLConfigChooser(redSize, greenSize, blueSize,
                                            alphaSize, depthSize, stencilSize);
                surface.setPreserveEGLContextOnPause(true);
                surface.setRenderer(this);
                surface.setRenderMode(android.opengl.GLSurfaceView.RENDERMODE_WHEN_DIRTY);
                activity.setContentView(surface);

            })).AsInterface());

            // wait for one onDrawFrame callback, which tells us that
            // GLSurfaceView finished initializing the GL context
            if (! waitObject.block(8000))
                throw new NoSuitableGraphicsDeviceException("cannot create GLSurfaceView");

            var clientBounds = GameRunner.Singleton.ClientBounds;
            if (SurfaceWidth != clientBounds.Width || SurfaceHeight != clientBounds.Height)
            {
                // while not common, it is possible for the screen to rotate,
                // between the time the Window/GameRunner is created, and the
                // time the renderer is created.  we want to identify this.
                if (actionOnChanged != null)
                    actionOnChanged();
            }
        }

        //
        // Send
        //

        public void Send(Action action)
        {
            Exception exc = null;
            if (paused.get() == 0)
            {
                var cond = new android.os.ConditionVariable();
                surface.queueEvent(((java.lang.Runnable.Delegate) (() =>
                {
                    var error = GLES20.glGetError();
                    if (error == GLES20.GL_NO_ERROR)
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception exc2)
                        {
                            exc = exc2;
                        }
                        error = GLES20.glGetError();
                    }
                    if (error != GLES20.GL_NO_ERROR)
                        exc = new Exception($"GL Error {error}");
                    cond.open();
                })).AsInterface());
                cond.block();
            }
            if (exc != null)
            {
                throw new AggregateException(exc.Message, exc);
            }
        }

        //
        // Present
        //

        public void Present()
        {
            waitObject.close();
            surface.requestRender();
            waitObject.block();
        }

        //
        // Renderer interface
        //

        [java.attr.RetainName]
        public void onSurfaceCreated(GL10 unused, EGLConfig config)
        {
            // if onSurfaceCreated is called while resuming from pause,
            // it means the GL context was lost
            paused.compareAndSet(1, -1);
        }

        [java.attr.RetainName]
        public void onSurfaceChanged(GL10 unused, int width, int height)
        {
            bool changed = (SurfaceWidth != width || SurfaceHeight != height)
                        && (SurfaceWidth != 0     || SurfaceHeight != 0);

            SurfaceWidth = width;
            SurfaceHeight = height;
            InitConfig();

            if (changed && actionOnChanged != null)
                actionOnChanged();
        }

        [java.attr.RetainName]
        public void onDrawFrame(GL10 unused) => waitObject.open();

        //
        // InitConfig
        //

        private void InitConfig()
        {
            var data = new int[5];
            GLES20.glGetIntegerv(GLES20.GL_DEPTH_BITS,                      data, 0);
            GLES20.glGetIntegerv(GLES20.GL_STENCIL_BITS,                    data, 1);
            GLES20.glGetIntegerv(GLES20.GL_MAX_TEXTURE_IMAGE_UNITS,         data, 2);
            GLES20.glGetIntegerv(GLES20.GL_MAX_TEXTURE_SIZE,                data, 3);
            GLES20.glGetIntegerv(GLES20.GL_NUM_COMPRESSED_TEXTURE_FORMATS,  data, 4);

            if (data[0] /* DEPTH_BITS */ >= 24)
            {
                SurfaceDepthFormat = (data[1] /* STENCIL_BITS */ >= 8)
                                   ? DepthFormat.Depth24Stencil8
                                   : DepthFormat.Depth24;
            }
            else if (data[0] /* DEPTH_BITS */ >= 16)
                SurfaceDepthFormat = DepthFormat.Depth16;
            else
                SurfaceDepthFormat = DepthFormat.None;

            TextureUnits = data[2]; // GL_MAX_TEXTURE_IMAGE_UNITS
            TextureSize  = data[3]; // GL_MAX_TEXTURE_SIZE

            TextureFormats = new int[data[4]]; // GL_NUM_COMPRESSED_TEXTURE_FORMATS
            GLES20.glGetIntegerv(GLES20.GL_COMPRESSED_TEXTURE_FORMATS, TextureFormats, 0);
        }

        //
        // Create
        //

        public static IntPtr Create(android.app.Activity activity, Action onChanged,
                                    int redSize, int greenSize, int blueSize,
                                    int alphaSize, int depthSize, int stencilSize)
        {
            for (;;)
            {
                lock (RendererObjects)
                {
                    var deviceId = java.lang.System.nanoTime();

                    foreach (var oldRendererObject in RendererObjects)
                    {
                        if (oldRendererObject.deviceId == deviceId)
                        {
                            deviceId = 0;
                            break;
                        }
                    }

                    if (deviceId == 0)
                    {
                        java.lang.Thread.sleep(1);
                        continue;
                    }

                    RendererObjects.Insert(0, new RendererObject()
                    {
                        deviceId = deviceId,
                        renderer = new Renderer(activity, onChanged,
                                                redSize, greenSize, blueSize,
                                                alphaSize, depthSize, stencilSize),
                        activity = new java.lang.@ref.WeakReference(activity),
                    });

                    return (IntPtr) deviceId;
                }
            }
        }

        //
        // GetRenderer
        //

        public static Renderer Get(IntPtr deviceId)
        {
            var longDeviceId = (long) deviceId;
            lock (RendererObjects)
            {
                foreach (var renderer in RendererObjects)
                {
                    if (renderer.deviceId == longDeviceId)
                        return renderer.renderer;
                }
            }
            throw new ArgumentException("invalid device ID");
        }

        //
        // Release
        //

        public void Release()
        {
            lock (RendererObjects)
            {
                for (int i = RendererObjects.Count; i-- > 0; )
                {
                    if (RendererObjects[i].renderer == this)
                    {
                        RendererObjects[i].renderer.surface = null;
                        RendererObjects[i].renderer = null;
                        RendererObjects.RemoveAt(i);
                    }
                }
            }
        }

        //
        // GetRenderersForActivity
        //

        private static List<Renderer> GetRenderersForActivity(android.app.Activity activity)
        {
            var list = new List<Renderer>();
            lock (RendererObjects)
            {
                foreach (var renderer in RendererObjects)
                {
                    if (renderer.activity.get() == activity)
                        list.Add(renderer.renderer);
                }
            }
            return list;
        }

        //
        // Pause
        //

        public static void Pause(android.app.Activity activity)
        {
            foreach (var renderer in GetRenderersForActivity(activity))
            {
                renderer.surface.onPause();
                renderer.paused.set(1);
            }
        }

        //
        // CanResume
        //

        public static bool CanResume(android.app.Activity activity)
        {
            foreach (var renderer in GetRenderersForActivity(activity))
            {
                if (renderer.paused.get() != 0)
                {
                    renderer.waitObject.close();
                    renderer.surface.onResume();
                    renderer.waitObject.block();

                    if (! renderer.paused.compareAndSet(1, 0))
                    {
                        // cannot resume because we lost the GL context,
                        // see also PauseRenderers and onSurfaceCreated
                        return false;
                    }
                }
            }
            return true;
        }

        //
        // data
        //

        private static List<RendererObject> RendererObjects = new List<RendererObject>();

        private class RendererObject
        {
            public long deviceId;
            public Renderer renderer;
            public java.lang.@ref.WeakReference activity;
        }

    }

}
