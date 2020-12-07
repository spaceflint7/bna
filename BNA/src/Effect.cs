
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using android.opengl;
#pragma warning disable 0436

namespace Microsoft.Xna.Framework.Graphics
{
    public class Effect : GraphicsResource
    {

        //
        // Effect
        //

        public Effect(GraphicsDevice graphicsDevice, byte[] effectCode)
        {
            GraphicsDevice = graphicsDevice;
            CreateProgram(System.Text.Encoding.ASCII.GetString(effectCode));
            CollectUniforms();
        }


        //
        // CreateProgram
        //

        void CreateProgram(string text)
        {
            var (vertOfs, vertHdr) = HeaderOffset(text, "--- vertex ---", 0);
            var (fragOfs, fragHdr) = HeaderOffset(text, "--- fragment ---", vertOfs);
            var (stopOfs, stopHdr) = HeaderOffset(text, "--- end ---", fragOfs);

            string versionDef   = "#version 300 es\n";
            string precisionDef = "#ifdef GL_FRAGMENT_PRECISION_HIGH  \n"
                                + "precision highp float;             \n"
                                + "#else                              \n"
                                + "precision mediump float;           \n"
                                + "#endif                             \n";

            string vertText = versionDef + text.Substring(vertOfs + vertHdr,
                                                          fragOfs - (vertOfs + vertHdr));
            string fragText = versionDef + precisionDef
                                         + text.Substring(fragOfs + fragHdr,
                                                          stopOfs - (fragOfs + fragHdr));

            var err = CreateProgram2(vertText, fragText);
            if (err != null)
                throw new System.InvalidProgramException("Effect: " + err);

            SetTechnique(text.Substring(0, vertOfs));

            (int, int) HeaderOffset(string text, string header, int index)
            {
                index = text.IndexOf(header, index);
                if (index == -1)
                    throw new System.InvalidProgramException("Effect: " + header);
                return (index, header.Length);
            }
        }

        //
        // CreateProgram2
        //

        public string CreateProgram2(string vertexText, string fragmentText)
        {
            string errText = null;
            Renderer.Get(GraphicsDevice.GLDevice).Send( () =>
            {
                (vertexId, errText) = CompileShader(
                            GLES20.GL_VERTEX_SHADER, "vertex", vertexText);
                if (errText == null)
                {
                    (fragmentId, errText) = CompileShader(
                            GLES20.GL_FRAGMENT_SHADER, "fragment", fragmentText);
                    if (errText == null)
                    {
                        (programId, errText) = LinkProgram(vertexId, fragmentId);
                        if (errText == null)
                        {
                            return; // success
                        }
                    }
                    GLES20.glDeleteShader(fragmentId);
                }
                GLES20.glDeleteShader(vertexId);
            });
            return errText;

            // CompileShader

            (int, string) CompileShader(int kind, string errKind, string text)
            {
                //GameRunner.Log($"SHADER PROGRAM: [[[" + text + "]]]");
                string errText = null;
                int shaderId = GLES20.glCreateShader(kind);
                int errCode = GLES20.glGetError();
                if (shaderId == 0 || errCode != 0)
                    errText = "glCreateShader";
                else
                {
                    GLES20.glShaderSource(shaderId, text);
                    errCode = GLES20.glGetError();
                    if (errCode != 0)
                        errText = "glShaderSource";
                    else
                    {
                        GLES20.glCompileShader(shaderId);
                        errCode = GLES20.glGetError();
                        if (errCode != 0)
                            errText = "glCompileShader";
                        else
                        {
                            var status = new int[1];
                            GLES20.glGetShaderiv(
                                shaderId, GLES20.GL_COMPILE_STATUS, status, 0);
                            errCode = GLES20.glGetError();
                            if (errCode == 0 && status[0] != 0)
                            {
                                return (shaderId, null); // success
                            }
                            errText = "compile error: "
                                    + GLES20.glGetShaderInfoLog(shaderId);
                        }
                        GLES20.glDeleteShader(shaderId);
                    }
                }
                if (errCode != 0)
                    errText = "GL error " + errCode + ": " + errText;
                errText = "in " + errKind + " shader: " + errText;
                return (0, errText);
            }

            // LinkProgram

            (int, string) LinkProgram(int vertexId, int fragmentId)
            {
                string errText = null;
                int programId = GLES20.glCreateProgram();
                int errCode = GLES20.glGetError();
                if (programId == 0 || errCode != 0)
                    errText = "glCreateProgram";
                else
                {
                    GLES20.glAttachShader(programId, vertexId);
                    errCode = GLES20.glGetError();
                    if (errCode != 0)
                        errText = "glAttachShader (vertex)";
                    else
                    {
                        GLES20.glAttachShader(programId, fragmentId);
                        errCode = GLES20.glGetError();
                        if (errCode != 0)
                            errText = "glAttachShader (fragment)";
                        else
                        {
                            GLES20.glLinkProgram(programId);
                            errCode = GLES20.glGetError();
                            if (errCode != 0)
                                errText = "glLinkProgram";
                            else
                            {
                                var status = new int[1];
                                GLES20.glGetProgramiv(
                                    programId, GLES20.GL_LINK_STATUS, status, 0);
                                errCode = GLES20.glGetError();
                                if (errCode == 0 && status[0] != 0)
                                {
                                    return (programId, null); // success
                                }
                                errText = "link error: "
                                        + GLES20.glGetProgramInfoLog(programId);
                            }
                            GLES20.glDetachShader(programId, fragmentId);
                        }
                        GLES20.glDetachShader(programId, vertexId);
                    }
                    GLES20.glDeleteProgram(programId);
                }
                if (errCode != 0)
                    errText = "GL error " + errCode + ": " + errText;
                errText = "in shader program: " + errText;
                return (0, errText);
            }
        }

