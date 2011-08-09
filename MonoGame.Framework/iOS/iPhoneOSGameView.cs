#region --- License ---
/* Licensed under the MIT/X11 license.
 * Copyright (c) 2009 Novell, Inc.
 * This notice may not be removed from any source distribution.
 * See license.txt for licensing detailed licensing details.
 */
#endregion

using System;
using System.ComponentModel;
using System.Drawing;

using MonoTouch.CoreAnimation;
using MonoTouch.CoreGraphics;
using MonoTouch.Foundation;
using MonoTouch.OpenGLES;
using MonoTouch.UIKit;
using MonoTouch.ObjCRuntime;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Platform;
using OpenTK.Platform.iPhoneOS;

using All  = OpenTK.Graphics.ES11.All;
using ES11 = OpenTK.Graphics.ES11;
using ES20 = OpenTK.Graphics.ES20;

namespace OpenTK.Platform.iPhoneOS
{
    sealed class GLCalls {
        public delegate void glBindFramebuffer(All target, int framebuffer);
        public delegate void glBindRenderbuffer(All target, int renderbuffer);
        public delegate void glDeleteFramebuffers(int n, ref int framebuffers);
        public delegate void glDeleteRenderbuffers(int n, ref int renderbuffers);
        public delegate void glFramebufferRenderbuffer(All target, All attachment, All renderbuffertarget, int renderbuffer);
        public delegate void glGenFramebuffers(int n, ref int framebuffers);
        public delegate void glGenRenderbuffers(int n, ref int renderbuffers);
        public delegate void glGetInteger(All name, ref int value);
        public delegate void glScissor(int x, int y, int width, int height);
        public delegate void glViewport(int x, int y, int width, int height);

        public glBindFramebuffer BindFramebuffer;
        public glBindRenderbuffer BindRenderbuffer;
        public glDeleteFramebuffers DeleteFramebuffers;
        public glDeleteRenderbuffers DeleteRenderbuffers;
        public glFramebufferRenderbuffer FramebufferRenderbuffer;
        public glGenFramebuffers GenFramebuffers;
        public glGenRenderbuffers GenRenderbuffers;
        public glGetInteger GetInteger;
        public glScissor Scissor;
        public glViewport Viewport;

        public static GLCalls GetGLCalls(EAGLRenderingAPI api)
        {
            switch (api) {
                case EAGLRenderingAPI.OpenGLES1: return CreateES1();
                case EAGLRenderingAPI.OpenGLES2: return CreateES2();
            }
            throw new ArgumentException("api");
        }

        static GLCalls CreateES1()
        {
            return new GLCalls() {
                BindFramebuffer         = (t, f)              => ES11.GL.Oes.BindFramebuffer(t, f),
                BindRenderbuffer        = (t, r)              => ES11.GL.Oes.BindRenderbuffer(t, r),
                DeleteFramebuffers      = (int n, ref int f)  => ES11.GL.Oes.DeleteFramebuffers(n, ref f),
                DeleteRenderbuffers     = (int n, ref int r)  => ES11.GL.Oes.DeleteRenderbuffers(n, ref r),
                FramebufferRenderbuffer = (t, a, rt, rb)      => ES11.GL.Oes.FramebufferRenderbuffer(t, a, rt, rb),
                GenFramebuffers         = (int n, ref int f)  => ES11.GL.Oes.GenFramebuffers(n, ref f),
                GenRenderbuffers        = (int n, ref int r)  => ES11.GL.Oes.GenRenderbuffers(n, ref r),
                GetInteger              = (All n, ref int v)  => ES11.GL.GetInteger(n, ref v),
                Scissor                 = (x, y, w, h)        => ES11.GL.Scissor(x, y, w, h),
                Viewport                = (x, y, w, h)        => ES11.GL.Viewport(x, y, w, h),
            };
        }

