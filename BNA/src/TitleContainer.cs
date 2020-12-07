
using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
#pragma warning disable 0436

namespace Microsoft.Xna.Framework
{

    internal static class TitleContainer
    {

        public static Stream OpenStream(string name)
        {
            var stream = GameRunner.Singleton.Activity
                            .getAssets().open(name.Replace('\\', '/'));
            if (stream == null)
                throw new System.IO.FileNotFoundException(name);
            return new TitleStream(stream, name);
        }

        public class TitleStream : Stream
        {
            public java.io.InputStream JavaStream;
            public string Name;

            public TitleStream(java.io.InputStream javaStream, string name)
            {
                JavaStream = javaStream;
                Name = name;
            }

            public override bool CanRead => true;
            public override bool CanWrite => false;
            public override bool CanSeek => false;

            public override int Read(byte[] buffer, int offset, int count)
                => JavaStream.read((sbyte[]) (object) buffer, offset, count);

            //
            // unused methods and properties
            //

            public override long Length => throw new System.PlatformNotSupportedException();
            public override long Position
            {
                get => throw new System.PlatformNotSupportedException();
                set => throw new System.PlatformNotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
                => throw new System.PlatformNotSupportedException();
            public override long Seek(long offset, System.IO.SeekOrigin origin)
                => throw new System.PlatformNotSupportedException();
            public override void SetLength(long value)
                => throw new System.PlatformNotSupportedException();
            public override void Flush() => throw new System.PlatformNotSupportedException();
        }

    }

    /*internal static class TitleLocation
    {
        // there is no file path for asset files which are part of the APK
        public static string Path => throw new System.PlatformNotSupportedException();
    }*/

}