        //
        // SetTechnique
        //

        void SetTechnique(string text)
        {
            string name;
            string search = "#technique ";
            int idx = text.IndexOf(search);
            if (idx != -1)
                name = text.Substring(idx + search.Length).Trim();
            else
                name = "Default";

            var passes = new List<EffectPass>();
            passes.Add(new EffectPass(name, null, this, IntPtr.Zero, 0));
            var list = new List<EffectTechnique>();
            technique = new EffectTechnique(name, IntPtr.Zero,
                                 new EffectPassCollection(passes), null);
            list.Add(technique);
            Techniques = new EffectTechniqueCollection(list);
        }



        //
        // CollectUniforms
        //

        void CollectUniforms()
        {
            var list = new List<EffectParameter>();
            var uniforms = GetProgramUniforms();
            if (uniforms != null)
            {
                for (int i = 0; i < uniforms.Length; i++)
                {
                    var (name, type, size) = uniforms[i];
                    list.Add(new EffectParameter(name, type, size));
                }
            }
            Parameters = new EffectParameterCollection(list);
        }

        //
        // GetProgramUniforms
        //

        public (string name, int type, int size)[] GetProgramUniforms()
        {
            (string, int, int)[] result = null;
            Renderer.Get(GraphicsDevice.GLDevice).Send( () =>
            {
                var count = new int[1];
                GLES20.glGetProgramiv(programId, GLES20.GL_ACTIVE_UNIFORM_MAX_LENGTH, count, 0);
                byte[] nameBuf = new byte[count[0] + 1];
                GLES20.glGetProgramiv(programId, GLES20.GL_ACTIVE_UNIFORMS, count, 0);
                if (count[0] == 0)
                    return;

                result = new (string name, int type, int size)[count[0]];
                var type = new int[1];
                var size = new int[1];
                for (int i = 0; i < result.Length; i++)
                {
                    GLES20.glGetActiveUniform(programId, i, nameBuf.Length,
                                 /* length, lengthOffset */ count, 0,
                                     /* size, sizeOffset */ size, 0,
                                     /* type, typeOffset */ type, 0,
                                     /* name, nameOffset */ (sbyte[]) (object) nameBuf, 0);
                    var nameStr = System.Text.Encoding.ASCII.GetString(nameBuf, 0, count[0]);
                    result[i] = (nameStr, type[0], size[0]);
                }
            });
            return result;
        }



        //
        // INTERNAL_applyEffect
        //

        public void INTERNAL_applyEffect(uint pass)
        {
            var graphicsDevice = GraphicsDevice;
            Renderer.Get(graphicsDevice.GLDevice).Send( () =>
            {
                GLES20.glUseProgram(programId);
                int n = Parameters.Count;
                for (int i = 0; i < n; i++)
                {
                    if (! Parameters[i].Apply(i, graphicsDevice))
                    {
                        throw new ArgumentException(
                            $"uniform {Parameters[i].Name} (#{i}) in effect {technique.Name}");
                    }
                }
            });
        }



        //
        // Dispose
        //

