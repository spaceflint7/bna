namespace Demo1FSharp

open System
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

type StencilDemo (game : Game) =
    inherit DrawableGameComponent(game)

    let mutable spriteBatch = null
    let mutable texture = null

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
        spriteBatch <- new SpriteBatch (game.GraphicsDevice)
        texture <- game.Content.Load("fsharp256")
        rectFunc <- Some logoRect
        colorFunc <- Some logoColor

    override Game.Draw gameTime =
        spriteBatch.Begin ()
        spriteBatch.Draw (texture, rectFunc.Value game, colorFunc.Value gameTime)
        spriteBatch.End ()