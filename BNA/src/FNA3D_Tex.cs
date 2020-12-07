
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
        // FNA3D_SupportsDXT1
        //

        public static byte FNA3D_SupportsDXT1(IntPtr device)
        {
            var fmts = Renderer.Get(device).TextureFormats;
            bool f = -1 != Array.IndexOf(fmts, GL_COMPRESSED_RGBA_S3TC_DXT1_EXT);
            return (byte) (f ? 1 : 0);
        }

        //
        // FNA3D_SupportsS3TC
        //

        public static byte FNA3D_SupportsS3TC(IntPtr device)
        {
            var fmts = Renderer.Get(device).TextureFormats;
            bool f = -1 != Array.IndexOf(fmts, GL_COMPRESSED_RGBA_S3TC_DXT3_EXT)
                  && -1 != Array.IndexOf(fmts, GL_COMPRESSED_RGBA_S3TC_DXT5_EXT);
            return (byte) (f ? 1 : 0);
        }

        //
        // Create Textures
        //

        private static int CreateTexture(Renderer renderer, int textureKind,
                                         SurfaceFormat format, int levelCount)
        {
            int[] id = new int[1];
            GLES20.glGenTextures(1, id, 0);
            if (id[0] != 0)
            {
                GLES20.glBindTexture(textureKind, id[0]);

                var state = (State) renderer.UserData;
                state.TextureConfigs[id[0]] =
                                new int[] { textureKind, (int) format, levelCount };

            }
            return id[0];
        }

        public static IntPtr FNA3D_CreateTexture2D(IntPtr device, SurfaceFormat format,
                                                   int width, int height, int levelCount,
                                                   byte isRenderTarget)
        {
            var renderer = Renderer.Get(device);
            if (    width <= 0 || width > renderer.TextureSize
                 || height <= 0 || height > renderer.TextureSize)
            {
                throw new ArgumentException($"bad texture size {width} x {height}");
            }

            int textureFormat = SurfaceFormatToTextureFormat[(int) format];
            int internalFormat = SurfaceFormatToTextureInternalFormat[(int) format];
            int dataType, dataSize;

            if (textureFormat == GLES20.GL_COMPRESSED_TEXTURE_FORMATS)
            {
                dataType = textureFormat;
                dataSize = SurfaceFormatToTextureDataSize[(int) format];
            }
            else
            {
                dataType = SurfaceFormatToTextureDataType[(int) format];
                dataSize = 0;
            }

            if (    textureFormat  == GLES20.GL_ZERO
                 || internalFormat == GLES20.GL_ZERO
                 || dataType       == GLES20.GL_ZERO)
            {
                throw new PlatformNotSupportedException(
                                        $"unsupported texture format {format}");
            }

            int textureId = 0;
            renderer.Send( () =>
            {
                int id = CreateTexture(renderer, GLES20.GL_TEXTURE_2D, format, levelCount);
                if (id != 0)
                {
                    textureId = id;

                    for (int level = 0; level < levelCount; level++)
                    {
                        if (textureFormat == GLES20.GL_COMPRESSED_TEXTURE_FORMATS)
                        {
                            int levelSize = dataSize *
                                    ((width + 3) / 4) * ((height + 3) / 4);

                            GLES20.glCompressedTexImage2D(GLES20.GL_TEXTURE_2D,
                                level, internalFormat,
                                width, height, /* border */ 0,
                                levelSize, null);
                        }
                        else
                        {
                            GLES20.glTexImage2D(GLES20.GL_TEXTURE_2D,
                                level, internalFormat,
                                width, height, /* border */ 0,
                                textureFormat, dataType, null);
                        }

                        width >>= 1;
                        height >>= 1;
                        if (width <= 0)
                            width = 1;
                        if (height <= 0)
                            height = 1;
                    }
                }
            });
            return (IntPtr) textureId;
        }



        //
        // Delete Textures
        //

        public static void FNA3D_AddDisposeTexture(IntPtr device, IntPtr texture)
        {
            var renderer = Renderer.Get(device);
            renderer.Send( () =>
            {
                GLES20.glDeleteTextures(1, new int[] { (int) texture }, 0);

                var state = (State) renderer.UserData;
                state.TextureConfigs.Remove((int) texture);
            });
        }



        //
        // Set Texture Data
        //

        private static void SetTextureData(Renderer renderer, int textureId,
                                           int x, int y, int w, int h, int level,
                                           object dataObject, int dataOffset, int dataLength)
        {
            java.nio.Buffer buffer = dataObject switch
            {
                sbyte[] byteArray =>
                    java.nio.ByteBuffer.wrap(byteArray, dataOffset, dataLength),

                int[] intArray =>
                    java.nio.IntBuffer.wrap(intArray, dataOffset / 4, dataLength / 4),

                _ => throw new ArgumentException(dataObject?.GetType().ToString()),
            };

            renderer.Send( () =>
            {
                GLES20.glBindTexture(GLES20.GL_TEXTURE_2D, textureId);

                var state = (State) renderer.UserData;
                var config = state.TextureConfigs[textureId];
                int format = config[1];

                int textureFormat = SurfaceFormatToTextureFormat[format];
                if (textureFormat == GLES20.GL_COMPRESSED_TEXTURE_FORMATS)
                {
                    int internalFormat = SurfaceFormatToTextureInternalFormat[format];

                    GLES20.glCompressedTexSubImage2D(GLES20.GL_TEXTURE_2D,
                            level, x, y, w, h, internalFormat, dataLength, buffer);
                }
                else
                {
                    int dataType = SurfaceFormatToTextureDataType[format];
                    int dataSize = SurfaceFormatToTextureDataSize[format];

                    if (dataSize != 4)
                        GLES20.glPixelStorei(GLES20.GL_UNPACK_ALIGNMENT, dataSize);

                    GLES20.glTexSubImage2D(GLES20.GL_TEXTURE_2D, level, x, y, w, h,
                                           textureFormat, dataType, buffer);

                    if (dataSize != 4)
                        GLES20.glPixelStorei(GLES20.GL_UNPACK_ALIGNMENT, 4);

                }
            });
        }

        public static void FNA3D_SetTextureData2D(IntPtr device, IntPtr texture,
                                                  int x, int y, int w, int h, int level,
                                                  IntPtr data, int dataLength)
        {
            // FNA Texture2D uses GCHandle::Alloc and GCHandle::AddrOfPinnedObject.
            // we use GCHandle::FromIntPtr to convert that address to an object reference.
            // see also:  system.runtime.interopservices.GCHandle struct in baselib.
            int dataOffset = (int) data;
            var dataObject = System.Runtime.InteropServices.GCHandle.FromIntPtr(data).Target;
            SetTextureData(Renderer.Get(device), (int) texture,
                           x, y, w, h, level, dataObject, dataOffset, dataLength);
        }



        //
        // ReadImageStream
        //

        public static IntPtr ReadImageStream(System.IO.Stream stream,
                                             out int width, out int height, out int len,
                                             int forceWidth, int forceHeight, bool zoom)
        {
            var bitmap = LoadBitmap(stream);

            int[] pixels;
            if (forceWidth == -1 || forceHeight == -1)
            {
                pixels = GetPixels(bitmap, out width, out height, out len);
            }
            else
            {
                pixels = CropAndScale(bitmap, forceWidth, forceHeight, zoom,
                                      out width, out height, out len);
            }

            // keep a strong reference until FNA3D_Image_Free is called
            ImagePixels.set(pixels);

            return System.Runtime.InteropServices.GCHandle.Alloc(
                        pixels, System.Runtime.InteropServices.GCHandleType.Pinned)
                            .AddrOfPinnedObject();


            android.graphics.Bitmap LoadBitmap(System.IO.Stream stream)
            {
                if (stream is Microsoft.Xna.Framework.TitleContainer.TitleStream titleStream)
                {
                    var bitmap = android.graphics.BitmapFactory
                                                .decodeStream(titleStream.JavaStream);
                    if (    bitmap == null
                         || bitmap.getConfig() != android.graphics.Bitmap.Config.ARGB_8888)
                    {
                        string reason = (bitmap == null) ? "unspecified error"
                                      : $"unsupported config '{bitmap.getConfig()}'";
                        throw new BadImageFormatException(
                            $"Load failed for bitmap image '{titleStream.Name}': {reason}");
                    }

                    return bitmap;
                }

                throw new ArgumentException(stream?.GetType()?.ToString());
            }


            int[] CropAndScale(android.graphics.Bitmap bitmap,
                               int newWidth, int newHeight, bool zoom,
                               out int width, out int height, out int len)
            {
                int oldWidth = bitmap.getWidth();
                int oldHeight = bitmap.getHeight();
                bool scaleWidth = zoom ? (oldWidth < oldHeight) : (oldWidth > oldHeight);
                float scaleFactor = scaleWidth ? ((float) newWidth / (float) oldWidth)
                                               : ((float) newHeight / (float) oldHeight);
                if (zoom)
                {
                    int x, y, w, h;
                    if (scaleWidth)
                    {
                        x = 0;
                        y = (int) (oldHeight / 2 - (newHeight / scaleFactor) / 2);
                        w = oldWidth;
                        h = (int) (newHeight / scaleFactor);
                    }
                    else
                    {
                        x = (int) (oldWidth / 2 - (newWidth / scaleFactor) / 2);
                        y = 0;
                        w = (int) (newWidth / scaleFactor);
                        h = oldHeight;
                    }
                    bitmap = android.graphics.Bitmap.createBitmap(bitmap, x, y, w, h);
                }
                else
                {
                    newWidth = (int) (oldWidth * scaleFactor);
                    newHeight = (int) (oldHeight * scaleFactor);
                }

                return GetPixels(android.graphics.Bitmap.createScaledBitmap(
                                                bitmap, newWidth, newHeight, false),
                                 out width, out height, out len);
            }


            int[] GetPixels(android.graphics.Bitmap bitmap,
                            out int width, out int height, out int len)
            {
                int w = bitmap.getWidth();
                int h = bitmap.getHeight();
                var pixels = new int[w * h];
                bitmap.copyPixelsToBuffer(java.nio.IntBuffer.wrap(pixels));
                width = w;
                height = h;
                len = w * h * 4;
                return pixels;
            }
        }

        //
        // FNA3D_Image_Free
        //

        public static void FNA3D_Image_Free(IntPtr mem)
        {
            // FNA calls this method after uploading the data returned by
            // ReadImageStream, so we can safely discard the reference
            ImagePixels.set(null);
        }

        //
        // FNA3D_VerifySampler
        //

        public static void FNA3D_VerifySampler(IntPtr device, int index, IntPtr texture,
                                               ref FNA3D_SamplerState sampler)
        {
            var samplerCopy = sampler;
            int textureId = (int) texture;
            var renderer = Renderer.Get(device);

            renderer.Send( () =>
            {
                var state = (State) renderer.UserData;
                var config = state.TextureConfigs[textureId];

                GLES20.glActiveTexture(GLES20.GL_TEXTURE0 + index);
                GLES20.glBindTexture(config[0], textureId);

                if (index == renderer.TextureUnits - 1)
                    state.TextureOnLastUnit = textureId;

                if (textureId == 0)
                    return;

                GLES20.glTexParameteri(config[0], GLES30.GL_TEXTURE_MAX_LEVEL,
                                       config[2] - 1);
                GLES20.glTexParameteri(config[0], GLES30.GL_TEXTURE_BASE_LEVEL,
                                       samplerCopy.maxMipLevel);

                GLES20.glTexParameteri(config[0], GLES20.GL_TEXTURE_WRAP_S,
                                       TextureWrapMode[(int) samplerCopy.addressU]);
                GLES20.glTexParameteri(config[0], GLES20.GL_TEXTURE_WRAP_T,
                                       TextureWrapMode[(int) samplerCopy.addressV]);
                if (config[0] == GLES30.GL_TEXTURE_3D)
                {
                    GLES20.glTexParameteri(config[0], GLES30.GL_TEXTURE_WRAP_R,
                                           TextureWrapMode[(int) samplerCopy.addressW]);
                }

                int magIndex = (int) samplerCopy.filter * 3;
                int minIndex = magIndex + (config[2] <= 1 ? 1 : 2);

                GLES20.glTexParameteri(config[0], GLES20.GL_TEXTURE_MAG_FILTER,
                                       TextureFilterMode[magIndex]);
                GLES20.glTexParameteri(config[0], GLES20.GL_TEXTURE_MIN_FILTER,
                                       TextureFilterMode[minIndex]);

            });
        }



        //
        // SurfaceFormatToTextureFormat
        //

        static int[] SurfaceFormatToTextureFormat = new int[]
        {
            GLES20.GL_RGBA,                         // SurfaceFormat.Color
            GLES20.GL_RGB,                          // SurfaceFormat.Bgr565
            GLES20.GL_ZERO,  // was GL_BGRA         // SurfaceFormat.Bgra5551
            GLES20.GL_ZERO,  // was GL_BGRA         // SurfaceFormat.Bgra4444
            GLES20.GL_COMPRESSED_TEXTURE_FORMATS,   // SurfaceFormat.Dxt1
            GLES20.GL_COMPRESSED_TEXTURE_FORMATS,   // SurfaceFormat.Dxt3
            GLES20.GL_COMPRESSED_TEXTURE_FORMATS,   // SurfaceFormat.Dxt5
            GLES30.GL_RG,                           // SurfaceFormat.NormalizedByte2
            GLES20.GL_RGBA,                         // SurfaceFormat.NormalizedByte4
            GLES20.GL_RGBA,                         // SurfaceFormat.Rgba1010102
            GLES30.GL_RG,                           // SurfaceFormat.Rg32
            GLES20.GL_RGBA,                         // SurfaceFormat.Rgba64
            GLES20.GL_ALPHA,                        // SurfaceFormat.Alpha8
            GLES30.GL_RED,                          // SurfaceFormat.Single
            GLES30.GL_RG,                           // SurfaceFormat.Vector2
            GLES20.GL_RGBA,                         // SurfaceFormat.Vector4
            GLES30.GL_RED,                          // SurfaceFormat.HalfSingle
            GLES30.GL_RG,                           // SurfaceFormat.HalfVector2
            GLES20.GL_RGBA,                         // SurfaceFormat.HalfVector4
            GLES20.GL_RGBA,                         // SurfaceFormat.HdrBlendable
            GLES20.GL_ZERO,  // was GL_BGRA         // SurfaceFormat.ColorBgraEXT
        };

        //
        // SurfaceFormatToTextureInternalFormat
        //

        static int[] SurfaceFormatToTextureInternalFormat = new int[]
        {
            GLES30.GL_RGBA8,                        // SurfaceFormat.Color
            GLES30.GL_RGB8,                         // SurfaceFormat.Bgr565
            GLES20.GL_RGB5_A1,                      // SurfaceFormat.Bgra5551
            GLES20.GL_RGBA4,                        // SurfaceFormat.Bgra4444
            GL_COMPRESSED_RGBA_S3TC_DXT1_EXT,       // SurfaceFormat.Dxt1
            GL_COMPRESSED_RGBA_S3TC_DXT3_EXT,       // SurfaceFormat.Dxt3
            GL_COMPRESSED_RGBA_S3TC_DXT5_EXT,       // SurfaceFormat.Dxt5
            GLES30.GL_RG8,                          // SurfaceFormat.NormalizedByte2
            GLES30.GL_RGBA8,                        // SurfaceFormat.NormalizedByte4
            GLES30.GL_RGB10_A2,  // was ..._A2_EXT  // SurfaceFormat.Rgba1010102
            GLES20.GL_ZERO,      // was GL_RG16     // SurfaceFormat.Rg32
            GLES20.GL_ZERO,      // was GL_RGBA16,  // SurfaceFormat.Rgba64
            GLES20.GL_ALPHA,                        // SurfaceFormat.Alpha8
            GLES30.GL_R32F,                         // SurfaceFormat.Single
            GLES30.GL_RG32F,                        // SurfaceFormat.Vector2
            GLES30.GL_RGBA32F,                      // SurfaceFormat.Vector4
            GLES30.GL_R16F,                         // SurfaceFormat.HalfSingle
            GLES30.GL_RG16F,                        // SurfaceFormat.HalfVector2
            GLES30.GL_RGBA16F,                      // SurfaceFormat.HalfVector4
            GLES30.GL_RGBA16F,                      // SurfaceFormat.HdrBlendable
            GLES30.GL_RGBA8,                        // SurfaceFormat.ColorBgraEXT
        };

        //
        // SurfaceFormatToTextureDataType
        //

        static int[] SurfaceFormatToTextureDataType = new int[]
        {
            GLES20.GL_UNSIGNED_BYTE,                // SurfaceFormat.Color
            GLES20.GL_UNSIGNED_SHORT_5_6_5,         // SurfaceFormat.Bgr565
            GLES20.GL_ZERO,  // was ..._5_5_5_1_REV // SurfaceFormat.Bgra5551
            GLES20.GL_ZERO,  // was ..._4_4_4_4_REV // SurfaceFormat.Bgra4444
            GLES20.GL_ZERO,  // not applicable      // SurfaceFormat.Dxt1
            GLES20.GL_ZERO,  // not applicable      // SurfaceFormat.Dxt3
            GLES20.GL_ZERO,  // not applicable      // SurfaceFormat.Dxt5
            GLES20.GL_BYTE,                         // SurfaceFormat.NormalizedByte2
            GLES20.GL_BYTE,                         // SurfaceFormat.NormalizedByte4
            GLES30.GL_UNSIGNED_INT_2_10_10_10_REV,  // SurfaceFormat.Rgba1010102
            GLES20.GL_UNSIGNED_SHORT,               // SurfaceFormat.Rg32
            GLES20.GL_UNSIGNED_SHORT,               // SurfaceFormat.Rgba64
            GLES20.GL_UNSIGNED_BYTE,                // SurfaceFormat.Alpha8
            GLES20.GL_FLOAT,                        // SurfaceFormat.Single
            GLES20.GL_FLOAT,                        // SurfaceFormat.Vector2
            GLES20.GL_FLOAT,                        // SurfaceFormat.Vector4
            GLES30.GL_HALF_FLOAT,                   // SurfaceFormat.HalfSingle
            GLES30.GL_HALF_FLOAT,                   // SurfaceFormat.HalfVector2
            GLES30.GL_HALF_FLOAT,                   // SurfaceFormat.HalfVector4
            GLES30.GL_HALF_FLOAT,                   // SurfaceFormat.HdrBlendable
            GLES20.GL_UNSIGNED_BYTE                 // SurfaceFormat.ColorBgraEXT
        };

        // from ubiquitous extension EXT_texture_compression_s3tc
        const int GL_COMPRESSED_RGBA_S3TC_DXT1_EXT = 0x83F1;
        const int GL_COMPRESSED_RGBA_S3TC_DXT3_EXT = 0x83F2;
        const int GL_COMPRESSED_RGBA_S3TC_DXT5_EXT = 0x83F3;

        //
        // SurfaceFormatToTextureDataSize
        //

        static int[] SurfaceFormatToTextureDataSize = new int[]
        {
            4,                                      // SurfaceFormat.Color
            2,                                      // SurfaceFormat.Bgr565
            2,                                      // SurfaceFormat.Bgra5551
            2,                                      // SurfaceFormat.Bgra4444
            8,                                      // SurfaceFormat.Dxt1
            16,                                     // SurfaceFormat.Dxt3
            16,                                     // SurfaceFormat.Dxt5
            2,                                      // SurfaceFormat.NormalizedByte2
            4,                                      // SurfaceFormat.NormalizedByte4
            4,                                      // SurfaceFormat.Rgba1010102
            4,                                      // SurfaceFormat.Rg32
            8,                                      // SurfaceFormat.Rgba64
            1,                                      // SurfaceFormat.Alpha8
            4,                                      // SurfaceFormat.Single
            8,                                      // SurfaceFormat.Vector2
            16,                                     // SurfaceFormat.Vector4
            2,                                      // SurfaceFormat.HalfSingle
            4,                                      // SurfaceFormat.HalfVector2
            8,                                      // SurfaceFormat.HalfVector4
            8,                                      // SurfaceFormat.HdrBlendable
            4,                                      // SurfaceFormat.ColorBgraEXT
        };

        //
        // TextureWrapMode
        //

        static int[] TextureWrapMode = new int[]
        {
            GLES20.GL_REPEAT,           // TextureAddressMode.Wrap
            GLES20.GL_CLAMP_TO_EDGE,    // TextureAddressMode.Clamp
            GLES20.GL_MIRRORED_REPEAT   // TextureAddressMode.Mirror
        };

        //
        // TextureFilterMode
        //

        static int[] TextureFilterMode = new int[]
        {
            // TextureFilter.Linear: mag filter, min filter, mipmap filter
            GLES20.GL_LINEAR, GLES20.GL_LINEAR, GLES20.GL_LINEAR_MIPMAP_LINEAR,
            // TextureFilter.Point
            GLES20.GL_NEAREST, GLES20.GL_NEAREST, GLES20.GL_NEAREST_MIPMAP_NEAREST,
            // TextureFilter.Anisotropic
            GLES20.GL_LINEAR, GLES20.GL_LINEAR, GLES20.GL_LINEAR_MIPMAP_LINEAR,
            // TextureFilter.LinearMipPoint
            GLES20.GL_LINEAR, GLES20.GL_LINEAR, GLES20.GL_LINEAR_MIPMAP_NEAREST,
            // TextureFilter.PointMipLinear
            GLES20.GL_NEAREST, GLES20.GL_NEAREST, GLES20.GL_NEAREST_MIPMAP_LINEAR,
            // TextureFilter.MinLinearMagPointMipLinear
            GLES20.GL_NEAREST, GLES20.GL_LINEAR, GLES20.GL_LINEAR_MIPMAP_LINEAR,
            // TextureFilter.MinLinearMagPointMipPoint
            GLES20.GL_NEAREST, GLES20.GL_LINEAR, GLES20.GL_LINEAR_MIPMAP_NEAREST,
            // TextureFilter.MinPointMagLinearMipLinear
            GLES20.GL_LINEAR, GLES20.GL_NEAREST, GLES20.GL_NEAREST_MIPMAP_LINEAR,
            // TextureFilter.MinPointMagLinearMipPoint
            GLES20.GL_LINEAR, GLES20.GL_NEAREST, GLES20.GL_NEAREST_MIPMAP_NEAREST,
        };

        //
        // FNA3D_SamplerState
        //

        public struct FNA3D_SamplerState
        {
            public TextureFilter filter;
            public TextureAddressMode addressU;
            public TextureAddressMode addressV;
            public TextureAddressMode addressW;
            public float mipMapLevelOfDetailBias;
            public int maxAnisotropy;
            public int maxMipLevel;
        }



        //
        // data
        //

        private static readonly java.lang.ThreadLocal ImagePixels = new java.lang.ThreadLocal();

        //
        // state
        //

        private partial class State
        {
            // texture config array:
            // #0 - target type
            // #1 - SurfaceFormat
            // #2 - levels count
            public Dictionary<int, int[]> TextureConfigs = new Dictionary<int, int[]>();

            public int TextureOnLastUnit;
        }

    }
}