        protected override void Dispose(bool disposing)
        {
            if (! base.IsDisposed)
            {
                Renderer.Get(GraphicsDevice.GLDevice).Send( () =>
                {
                    GLES20.glDeleteShader(fragmentId);
                    GLES20.glDeleteShader(vertexId);
                    GLES20.glDeleteProgram(programId);
                    fragmentId = 0;
                    vertexId = 0;
                    programId = 0;
                });
            }
        }



        //
        // data
        //

        private int programId;
        private int vertexId, fragmentId;

        public EffectParameterCollection Parameters { get; private set; }
        public EffectTechniqueCollection Techniques { get; private set; }

        private EffectTechnique technique;
        public EffectTechnique CurrentTechnique
        {
            get => technique;
            set => throw new System.PlatformNotSupportedException();
        }

        protected internal virtual void OnApply() { }
    }



    public sealed class EffectParameter
    {
        public unsafe EffectParameter(string name, int type, int size)
        {
            Name = name;
            this.type = type;

            int floatCount = 0;
            int intCount = 0;

            if (type == android.opengl.GLES20.GL_FLOAT)
            {
                floatCount = 1;
                if (size == 1 && name == "MultiplierY")
                    kind = 'Y';
            }

            else if (type == android.opengl.GLES20.GL_FLOAT_VEC2)
                floatCount = 2;
            else if (type == android.opengl.GLES20.GL_FLOAT_VEC3)
                floatCount = 3;
            else if (type == android.opengl.GLES20.GL_FLOAT_VEC4)
                floatCount = 4;

            else if (type == android.opengl.GLES20.GL_FLOAT_MAT4)
                floatCount = 4 * 4;
            else if (type == android.opengl.GLES20.GL_FLOAT_MAT3)
                floatCount = 3 * 3;
            else if (type == android.opengl.GLES20.GL_FLOAT_MAT2)
                floatCount = 2 * 2;
            else if (    type == android.opengl.GLES30.GL_FLOAT_MAT3x4
                      || type == android.opengl.GLES30.GL_FLOAT_MAT4x3)
            {
                floatCount = 4 * 3;
            }

            else if (    type == android.opengl.GLES20.GL_INT
                      || type == android.opengl.GLES20.GL_BOOL)
            {
                intCount = 1;
            }

            else if (    type == android.opengl.GLES20.GL_SAMPLER_2D
                      || type == android.opengl.GLES30.GL_SAMPLER_3D
                      || type == android.opengl.GLES20.GL_SAMPLER_CUBE)
            {
                kind = 'S';
            }

            else
            {
                throw new System.InvalidProgramException(
                    $"Effect: unsupported type {type:X8} in uniform '{name}'");
            }

            if (floatCount != 0)
            {
                var floatArray = new float[floatCount * size];
                fixed (float* floatPointer = &floatArray[0])
                    values = (IntPtr) (void*) floatPointer;
                storage = floatArray;
                storageCopy = java.util.Arrays.copyOf(floatArray, floatArray.Length);
            }

            else if (intCount != 0)
            {
                var intArray = new int[intCount * size];
                fixed (int* intPointer = &intArray[0])
                    values = (IntPtr) (void*) intPointer;
                storage = intArray;
                storageCopy = java.util.Arrays.copyOf(intArray, intArray.Length);
            }

            //GameRunner.Log($"EFFECT PARAMETER {Name} VALUES {values} STORAGE {this.storage}");
        }

        //
        // bool value
        //

        public bool GetValueBoolean() => GetValueBooleanArray(1)[0];

        public bool[] GetValueBooleanArray(int count)
        {
            CheckCount(count);
            var intArray = IntArray(android.opengl.GLES20.GL_BOOL, count);
            var value = new bool[count];
            for (int i = 0; i < count; i++)
                value[i] = intArray[i] != 0;
            return value;
        }

        public void SetValue(bool[] value)
        {
            int count = value.Length;
            var intArray = IntArray(android.opengl.GLES20.GL_BOOL, count);
            for (int i = 0; i < count; i++)
                intArray[i] = value[i] ? 1 : 0;
        }

        public void SetValue(bool value) => SetValue(new bool[] { value });

        //
        // int value
        //

        public int GetValueInt32() => GetValueInt32Array(1)[0];

        public int[] GetValueInt32Array(int count)
        {
            CheckCount(count);
            return java.util.Arrays.copyOf(
                        IntArray(android.opengl.GLES20.GL_INT, count), count);
        }

        public void SetValue(int[] value)
        {
            int count = value.Length;
            var intArray = IntArray(android.opengl.GLES20.GL_INT, count);
            for (int i = 0; i < count; i++)
                intArray[i] = value[i];
        }

