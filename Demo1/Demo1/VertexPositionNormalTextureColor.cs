
using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Demo1
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexPositionNormalTextureColor : IVertexType
    {
        VertexDeclaration IVertexType.VertexDeclaration
        {
            get
            {
                return VertexDeclaration;
            }
        }

        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TextureCoordinate;
        public Color Color;

        public static readonly VertexDeclaration VertexDeclaration;

        static VertexPositionNormalTextureColor()
        {
            VertexDeclaration = new VertexDeclaration(
                new VertexElement[]
                {
                    new VertexElement(
                        0,
                        VertexElementFormat.Vector3,
                        VertexElementUsage.Position,
                        0
                    ),
                    new VertexElement(
                        12,
                        VertexElementFormat.Vector3,
                        VertexElementUsage.Normal,
                        0
                    ),
                    new VertexElement(
                        24,
                        VertexElementFormat.Vector2,
                        VertexElementUsage.TextureCoordinate,
                        0
                    ),
                    new VertexElement(
                        32,
                        VertexElementFormat.Color,
                        VertexElementUsage.Color,
                        0
                    ),
                }
            );
        }

        public VertexPositionNormalTextureColor(
            Vector3 position,
            Vector3 normal,
            Vector2 textureCoordinate,
            Color color)
        {
            Position = position;
            Normal = normal;
            TextureCoordinate = textureCoordinate;
            Color = color;
        }

        public override int GetHashCode()
        {
            // TODO: Fix GetHashCode
            return 0;
        }

        public override string ToString()
        {
            return (
                "{{Position:" + Position.ToString() +
                " Normal:" + Normal.ToString() +
                " TextureCoordinate:" + TextureCoordinate.ToString() +
                " Color:" + Color.ToString() +
                "}}"
            );
        }

        public static bool operator ==(VertexPositionNormalTextureColor left, VertexPositionNormalTextureColor right)
        {
            return (    (left.Position == right.Position) &&
                    (left.Normal == right.Normal) &&
                    (left.TextureCoordinate == right.TextureCoordinate) &&
                    (left.Color == right.Color) 
                    );
        }

        public static bool operator !=(VertexPositionNormalTextureColor left, VertexPositionNormalTextureColor right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj.GetType() != base.GetType())
            {
                return false;
            }
            return (this == ((VertexPositionNormalTextureColor) obj));
        }

    }
}