        static GLCalls CreateES2()
        {
            return new GLCalls() {
                BindFramebuffer         = (t, f)              => ES20.GL.BindFramebuffer((ES20.All) t, f),
                BindRenderbuffer        = (t, r)              => ES20.GL.BindRenderbuffer((ES20.All) t, r),
                DeleteFramebuffers      = (int n, ref int f)  => ES20.GL.DeleteFramebuffers(n, ref f),
                DeleteRenderbuffers     = (int n, ref int r)  => ES20.GL.DeleteRenderbuffers(n, ref r),
                FramebufferRenderbuffer = (t, a, rt, rb)      => ES20.GL.FramebufferRenderbuffer((ES20.All) t, (ES20.All) a, (ES20.All) rt, rb),
                GenFramebuffers         = (int n, ref int f)  => ES20.GL.GenFramebuffers(n, ref f),
                GenRenderbuffers        = (int n, ref int r)  => ES20.GL.GenRenderbuffers(n, ref r),
                GetInteger              = (All n, ref int v)  => ES20.GL.GetInteger((ES20.All) n, ref v),
                Scissor                 = (x, y, w, h)        => ES20.GL.Scissor(x, y, w, h),
                Viewport                = (x, y, w, h)        => ES20.GL.Viewport(x, y, w, h),
            };
        }
    }

    public class iPhoneOSGameView : UIView
    {
        bool disposed;

        int framebuffer, renderbuffer;

        GLCalls gl;

        [Export("initWithCoder:")]
        public iPhoneOSGameView(NSCoder coder)
            : base(coder)
        {
        }

        [Export("initWithFrame:")]
        public iPhoneOSGameView(RectangleF frame)
            : base(frame)
        {
        }

        [Export ("layerClass")]
        public static Class GetLayerClass ()
        {
            return new Class (typeof (CAEAGLLayer));
        }

        void AssertValid()
        {
            if (disposed)
                throw new ObjectDisposedException("");
        }

        void AssertContext()
        {
            if (GraphicsContext == null)
                throw new InvalidOperationException("Operation requires a GraphicsContext, which hasn't been created yet.");
        }

        EAGLRenderingAPI api;
        public EAGLRenderingAPI ContextRenderingApi {
            get {
                AssertValid();
                return api;
            }
            set {
                AssertValid();
                if (GraphicsContext != null)
                    throw new NotSupportedException("Can't change RenderingApi after GraphicsContext is constructed.");
                this.api = value;
            }
        }

        public IGraphicsContext GraphicsContext {get; set;}
        public EAGLContext EAGLContext {
            get {
                AssertValid();
                if (GraphicsContext != null) {
                    iPhoneOSGraphicsContext c = GraphicsContext as iPhoneOSGraphicsContext;
                    if (c != null)
                        return c.EAGLContext;
                    var i = GraphicsContext as IGraphicsContextInternal;
                    IGraphicsContext ic = i == null ? null : i.Implementation;
                    c = ic as iPhoneOSGraphicsContext;
                    if (c != null)
                        return c.EAGLContext;
                }
                return null;
            }
        }

        bool retainedBacking;
        public bool LayerRetainsBacking {
            get {
                AssertValid();
                return retainedBacking;
            }
            set {
                AssertValid();
                if (GraphicsContext != null)
                    throw new NotSupportedException("Can't change LayerRetainsBacking after GraphicsContext is constructed.");
                retainedBacking = value;
            }
        }

        NSString colorFormat;
        public NSString LayerColorFormat {
            get {
                AssertValid();
                return colorFormat;
            }
            set {
                AssertValid();
                if (GraphicsContext != null)
                    throw new NotSupportedException("Can't change LayerColorFormat after GraphicsContext is constructed.");
                colorFormat = value;
            }
        }

        public int Framebuffer {
            get {return framebuffer;}
        }

        public int Renderbuffer {
            get {return renderbuffer;}
        }

        public bool AutoResize {get; set;}

        UIViewController GetViewController()
        {
            UIResponder r = this;
            while (r != null) {
                var c = r as UIViewController;
                if (c != null)
                    return c;
                r = r.NextResponder;
            }
            return null;
        }