        public void SetValue(int value) => SetValue(new int[] { value });

        //
        // float value
        //

        public float GetValueSingle() => GetValueSingleArray(1)[0];

        public float[] GetValueSingleArray(int count)
        {
            CheckCount(count);
            return java.util.Arrays.copyOf(
                        FloatArray(android.opengl.GLES20.GL_FLOAT, count), count);
        }

        public void SetValue(float[] value)
        {
            int count = value.Length;
            var floatArray = FloatArray(android.opengl.GLES20.GL_FLOAT, count);
            for (int i = 0; i < count; i++)
                floatArray[i] = value[i];
        }

        public void SetValue(float value) => SetValue(new float[] { value });

        //
        // Matrix
        //

        public Matrix GetValueMatrix() => GetValueMatrixArray(1)[0];

        public Matrix[] GetValueMatrixArray(int count)
        {
            CheckCount(count);

            int i = 0, j;
            float[] floatArray;
            // allocate an array of values without allocating each element
            var value = (Matrix[]) java.lang.reflect.Array.newInstance(
                            (java.lang.Class) typeof(Matrix), count);
            Matrix matrix;

            if (type == android.opengl.GLES20.GL_FLOAT_MAT4)
            {
                floatArray = FloatArray(type, 4 * 4 * count);
                for (j = 0; j < count; j++)
                {
                    matrix.M11 = floatArray[i++]; // 0
                    matrix.M21 = floatArray[i++];
                    matrix.M31 = floatArray[i++];
                    matrix.M41 = floatArray[i++];
                    matrix.M12 = floatArray[i++]; // 4
                    matrix.M22 = floatArray[i++];
                    matrix.M32 = floatArray[i++];
                    matrix.M42 = floatArray[i++];
                    matrix.M13 = floatArray[i++]; // 8
                    matrix.M23 = floatArray[i++];
                    matrix.M33 = floatArray[i++];
                    matrix.M43 = floatArray[i++];
                    matrix.M14 = floatArray[i++]; // 12
                    matrix.M24 = floatArray[i++];
                    matrix.M34 = floatArray[i++];
                    matrix.M44 = floatArray[i++];
                    java.lang.reflect.Array.set(value, j, matrix); // matrix is cloned
                }
            }

            else if (type == android.opengl.GLES20.GL_FLOAT_MAT3)
            {
                matrix.M41 = matrix.M42 = matrix.M43 =
                matrix.M14 = matrix.M24 = matrix.M34 = matrix.M44 = 0;
                floatArray = FloatArray(type, 3 * 3 * count);
                for (j = 0; j < count; j++)
                {
                    matrix.M11 = floatArray[i++]; // 0
                    matrix.M21 = floatArray[i++];
                    matrix.M31 = floatArray[i++];
                    matrix.M12 = floatArray[i++]; // 3
                    matrix.M22 = floatArray[i++];
                    matrix.M32 = floatArray[i++];
                    matrix.M13 = floatArray[i++]; // 6
                    matrix.M23 = floatArray[i++];
                    matrix.M33 = floatArray[i++];
                    java.lang.reflect.Array.set(value, j, matrix); // matrix is cloned
                }
            }

            else
                throw new InvalidCastException(Name + " MAT TYPE " + type);

            return value;
        }

        public void SetValue(Matrix[] value)
        {
            int count = value.Length;

            int i = 0, j;
            float[] floatArray;
            Matrix matrix;

            if (type == android.opengl.GLES20.GL_FLOAT_MAT4)
            {
                floatArray = FloatArray(type, 4 * 4 * count);
                for (j = 0; j < count; j++)
                {
                    matrix = (Matrix) java.lang.reflect.Array.get(value, j);
                    floatArray[i++] = matrix.M11; // 0
                    floatArray[i++] = matrix.M21;
                    floatArray[i++] = matrix.M31;
                    floatArray[i++] = matrix.M41;
                    floatArray[i++] = matrix.M12; // 4
                    floatArray[i++] = matrix.M22;
                    floatArray[i++] = matrix.M32;
                    floatArray[i++] = matrix.M42;
                    floatArray[i++] = matrix.M13; // 8
                    floatArray[i++] = matrix.M23;
                    floatArray[i++] = matrix.M33;
                    floatArray[i++] = matrix.M43;
                    floatArray[i++] = matrix.M14; // 12
                    floatArray[i++] = matrix.M24;
                    floatArray[i++] = matrix.M34;
                    floatArray[i++] = matrix.M44;
                }
            }

            else if (type == android.opengl.GLES20.GL_FLOAT_MAT3)
            {
                floatArray = FloatArray(type, 3 * 3 * count);
                for (j = 0; j < count; j++)
                {
                    matrix = (Matrix) java.lang.reflect.Array.get(value, j);
                    floatArray[i++] = matrix.M11; // 0
                    floatArray[i++] = matrix.M21;
                    floatArray[i++] = matrix.M31;
                    floatArray[i++] = matrix.M12; // 3
                    floatArray[i++] = matrix.M22;
                    floatArray[i++] = matrix.M32;
                    floatArray[i++] = matrix.M13; // 6
                    floatArray[i++] = matrix.M23;
                    floatArray[i++] = matrix.M33;
                }
            }

            else
                throw new InvalidCastException(Name + " MAT TYPE " + type);
        }

