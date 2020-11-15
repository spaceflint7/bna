
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
        // FNA3D_SupportsNoOverwrite
        //

        public static byte FNA3D_SupportsNoOverwrite(IntPtr device)
        {
            // prevent flag SetDataOptions.NoOverwrite in Set*BufferData calls
            return 0;
        }



        //
        // Create Buffers
        //

        private static int CreateBuffer(Renderer renderer, int target, byte dynamic, int size)
        {
            int bufferId = 0;
            renderer.Send( () =>
            {
                int[] id = new int[1];
                GLES20.glGenBuffers(1, id, 0);
                if (id[0] != 0)
                {
                    bufferId = id[0];
                    int usage = (dynamic != 0 ? GLES20.GL_STREAM_DRAW
                                              : GLES20.GL_STATIC_DRAW);
                    GLES20.glBindBuffer(target, bufferId);
                    GLES20.glBufferData(target, size, null, usage);

                    var state = (State) renderer.UserData;
                    state.BufferSizeUsage[bufferId] = new int[] { size, usage };
                }
            });
            return bufferId;
        }

        public static IntPtr FNA3D_GenVertexBuffer(IntPtr device, byte dynamic,
                                                   BufferUsage usage, int sizeInBytes)
        {
            return (IntPtr) CreateBuffer(Renderer.Get(device),
                                         GLES20.GL_ARRAY_BUFFER,
                                         dynamic, sizeInBytes);
        }

        public static IntPtr FNA3D_GenIndexBuffer(IntPtr device, byte dynamic,
                                                  BufferUsage usage, int sizeInBytes)
        {
            return (IntPtr) CreateBuffer(Renderer.Get(device),
                                         GLES20.GL_ELEMENT_ARRAY_BUFFER,
                                         dynamic, sizeInBytes);
        }



        //
        // Delete Buffers
        //

        public static void FNA3D_AddDisposeVertexBuffer(IntPtr device, IntPtr buffer)
        {
            var renderer = Renderer.Get(device);
            renderer.Send( () =>
            {
                GLES20.glDeleteBuffers(1, new int[] { (int) buffer }, 0);

                var state = (State) renderer.UserData;
                state.BufferSizeUsage.Remove((int) buffer);
            });
        }

        public static void FNA3D_AddDisposeIndexBuffer(IntPtr device, IntPtr buffer)
        {
            FNA3D_AddDisposeVertexBuffer(device, buffer);
        }



        //
        // Set Buffer Data
        //

        private static void SetBufferData(Renderer renderer, int target, int bufferId,
                                          int bufferOffset, bool discard,
                                          IntPtr dataPointer, int dataLength)
        {
            var state = (State) renderer.UserData;
            var dataBuffer = BufferSerializer.Convert(
                                    dataPointer, dataLength, state, bufferId);

            renderer.Send( () =>
            {
                GLES20.glBindBuffer(target, bufferId);

                if (discard)
                {
                    var sizeUsage = state.BufferSizeUsage[bufferId];
                    GLES20.glBufferData(target, sizeUsage[0], null, sizeUsage[1]);
                }

                GLES20.glBufferSubData(target, bufferOffset, dataLength, dataBuffer);
            });
        }

        public static void FNA3D_SetVertexBufferData(IntPtr device, IntPtr buffer,
                                                     int offsetInBytes, IntPtr data,
                                                     int elementCount, int elementSizeInBytes,
                                                     int vertexStride, SetDataOptions options)
        {
            if (elementSizeInBytes != vertexStride)
                throw new System.ArgumentException("elementSizeInBytes != vertexStride");

            SetBufferData(Renderer.Get(device), GLES20.GL_ARRAY_BUFFER,
                          (int) buffer, offsetInBytes,
                          (options == SetDataOptions.Discard),
                          data, elementCount * elementSizeInBytes);
        }

        public static void FNA3D_SetIndexBufferData(IntPtr device, IntPtr buffer,
                                                    int offsetInBytes, IntPtr data,
                                                    int dataLength, SetDataOptions options)
        {
            SetBufferData(Renderer.Get(device), GLES20.GL_ELEMENT_ARRAY_BUFFER,
                          (int) buffer, offsetInBytes,
                          (options == SetDataOptions.Discard),
                          data, dataLength);
        }



        //
        // Set Buffer Attributes
        //

        public static unsafe void FNA3D_ApplyVertexBufferBindings(IntPtr device,
                                                                  FNA3D_VertexBufferBinding* bindings,
                                                                  int numBindings, byte bindingsUpdated,
                                                                  int baseVertex)
        {
            var bindingsCopy = new FNA3D_VertexBufferBinding[numBindings];
            for (int i = 0; i < numBindings; i++)
                bindingsCopy[i] = bindings[i];

            Renderer.Get(device).Send( () =>
            {
                int nextAttribIndex = 0;
                foreach (var binding in bindingsCopy)
                {
                    if (binding.instanceFrequency != 0)
                        throw new ArgumentException("InstanceFrequnecy != 0");
                    GLES20.glBindBuffer(GLES20.GL_ARRAY_BUFFER, (int) binding.vertexBuffer);

                    var vertexDecl = binding.vertexDeclaration;
                    var elements = (VertexElement[]) System.Runtime.InteropServices.GCHandle
                                                        .FromIntPtr(vertexDecl.elements).Target;
                    for (int j = 0; j < vertexDecl.elementCount; j++)
                    {
                        if (elements[j].UsageIndex != 0)
                            throw new ArgumentException("UsageIndex != 0");
                        var fmt = elements[j].VertexElementFormat;

                        int size = VertexElementToBindingSize[(int) fmt];
                        int type = VertexElementToBindingType[(int) fmt];
                        bool norm = (    elements[j].VertexElementUsage ==
                                                            VertexElementUsage.Color
                                      || fmt == VertexElementFormat.NormalizedShort2
                                      || fmt == VertexElementFormat.NormalizedShort4);

                        int stride = vertexDecl.vertexStride;
                        int offset = (binding.vertexOffset + baseVertex) * stride
                                   + elements[j].Offset;

                        GLES20.glVertexAttribPointer(nextAttribIndex, size, type, norm,
                                                     stride, offset);

                        GLES20.glEnableVertexAttribArray(nextAttribIndex++);
                    }
                }
            });
        }

        //
        // FNA3D_VertexBufferBinding
        //

        public struct FNA3D_VertexBufferBinding
        {
            public IntPtr vertexBuffer;
            public FNA3D_VertexDeclaration vertexDeclaration;
            public int vertexOffset;
            public int instanceFrequency;
        }

        //
        // FNA3D_VertexDeclaration
        //

        public struct FNA3D_VertexDeclaration
        {
            public int vertexStride;
            public int elementCount;
            public IntPtr elements;
        }

        //
        // VertexElementToBindingSize
        //

        static int[] VertexElementToBindingSize = new int[]
        {
            1,      // VertexElementFormat.Single
            2,      // VertexElementFormat.Vector2
            3,      // VertexElementFormat.Vector3
            4,      // VertexElementFormat.Vector4
            4,      // VertexElementFormat.Color
            4,      // VertexElementFormat.Byte4
            2,      // VertexElementFormat.Short2
            4,      // VertexElementFormat.Short4
            2,      // VertexElementFormat.NormalizedShort2
            4,      // VertexElementFormat.NormalizedShort4
            2,      // VertexElementFormat.HalfVector2
            4       // VertexElementFormat.HalfVector4
        };

        //
        // VertexElementToBindingType
        //

        static int[] VertexElementToBindingType = new int[]
        {
            GLES20.GL_FLOAT,            // VertexElementFormat.Single
            GLES20.GL_FLOAT,            // VertexElementFormat.Vector2
            GLES20.GL_FLOAT,            // VertexElementFormat.Vector3
            GLES20.GL_FLOAT,            // VertexElementFormat.Vector4
            GLES20.GL_UNSIGNED_BYTE,    // VertexElementFormat.Color
            GLES20.GL_UNSIGNED_BYTE,    // VertexElementFormat.Byte4
            GLES20.GL_SHORT,            // VertexElementFormat.Short2
            GLES20.GL_SHORT,            // VertexElementFormat.Short4
            GLES20.GL_SHORT,            // VertexElementFormat.NormalizedShort2
            GLES20.GL_SHORT,            // VertexElementFormat.NormalizedShort4
            GLES30.GL_HALF_FLOAT,       // VertexElementFormat.HalfVector2
            GLES30.GL_HALF_FLOAT        // VertexElementFormat.HalfVector4
        };



        //
        // BufferSerializer
        //

        private static class BufferSerializer
        {

            public static java.nio.Buffer Convert(IntPtr data, int length,
                                                  State state, int bufferId)
            {
                java.nio.Buffer oldBuffer, newBuffer;
                lock (state.BufferCache)
                {
                    state.BufferCache.TryGetValue(bufferId, out oldBuffer);
                }

                // FNA IndexBuffer uses GCHandle::Alloc and GCHandle::AddrOfPinnedObject.
                // we use GCHandle::FromIntPtr to convert that address to an object reference.
                // see also:  system.runtime.interopservices.GCHandle struct in baselib.
                int offset = (int) data;
                newBuffer = Convert(GCHandle.FromIntPtr(data - offset).Target,
                                    offset, length, oldBuffer);

                if (newBuffer != oldBuffer)
                {
                    lock (state.BufferCache)
                    {
                        state.BufferCache[bufferId] = newBuffer;
                    }
                }

                return newBuffer;
            }


            public static java.nio.Buffer Convert(object data, int offset, int length,
                                                  java.nio.Buffer buffer)
            {
                if (data is short[])
                {
                    return FromShort((short[]) data, offset, length);
                }

                var byteBuffer = (buffer != null && buffer.limit() >= length)
                               ? (java.nio.ByteBuffer) buffer
                               : java.nio.ByteBuffer.allocateDirect(length)
                                        .order(java.nio.ByteOrder.nativeOrder());

                if (data is SpriteBatch.VertexPositionColorTexture4[])
                {
                    FromVertexPositionColorTexture4(
                        (SpriteBatch.VertexPositionColorTexture4[]) data,
                        offset, length, byteBuffer);
                }

                else if (data is VertexPositionColor[])
                {
                    FromVertexPositionColor(
                        (VertexPositionColor[]) data, offset, length, byteBuffer);
                }

                else if (data is VertexPositionColorTexture[])
                {
                    FromVertexPositionColorTexture(
                        (VertexPositionColorTexture[]) data, offset, length, byteBuffer);
                }

                else if (data is VertexPositionNormalTexture[])
                {
                    FromVertexPositionNormalTexture(
                        (VertexPositionNormalTexture[]) data, offset, length, byteBuffer);
                }

                else if (data is VertexPositionTexture[])
                {
                    FromVertexPositionTexture(
                        (VertexPositionTexture[]) data, offset, length, byteBuffer);
                }

                else
                {
                    /*IVertexType iVertexType,
                        => FromVertexDeclaration(iVertexType.VertexDeclaration,
                                                 data, offset, length),*/

                    throw new ArgumentException($"unsupported buffer type '{data.GetType()}'");
                };

                return byteBuffer.position(0);
            }

            private static void ValidateOffsetAndLength(int offset, int length, int divisor)
            {
                if ((offset % divisor) != 0 || (length % divisor) != 0)
                    throw new ArgumentException(
                            $"length and offset of buffer should be divisible by {divisor}");
            }

            private static java.nio.Buffer FromShort(short[] array, int offset, int length)
            {
                ValidateOffsetAndLength(offset, length, 2);
                return java.nio.ShortBuffer.wrap(array,
                                                 offset / sizeof(short),
                                                 length / sizeof(short));
            }

            private static void FromVertexPositionColorTexture4(
                    SpriteBatch.VertexPositionColorTexture4[] array,
                    int offset, int length, java.nio.ByteBuffer buffer)
            {
                ValidateOffsetAndLength(offset, length, 96);

                int index = offset / 96;
                int count = length / 96;
                for (; count-- > 0; index++)
                {
                             PutVector3(buffer, ref array[index].Position0);
                                  PutColor(buffer, ref array[index].Color0);
                    PutVector2(buffer, ref array[index].TextureCoordinate0);

                             PutVector3(buffer, ref array[index].Position1);
                                  PutColor(buffer, ref array[index].Color1);
                    PutVector2(buffer, ref array[index].TextureCoordinate1);

                             PutVector3(buffer, ref array[index].Position2);
                                  PutColor(buffer, ref array[index].Color2);
                    PutVector2(buffer, ref array[index].TextureCoordinate2);

                             PutVector3(buffer, ref array[index].Position3);
                                  PutColor(buffer, ref array[index].Color3);
                    PutVector2(buffer, ref array[index].TextureCoordinate3);
                }
            }

            private static void FromVertexPositionColor(
                    VertexPositionColor[] array,
                    int offset, int length, java.nio.ByteBuffer buffer)
            {
                ValidateOffsetAndLength(offset, length, 16);

                int index = offset / 16;
                int count = length / 16;
                for (; count-- > 0; index++)
                {
                    PutVector3(buffer, ref array[index].Position);
                      PutColor(buffer, ref array[index].Color);
                }
            }

            private static void FromVertexPositionColorTexture(
                    VertexPositionColorTexture[] array,
                    int offset, int length, java.nio.ByteBuffer buffer)
            {
                ValidateOffsetAndLength(offset, length, 24);

                int index = offset / 24;
                int count = length / 24;
                for (; count-- > 0; index++)
                {
                    PutVector3(buffer, ref array[index].Position);
                      PutColor(buffer, ref array[index].Color);
                    PutVector2(buffer, ref array[index].TextureCoordinate);
                }
            }

            private static void FromVertexPositionNormalTexture(
                    VertexPositionNormalTexture[] array,
                    int offset, int length, java.nio.ByteBuffer buffer)
            {
                ValidateOffsetAndLength(offset, length, 32);

                int index = offset / 32;
                int count = length / 32;
                for (; count-- > 0; index++)
                {
                    PutVector3(buffer, ref array[index].Position);
                    PutVector3(buffer, ref array[index].Normal);
                    PutVector2(buffer, ref array[index].TextureCoordinate);
                }
            }

            private static void FromVertexPositionTexture(
                    VertexPositionTexture[] array,
                    int offset, int length, java.nio.ByteBuffer buffer)
            {
                ValidateOffsetAndLength(offset, length, 20);

                int index = offset / 20;
                int count = length / 20;
                for (; count-- > 0; index++)
                {
                    PutVector3(buffer, ref array[index].Position);
                    PutVector2(buffer, ref array[index].TextureCoordinate);
                }
            }

            /*private static java.nio.Buffer FromVertexDeclaration(
                    VertexDeclaration vertexDeclaration,
                    object data, int offset, int length) { } */

            private static void PutVector3(java.nio.ByteBuffer buffer, ref Vector3 vector3)
            {
                buffer.putFloat(vector3.X);
                buffer.putFloat(vector3.Y);
                buffer.putFloat(vector3.Z);
            }

            private static void PutVector2(java.nio.ByteBuffer buffer, ref Vector2 vector2)
            {
                buffer.putFloat(vector2.X);
                buffer.putFloat(vector2.Y);
            }

            private static void PutColor(java.nio.ByteBuffer buffer, ref Color color)
            {
                buffer.put((sbyte) color.R);
                buffer.put((sbyte) color.G);
                buffer.put((sbyte) color.B);
                buffer.put((sbyte) color.A);
            }

        }



        //
        // State
        //

        private partial class State
        {
            public Dictionary<int, int[]> BufferSizeUsage = new Dictionary<int, int[]>();
            public Dictionary<int, java.nio.Buffer> BufferCache = new Dictionary<int, java.nio.Buffer>();
        }

    }

}
