
using System;
using android.opengl;
#pragma warning disable 0436

namespace Microsoft.Xna.Framework.Graphics
{

    public static partial class FNA3D
    {

        //
        // FNA3D_CreateDevice
        //

        public static IntPtr FNA3D_CreateDevice(
                                ref FNA3D_PresentationParameters presentationParameters,
                                byte debugMode)
        {
            int depthSize, stencilSize = 0;
            switch (presentationParameters.depthStencilFormat)
            {
                case DepthFormat.None:              depthSize = 0; break;
                case DepthFormat.Depth16:           depthSize = 16; break;
                case DepthFormat.Depth24:           depthSize = 24; break;
                case DepthFormat.Depth24Stencil8:   depthSize = 24; stencilSize = 8; break;
                default:                throw new ArgumentException("depthStencilFormat");
            }

            var device = Renderer.Create(GameRunner.Singleton.Activity,
                                         GameRunner.Singleton.OnSurfaceChanged,
                                         8, 8, 8, 0, depthSize, stencilSize);
            FNA3D_ResetBackbuffer(device, ref presentationParameters);
            return device;

            /*var renderer = Renderer.Get(device);
            renderer.UserData = new State()
            {
                BackBufferWidth    = presentationParameters.backBufferWidth,
                BackBufferHeight   = presentationParameters.backBufferHeight,
                AdjustViewport     = // see also FNA3D_SetViewport
                    (presentationParameters.displayOrientation == DisplayOrientation.Default)
            };*/
        }

        //
        // FNA3D_ResetBackbuffer
        //

        public static void FNA3D_ResetBackbuffer(IntPtr device,
                                ref FNA3D_PresentationParameters presentationParameters)
        {
            var renderer = Renderer.Get(device);

            var state = (State) renderer.UserData;
            if (state == null)
            {
                state = new State();
                renderer.UserData = state;
            }

            state.BackBufferWidth  = presentationParameters.backBufferWidth;
            state.BackBufferHeight = presentationParameters.backBufferHeight;
            state.AdjustViewport   = // see also FNA3D_SetViewport
                (presentationParameters.displayOrientation == DisplayOrientation.Default);

            presentationParameters.backBufferFormat = SurfaceFormat.Color;
            presentationParameters.isFullScreen = 1;
        }

        //
        // FNA3D_DestroyDevice
        //

        public static void FNA3D_DestroyDevice(IntPtr device)
        {
            Renderer.Get(device).Release();
        }

        //
        // FNA3D_GetMaxTextureSlots
        //

        public static void FNA3D_GetMaxTextureSlots(IntPtr device,
                                                    out int textures, out int vertexTextures)
        {
            // XNA GraphicsDevice Limits from FNA3D/src/FNA3D_Driver.h
            const int MAX_TEXTURE_SAMPLERS = 16;
            const int MAX_VERTEXTEXTURE_SAMPLERS = 4;

            var renderer = Renderer.Get(device);
            int numSamplers = renderer.TextureUnits;
            // number of texture slots
            textures = Math.Min(numSamplers, MAX_TEXTURE_SAMPLERS);
            // number of vertex texture slots
            vertexTextures = Math.Min(Math.Max(numSamplers - MAX_TEXTURE_SAMPLERS, 0),
                                      MAX_VERTEXTEXTURE_SAMPLERS);
        }

        //
        // FNA3D_GetBackbufferDepthFormat
        //

        public static DepthFormat FNA3D_GetBackbufferDepthFormat(IntPtr device)
        {
            return Renderer.Get(device).SurfaceDepthFormat;
        }

        //
        // FNA3D_PresentationParameters
        //

        public struct FNA3D_PresentationParameters
        {
            public int backBufferWidth;
            public int backBufferHeight;
            public SurfaceFormat backBufferFormat;
            public int multiSampleCount;
            public IntPtr deviceWindowHandle;
            public byte isFullScreen;
            public DepthFormat depthStencilFormat;
            public PresentInterval presentationInterval;
            public DisplayOrientation displayOrientation;
            public RenderTargetUsage renderTargetUsage;
        }

        //
        // State
        //

        private partial class State
        {
            public int BackBufferWidth;
            public int BackBufferHeight;
            public bool AdjustViewport;
        }

    }

}
