

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

namespace Demo1
{

    public class CubeDemo : DrawableGameComponent
    {

#if CUSTOM_VERTEX_BUFFER
        // custom vertex buffers are not supported at this time
        private VertexPositionNormalTextureColor[] cube;
#else
        private VertexPositionNormalTexture[] cube;
#endif
        private BasicEffect theEffect;
        private float angle;
        private int shaderCycler;


        public CubeDemo(Game game) : base(game)
        {
            var face = new Vector3[]
            {
                //TopLeft-BottomLeft-TopRight
                new Vector3(-1f,  1f, 0f), new Vector3(-1f, -1f, 0f), new Vector3( 1f,  1f, 0f),
                //BottomLeft-BottomRight-TopRight
                new Vector3(-1f, -1f, 0f), new Vector3( 1f, -1f, 0f), new Vector3( 1f,  1f, 0f),
            };

            var faceNormals = new Vector3[]
            {
                Vector3.UnitZ, -Vector3.UnitZ,   //Front & Back faces
                Vector3.UnitX, -Vector3.UnitX,   //Left & Right faces
                Vector3.UnitY, -Vector3.UnitY,   //Top & Bottom faces
            };

            var ang90 = (float) Math.PI / 2f;
            var faceRotations = new Matrix[]
            {
                Matrix.CreateRotationY(2f * ang90),
                Matrix.CreateRotationY(0f),
                Matrix.CreateRotationY(-ang90),
                Matrix.CreateRotationY(ang90),
                Matrix.CreateRotationX(ang90),
                Matrix.CreateRotationX(-ang90)
            };

#if CUSTOM_VERTEX_BUFFER
            cube = new VertexPositionNormalTextureColor[36];
            for (int x = 0; x < cube.Length; x++)
            {
                var i = x % 6;
                var j = x / 6;
                cube[x] = new VertexPositionNormalTextureColor(
                    Vector3.Transform(face[i], faceRotations[j]) + faceNormals[j],
                    faceNormals[j], Vector2.Zero, Color.Red);
            }
#else
            var uvCoords = new Vector2[] {
                Vector2.Zero, Vector2.UnitY, Vector2.UnitX, Vector2.UnitY, Vector2.One, Vector2.UnitX };
            cube = new VertexPositionNormalTexture[36];
            for (int x = 0; x < cube.Length; x++)
            {
                var i = x % 6;
                var j = x / 6;
                cube[x] = new VertexPositionNormalTexture(
                    Vector3.Transform(face[i], faceRotations[j]) + faceNormals[j],
                    faceNormals[j], uvCoords[i] * 2f);
            }
#endif
        }


        public override void Initialize()
        {
            theEffect = new BasicEffect(Game.GraphicsDevice)
            {
#if CUSTOM_VERTEX_BUFFER
                VertexColorEnabled = true,
#endif
                AmbientLightColor = new Vector3(0f, 0.2f, 0f),
                LightingEnabled = true,
                TextureEnabled = true,
                PreferPerPixelLighting = true,
                FogStart = -10f, FogEnd = 20f,
                View = Matrix.CreateTranslation(0f, 0f, -10f),
            };

            theEffect.Texture = Game.Content.Load<Texture2D>("4x4");

            theEffect.DirectionalLight0.Enabled = true;
            theEffect.DirectionalLight0.DiffuseColor = new Vector3(1f, 1f, 0f);
            theEffect.DirectionalLight0.Direction = Vector3.Down;

            theEffect.DirectionalLight1.Enabled = true;
            theEffect.DirectionalLight1.DiffuseColor = new Vector3(0f, 1f, 0f);
            theEffect.DirectionalLight1.SpecularColor = new Vector3(0f, 0f, 1f);
            theEffect.DirectionalLight1.Direction = Vector3.Right;

            theEffect.DirectionalLight2.Enabled = true;
            theEffect.DirectionalLight2.DiffuseColor = new Vector3(1f, 0f, 0f);
            theEffect.DirectionalLight2.SpecularColor = new Vector3(0f, 0f, 1f);
            theEffect.DirectionalLight2.Direction = Vector3.Left;

            shaderCycler = Storage.GetInt("CubeDemo_ShaderCycler", 1);
            UpdateEffect();

            angle = Storage.GetFloat("CubeDemo_Angle", 0f);

            base.Initialize();
        }



        public override void Update(GameTime gameTime)
        {
            angle += 0.005f;
            if (angle > 2f * (float) Math.PI)
                angle = 0f;
            var R = Matrix.CreateRotationY(angle) * Matrix.CreateRotationX(0.4f);
            var T = Matrix.CreateTranslation(0f, 0f, 5f);
            theEffect.World = R * T;

            if (Touch.LastGesture.GestureType == Microsoft.Xna.Framework.Input.Touch.GestureType.FreeDrag)
            {
                angle += Touch.LastGesture.Delta.X * 0.01f;
                Touch.LastGesture = default(GestureSample);
            }

            Storage.Set("CubeDemo_Angle", angle);
            base.Update(gameTime);
        }


        public override void Draw(GameTime gameTime)
        {
            theEffect.Projection = Matrix.CreatePerspectiveFieldOfView(
                            (float)Math.PI / 4.0f,
                            (float)Config.ClientWidth / (float)Config.ClientHeight,
                            1f, 10f);

            GraphicsDevice.SamplerStates[0] = ((shaderCycler & 16) == 0) ? SamplerState.PointWrap
                                                                         : SamplerState.LinearWrap;

            Game.GraphicsDevice.RasterizerState = new RasterizerState();
            foreach (var pass in theEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Game.GraphicsDevice.DrawUserPrimitives(
                        PrimitiveType.TriangleList, cube, 0, 12);
            }

            var shaderIndex = theEffect.Parameters["ShaderIndex"].GetValueInt32();
            var rect = ((Game1) Game).DrawText(
                    $"SHADER INDEX {shaderIndex}\nTAP HERE TO CYCLE",      0.2f, 0.4f,     1.5f, 0.7f);

            if (Touch.Clicked(rect))
            {
                Storage.Set("CubeDemo_ShaderCycler", ++shaderCycler);
                UpdateEffect();
            }

            base.Draw(gameTime);
        }


        void UpdateEffect()
        {
            theEffect.FogEnabled = ((shaderCycler & 1) != 0) ? false : true;
            theEffect.TextureEnabled = ((shaderCycler & 2) != 0) ? false : true;
            theEffect.PreferPerPixelLighting = ((shaderCycler & 4) != 0) ? false : true;
            theEffect.DirectionalLight1.Enabled = ((shaderCycler & 8) != 0) ? false : true;
            theEffect.DirectionalLight2.Enabled = ((shaderCycler & 8) != 0) ? false : true;
            theEffect.LightingEnabled = ((shaderCycler & 16) != 0) ? false : true;
        }

    }

}
