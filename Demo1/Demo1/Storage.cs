
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Storage;

namespace Demo1
{
    public static class Storage
    {

        private const int VERSION_1 = 0x10101;
        private static readonly int INTEGER = BitConverter.ToInt32(Encoding.ASCII.GetBytes("int "), 0);
        private static readonly int FLOAT   = BitConverter.ToInt32(Encoding.ASCII.GetBytes("flt "), 0);
        private static readonly int STRING  = BitConverter.ToInt32(Encoding.ASCII.GetBytes("str "), 0);

        private static Stream file;
        private static Dictionary<string, object> dict;

        public static void Init()
        {
            dict = new Dictionary<string, object>();

            // on Android, StorageDevice and StorageContainer are "thin"
            // objects that do little more than translate the relative path
            // specified in StorageContainer::OpenFile, to a full path in
            // the app folder, which is then passed to System.IO.File.Open.

            // the basic persistence model here is:  game components update
            // a dictionary with current values.  the dictionary is written
            // to a file on pause (Game::OnDeactivated calls Storage::Sync),
            // and read from the file on startup (Game::Initialize calls
            // Storage::Init).

            try
            {
                var result = StorageDevice.BeginShowSelector(null, null);
                result.AsyncWaitHandle.WaitOne();
                var device = StorageDevice.EndShowSelector(result);
                result.AsyncWaitHandle.Close();

                result = device.BeginOpenContainer("Demo1", null, null);
                result.AsyncWaitHandle.WaitOne();
                var container = device.EndOpenContainer(result);
                result.AsyncWaitHandle.Close();

                file = container.OpenFile("state-v1.bin", FileMode.OpenOrCreate);
                if (file.Length > 4)
                    Read(file);

                // reset file in case we crash
                file.SetLength(0);
                file.Flush();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while reading state: " + e);
                dict.Clear();
            }
        }

        public static void Sync()
        {
            file.Position = 0;
            try
            {
                Write(file);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while writing state: " + e);
                file.SetLength(0);
            }
            file.Flush();
        }

        public static void Clear()
        {
            dict.Clear();
            Sync();
        }

        static void Read(Stream file)
        {
            using (var reader = new BinaryReader(file, Encoding.UTF8, true))
            {
                var version = reader.ReadInt32();
                if (version == VERSION_1)
                {
                    for (var count = reader.ReadInt32(); count > 0; count--)
                    {
                        var name = reader.ReadString();
                        var type = reader.ReadInt32();
                        var obj = (type == INTEGER) ? (object) reader.ReadInt32()
                                : (type == FLOAT) ? (object) reader.ReadSingle()
                                : (type == STRING) ? (object) reader.ReadString()
                                : null;
                        if (obj == null)
                            throw new NullReferenceException();
                        dict[name] = obj;
                    }
                }
            }
        }

        static void Write(Stream file)
        {
            using (var writer = new BinaryWriter(file, Encoding.UTF8, true))
            {
                writer.Write(VERSION_1);
                writer.Write(dict.Count);
                foreach (var kvp in dict)
                {
                    writer.Write(kvp.Key);
                    if (kvp.Value is int intValue)
                    {
                        writer.Write(INTEGER);
                        writer.Write(intValue);
                    }
                    else if (kvp.Value is float floatValue)
                    {
                        writer.Write(FLOAT);
                        writer.Write(floatValue);
                    }
                    else if (kvp.Value is string stringValue)
                    {
                        writer.Write(STRING);
                        writer.Write(stringValue);
                    }
                }
            }
        }

        public static int GetInt(string name, int defValue = 0)
        {
            return    dict.TryGetValue(name, out var v)
                   && (v is int intValue) ? intValue : defValue;
        }

        public static float GetFloat(string name, float defValue = 0f)
        {
            return    dict.TryGetValue(name, out var v)
                   && (v is float floatValue) ? floatValue : defValue;
        }

        public static string GetString(string name, string defValue = "")
        {
            return    dict.TryGetValue(name, out var v)
                   && (v is string stringValue) ? stringValue : defValue;
        }

        public static void Set(string name, object value)
        {
            dict[name] = value;
        }

    }
}