        public virtual string Title {
            get {
                AssertValid();
                var c = GetViewController();
                if (c != null)
                    return c.Title;
                throw new NotSupportedException();
            }
            set {
                AssertValid();
                var c = GetViewController();
                if (c != null) {
                    if (c.Title != value) {
                        c.Title = value;
                        OnTitleChanged(EventArgs.Empty);
                    }
                }
                else
                    throw new NotSupportedException();
            }
        }

        protected virtual void OnTitleChanged(EventArgs e)
        {
            var h = TitleChanged;
            if (h != null)
                h (this, EventArgs.Empty);
        }

        bool Focused {
            get {throw new NotImplementedException();}
        }

        public virtual bool Visible {
            get {
                AssertValid();
                return !base.Hidden;
            }
            set {
                AssertValid();
                if (base.Hidden != !value) {
                    base.Hidden = !value;
                    OnVisibleChanged(EventArgs.Empty);
                }
            }
        }

        protected virtual void OnVisibleChanged(EventArgs e)
        {
            var h = VisibleChanged;
            if (h != null)
                h (this, EventArgs.Empty);
        }

        bool Exists {
            get {throw new NotImplementedException();}
        }

        public virtual IWindowInfo WindowInfo {
            get {
                AssertValid();
                return null;
            }
        }

        public virtual WindowState WindowState {
            get {
                AssertValid();
                var c = GetViewController();
                if (c != null && c.WantsFullScreenLayout)
                    return WindowState.Fullscreen;
                return WindowState.Normal;
            }
            set {
                AssertValid();
                var c = GetViewController();
                if (c != null) {
                    if (c.WantsFullScreenLayout == (value == WindowState.Fullscreen)) {
                        c.WantsFullScreenLayout = value == WindowState.Fullscreen;
                        OnWindowStateChanged(EventArgs.Empty);
                    }
                }
            }
        }

        protected virtual void OnWindowStateChanged(EventArgs e)
        {
            var h = WindowStateChanged;
            if (h != null)
                h (this, EventArgs.Empty);
        }

        public virtual WindowBorder WindowBorder {
            get {
                AssertValid();
                return WindowBorder.Hidden;
            }
            set {}
        }

        Rectangle Bounds {
            get {throw new NotSupportedException();}
            set {throw new NotSupportedException();}
        }

        Point Location {
            get {throw new NotSupportedException();}
            set {throw new NotSupportedException();}
        }

        Size size;
        public Size Size {
            get {
                AssertValid();
                return size;
            }
            set {
                AssertValid();
                if (size != value) {
                    size = value;
                    OnResize(EventArgs.Empty);
                }
            }
        }

        protected virtual void OnResize(EventArgs e)
        {
            var h = Resize;
            if (h != null)
                h (this, e);
        }

        int X {
            get {throw new NotSupportedException();}
            set {throw new NotSupportedException();}
        }

        int Y {
            get {throw new NotSupportedException();}
            set {throw new NotSupportedException();}
        }

        int Width {
            get {throw new NotSupportedException();}
            set {throw new NotSupportedException();}
        }

        int Height {
            get {throw new NotSupportedException();}
            set {throw new NotSupportedException();}
        }

        Rectangle ClientRectangle {
            get {throw new NotSupportedException();}
            set {throw new NotSupportedException();}
        }

        Size ClientSize {
            get {throw new NotSupportedException();}
            set {throw new NotSupportedException();}
        }