        public void SetValue(Matrix value)
        {
            // allocate an array of values without allocating each element
            var array = (Matrix[]) java.lang.reflect.Array.newInstance(
                            (java.lang.Class) typeof(Matrix), 1);
            java.lang.reflect.Array.set(array, 0, value);
            SetValue(array);
        }

        //
        // Vector3
        //

        public Vector3 GetValueVector3() => GetValueVector3Array(1)[0];

        public Vector3[] GetValueVector3Array(int count)
        {
            CheckCount(count);
            var floatArray = FloatArray(android.opengl.GLES20.GL_FLOAT_VEC3, 3 * count);

            // allocate an array of values without allocating each element
            var value = (Vector3[]) java.lang.reflect.Array.newInstance(
                            (java.lang.Class) typeof(Vector3), count);
            Vector3 vector;

            int i = 0;
            for (int j = 0; j < count; j++)
            {
                vector.X = floatArray[i++];
                vector.Y = floatArray[i++];
                vector.Z = floatArray[i++];
                java.lang.reflect.Array.set(value, j, vector); // vector is cloned
            }
            return value;
        }

        public void SetValue(Vector3[] value)
        {
            int count = value.Length;
            var floatArray = FloatArray(android.opengl.GLES20.GL_FLOAT_VEC3, 3 * count);
            Vector3 vector;
            int i = 0;
            for (int j = 0; j < count; j++)
            {
                vector = (Vector3) java.lang.reflect.Array.get(value, j);
                floatArray[i++] = vector.X;
                floatArray[i++] = vector.Y;
                floatArray[i++] = vector.Z;
            }
        }

        public void SetValue(Vector3 value)
        {
            // allocate an array of values without allocating each element
            var array = (Vector3[]) java.lang.reflect.Array.newInstance(
                            (java.lang.Class) typeof(Vector3), 1);
            java.lang.reflect.Array.set(array, 0, value);
            SetValue(array);
        }

        //
        // Vector4
        //

        public Vector4 GetValueVector4() => GetValueVector4Array(1)[0];

        public Vector4[] GetValueVector4Array(int count)
        {
            CheckCount(count);
            var floatArray = FloatArray(android.opengl.GLES20.GL_FLOAT_VEC4, 4 * count);

            // allocate an array of values without allocating each element
            var value = (Vector4[]) java.lang.reflect.Array.newInstance(
                            (java.lang.Class) typeof(Vector4), count);
            Vector4 vector;

            int i = 0;
            for (int j = 0; j < count; j++)
            {
                vector.X = floatArray[i++];
                vector.Y = floatArray[i++];
                vector.Z = floatArray[i++];
                vector.W = floatArray[i++];
                java.lang.reflect.Array.set(value, j, vector); // vector is cloned
            }
            return value;
        }

        public void SetValue(Vector4[] value)
        {
            int count = value.Length;
            var floatArray = FloatArray(android.opengl.GLES20.GL_FLOAT_VEC4, 4 * count);
            Vector4 vector;
            int i = 0;
            for (int j = 0; j < count; j++)
            {
                vector = (Vector4) java.lang.reflect.Array.get(value, j);
                floatArray[i++] = vector.X;
                floatArray[i++] = vector.Y;
                floatArray[i++] = vector.Z;
                floatArray[i++] = vector.W;
            }
        }

        public void SetValue(Vector4 value)
        {
            // allocate an array of values without allocating each element
            var array = (Vector4[]) java.lang.reflect.Array.newInstance(
                            (java.lang.Class) typeof(Vector4), 1);
            java.lang.reflect.Array.set(array, 0, value);
            SetValue(array);
        }

