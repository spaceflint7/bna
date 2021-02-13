
using System;
using android.opengl;
#pragma warning disable 0436

namespace Microsoft.Xna.Framework.Graphics
{

    public static partial class FNA3D
    {

        //
        // FNA3D_SetViewport
        //

        public static void FNA3D_SetViewport(IntPtr device, ref FNA3D_Viewport viewport)
        {
            var renderer = Renderer.Get(device);
            var state = (State) renderer.UserData;
            var v = viewport;

            if (    state.AdjustViewport && (! state.RenderToTexture)
                 && v.x == 0 && v.y == 0 && v.w > 0 && v.h > 0
                 && v.w == state.BackBufferWidth && v.h == state.BackBufferHeight)
            {
                var (s_w, s_h) = (renderer.SurfaceWidth, renderer.SurfaceHeight);
                if (v.w >= v.h)
                {
                    // adjust from virtual landscape
                    v.h = (int) ((v.h * s_w) / (float) v.w);
                    v.w = s_w;
                    v.y = (s_h - v.h) / 2;
                }
                else
                {
                    // adjust from virtual portrait
                    v.w = (int) ((v.w * s_w) / (float) v.h);
                    v.h = s_h;
                    v.x = (s_w - v.w) / 2;
                }
            }

            Renderer.Get(device).Send(false, () =>
            {
                GLES20.glViewport(v.x, v.y, v.w, v.h);
                GLES20.glDepthRangef(v.minDepth, v.maxDepth);
            });
        }

        //
        // FNA3D_SetScissorRect
        //

        public static void FNA3D_SetScissorRect(IntPtr device, ref Rectangle scissor)
        {
            var s = scissor;
            Renderer.Get(device).Send(false, () =>
            {
                GLES20.glScissor(s.X, s.Y, s.Width, s.Height);
            });
        }

        //
        // FNA3D_Clear
        //

