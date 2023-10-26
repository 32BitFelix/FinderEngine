
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using OpenTK.Graphics.OpenGL4;

using OpenTK.Mathematics;

using Rendering;

using SuperArray;

namespace FinderEngine
{
    using Scenes;

    public class Application : GameWindow, IApplication
    {
        public Application(int width, int height, string title) 
            : base(GameWindowSettings.Default, new NativeWindowSettings(){ Size = (width, height), Title = title})
        {

        }

        public Vector2 windowSize => this.Size;

        public float deltaTime => (float)this.UpdateTime;

        public KeyboardState kState => this.KeyboardState;

        public MouseState mState => this.MouseState;

        public CursorState cState 
        {
            set => this.CursorState = value;

            get => this.CursorState;
        }

        public bool isWindowFocused => this.IsFocused;

        public void End() => this.Close();

        public void Start(string[] cmds)
        {
            CursorState = CursorState.Grabbed;

            base.Run();
        }

        private SuperArray<Scene> scenes = new SuperArray<Scene>(10);

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.Enable(EnableCap.CullFace);

            scenes.Add(new Sample.SampleScene("s", this, new Transforming.WorldSpaceInfo((100, 100, 100))));

            GL.ClearColor(0.2f, 0.2f, 0.2f, 1);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            foreach(IScene s in scenes)
            {
                s.OnUpdate();
            }            
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            foreach(IScene s in scenes)
            {
                s.OnLateUpdate();
            }

            GL.Flush();

            this.SwapBuffers();
        }
    }

    public interface IApplication
    {
        public Vector2 windowSize { get; }

        public float deltaTime { get; }

        public KeyboardState kState { get; }

        public MouseState mState { get; }

        public CursorState cState { get; set; }

        public bool isWindowFocused { get; }

        public void End();
    }

    namespace Scenes
    {
        using Rendering;

        using Entities;

        using Transforming;

        public interface IScene
        {
            public virtual void OnUpdate(){}

            public virtual void OnLateUpdate(){}
        }

        public class Scene : IScene, IDisposable
        {
            public Scene(string i_name, IApplication i_application, WorldSpaceInfo i_info)
            {
                name = i_name;

                application = i_application;

                camera = new Camera(Vector3.UnitZ * 3, application.windowSize.X / (float)application.windowSize.Y)
                {
                    Fov = 120
                };

                renderer = new Renderer(camera);

                entitySystem = new EntitySystem();

                transformSystem = new TransformSystem(i_info);

                OnStart();
            }

            public readonly string name; 

            protected IApplication application;

            protected IRenderer renderer;

            protected Camera camera;            

            protected EntitySystem entitySystem;

            protected TransformSystem transformSystem;

            protected virtual void OnStart()
            {

            }

            public virtual void OnUpdate()
            {

            }

            public virtual void OnLateUpdate()
            {

            }

            protected virtual void OnEnd()
            {

            }

            private bool disposed;

            public void Dispose()
            {
                if(!disposed)
                {
                    disposed = true;

                    GC.SuppressFinalize(this);                    

                    renderer.Destroy();

                    transformSystem.Dispose();

                    entitySystem.Dispose();
                }
            }

            ~Scene()
            {
                Dispose();
            }            
        }
    }

    namespace AutoThreader
    {

    }
}