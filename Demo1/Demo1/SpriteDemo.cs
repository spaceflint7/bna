
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Demo1
{
    public class SpriteDemo : DrawableGameComponent
    {

        private Texture2D ball;
        private SpriteBatch spriteBatch;
        private int x, y;
        private int dx, dy;


        public SpriteDemo(Game game) : base(game)
        {
            spriteBatch = new SpriteBatch(game.GraphicsDevice);
        }


        public override void Initialize()
        {
            ball = Game.Content.Load<Texture2D>("circle");

            x  = Storage.GetInt("SpriteDemo_X",  Config.ClientWidth / 2);
            y  = Storage.GetInt("SpriteDemo_Y",  Config.ClientHeight / 2);
            dx = Storage.GetInt("SpriteDemo_DX", 1);
            dy = Storage.GetInt("SpriteDemo_DY", 1);
        }


        public override void Update(GameTime gameTime)
        {
            if (x < 0 || x + Config.PixelsPerInch > Config.ClientWidth)
                dx = -dx;
            if (y < 0 || y + Config.PixelsPerInch > Config.ClientHeight)
                dy = -dy;
            x += dx * 2;
            y += dy * 2;

            Storage.Set("SpriteDemo_X",  x);
            Storage.Set("SpriteDemo_Y",  y);
            Storage.Set("SpriteDemo_DX", dx);
            Storage.Set("SpriteDemo_DY", dy);
        }


        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            spriteBatch.Draw(ball, new Rectangle(x, y, Config.PixelsPerInch, Config.PixelsPerInch), Color.Red);
            spriteBatch.End();
        }

    }

}
