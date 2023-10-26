
using FinderEngine;
using FinderEngine.Scenes;

using OpenTK.Windowing.GraphicsLibraryFramework;

using OpenTK.Mathematics;

using Rendering;

using Transforming;
using Entities;

namespace Sample
{
    public sealed class SampleScene : Scene
    {
        public SampleScene(string i_name, IApplication i_application, WorldSpaceInfo i_info) : base(i_name, i_application, i_info)
        {

        }

        ITransform gObj;

        Entity entity;

        protected override void OnStart()
        {
            base.OnStart();

            entity = entitySystem.CreateEntity("box");

            gObj = transformSystem.CreateTransform((1, 1, 1), (0, 0, 0), (0, 0, 0), entity.GetHashCode());

            renderer.Add(new GraphicsObject2D(gObj));
        }

        public override void OnUpdate()
        {
            KeyboardState input = application.kState;

            if(!application.isWindowFocused) return;

            if(input.IsKeyDown(Keys.Escape)) application.End();

            float multi = (input.IsKeyDown(Keys.LeftShift) ? 2 : 1) * application.deltaTime;

            if(input.IsKeyDown(Keys.W)) camera.Position += camera.Front * camera.Speed * multi;

            if(input.IsKeyDown(Keys.S)) camera.Position -= camera.Front * camera.Speed * multi;

            if(input.IsKeyDown(Keys.A)) camera.Position -= Vector3.Normalize(Vector3.Cross(camera.Front, camera.Up)) * camera.Speed * multi;

            if(input.IsKeyDown(Keys.D)) camera.Position += Vector3.Normalize(Vector3.Cross(camera.Front, camera.Up)) * camera.Speed * multi;

            if(input.IsKeyDown(Keys.Space)) camera.Position += Vector3.UnitY * camera.Speed * multi;

            if(input.IsKeyDown(Keys.LeftControl)) camera.Position -= Vector3.UnitY * camera.Speed * multi ;

            MouseState m = application.mState;   

            camera.Yaw += m.Delta.X * application.deltaTime * 500;

            camera.Pitch = Math.Clamp(camera.Pitch - m.Delta.Y * application.deltaTime * 500, -90.0f, 90.0f);
        }

        public override void OnLateUpdate()
        {
            base.OnLateUpdate();

            renderer.Run();
        }
    }

}