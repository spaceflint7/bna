
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Demo1
{

    public class Font
    {

        private SpriteBatch spriteBatch;
        private SpriteFont spriteFont;


        public Font(Game game, SpriteBatch spriteBatch, string fontName)
        {
            spriteFont = game.Content.Load<SpriteFont>(fontName);
            this.spriteBatch = spriteBatch;
        }


        public Rectangle Measure(Vector4 pos, Vector4 size, string text)
        {
            var mm = spriteFont.MeasureString(text);

            var wh = new Vector2(size.X, size.Y) * Config.PixelsPerInch;
            var xy = new Vector2(pos.X, pos.Y) * Config.PixelsPerInch;
            xy.X += pos.Z * Config.ClientWidth  - size.Z * wh.X;
            xy.Y += pos.W * Config.ClientHeight - size.W * wh.Y;

            return new Rectangle((int) xy.X, (int) xy.Y, (int) wh.X, (int) wh.Y);
        }



        public Rectangle Measure(Vector2 pos, Vector2 size, string text)
        {
            var pos4  = new Vector4(pos.X, pos.Y,
                                    pos.X >= 0f ? 0f : 1f,
                                    pos.Y >= 0f ? 0f : 1f);
            var size4 = new Vector4(size.X, size.Y, 0f, 0f);
            return Measure(pos4, size4, text);
        }


        public void Draw(Rectangle rect, Color color, string text)
        {
            var mm = spriteFont.MeasureString(text);

            var xy = new Vector2(rect.Left, rect.Top);
            var wh = new Vector2(rect.Width, rect.Height);
            var scl = wh / mm;

            spriteBatch.DrawString(spriteFont, text, xy, color, 0f, Vector2.Zero, scl,
                                   SpriteEffects.None, 0f);
        }


        public void Draw(Vector4 pos, Vector4 size, Color color, string text)
        {
            var mm = spriteFont.MeasureString(text);

            var wh = new Vector2(size.X, size.Y) * Config.PixelsPerInch;
            var xy = new Vector2(pos.X, pos.Y) * Config.PixelsPerInch;
            xy.X += pos.Z * Config.ClientWidth- size.Z * wh.X;
            xy.Y += pos.W * Config.ClientHeight - size.W * wh.Y;

            var scl = wh / mm;

            spriteBatch.DrawString(spriteFont, text, xy, color, 0f, Vector2.Zero, scl,
                                   SpriteEffects.None, 0f);
        }

        public void Draw(Vector2 pos, Vector2 size, Color color, string text)
        {
            var pos4  = new Vector4(pos.X, pos.Y,
                                    pos.X >= 0f ? 0f : 1f,
                                    pos.Y >= 0f ? 0f : 1f);
            var size4 = new Vector4(size.X, size.Y, 0f, 0f);
            Draw(pos4, size4, color, text);
        }
    }

}