        //
        // texture
        //

        public Texture2D GetValueTexture2D() => (Texture2D) storage;

        public Texture3D GetValueTexture3D() => (Texture3D) storage;

        public TextureCube GetValueTextureCube() => (TextureCube) storage;

        public void SetValue(Texture value)
        {
            if (    type == android.opengl.GLES20.GL_SAMPLER_2D
                 || type == android.opengl.GLES30.GL_SAMPLER_3D
                 || type == android.opengl.GLES20.GL_SAMPLER_CUBE)
            {
                storage = value;
            }
        }

        //
        // string
        //

        public void SetValue(string value) => throw new NotImplementedException(Name);

        //
        // Array access
        //

        int[] IntArray(int checkType, int checkCount)
        {
            if (type != checkType)
                throw new InvalidCastException();
            var intArray = (int[]) storage;
            if (intArray.Length < checkCount)
                throw new InvalidCastException();
            return intArray;
        }

        float[] FloatArray(int checkType, int checkCount)
        {
            if (type != checkType)
                throw new InvalidCastException(Name);
            var floatArray = (float[]) storage;
            if (floatArray.Length < checkCount)
                throw new InvalidCastException(Name);
            return floatArray;
        }

        void CheckCount(int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(Name);
        }

        //
        // Apply
        //

        public bool Apply(int uni, GraphicsDevice graphicsDevice)
        {
            if (storage is float[] floatArray)
            {
                if (kind == 'Y')
                    return ApplyMultiplierY(floatArray, uni, graphicsDevice);
                else
                    return Apply(uni, floatArray);
            }

            if (storage is int[] intArray)
                return Apply(uni, intArray);

            if (kind == 'S')
            {
                if (storage != null)
                {
                    // note that the uniform sampler (i.e. the texture
                    // unit selector) always remains set to default value
                    graphicsDevice.Textures[0] = (Texture) storage;
                }
                return true;
            }

            return false;
        }

        private bool Apply(int uni, float[] floatArray)
        {
            var floatArrayCopy = (float[]) storageCopy;
            if (! java.util.Arrays.@equals(floatArray, floatArrayCopy))
            {
                int num = floatArray.Length;
                for (int i = 0; i < num; i++)
                    floatArrayCopy[i] = floatArray[i];

                if (type == GLES20.GL_FLOAT)
                    GLES20.glUniform1fv(uni, num, floatArray, 0);
                else if (type == GLES20.GL_FLOAT_VEC2)
                    GLES20.glUniform2fv(uni, num / 2, floatArray, 0);
                else if (type == GLES20.GL_FLOAT_VEC3)
                    GLES20.glUniform3fv(uni, num / 3, floatArray, 0);
                else if (type == GLES20.GL_FLOAT_VEC4)
                    GLES20.glUniform4fv(uni, num / 4, floatArray, 0);
                else if (type == GLES20.GL_FLOAT_MAT4)
                    GLES20.glUniformMatrix4fv(uni, num / 16, true, floatArray, 0);
                else if (type == GLES20.GL_FLOAT_MAT3)
                    GLES20.glUniformMatrix3fv(uni, num / 9, true, floatArray, 0);
                else
                    return false;
            }
            return true;
        }

        private bool Apply(int uni, int[] intArray)
        {
            var intArrayCopy = (int[]) storageCopy;
            if (! java.util.Arrays.@equals(intArray, intArrayCopy))
            {
                int num = intArray.Length;
                for (int i = 0; i < num; i++)
                    intArrayCopy[i] = intArray[i];

                if (    type == android.opengl.GLES20.GL_INT
                     || type == android.opengl.GLES20.GL_BOOL
                     || type == android.opengl.GLES20.GL_SAMPLER_2D)
                {
                    GLES20.glUniform1iv(uni, num, intArray, 0);
                }
                else
                    return false;
            }
            return true;
        }

        private bool ApplyMultiplierY(float[] floatArray, int uni, GraphicsDevice graphicsDevice)
        {
            // when rendering to texture, we have to flip vertically
            var floatValue = FNA3D.IsRenderToTexture(graphicsDevice) ? -1f : 1f;
            if (floatValue != floatArray[0])
            {
                GLES20.glUniform1f(uni, floatValue);
                floatArray[0] = floatValue;
            }
            return true;
        }

        //
        // data
        //

        object storage;
        object storageCopy;
        int type;
        char kind;

        public IntPtr values;
        public string Name { get; private set; }
    }

}
