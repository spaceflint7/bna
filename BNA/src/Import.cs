
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Graphics;
#pragma warning disable 0436

namespace Microsoft.Xna.Framework
{

    //
    // GameWindow
    //

    [java.attr.Discard] // discard in output
    public abstract class GameWindow
    {
        public abstract IntPtr Handle { get; }
        public abstract bool AllowUserResizing { get; set; }
        public abstract Rectangle ClientBounds { get; }
        public abstract string ScreenDeviceName { get; }
        public abstract void SetSupportedOrientations(DisplayOrientation orientations);
        public abstract DisplayOrientation CurrentOrientation { get; set; }
        public abstract void BeginScreenDeviceChange(bool willBeFullScreen);
        public abstract void EndScreenDeviceChange(string screenDeviceName, int clientWidth, int clientHeight);
        public abstract void SetTitle(string title);
        protected void OnActivated() { }
        protected void OnDeactivated() { }
        protected void OnPaint() { }
        protected void OnScreenDeviceNameChanged() { }
        protected void OnClientSizeChanged() { }
        protected void OnOrientationChanged() { }
        public static readonly int DefaultClientWidth = 800;
        public static readonly int DefaultClientHeight = 600;
    }

    //
    // Game
    //

    [java.attr.Discard] // discard in output
    public abstract class Game
    {
        public bool IsActive { get; set; }
        public bool RunApplication;
        public abstract void Tick();
    }
}



namespace Microsoft.Xna.Framework.Graphics
{

    //
    // GraphicsDevice
    //

    [java.attr.Discard] // discard in output
    public class GraphicsDevice
    {
        public readonly IntPtr GLDevice;
        public TextureCollection Textures { get; }
    }

    //
    // DisplayMode
    //

    [java.attr.Discard] // discard in output
    public class DisplayMode
    {
        public DisplayMode(int width, int height, SurfaceFormat format) { }
    }

    //
    // DisplayModeCollection
    //

    [java.attr.Discard] // discard in output
    public class DisplayModeCollection
    {
        public DisplayModeCollection(List<DisplayMode> setmodes) { }
    }

    //
    // GraphicsAdapter
    //

    [java.attr.Discard] // discard in output
    public sealed class GraphicsAdapter
    {
        public GraphicsAdapter(DisplayModeCollection modes, string name, string description) { }
        public static ReadOnlyCollection<GraphicsAdapter> Adapters => null;
    }

    //
    // GraphicsResource
    //

    [java.attr.Discard] // discard in output
    public abstract class GraphicsResource
    {
        public GraphicsDevice GraphicsDevice { get; set; }
        protected virtual void Dispose(bool disposing) { }
        public virtual bool IsDisposed => false;
    }

    //
    // SpriteBatch
    //

    [java.attr.Discard] // discard in output
    public class SpriteBatch
    {
        public struct VertexPositionColorTexture4
        {
            public Vector3 Position0;
            public Color Color0;
            public Vector2 TextureCoordinate0;

            public Vector3 Position1;
            public Color Color1;
            public Vector2 TextureCoordinate1;

            public Vector3 Position2;
            public Color Color2;
            public Vector2 TextureCoordinate2;

            public Vector3 Position3;
            public Color Color3;
            public Vector2 TextureCoordinate3;
        }
    }

    //
    // EffectParameterCollection
    //

    [java.attr.Discard] // discard in output
    public class EffectParameterCollection
    {
        public EffectParameterCollection(List<EffectParameter> value) { }
        public EffectParameter this[int index] => null;
        public int Count { get; }
    }

    //
    // EffectPass
    //

    [java.attr.Discard] // discard in output
    public sealed class EffectPass
    {
        public EffectPass(string name, EffectAnnotationCollection annotations,
                          Effect parent, IntPtr technique, uint passIndex) { }
    }

    //
    // EffectPassCollection
    //

    [java.attr.Discard] // discard in output
    public class EffectPassCollection
    {
        public EffectPassCollection(List<EffectPass> value) { }
    }

    //
    // EffectTechnique
    //

    [java.attr.Discard] // discard in output
    public sealed class EffectTechnique
    {
        public EffectTechnique(string name, IntPtr pointer, EffectPassCollection passes,
                               EffectAnnotationCollection annotations) { }
        public string Name { get; }
    }

    //
    // EffectTechniqueCollection
    //

    [java.attr.Discard] // discard in output
    public class EffectTechniqueCollection
    {
        public EffectTechniqueCollection(List<EffectTechnique> value) { }
    }

}



namespace Microsoft.Xna.Framework.Input
{
    [java.attr.Discard] // discard in output
    public struct MouseState
    {
        public int X { get; set; }
        public int Y { get; set; }
        public ButtonState LeftButton { get; set; }
    }
}



namespace Microsoft.Xna.Framework.Input.Touch
{

    //
    // TouchPanelCapabilities
    //

    [java.attr.Discard] // discard in output
    public struct TouchPanelCapabilities
    {
        public TouchPanelCapabilities(bool isConnected, int maximumTouchCount) { }
    }

    [java.attr.Discard] // discard in output
    public class TouchPanel
    {
        public static void INTERNAL_onTouchEvent(int fingerId, TouchLocationState state,
                                                 float x, float y, float dx, float dy) { }
    }

}



namespace Microsoft.Xna.Framework.Media
{

    //
    // MediaQueue
    //

    [java.attr.Discard] // discard in output
    public class MediaQueue
    {
        public MediaQueue() { }
        public Song ActiveSong { get; }
        public int ActiveSongIndex { get; set; }
        public void Add(Song song) { }
        public void Clear() { }
    }

}
