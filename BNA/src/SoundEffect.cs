
using System;
using System.IO;
#pragma warning disable 0436

namespace Microsoft.Xna.Framework.Audio
{

    public sealed class SoundEffect : IDisposable
    {

        [java.attr.RetainType] public object dataArray;
        [java.attr.RetainType] public int dataCount;
        [java.attr.RetainType] public int sampleRate;
        [java.attr.RetainType] public int channelConfig;
        [java.attr.RetainType] public int markerFrame;

        [java.attr.RetainType] public static java.util.ArrayList instancesList = new java.util.ArrayList();
        [java.attr.RetainType] public static java.util.concurrent.locks.ReentrantLock instancesLock = new java.util.concurrent.locks.ReentrantLock();

        //
        // Constructor (for ContentReader)
        //

        public SoundEffect(string name, byte[] buffer, int offset, int count,
                           ushort wFormatTag, ushort nChannels,
                           uint nSamplesPerSec, uint nAvgBytesPerSec,
                           ushort nBlockAlign, ushort wBitsPerSample,
                           int loopStart, int loopLength)
        {
            if (wFormatTag != 1 /* WAVE_FORMAT_PCM */)
                throw new ArgumentException("bad wFormatTag");
            if (offset != 0)
                throw new ArgumentException("bad offset");
            if (nBlockAlign != nChannels * wBitsPerSample / 8)
                throw new ArgumentException("bad nBlockAlign");
            if (nAvgBytesPerSec != nSamplesPerSec * nBlockAlign)
                throw new ArgumentException("bad nAvgBytesPerSec");

            sampleRate = (int) nSamplesPerSec;
            channelConfig = (nChannels == 1) ? android.media.AudioFormat.CHANNEL_OUT_MONO
                          : (nChannels == 2) ? android.media.AudioFormat.CHANNEL_OUT_STEREO
                          : throw new ArgumentException("bad nChannels");

            if (wBitsPerSample == 8)
            {
                dataArray = buffer;
                dataCount = count;
            }
            else if (wBitsPerSample == 16)
            {
                int shortCount = count / 2;
                var shortBuffer = new short[shortCount];
                java.nio.ByteBuffer.wrap((sbyte[]) (object) buffer)
                                   .order(java.nio.ByteOrder.LITTLE_ENDIAN).asShortBuffer()
                                   .get(shortBuffer);
                dataArray = shortBuffer;
                dataCount = shortCount;
            }
            else
                throw new ArgumentException("bad wBitsPerSample");

            markerFrame = dataCount / nChannels;

            Name = name;
            Duration = TimeSpan.FromSeconds(count / (double) nAvgBytesPerSec);
        }

        //
        // Constructor
        //

        public SoundEffect(byte[] buffer, int sampleRate, AudioChannels channels)
            : this(null, buffer, 0, buffer.Length,
                   1 /* WAVE_FORMAT_PCM */, (ushort) channels, (uint) sampleRate,
                   (uint) (sampleRate * ((ushort) channels * 2)),
                   (ushort) ((ushort) channels * 2), 16, 0, 0)
        {
        }

        //
        // Constructor
        //

        public SoundEffect(byte[] buffer, int offset, int count, int sampleRate,
                           AudioChannels channels, int loopStart, int loopLength)
            : this(null, buffer, offset, count,
                   1 /* WAVE_FORMAT_PCM */, (ushort) channels,  (uint) sampleRate,
                   (uint) (sampleRate * ((ushort) channels * 2)),
                   (ushort) ((ushort) channels * 2), 16, loopStart, loopLength)
        {
        }

        //
        // FromStream
        //

