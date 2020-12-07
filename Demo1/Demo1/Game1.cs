
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace Demo1
{

    public class Game1 : Microsoft.Xna.Framework.Game
    {

        private DrawableGameComponent pageComponent;
        public Texture2D white;
        private SpriteBatch spriteBatch;
        private Font myFont;
        private SoundEffectInstance effect;
        private bool playEffect;
        private int pageNumber, pageNumberOld;
        private bool paused;
        private bool anyDrawText;

        private float framesPerSecond = 60f;
        private float countSeconds;
        private int countFrames;


        public Game1()
        {
            Content.RootDirectory = "Content";

            // set up callbacks for window creation and resizing
            Config.InitGraphics(this);
        }


        protected override void Initialize()
        {
            base.Initialize();
            Config.InitWindow(Window);
            Storage.Init();
            IsMouseVisible = true;
            pageNumber = Storage.GetInt("Game_PageNumber", 1);
            Components.Add(new Touch(this));
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            myFont = new Font(this, spriteBatch, "MyFont");
            white = Content.Load<Texture2D>("white");

            // Android does not have universal support for DXT compression,
            // and DXT compression generally creates larger files than PNG.
            // thus it may be preferrable to avoid DXT compression altogether
            // and load the unprocessed PNG as below:
            //
            // var stream = TitleContainer.OpenStream(
            //                  Content.RootDirectory + "/image.png");
            // Texture2D.FromStream(GraphicsDevice, stream);
            //
            // to disable processing, open Properties on the image in the
            // Content project, and change the following in the Advanced tab:
            // "Build Action: None" and "Copy to Output Directory: Copy if newer".

            effect = Content.Load<SoundEffect>("effect").CreateInstance();
            effect.Pitch = 0.2f;

            // the XNA content processor for Song converts music files to WMA
            // format, which Android does not support playing.  use MP3 instead.
            //
            // to disable processing, open Properties on the image in the
            // Content project, and change the following in the Advanced tab:
            // "Build Action: None" and "Copy to Output Directory: Copy if newer".
            //
            // note that XNA on Windows may not play MP3 files which contain ID3 
            // tags.  use one of the many free utilities to remove such tags.
            //
            // unsupported BNA MediaPlayer:  queueing more than one song at a time;
            // the ActiveSongChanged event; visualization data.

            Microsoft.Xna.Framework.Media.MediaPlayer.Volume = 0.05f;
            Microsoft.Xna.Framework.Media.MediaPlayer.Play(
                Microsoft.Xna.Framework.Media.Song.FromUri("Song1",
                new System.Uri(Content.RootDirectory + "/music.mp3", UriKind.Relative)));
            Microsoft.Xna.Framework.Media.MediaPlayer.IsRepeating = true;
            // song duration is populated during the call to Play()
            // Console.WriteLine(Microsoft.Xna.Framework.Media.MediaPlayer.Queue.ActiveSong.Duration);

        }


        protected override void UnloadContent()
        {
        }


        protected override void Update(GameTime gameTime)
        {
            // when resuming the Android activity after it was paused, Update()
            // may be invoked multiple times to "catch up" within a time span
            // of up to half a second, and then Draw() will be called, and then
            // normal processing is resumed.
            // if this "catch up" is not desireable, a simple workaround is to
            // set a "paused" flag in OnDeactivated(), and clear it in Draw().

            if (paused)
                return;

            if (playEffect)
            {
                effect.Play();
                playEffect = false;
            }

            // basic scene management for the purpose of this demo:
            // after either arrow at the top of the screen is clicked, and
            // the current page number has changed, create the new 'page'.

                if (pageNumber != pageNumberOld)
            {
                if (pageComponent != null)
                {
                    if (effect.State == SoundState.Playing)
                        effect.Stop();
                    effect.Pan = (pageNumber > pageNumberOld) ? 1f : -1f;
                    playEffect = true;
                }

                const int LAST_PAGE = 4;
                if (pageNumber <= 0)
                    pageNumber = LAST_PAGE;
                else if (pageNumber > LAST_PAGE)
                    pageNumber = 1;

                pageNumberOld = pageNumber;
                Storage.Set("Game_PageNumber", pageNumber);

                if (pageComponent != null)
                    Components.Remove(pageComponent);

                pageComponent = pageNumber switch
                {
                    1 => new SpriteDemo(this),
                    2 => new CubeDemo(this),
                    3 => new RenderDemo(this),
                    // the F# example is in project Demo1FSharp
                    4 => new Demo1FSharp.StencilDemo(this),
                    _ => throw new InvalidOperationException(),
                };
                Components.Add(pageComponent);
            }

            base.Update(gameTime);
        }


        protected override void Draw(GameTime gameTime)
        {
            // BNA does not clear the screen at the start of the frame.
            // since we are alpha blending, we need to make sure we clear.

            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();

            // display frames per second at the bottom of the screen.

            countSeconds += (float) gameTime.ElapsedGameTime.TotalSeconds;
            if (countSeconds > 1f)
            {
                framesPerSecond = countFrames / countSeconds;
                countSeconds -= 1f;
                countFrames = 0;
            }
            else
                countFrames++;

            var fps = framesPerSecond;
            myFont.Draw(new Vector2(-0.7f, -0.2f), new Vector2(0.6f, 0.2f), Color.Yellow, $"FPS {fps:N2}");

            // display the logo at the top of the screen

            var ttl = $"BNA Demo (pg. {pageNumber})";
            var rect = myFont.Measure(new Vector4(0f, 0f, 0.5f, 0f), new Vector4(1.25f, 0.3f, 0.5f, 0f), ttl);
            spriteBatch.Draw(white, new Rectangle(0, 0, Window.ClientBounds.Width, (int) (rect.Height * 0.9f)), Color.Yellow);
            myFont.Draw(rect, Color.Green, ttl);

            // display the arrows at the top of the screen, and check for
            // for taps.  the Touch class uses the XNA Mouse class, which is
            // simulated from the touch screen on Android.  see Touch class.

            var btn = "<<<";
            rect = myFont.Measure(new Vector2(0f, 0.05f), new Vector2(0.5f, 0.2f), btn);
            myFont.Draw(rect, Color.Black, btn);
            if (Touch.Clicked(rect))
                pageNumber--;

            btn = ">>>";
            rect = myFont.Measure(new Vector2(-0.5f, 0.05f), new Vector2(0.5f, 0.2f), btn);
            myFont.Draw(rect, Color.Black, btn);
            if (Touch.Clicked(rect))
                pageNumber++;

            spriteBatch.End();
            base.Draw(gameTime);

            // reset the "paused" flag.  see comment at top of Update()
            paused = false;
        }


        //
        // utility methods for the 'page' components
        //


        public Rectangle DrawText(string txt, float x, float y, float w, float h)
        {
            if (! anyDrawText)
            {
                spriteBatch.Begin();
                anyDrawText = true;
            }

            var rect = myFont.Measure(new Vector2(x, y), new Vector2(w, h), txt);
            myFont.Draw(rect, Color.Black, txt);
            rect.Offset(3, 3);
            myFont.Draw(rect, Color.White, txt);
            return rect;
        }


        public void DrawSprite(Texture2D sprite, Rectangle rect)
        {
            if (! anyDrawText)
            {
                spriteBatch.Begin();
                anyDrawText = true;
            }

            spriteBatch.Draw(sprite, rect, Color.White);
        }



        public void DrawFlushBatch()
        {
            if (anyDrawText)
            {
                spriteBatch.End();
                anyDrawText = false;
            }
        }



        protected override void EndDraw()
        {
            DrawFlushBatch();
            base.EndDraw();
        }


        protected override void OnActivated(object sender, EventArgs args)
        {
        }


        protected override void OnDeactivated(object sender, EventArgs args)
        {
            // set the "paused" flag.  see comment at top of Update()
            paused = true;

            // it is important to save the state when Android is pausing,
            // and restore the state on start up, because we never know
            // when we might get destroyed, but we know we will always
            // get the OnDeactivated callback before destruction.
            // see also:  Storage::Init()
            Storage.Sync();
        }


        protected override void OnExiting(object sender, EventArgs args)
        {
            // Storage.Clear();
        }

    }
}
