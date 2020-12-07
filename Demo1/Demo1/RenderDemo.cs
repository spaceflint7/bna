
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Demo1
{
    public class RenderDemo : DrawableGameComponent
    {

        private bool renderToTexture;
        private int renderTargetWidth, renderTargetHeight;
        private RenderTarget2D renderTarget;
        private Rectangle clickRectangle;

        public RenderDemo(Game game) : base(game)
        {
        }


        public override void Initialize()
        {
            renderToTexture = Storage.GetInt("RenderDemo_RenderToTexture", 0) != 0;
            base.Initialize();
        }


        public override void Draw(GameTime gameTime)
        {
            // multiple draw calls, particular for drawing fonts, are slow.
            // this example shows rendering a lot of text to one texture,
            // and drawing just a single texture.

            if (renderToTexture && renderTargetWidth == Config.ClientWidth
                                && renderTargetHeight == Config.ClientHeight)
            {
                ((Game1)Game).DrawSprite(renderTarget,
                                new Rectangle(0, 0, renderTargetWidth, renderTargetHeight));
            }
            else
            {
                if (renderToTexture)
                {
                    ((Game1) Game).DrawFlushBatch();
                    renderTargetWidth = Config.ClientWidth;
                    renderTargetHeight = Config.ClientHeight;
                    if (renderTarget != null)
                        renderTarget.Dispose();
                    renderTarget = new RenderTarget2D(GraphicsDevice,
                                                      renderTargetWidth, renderTargetHeight,
                                                      true, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
                    GraphicsDevice.SetRenderTarget(renderTarget);
                    GraphicsDevice.Clear(Color.Transparent);
                }

                ReallyDraw();

                if (renderToTexture)
                {
                    ((Game1)Game).DrawFlushBatch();
                    GraphicsDevice.SetRenderTarget(null);
                    ((Game1)Game).DrawSprite(renderTarget,
                                    new Rectangle(0, 0, renderTargetWidth, renderTargetHeight));
                }
            }

            if (Touch.Clicked(clickRectangle))
            {
                renderToTexture = ! renderToTexture;
                renderTargetWidth = -1;
                renderTargetHeight = -1;
                Storage.Set("RenderDemo_RenderToTexture", renderToTexture ? 1 : 0);
            }
        }


        private void ReallyDraw()
        {
            float widthInInches = Config.ClientWidth / (float)Config.PixelsPerInch;
            float heightInInches = Config.ClientHeight / (float)Config.PixelsPerInch - 1f;

            for (int y = 0; y < 20; y++)
            {
                for (int x = 0; x < 20; x++)
                {
                    string s = char.ConvertFromUtf32((int)'A' + (x + y) % 26);
                    float sx = 0.05f + widthInInches * 0.05f * x;
                    float sy = 0.7f + heightInInches * 0.05f * y;
                    ((Game1)Game).DrawText(s, sx, sy, 0.1f, 0.1f);
                }
            }

            string what = renderToTexture ? "DISABLE" : "ENABLE";
            clickRectangle = ((Game1)Game).DrawText(
                $" TAP TO {what} RENDER TEXTURE ", 0f, 0.35f, widthInInches, 0.2f);

        }

    }

}