        public static void FNA3D_Clear(IntPtr device, ClearOptions options, ref Vector4 color,
                                       float depth, int stencil)
        {
            var clearColor = color;
            var renderer = Renderer.Get(device);
            renderer.Send(false, () =>
            {
                var state = (State) renderer.UserData;
                var WriteMask = state.WriteMask;

                if (state.ScissorTest)
                {
                    // disable scissor before clear
                    GLES20.glDisable(GLES20.GL_SCISSOR_TEST);
                }

                bool restoreColorMask = false;
                bool restoreDepthMask = false;
                bool restoreStencilMask = false;

                int mask = 0;
                if ((options & ClearOptions.Target) != 0)
                {
                    mask |= GLES20.GL_COLOR_BUFFER_BIT;

                    if (clearColor != state.ClearColor)
                    {
                        state.ClearColor = clearColor;
                        GLES20.glClearColor(
                            clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
                    }

                    if ((WriteMask & (RED_MASK | GREEN_MASK | BLUE_MASK | ALPHA_MASK))
                                  != (RED_MASK | GREEN_MASK | BLUE_MASK | ALPHA_MASK))
                    {
                        // reset color masks before clear
                        GLES20.glColorMask(true, true, true, true);
                        restoreColorMask = true;
                    }
                }

                if ((options & ClearOptions.DepthBuffer) != 0)
                {
                    mask |= GLES20.GL_DEPTH_BUFFER_BIT;

                    if (depth != state.ClearDepth)
                    {
                        state.ClearDepth = depth;
                        GLES20.glClearDepthf(depth);
                    }

                    if ((state.WriteMask & DEPTH_MASK) == 0)
                    {
                        // reset depth mask before clear
                        GLES20.glDepthMask(true);
                        restoreDepthMask = true;
                    }
                }

                if ((options & ClearOptions.Stencil) != 0)
                {
                    mask |= GLES20.GL_STENCIL_BUFFER_BIT;

                    if (stencil != state.ClearStencil)
                    {
                        state.ClearStencil = stencil;
                        GLES20.glClearStencil(stencil);
                    }

                    if ((WriteMask & STENCIL_MASK) == 0)
                    {
                        // reset stencil mask before clear
                        GLES20.glStencilMask(-1);
                        restoreStencilMask = true;
                    }
                }

                GLES20.glClear(mask);

                if (restoreStencilMask)
                    GLES20.glStencilMask(WriteMask & STENCIL_MASK);

                if (restoreDepthMask)
                    GLES20.glDepthMask(false);

                if (restoreColorMask)
                {
                    GLES20.glColorMask((WriteMask & RED_MASK)   != 0 ? true : false,
                                       (WriteMask & GREEN_MASK) != 0 ? true : false,
                                       (WriteMask & BLUE_MASK)  != 0 ? true : false,
                                       (WriteMask & ALPHA_MASK) != 0 ? true : false);
                }

                if (state.ScissorTest)
                {
                    // restore scissor after clear
                    GLES20.glEnable(GLES20.GL_SCISSOR_TEST);
                }
            });
        }

        //
        // FNA3D_SwapBuffers
        //

        public static void FNA3D_SwapBuffers(IntPtr device,
                                             IntPtr sourceRectangle,
                                             IntPtr destinationRectangle,
                                             IntPtr overrideWindowHandle)
        {
            if (    (long) sourceRectangle != 0
                 || (long) destinationRectangle != 0
                 || (long) overrideWindowHandle != 0)
            {
                throw new PlatformNotSupportedException();
            }
            Renderer.Get(device).Present();
        }

        //
        // FNA3D_SetBlendState
        //

        public static void FNA3D_SetBlendState(IntPtr device, ref FNA3D_BlendState blendState)
        {
            var input = blendState;
            var renderer = Renderer.Get(device);
            Renderer.Get(device).Send(false, () =>
            {
                var state = (State) renderer.UserData;

                if (    input.colorSourceBlend      != Blend.One
                     || input.colorDestinationBlend != Blend.Zero
                     || input.alphaSourceBlend      != Blend.One
                     || input.alphaDestinationBlend != Blend.Zero)
                {
                    //
                    // blend state
                    //

                    if (! state.BlendEnable)
                    {
                        state.BlendEnable = true;
                        GLES20.glEnable(GLES20.GL_BLEND);
                    }

                    //
                    // XNA blend factor / GL blend color
                    //

                    if (input.blendFactor != state.BlendColor)
                    {
                        state.BlendColor = input.blendFactor;

                        GLES20.glBlendColor(state.BlendColor.R / 255f, state.BlendColor.G / 255f,
                                            state.BlendColor.B / 255f, state.BlendColor.A / 255f);
                    }

                    //
                    // XNA blend mode / GL blend function
                    //

                    if (    input.colorSourceBlend      != state.BlendSrcColor
                         || input.colorDestinationBlend != state.BlendDstColor
                         || input.alphaSourceBlend      != state.BlendSrcAlpha
                         || input.alphaDestinationBlend != state.BlendDstAlpha)
                    {
                        state.BlendSrcColor = input.colorSourceBlend;
                        state.BlendDstColor = input.colorDestinationBlend;
                        state.BlendSrcAlpha = input.alphaSourceBlend;
                        state.BlendDstAlpha = input.alphaDestinationBlend;

                        GLES20.glBlendFuncSeparate(
                                        BlendModeToBlendFunc[(int) state.BlendSrcColor],
                                        BlendModeToBlendFunc[(int) state.BlendDstColor],
                                        BlendModeToBlendFunc[(int) state.BlendSrcAlpha],
                                        BlendModeToBlendFunc[(int) state.BlendDstAlpha]);
                    }

                    //
                    // XNA blend function / GL blend equation
                    //

                    if (    input.colorBlendFunction != state.BlendFuncColor
                         || input.alphaBlendFunction != state.BlendFuncAlpha)
                    {
                        state.BlendFuncColor = input.colorBlendFunction;
                        state.BlendFuncAlpha = input.alphaBlendFunction;

                        GLES20.glBlendEquationSeparate(
                                BlendFunctionToBlendEquation[(int) state.BlendFuncColor],
                                BlendFunctionToBlendEquation[(int) state.BlendFuncAlpha]);
                    }

                    //
                    // color write mask
                    //

                    bool inputRed   = ((input.colorWriteEnable & ColorWriteChannels.Red)   != 0);
                    bool inputGreen = ((input.colorWriteEnable & ColorWriteChannels.Green) != 0);
                    bool inputBlue  = ((input.colorWriteEnable & ColorWriteChannels.Blue)  != 0);
                    bool inputAlpha = ((input.colorWriteEnable & ColorWriteChannels.Alpha) != 0);
                    var WriteMask = state.WriteMask;

                    if (    inputRed   != ((WriteMask & RED_MASK)   != 0)
                         || inputGreen != ((WriteMask & GREEN_MASK) != 0)
                         || inputBlue  != ((WriteMask & BLUE_MASK)  != 0)
                         || inputAlpha != ((WriteMask & ALPHA_MASK) != 0))
                    {
                        state.WriteMask = (inputRed   ? RED_MASK   : 0)
                                        | (inputGreen ? GREEN_MASK : 0)
                                        | (inputBlue  ? BLUE_MASK  : 0)
                                        | (inputAlpha ? ALPHA_MASK : 0);

                        GLES20.glColorMask(inputRed, inputGreen, inputBlue, inputAlpha);
                    }
                }
                else
                {
                    state.BlendEnable = false;
                    GLES20.glDisable(GLES20.GL_BLEND);
                }
            });
        }

        static int[] BlendModeToBlendFunc = new int[]
        {
            GLES20.GL_ONE,                      // Blend.One
            GLES20.GL_ZERO,                     // Blend.Zero
            GLES20.GL_SRC_COLOR,                // Blend.SourceColor
            GLES20.GL_ONE_MINUS_SRC_COLOR,      // Blend.InverseSourceColor
            GLES20.GL_SRC_ALPHA,                // Blend.SourceAlpha
            GLES20.GL_ONE_MINUS_SRC_ALPHA,      // Blend.InverseSourceAlpha
            GLES20.GL_DST_COLOR,                // Blend.DestinationColor
            GLES20.GL_ONE_MINUS_DST_COLOR,      // Blend.InverseDestinationColor
            GLES20.GL_DST_ALPHA,                // Blend.DestinationAlpha
            GLES20.GL_ONE_MINUS_DST_ALPHA,      // Blend.InverseDestinationAlpha
            GLES20.GL_CONSTANT_COLOR,           // Blend.BlendFactor
            GLES20.GL_ONE_MINUS_CONSTANT_COLOR, // Blend.InverseBlendFactor
            GLES20.GL_SRC_ALPHA_SATURATE        // Blend.SourceAlphaSaturation
        };

        static int[] BlendFunctionToBlendEquation = new int[]
        {
            GLES20.GL_FUNC_ADD,                 // BlendFunction.Add
            GLES20.GL_FUNC_SUBTRACT,            // BlendFunction.Subtract
            GLES20.GL_FUNC_REVERSE_SUBTRACT,    // BlendFunction.ReverseSubtract
            GLES30.GL_MAX,                      // BlendFunction.Max
            GLES30.GL_MIN                       // BlendFunction.Min
        };

        //
        // FNA3D_SetDepthStencilState
        //

        public static void FNA3D_SetDepthStencilState(IntPtr device,
                                                      ref FNA3D_DepthStencilState depthStencilState)
        {
        }

        //
        // FNA3D_ApplyRasterizerState
        //

        public static void FNA3D_ApplyRasterizerState(IntPtr device,
                                                      ref FNA3D_RasterizerState rasterizerState)
        {
            var input = rasterizerState;
            var renderer = Renderer.Get(device);
            Renderer.Get(device).Send(false, () =>
            {
                var state = (State) renderer.UserData;

                var inputScissorTest = (input.scissorTestEnable != 0);
                if (inputScissorTest != state.ScissorTest)
                {
                    state.ScissorTest = inputScissorTest;
                    if (inputScissorTest)
                        GLES20.glEnable(GLES20.GL_SCISSOR_TEST);
                    else
                        GLES20.glDisable(GLES20.GL_SCISSOR_TEST);
                }

                int inputCullMode;
                if (state.RenderToTexture)
                {
                    // select culling mode when rendering to texture, where we
                    // flip vertically;  see also EffectParameter::ApplyMultiplierY
                    inputCullMode =
                        (input.cullMode == CullMode.CullCounterClockwiseFace) ? GLES20.GL_BACK
                      : (input.cullMode == CullMode.CullClockwiseFace) ? GLES20.GL_FRONT : 0;
                }
                else
                {
                    // select culling mode when rendering directly to the screen
                    inputCullMode =
                        (input.cullMode == CullMode.CullCounterClockwiseFace) ? GLES20.GL_FRONT
                      : (input.cullMode == CullMode.CullClockwiseFace) ? GLES20.GL_BACK : 0;
                }
                if (inputCullMode != state.CullMode)
                {
                    state.CullMode = inputCullMode;
                    if (inputCullMode == 0)
                        GLES20.glDisable(GLES20.GL_CULL_FACE);
                    else
                    {
                        GLES20.glEnable(GLES20.GL_CULL_FACE);
                        GLES20.glCullFace(inputCullMode);
                    }
                }

                // fillMode (glPolygonMode) is not supported on GL ES,
                // so we also ignore depthBias (glPolygonOffset)
            });
        }

        //
        // FNA3D_GetBackbufferSize
        //

        public static void FNA3D_GetBackbufferSize(IntPtr device, out int w, out int h)
        {
            var bounds = GameRunner.Singleton.ClientBounds;
            w = bounds.Width;
            h = bounds.Height;
        }

        //
        // FNA3D_GetBackbufferSurfaceFormat
        //

        public static SurfaceFormat FNA3D_GetBackbufferSurfaceFormat(IntPtr device)
        {
            return SurfaceFormat.Color;
        }

        //
        // FNA3D_GetMaxMultiSampleCount
        //

        public static int FNA3D_GetMaxMultiSampleCount(IntPtr device, SurfaceFormat format,
                                                       int preferredMultiSampleCount)
        {
            return 0;

            #if false
                for (int i = 0; i < 21; i++)
                    GLES30.glGetInternalformativ(GLES20.GL_RENDERBUFFER,
                        SurfaceFormatToTextureInternalFormat[i],
                        GLES20.GL_SAMPLES, 1, count, 0);
                    if (GLES20.glGetError() == 0 && count[0] < preferredMultiSampleCount)
                        preferredMultiSampleCount = count[0];
            #endif
        }

        //
        // FNA3D_DrawIndexedPrimitives
        //

        public static void FNA3D_DrawIndexedPrimitives(IntPtr device, PrimitiveType primitiveType,
                                                       int baseVertex, int minVertexIndex,
                                                       int numVertices, int startIndex,
                                                       int primitiveCount, IntPtr indices,
                                                       IndexElementSize indexElementSize)
        {
            int elementSize, elementType;
            if (indexElementSize == IndexElementSize.SixteenBits)
            {
                elementSize = 2;
                elementType = GLES20.GL_UNSIGNED_SHORT;
            }
            else if (indexElementSize == IndexElementSize.ThirtyTwoBits)
            {
                elementSize = 4;
                elementType = GLES20.GL_UNSIGNED_INT;
            }
            else
                throw new ArgumentException("invalid IndexElementSize");

            int drawMode = PrimitiveTypeToDrawMode[(int) primitiveType];
            int maxVertexIndex = minVertexIndex + numVertices - 1;
            int indexOffset = startIndex * elementSize;
            primitiveCount = PrimitiveCount(primitiveType, primitiveCount);

            Renderer.Get(device).Send(false, () =>
            {
                GLES20.glBindBuffer(GLES20.GL_ELEMENT_ARRAY_BUFFER, (int) indices);
                GLES30.glDrawRangeElements(drawMode, minVertexIndex, maxVertexIndex,
                                           primitiveCount, elementType, indexOffset);
            });
        }

        static int[] PrimitiveTypeToDrawMode = new int[]
        {
            GLES20.GL_TRIANGLES,            // PrimitiveType.TriangleList
            GLES20.GL_TRIANGLE_STRIP,       // PrimitiveType.TriangleStrip
            GLES20.GL_LINES,                // PrimitiveType.LineList
            GLES20.GL_LINE_STRIP,           // PrimitiveType.LineStrip
        };

        static int PrimitiveCount(PrimitiveType primitiveType, int primitiveCount)
        {
            return primitiveType switch
            {
                PrimitiveType.TriangleList  => primitiveCount * 3,
                PrimitiveType.TriangleStrip => primitiveCount + 2,
                PrimitiveType.LineList      => primitiveCount * 2,
                PrimitiveType.LineStrip     => primitiveCount + 1,
                _ => throw new ArgumentException("invalid PrimitiveType"),
            };
        }

        //
        // FNA3D_DrawPrimitives
        //

        public static void FNA3D_DrawPrimitives(IntPtr device, PrimitiveType primitiveType,
                                                int vertexStart, int primitiveCount)
        {
            int drawMode = PrimitiveTypeToDrawMode[(int) primitiveType];
            primitiveCount = PrimitiveCount(primitiveType, primitiveCount);

            Renderer.Get(device).Send(false, () =>
            {
                GLES20.glDrawArrays(drawMode, vertexStart, primitiveCount);
            });
        }

        //
        // FNA3D_BlendState
        //

        public struct FNA3D_BlendState
        {
            public Blend colorSourceBlend;
            public Blend colorDestinationBlend;
            public BlendFunction colorBlendFunction;
            public Blend alphaSourceBlend;
            public Blend alphaDestinationBlend;
            public BlendFunction alphaBlendFunction;
            public ColorWriteChannels colorWriteEnable;
            public ColorWriteChannels colorWriteEnable1;
            public ColorWriteChannels colorWriteEnable2;
            public ColorWriteChannels colorWriteEnable3;
            public Color blendFactor;
            public int multiSampleMask;
        }

        //
        // FNA3D_DepthStencilState
        //

        public struct FNA3D_DepthStencilState
        {
            public byte depthBufferEnable;
            public byte depthBufferWriteEnable;
            public CompareFunction depthBufferFunction;
            public byte stencilEnable;
            public int stencilMask;
            public int stencilWriteMask;
            public byte twoSidedStencilMode;
            public StencilOperation stencilFail;
            public StencilOperation stencilDepthBufferFail;
            public StencilOperation stencilPass;
            public CompareFunction stencilFunction;
            public StencilOperation ccwStencilFail;
            public StencilOperation ccwStencilDepthBufferFail;
            public StencilOperation ccwStencilPass;
            public CompareFunction ccwStencilFunction;
            public int referenceStencil;
        }

        //
        // FNA3D_RasterizerState
        //

        public struct FNA3D_RasterizerState
        {
            public FillMode fillMode;
            public CullMode cullMode;
            public float depthBias;
            public float slopeScaleDepthBias;
            public byte scissorTestEnable;
            public byte multiSampleAntiAlias;
        }

        //
        // FNA3D_Viewport
        //

        public struct FNA3D_Viewport
        {
            public int x;
            public int y;
            public int w;
            public int h;
            public float minDepth;
            public float maxDepth;
        }

        //
        // State
        //

        private partial class State
        {
            public Vector4 ClearColor;
            public float ClearDepth;
            public int ClearStencil;
            public int WriteMask                    = -1;
            public int CullMode;
            public bool ScissorTest;

            public bool BlendEnable;
            public Color BlendColor;

            public Blend BlendSrcColor;
            public Blend BlendDstColor              = Blend.Zero;
            public Blend BlendSrcAlpha;
            public Blend BlendDstAlpha              = Blend.Zero;
            public BlendFunction BlendFuncColor;
            public BlendFunction BlendFuncAlpha;
        }

        private const int DEPTH_MASK   = 0x40000000;
        private const int ALPHA_MASK   = 0x20000000;
        private const int BLUE_MASK    = 0x04000000;
        private const int GREEN_MASK   = 0x02000000;
        private const int RED_MASK     = 0x01000000;
        private const int STENCIL_MASK = 0x00FFFFFF;
    }

}