        public static SoundEffect FromStream(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                for (;;)
                {
                    if (new string(reader.ReadChars(4)) != "RIFF")
                        break;
                    reader.ReadUInt32(); // skip chunk size
                    if (new string(reader.ReadChars(4)) != "WAVE")
                        break;

                    if (new string(reader.ReadChars(4)) != "fmt ")
                        break;
                    if (reader.ReadInt32() != 16) // fmt chunk size always 16
                        break;

                    var wFormatTag = reader.ReadUInt16();
                    var nChannels = reader.ReadUInt16();
                    var nSamplesPerSec = reader.ReadUInt32();
                    var nAvgBytesPerSec = reader.ReadUInt32();
                    var nBlockAlign = reader.ReadUInt16();
                    var wBitsPerSample = reader.ReadUInt16();

                    if (new string(reader.ReadChars(4)) != "data")
                        break;

                    var count = reader.ReadInt32();
                    var buffer = reader.ReadBytes(count);

                    return new SoundEffect(null, buffer, 0, count, wFormatTag, nChannels,
                                           nSamplesPerSec, nAvgBytesPerSec,
                                           nBlockAlign, wBitsPerSample, 0, 0);
                }
            }

            throw new BadImageFormatException("invalid wave data for sound effect");
        }

        //
        // Destructor
        //

        ~SoundEffect() => Dispose();

        //
        // Dispose
        //

        public void Dispose()
        {
            if (! IsDisposed)
            {
                IsDisposed = true;
                DiscardInstance(null, this);
            }
        }

        //
        // Properties
        //

        public string Name { get; set; }
        public TimeSpan Duration { get; private set; }
        public bool IsDisposed { get; private set; }

        //
        // Play
        //

        public bool Play() => Play(1f, 0f, 0f);

        public bool Play(float volume, float pitch, float pan)
            => CreateInstance().Play(volume, pitch, pan);

        //
        // CreateInstance
        //

        public SoundEffectInstance CreateInstance()
        {
            var inst = new SoundEffectInstance(this);
            try
            {
                instancesLock.@lock();
                instancesList.add(new java.lang.@ref.WeakReference(inst));
            }
            finally
            {
                instancesLock.unlock();
            }
            return inst;
        }

        //
        // DiscardInstance
        //

        public static void DiscardInstance(SoundEffectInstance discardInstance, SoundEffect discardEffect)
        {
            if (instancesLock.isHeldByCurrentThread())
                return;
            try
            {
                instancesLock.@lock();
                for (int idx = instancesList.size(); idx-- > 0;)
                {
                    var instRef = (java.lang.@ref.WeakReference) instancesList.get(idx);
                    var inst = (SoundEffectInstance) instRef.get();
                    if (inst == discardInstance || inst == null || inst.ShouldDiscard(discardEffect))
                        instancesList.remove(idx);
                }
            }
            finally
            {
                instancesLock.unlock();
            }
        }

        //
        // ReleaseInstance
        //

        public static bool ReleaseInstance()
        {
            bool found = false;
            try
            {
                instancesLock.@lock();
                int num = instancesList.size();
                for (int idx = 0; idx < num; idx++)
                {
                    var instRef = (java.lang.@ref.WeakReference) instancesList.get(idx);
                    var inst = (SoundEffectInstance) instRef.get();
                    if (inst != null && inst.ReleaseTrack(false))
                    {
                        found = true;
                        break;
                    }
                }
            }
            finally
            {
                instancesLock.unlock();
            }
            return found;
        }

        //
        // GetSampleDuration, GetSampleSizeInBytes
        //

        public static TimeSpan GetSampleDuration(int sizeInBytes, int sampleRate,
                                                 AudioChannels channels)
            => TimeSpan.FromSeconds(
                    ((sizeInBytes / (2 * (int) channels)) / (float) sampleRate));

        public static int GetSampleSizeInBytes(TimeSpan duration, int sampleRate,
                                               AudioChannels channels)
            => (int) (duration.TotalSeconds * sampleRate * 2 * (int) channels);

        //
        // MasterVolume, DistanceScale, DopplerScale, SpeedOfSound (no-op)
        //