        protected virtual void CreateFrameBuffer()
        {
            AssertValid();
            if (LayerColorFormat == null)
                throw new InvalidOperationException("Set the LayerColorFormat property to an EAGLColorFormat value before calling Run().");

            CAEAGLLayer eaglLayer = (CAEAGLLayer) Layer;
            eaglLayer.DrawableProperties = NSDictionary.FromObjectsAndKeys (
                    new NSObject [] {NSNumber.FromBoolean(LayerRetainsBacking), LayerColorFormat},
                    new NSObject [] {EAGLDrawableProperty.RetainedBacking,      EAGLDrawableProperty.ColorFormat}
            );
            ConfigureLayer(eaglLayer);

            GraphicsContext = Utilities.CreateGraphicsContext(ContextRenderingApi);
            gl = GLCalls.GetGLCalls(ContextRenderingApi);

            int oldFramebuffer = 0, oldRenderbuffer = 0;
            gl.GetInteger(All.FramebufferBindingOes, ref oldFramebuffer);
            gl.GetInteger(All.RenderbufferBindingOes, ref oldRenderbuffer);

            gl.GenRenderbuffers(1, ref renderbuffer);
            gl.BindRenderbuffer(All.RenderbufferOes, renderbuffer);

            if (!EAGLContext.RenderBufferStorage((uint) All.RenderbufferOes, eaglLayer)) {
                gl.DeleteRenderbuffers(1, ref renderbuffer);
                renderbuffer = 0;
                gl.BindRenderbuffer(All.RenderbufferBindingOes, oldRenderbuffer);
                throw new InvalidOperationException("Error with EAGLContext.RenderBufferStorage!");
            }

            gl.GenFramebuffers (1, ref framebuffer);
            gl.BindFramebuffer (All.FramebufferOes, framebuffer);
            gl.FramebufferRenderbuffer (All.FramebufferOes, All.ColorAttachment0Oes, All.RenderbufferOes, renderbuffer);

            Size newSize = new Size(
                    (int) Math.Round(eaglLayer.Bounds.Size.Width), 
                    (int) Math.Round(eaglLayer.Bounds.Size.Height));
            Size = newSize;

            gl.Viewport(0, 0, newSize.Width, newSize.Height);
            gl.Scissor(0, 0, newSize.Width, newSize.Height);
        }

        protected virtual void ConfigureLayer(CAEAGLLayer eaglLayer)
        {
        }

        protected virtual void DestroyFrameBuffer()
        {
            AssertValid();
            AssertContext();
            EAGLContext oldContext = EAGLContext.CurrentContext;
            if (!GraphicsContext.IsCurrent)
                MakeCurrent();

            gl.DeleteFramebuffers (1, ref framebuffer);
            gl.DeleteRenderbuffers (1, ref renderbuffer);
            framebuffer = renderbuffer = 0;

            if (oldContext != EAGLContext)
                EAGLContext.SetCurrentContext(oldContext);
            else
                EAGLContext.SetCurrentContext(null);

            EAGLContext.Dispose();
            GraphicsContext = null;
            gl = null;
        }

        public virtual void Close()
        {
            AssertValid();
            OnClosed(EventArgs.Empty);
        }

        protected virtual void OnClosed(EventArgs e)
        {
            var h = Closed;
            if (h != null)
                h (this, e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing) {
                DestroyFrameBuffer();
            }
            base.Dispose (disposing);
            disposed = true;
            if (disposing)
                OnDisposed(EventArgs.Empty);
        }

        protected virtual void OnDisposed(EventArgs e)
        {
            var h = Disposed;
            if (h != null)
                h (this, e);
        }

        void ProcessEvents()
        {
            throw new NotSupportedException();
        }

        Point PointToClient(Point point)
        {
            return point;
        }

        Point PointToScreen(Point point)
        {
            return point;
        }

        public override void LayoutSubviews()
        {
            if (GraphicsContext == null)
                return;

            var bounds = UIScreen.MainScreen.Bounds;
            if (AutoResize && (bounds.Width != Size.Width ||
                        bounds.Height != Size.Height)) {
                DestroyFrameBuffer();
                CreateFrameBuffer();
            }
        }

        public virtual void MakeCurrent()
        {
            AssertValid();
            AssertContext();
            GraphicsContext.MakeCurrent(WindowInfo);
        }

        public virtual void SwapBuffers()
        {
            AssertValid();
            AssertContext();
            gl.BindRenderbuffer(All.RenderbufferOes, renderbuffer);
            GraphicsContext.SwapBuffers();
        }

        NSTimer timer;

