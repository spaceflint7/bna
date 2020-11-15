
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using android.opengl;
#pragma warning disable 0436

namespace Microsoft.Xna.Framework.Graphics
{

    public static partial class FNA3D
    {

        //
        // FNA3D_SetRenderTargets
        //

        public static unsafe void FNA3D_SetRenderTargets(IntPtr device,
                                                         FNA3D_RenderTargetBinding* renderTargets,
                                                         int numRenderTargets,
                                                         IntPtr depthStencilBuffer,
                                                         DepthFormat depthFormat,
                                                         byte preserveContents)
        {
            var renderTargetsCopy = new FNA3D_RenderTargetBinding[numRenderTargets];
            for (int i = 0; i < numRenderTargets; i++)
                renderTargetsCopy[i] = renderTargets[i];

            var renderer = Renderer.Get(device);
            renderer.Send( () =>
            {
                var state = (State) renderer.UserData;
                if (state.TargetFramebuffer == 0)
                {
                    var id = new int[1];
                    GLES20.glGenFramebuffers(1, id, 0);
                    if ((state.TargetFramebuffer = id[0]) == 0)
                        return;
                }
                GLES20.glBindFramebuffer(GLES20.GL_FRAMEBUFFER, state.TargetFramebuffer);

                int attachmentIndex = GLES20.GL_COLOR_ATTACHMENT0;
                foreach (var renderTarget in renderTargetsCopy)
                {
                    if (renderTarget.colorBuffer != IntPtr.Zero)
                    {
                        // a color buffer is only created if a non-zero result
                        // from FNA3D_GetMaxMultiSampleCount, which we never do
                        throw new PlatformNotSupportedException();
                        /*GLES20.glFramebufferRenderbuffer(
                            GLES20.GL_FRAMEBUFFER, attachmentIndex,
                            GLES20.GL_RENDERBUFFER, (int) renderTarget.colorBuffer);*/
                    }
                    else
                    {
                        int attachmentType = GLES20.GL_TEXTURE_2D;
                        if (renderTarget.type != /* FNA3D_RENDERTARGET_TYPE_2D */ 0)
                        {
                            attachmentType = GLES20.GL_TEXTURE_CUBE_MAP_POSITIVE_X
                                           + renderTarget.data2;
                        }
                        GLES20.glFramebufferTexture2D(
                            GLES20.GL_FRAMEBUFFER, attachmentIndex,
                            attachmentType, (int) renderTarget.texture, 0);
                    }
                    attachmentIndex++;
                }

                int lastAttachmentPlusOne = state.ActiveAttachments;
                state.ActiveAttachments = attachmentIndex;
                while (attachmentIndex < lastAttachmentPlusOne)
                {
                    GLES20.glFramebufferRenderbuffer(
                        GLES20.GL_FRAMEBUFFER, attachmentIndex++,
                        GLES20.GL_RENDERBUFFER, 0);
                }

                GLES20.glFramebufferRenderbuffer(
                    GLES20.GL_FRAMEBUFFER, GLES30.GL_DEPTH_STENCIL_ATTACHMENT,
                    GLES20.GL_RENDERBUFFER, (int) depthStencilBuffer);

                state.RenderToTexture = true;
            });
        }

        //
        // FNA3D_SetRenderTargets
        //

        public static void FNA3D_SetRenderTargets(IntPtr device,
                                                  IntPtr renderTargets,
                                                  int numRenderTargets,
                                                  IntPtr depthStencilBuffer,
                                                  DepthFormat depthFormat,
                                                  byte preserveContents)
        {
            if (renderTargets != IntPtr.Zero || numRenderTargets != 0)
                throw new PlatformNotSupportedException();

            var renderer = Renderer.Get(device);
            renderer.Send( () =>
            {
                GLES20.glBindFramebuffer(GLES20.GL_FRAMEBUFFER, 0);

                var state = (State) renderer.UserData;
                state.RenderToTexture = false;
            });
        }

        //
        // FNA3D_ResolveTarget
        //

