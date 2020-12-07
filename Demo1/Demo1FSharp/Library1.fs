namespace Demo1FSharp

open System
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

type StencilDemo (game : Game) =
    inherit DrawableGameComponent(game)

    let mutable spriteBatch = new SpriteBatch (game.GraphicsDevice)
    let mutable logoTexture = game.Content.Load("fsharp256")
    let mutable backTexture = new Texture2D(game.GraphicsDevice, 16, 16)
    let mutable counter = 0

    // using option here solely to force a dependency on FSharp.Core.dll
    let mutable rectFunc : (Game -> Rectangle) option = None
    let mutable colorFunc : (GameTime -> Color) option = None

    let logoRect (game : Game) = 
        let (sw, sh) = (game.Window.ClientBounds.Width, game.Window.ClientBounds.Height)
        let (iw, ih) = ((int) ((single) sw * 0.75f), (int) ((single) sh * 0.75f))
        let (ix, iy) = ((sw - iw) / 2, (sh - ih) / 2)
        Rectangle(ix, iy, iw, ih)

    let logoColor (gameTime : GameTime) = 
        Color(0.f, 
              (float32) (Math.Sin(gameTime.TotalGameTime.TotalMilliseconds * 0.001)), 
              (float32) (Math.Cos(gameTime.TotalGameTime.TotalMilliseconds * 0.001)))

    override Game.Initialize() =
        rectFunc <- Some logoRect
        colorFunc <- Some logoColor

    override Game.Draw gameTime =
        
        let backArray = Array.zeroCreate (16 * 16)
        backTexture.GetData backArray

        if counter = 0
        then 
            backArray.[12 * 16] <- 0xFF0000FF
            counter <- 1
        else
            for y = 4 to 11 do
                for x = 0 to 15 do
                    let idx = y * 16 + x
                    backArray.[idx] <- backArray.[idx + 1]
                    backArray.[idx + 1] <- 0
            counter <- match counter with
                            | 60 -> 0
                            | n -> n + 1
        backTexture.SetData backArray

        spriteBatch.Begin (SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, new RasterizerState())
        spriteBatch.Draw (backTexture, 
                          Rectangle(0, 0, game.Window.ClientBounds.Width, game.Window.ClientBounds.Height), 
                          Nullable (Rectangle(0, 0, 16, 16)), 
                          Color.White)
        spriteBatch.End ()

        spriteBatch.Begin ()
        spriteBatch.Draw (logoTexture, rectFunc.Value game, colorFunc.Value gameTime)
        spriteBatch.End ()