        public void Run()
        {
            AssertValid();
            CreateFrameBuffer();
            OnLoad(EventArgs.Empty);
            timer = NSTimer.CreateRepeatingScheduledTimer(1.0/ 60.0,RunIteration);
        }

        public void Run(double updatesPerSecond)
        {
            AssertValid();
            if (updatesPerSecond == 0.0) {
                Run();
                return;
            }
            CreateFrameBuffer();
            OnLoad(EventArgs.Empty);
            // Can't use TimeSpan.FromSeconds() as that only has 1ms
            // resolution, and we need better (e.g. 60fps doesn't fit nicely
            // in 1ms resolution, but does in ticks).
            var timeout = new TimeSpan ((long) (((1.0 * TimeSpan.TicksPerSecond) / updatesPerSecond) + 0.5));
            timer = NSTimer.CreateRepeatingScheduledTimer(timeout, RunIteration);
        }

        public void Stop()
        {
            AssertValid();
            if (timer != null)
                timer.Invalidate();
            timer = null;
            OnUnload(EventArgs.Empty);
        }

        public override void WillMoveToWindow(UIWindow window)
        {
            if (timer != null && window == null)
                Stop();
        }

        DateTime prevUpdateTime;
        DateTime prevRenderTime;

        FrameEventArgs updateEventArgs = new FrameEventArgs();
        FrameEventArgs renderEventArgs = new FrameEventArgs();
		[Export("RunIteration")]
        void RunIteration()
        {
            var curUpdateTime = DateTime.Now;
            if (prevUpdateTime.Ticks == 0)
                prevUpdateTime = curUpdateTime;
            var t = (curUpdateTime - prevUpdateTime).TotalSeconds;
            updateEventArgs = new FrameEventArgs(t);
            OnUpdateFrame(updateEventArgs);
            prevUpdateTime = curUpdateTime;

            gl.BindFramebuffer(All.FramebufferOes, framebuffer);

            var curRenderTime = DateTime.Now;
            if (prevRenderTime.Ticks == 0)
                prevRenderTime = curRenderTime;
            t = (curRenderTime - prevRenderTime).TotalSeconds;
            renderEventArgs= new FrameEventArgs( t);
            OnRenderFrame(renderEventArgs);
            prevRenderTime = curRenderTime;
        }

        protected virtual void OnLoad(EventArgs e)
        {
            var h = Load;
            if (h != null)
                h (this, e);
        }

        protected virtual void OnUnload(EventArgs e)
        {
            var h = Unload;
            DestroyFrameBuffer();
            if (h != null)
                h (this, e);
        }

        protected virtual void OnUpdateFrame(FrameEventArgs e)
        {
            var h = UpdateFrame;
            if (h != null)
                h (this, e);
        }

        protected virtual void OnRenderFrame(FrameEventArgs e)
        {
            var h = RenderFrame;
            if (h != null)
                h (this, e);
        }

        event EventHandler<EventArgs> Move {
            add {throw new NotSupportedException();}
            remove {throw new NotSupportedException();}
        }
        public event EventHandler<EventArgs> Resize;

        public event EventHandler<EventArgs> Closed;
        public event EventHandler<EventArgs> Disposed;
        public event EventHandler<EventArgs> TitleChanged;
        public event EventHandler<EventArgs> VisibleChanged;
        event EventHandler<EventArgs> FocusedChanged {
            add {throw new NotSupportedException();}
            remove {throw new NotSupportedException();}
        }
        event EventHandler<EventArgs> WindowBorderChanged {
            add {throw new NotSupportedException();}
            remove {throw new NotSupportedException();}
        }
        public event EventHandler<EventArgs> WindowStateChanged;
        event EventHandler<KeyPressEventArgs> KeyPress {
            add {throw new NotSupportedException();}
            remove {throw new NotSupportedException();}
        }

        public event EventHandler<EventArgs> Load;
        public event EventHandler<EventArgs> Unload;
        public event EventHandler<FrameEventArgs> UpdateFrame;
        public event EventHandler<FrameEventArgs> RenderFrame;
    }
}

// vim: et ts=4 sw=4