        public static void FNA3D_ResolveTarget(IntPtr device,
                                               ref FNA3D_RenderTargetBinding renderTarget)
        {
            if (renderTarget.multiSampleCount > 0)
            {
                // no support for multisampling; see FNA3D_SetRenderTargets
                throw new PlatformNotSupportedException();
            }

            if (renderTarget.levelCount > 1)
            {
                int attachmentType = GLES20.GL_TEXTURE_2D;
                if (renderTarget.type != /* FNA3D_RENDERTARGET_TYPE_2D */ 0)
                {
                    attachmentType = GLES20.GL_TEXTURE_CUBE_MAP_POSITIVE_X
                                   + renderTarget.data2;
                }
                int texture = (int) renderTarget.texture;

                var renderer = Renderer.Get(device);
                renderer.Send( () =>
                {
                    int textureUnit = GLES20.GL_TEXTURE0 + renderer.TextureUnits - 1;
                    GLES20.glActiveTexture(textureUnit);

                    GLES20.glBindTexture(attachmentType, texture);
                    GLES20.glGenerateMipmap(attachmentType);
                    GLES20.glBindTexture(attachmentType, 0);
                });
            }
        }

        //
        // FNA3D_GenDepthStencilRenderbuffer
        //

        public static IntPtr FNA3D_GenDepthStencilRenderbuffer(IntPtr device,
                                                               int width, int height,
                                                               DepthFormat format,
                                                               int multiSampleCount)
        {
            if (multiSampleCount > 0)
            {
                // no support for multisampling; see FNA3D_SetRenderTargets
                throw new PlatformNotSupportedException();
            }

            int bufferId = 0;

            var renderer = Renderer.Get(device);
            renderer.Send( () =>
            {
                var state = (State) renderer.UserData;

                var id = new int[1];
                GLES20.glGenRenderbuffers(1, id, 0);
                if (id[0] != 0)
                {
                    GLES20.glBindRenderbuffer(GLES20.GL_RENDERBUFFER, id[0]);
                    GLES20.glRenderbufferStorage(GLES20.GL_RENDERBUFFER,
                                                 DepthFormatToDepthStorage[(int) format],
                                                 width, height);

                    bufferId = id[0];
                }
            });

            return (IntPtr) bufferId;
        }

        //
        // Delete Render Target
        //

        public static void FNA3D_AddDisposeRenderbuffer(IntPtr device, IntPtr renderbuffer)
        {
            var renderer = Renderer.Get(device);
            renderer.Send( () =>
            {
                GLES20.glDeleteRenderbuffers(1, new int[] { (int) renderbuffer }, 0);
            });
        }

        //
        // IsRenderToTexture
        //

        public static bool IsRenderToTexture(GraphicsDevice graphicsDevice)
        {
            // should be called in the renderer thread context
            return ((State) Renderer.Get(graphicsDevice.GLDevice).UserData).RenderToTexture;
        }

        //
        // DepthFormatToDepthStorage
        //

        static int[] DepthFormatToDepthStorage = new int[]
        {
            GLES20.GL_ZERO,                 // invalid
            GLES20.GL_DEPTH_COMPONENT16,    // DepthFormat.Depth16
            GLES30.GL_DEPTH_COMPONENT24,    // DepthFormat.Depth24
            GLES30.GL_DEPTH24_STENCIL8,     // DepthFormat.Depth24Stencil8
        };

        //
        // FNA3D_RenderTargetBinding
        //

        public struct FNA3D_RenderTargetBinding
        {
            public byte type;   // 0 for RenderTarget2D, 1 for RenderTargetCube
            public int data1;   // 2D width or cube size
            public int data2;   // 2D height or cube face
            public int levelCount;
            public int multiSampleCount;
            public IntPtr texture;
            public IntPtr colorBuffer;
        }

        //
        // State
        //

        private partial class State
        {
            public bool RenderToTexture;
            public int TargetFramebuffer;
            public int ActiveAttachments;
        }

    }
}