        public static float MasterVolume { get; set; }
        public static float DistanceScale { get; set; }
        public static float DopplerScale { get; set; }
        public static float SpeedOfSound { get; set; }
    }



    //
    // SoundEffectInstance
    //

    public class SoundEffectInstance : IDisposable
    {
        [java.attr.RetainType] private SoundEffect effect;
        [java.attr.RetainType] private SoundEffectInstanceWatcher watcher;
        [java.attr.RetainType] private android.media.AudioTrack track;
        [java.attr.RetainType] private float pitch, pan, volume;
        [java.attr.RetainType] private bool isLooped;

        //
        // Constructor (for SoundEffect.CreateInstance)
        //

        public SoundEffectInstance(SoundEffect fromEffect)
        {
            effect = fromEffect;
            volume = 1f;
        }

        //
        // CreateTrack
        //

        private void CreateTrack(bool tryRelease)
        {
            int numToWrite, numWritten;

            if (effect.dataArray is sbyte[] byteData)
            {
                numToWrite = byteData.Length;
                track = new android.media.AudioTrack(
                                android.media.AudioManager.STREAM_MUSIC,
                                effect.sampleRate, effect.channelConfig,
                                android.media.AudioFormat.ENCODING_PCM_8BIT,
                                numToWrite, android.media.AudioTrack.MODE_STATIC);
                numWritten = track.write(byteData, 0, numToWrite);
            }

            else if (effect.dataArray is short[] shortData)
            {
                numToWrite = shortData.Length;
                track = new android.media.AudioTrack(
                                android.media.AudioManager.STREAM_MUSIC,
                                effect.sampleRate, effect.channelConfig,
                                android.media.AudioFormat.ENCODING_PCM_16BIT,
                                numToWrite * 2, android.media.AudioTrack.MODE_STATIC);
                numWritten = track.write(shortData, 0, numToWrite);
            }
            else
            {
                numToWrite = 0;
                numWritten = android.media.AudioTrack.ERROR_INVALID_OPERATION;
            }

            if (numWritten != numToWrite)
            {
                track = null;
                if (numWritten < 0)
                {
                    if (SoundEffect.ReleaseInstance())
                        CreateTrack(false);
                }
                if (track == null)
                {
                    GameRunner.Log($"SoundEffectInstance '{effect.Name}' error {numWritten}/{numToWrite}");
                }
                return;
            }

            track.setNotificationMarkerPosition(effect.markerFrame);
            track.setPlaybackPositionUpdateListener(watcher = new SoundEffectInstanceWatcher());
        }

        //
        // ReleaseTrack
        //

        public bool ReleaseTrack(bool disposing)
        {
            var track = this.track;
            if (track != null)
            {
                if (disposing || track.getPlayState() == 1 /* android.media.AudioTrack.PLAYSTATE_STOPPED */)
                {
                    this.track = null;
                    track.setPlaybackPositionUpdateListener(null);
                    track.stop();
                    track.release();
                    return true;
                }
            }
            return false;
        }

        //
        // Destructor
        //

        ~SoundEffectInstance() => Dispose(true);

        //
        // Dispose
        //

        protected virtual void Dispose(bool disposing)
        {
            if (! IsDisposed)
            {
                ReleaseTrack(true);
                SoundEffect.DiscardInstance(this, null);
                effect = null;
                IsDisposed = true;
            }
        }

        public void Dispose() => Dispose(true);

        //
        // ShouldDiscard
        //

        public bool ShouldDiscard(SoundEffect fromEffect)
        {
            if (effect == fromEffect)
                Dispose(true);
            return IsDisposed;
        }

        //
        // Properties
        //

        public SoundState State
        {
            get
            {
                var track = this.track;
                if (track == null)
                    return SoundState.Stopped;

                return track.getPlayState() switch
                {
                    1 /* android.media.AudioTrack.PLAYSTATE_STOPPED */ => SoundState.Stopped,
                    2 /* android.media.AudioTrack.PLAYSTATE_PAUSED */  => SoundState.Paused,
                    3 /* android.media.AudioTrack.PLAYSTATE_PLAYING */ => SoundState.Playing,
                    _ => throw new InvalidOperationException()
                };
            }
        }

        public bool IsDisposed { get; protected set; }
        public float Pitch
        {
            get => pitch;
            set => SetPlaybackRate(value, State == SoundState.Playing);
        }
        public float Pan
        {
            get => pan;
            set => SetStereoVolume(volume, value, State == SoundState.Playing);
        }
        public float Volume
        {
            get => volume;
            set => SetStereoVolume(value, pan, State == SoundState.Playing);
        }

        //
        // IsLooped
        //
        // note that looping does not respect any custom loop points,
        // and always occurs on the entire effect
        //

        public virtual bool IsLooped
        {
            get => isLooped;
            set
            {
                if (State == SoundState.Playing)
                    throw new InvalidOperationException();
                isLooped = value;
            }
        }

        //
        // SetPlaybackRate
        //

        private void SetPlaybackRate(float pitch, bool playing)
        {
            if (pitch < -1f)
                pitch = -1f;
            else if (pitch > 1f)
                pitch = 1f;
            this.pitch = pitch;

            if (playing)
            {
                // convert pitch from range 0 .. 1 to range 0.5 .. 2
                // (which represents half to twice the sample rate)
                if (pitch < 0f)
                    pitch = 1f + pitch * 0.5f;
                else if (pitch > 0f)
                    pitch = 1f + pitch;
                int r = (int) (effect.sampleRate * pitch);
                var track = this.track;
                if (track != null)
                    track.setPlaybackRate(r);
            }
        }

        //
        // SetStereoVolume
        //

        private void SetStereoVolume(float volume, float pan, bool playing)
        {
            if (volume < 0f)
                volume = 0f;
            else if (volume > 1f)
                volume = 1f;
            this.volume = volume;

            if (pan < -1f)
                pan = -1f;
            else if (pan > 1f)
                pan = 1f;
            this.pan = pan;

            if (playing)
            {
                float leftGain = 1f;
                float rightGain = 1f;
                if (pan < 0f)
                {
                    leftGain *= 0f - pan;
                    rightGain *= pan + 1f;
                }
                else if (pan > 0f)
                {
                    rightGain *= pan;
                    leftGain *= 1f - pan;
                }
                var track = this.track;
                if (track != null)
                    track.setStereoVolume(leftGain * volume, rightGain * volume);
            }
        }

        //
        // Play, Pause, Resume, Stop
        //

        public virtual void Play()
        {
            if (State != SoundState.Playing)
            {
                var track = this.track;
                if (track == null)
                {
                    CreateTrack(true);
                    track = this.track;
                }

                if (track != null)
                {
                    SetPlaybackRate(pitch, true);
                    SetStereoVolume(volume, pan, true);

                    watcher.instance = this;
                    track.play();
                }
            }
        }

        public void Pause()
        {
            var track = this.track;
            if (track != null && State == SoundState.Playing)
                track.pause();
        }

        public void Resume() => Play();

        public void Stop() => Stop(true);

        public void Stop(bool immediate)
        {
            if (immediate)
            {
                var track = this.track;
                if (track != null && State != SoundState.Stopped)
                    track.stop();
            }
            if (watcher != null)
                watcher.instance = null;
        }

        //
        // Play (for SoundEffect.Play)
        //

        public bool Play(float volume, float pitch, float pan)
        {
            this.volume = volume;
            this.pitch = pitch;
            this.pan = pan;
            Play();
            if (track == null)
            {
                Dispose(true);
                return false;
            }
            return true;
        }

        //
        // Apply3D (no-op)
        //

        public void Apply3D(AudioListener listener, AudioEmitter emitter) { }
        public void Apply3D(AudioListener[] listeners, AudioEmitter emitter) { }

    }



    //
    // SoundEffectInstanceWatcher
    //

    public class SoundEffectInstanceWatcher :
                        android.media.AudioTrack.OnPlaybackPositionUpdateListener
    {
        [java.attr.RetainType] public SoundEffectInstance instance;

        [java.attr.RetainName]
        public void onMarkerReached(android.media.AudioTrack track)
        {
            // release the strong reference to the SoundEffectInstance,
            // so it can be garbage collected if not otherwise referenced
            track.stop();
            var instance = this.instance;
            if (instance != null)
            {
                if (instance.IsLooped && (! instance.IsDisposed))
                    track.play();
                else
                    this.instance = null;
            }
        }

        [java.attr.RetainName]
        public void onPeriodicNotification(android.media.AudioTrack track) { }
    }

}
