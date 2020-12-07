
using System;
using System.Diagnostics;
#pragma warning disable 0436

namespace Microsoft.Xna.Framework.Media
{

    public static class MediaPlayer
    {

        [java.attr.RetainType] private static android.media.MediaPlayer player;
        [java.attr.RetainType] private static MediaQueue queue;
        [java.attr.RetainType] private static java.util.concurrent.atomic.AtomicInteger state;
        [java.attr.RetainType] private static float volume;
        [java.attr.RetainType] private static bool muted;
        [java.attr.RetainType] private static bool looping;

        //
        // static constructor
        //

        static MediaPlayer()
        {
            volume = 1f;
            state = new java.util.concurrent.atomic.AtomicInteger(0);
            queue = new MediaQueue();
            var watcher = new Watcher();
            player = new android.media.MediaPlayer();
            player.setOnPreparedListener(watcher);
            player.setOnCompletionListener(watcher);
        }

        //
        // PrepareAndStart
        //

        private static void PrepareAndStart()
        {
            int oldState = state.getAndSet((int) MediaState.Playing);
            player.prepareAsync();
            if (oldState != (int) MediaState.Playing && MediaStateChanged != null)
            {
                MediaStateChanged(null, EventArgs.Empty);
            }
        }

        //
        // Play
        //

        public static void Play(Song song)
        {
            if (ActiveSongChanged != null)
                throw new PlatformNotSupportedException("ActiveSongChanged");

            if (song.isAsset)
            {
                var asset = GameRunner.Singleton.Activity.getAssets().openFd(song.path);
                player.setDataSource(asset.getFileDescriptor(),
                                     asset.getStartOffset(), asset.getLength());
            }
            else
                player.setDataSource(song.path);

            Queue.Clear();
            Queue.Add(song);
            Queue.ActiveSongIndex = 0;

            PrepareAndStart();
        }

        //
        // Play (SongCollection)
        //

        public static void Play(SongCollection songs, int index)
        {
            if (songs.Count == 0)
            {
                Queue.Clear();
                Stop();
            }
            else if (songs.Count == 1)
            {
                Play((Song) (object) songs[0]);
            }
            else
                throw new PlatformNotSupportedException();
        }

        public static void Play(SongCollection songs) => Play(songs, 0);

        //
        // Pause
        //

        public static void Pause()
        {
            if (State == MediaState.Playing)
            {
                if (player.isPlaying())
                    player.pause();
                State = MediaState.Paused;
            }
        }

        //
        // Resume
        //

        public static void Resume()
        {
            if (State == MediaState.Paused)
            {
                player.start();
                State = MediaState.Playing;
            }
        }

        //
        // Stop
        //

        public static void Stop()
        {
            if (State != MediaState.Stopped)
            {
                player.stop();
                State = MediaState.Stopped;
            }
        }

        //
        // MoveNext, MovePrevious
        //

        public static void MoveNext()
        {
            Stop();
            if (looping)
                PrepareAndStart();
        }

        public static void MovePrevious() => MoveNext();

        //
        // IsMuted, Volume
        //

        public static bool IsMuted
        {
            get => muted;
            set
            {
                muted = value;
                Volume = volume;
            }
        }

        public static float Volume
        {
            get => volume;
            set
            {
                if (value < 0f)
                    value = 0f;
                else if (value > 1f)
                    value = 1f;
                volume = value;

                if (muted)
                    value = 0f;
                player.setVolume(value, value);
            }
        }

        public static bool GameHasControl => true;
        public static bool IsShuffled { get; set; }

        //
        // IsRepeating
        //

        public static bool IsRepeating
        {
            get => looping;
            set
            {
                if (value != looping)
                {
                    looping = value;
                    player.setLooping(value);
                }
            }
        }

        //
        // State
        //

        public static MediaState State
        {
            get => (MediaState) state.get();
            set
            {
                if (    state.getAndSet((int) value) != (int) value
                     && MediaStateChanged != null)
                {
                    MediaStateChanged(null, EventArgs.Empty);
                }
            }
        }

        //
        // PlayPosition
        //

        public static TimeSpan PlayPosition
            => TimeSpan.FromMilliseconds(player.getCurrentPosition());

        //
        // Properties
        //

        public static event EventHandler<EventArgs> ActiveSongChanged;
        public static event EventHandler<EventArgs> MediaStateChanged;

        public static MediaQueue Queue => queue;

        //
        // IsVisualizationEnabled
        //

        public static bool IsVisualizationEnabled
        {
            get => false;
            set => throw new PlatformNotSupportedException();
        }

        //
        // Watcher
        //

        private class Watcher : android.media.MediaPlayer.OnPreparedListener,
                                android.media.MediaPlayer.OnCompletionListener
        {

            //
            // onPrepared
            //

            [java.attr.RetainName]
            public void onPrepared(android.media.MediaPlayer player)
            {
                if (MediaPlayer.State == MediaState.Playing)
                {
                    try
                    {
                        var duration = player.getDuration();
                        if (duration != -1)
                        {
                            MediaPlayer.Queue.ActiveSong.Duration =
                                    TimeSpan.FromMilliseconds(duration);
                        }
                    }
                    catch (Exception)
                    {
                    }

                    player.start();
                }
            }

            //
            // onCompletion
            //

            [java.attr.RetainName]
            public void onCompletion(android.media.MediaPlayer player)
            {
                MediaPlayer.Stop();
            }

        }
    }

    //
    // Song
    //

    public sealed class Song : IEquatable<Song>, IDisposable
    {
        [java.attr.RetainType] public string path;
        [java.attr.RetainType] public bool isAsset;

        public bool IsDisposed { get; private set; }
        public string Name { get; private set; }
        public TimeSpan Duration { get; set; }
        public bool IsProtected => false;
        public bool IsRated => false;
        public int PlayCount => 0;
        public int Rating => 0;
        public int TrackNumber => 0;

        public static Song FromUri(string name, Uri uri)
        {
            var song = new Song() { Name = name };
            if (uri.IsAbsoluteUri && uri.IsFile)
                song.path = uri.LocalPath;
            else
            {
                song.path = uri.ToString();
                song.isAsset = true;
            }
            return song;
        }

        ~Song() => Dispose();
        public void Dispose() => IsDisposed = true;

        public override int GetHashCode() => base.GetHashCode();
        public bool Equals(Song other) => (((object) other) != null) && (path == other.path);
        public override bool Equals(object other) => Equals(other as Song);
        public static bool operator ==(Song song1, Song song2)
            => (song1 == null) ? (song2 == null) : song1.Equals(song2);
        public static bool operator !=(Song song1, Song song2) => ! (song1 == song2);

    }

}
