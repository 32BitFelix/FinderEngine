using OpenTK.Mathematics;

using OpenTK.Graphics.OpenGL4;

using StbImageSharp;

using SuperArray;

using Transforming;

namespace Rendering
{
    public interface IRenderer
    {
        public void Add(GraphicsObject2D val);

        public void Run();

        public void Destroy();
    }

    public class Renderer : IRenderer
    {
        public Renderer(ICamera i_r_cam)
        {
            r_cam = i_r_cam;
        }

        public void Add(GraphicsObject2D val)
        {
            graphics2D.Add(val);
        }

        private void Recreate2D()
        {
            foreach(IGraphicsObject2D g in graphics2D)
            {
                g.Destroy();

                g.Create();
            }
        }

        private readonly SuperArray<IGraphicsObject2D> graphics2D = new SuperArray<IGraphicsObject2D>(999);

        private ICamera r_cam;

        public void Run()
        {
            if(graphics2D.isDirty) Recreate2D();

            foreach(IGraphicsObject2D g in graphics2D)
            {
                g.Render(r_cam.GetViewMatrix(), r_cam.GetProjectionMatrix());
            }
        }

        public void Destroy()
        {
            foreach(IGraphicsObject2D g in graphics2D)
            {
                g.Destroy();
            }
        }
    }

    public interface ICamera
    {
        public Matrix4 GetViewMatrix();

        public Matrix4 GetProjectionMatrix();    
    }

    public class Camera : ICamera
    {
        // Those vectors are directions pointing outwards from the camera to define how it rotated.
        private Vector3 _front = -Vector3.UnitZ;

        private Vector3 _up = Vector3.UnitY;

        private Vector3 _right = Vector3.UnitX;

        private float _speed = 1.5f; 

        // Rotation around the X axis (radians)
        private float _pitch = 0;

        // Rotation around the Y axis (radians)
        private float _yaw = -MathHelper.PiOver2; // Without this, you would be started rotated 90 degrees right.

        // The field of view of the camera (radians)
        private float _fov = MathHelper.PiOver2;

        public Camera(Vector3 position, float aspectRatio)
        {
            Position = position;
            AspectRatio = aspectRatio;
        }

        // The position of the camera
        public Vector3 Position { get; set; }

        // This is simply the aspect ratio of the viewport, used for the projection matrix.
        public float AspectRatio { private get; set; }

        public Vector3 Front => _front;

        public Vector3 Up => _up;

        public Vector3 Right => _right;

        public float Speed => _speed;

        // We convert from degrees to radians as soon as the property is set to improve performance.
        public float Pitch
        {
            get => MathHelper.RadiansToDegrees(_pitch);
            set
            {
                // We clamp the pitch value between -89 and 89 to prevent the camera from going upside down, and a bunch
                // of weird "bugs" when you are using euler angles for rotation.
                // If you want to read more about this you can try researching a topic called gimbal lock
                var angle = MathHelper.Clamp(value, -89f, 89f);
                _pitch = MathHelper.DegreesToRadians(angle);
                UpdateVectors();
            }
        }

        // We convert from degrees to radians as soon as the property is set to improve performance.
        public float Yaw
        {
            get => MathHelper.RadiansToDegrees(_yaw);
            set
            {
                _yaw = MathHelper.DegreesToRadians(value);
                UpdateVectors();
            }
        }

        // The field of view (FOV) is the vertical angle of the camera view.
        // This has been discussed more in depth in a previous tutorial,
        // but in this tutorial, you have also learned how we can use this to simulate a zoom feature.
        // We convert from degrees to radians as soon as the property is set to improve performance.
        public float Fov
        {
            get => MathHelper.RadiansToDegrees(_fov);
            set
            {
                var angle = MathHelper.Clamp(value, 1, 120);
                _fov = MathHelper.DegreesToRadians(angle);
            }
        }

        // Get the view matrix using the amazing LookAt function described more in depth on the web tutorials
        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + _front, _up);
        }

        // Get the projection matrix using the same method we have used up until this point
        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, 0.01f, 100f);
        }

        // This function is going to update the direction vertices using some of the math learned in the web tutorials.
        private void UpdateVectors()
        {
            // First, the front matrix is calculated using some basic trigonometry.
            _front.X = MathF.Cos(_pitch) * MathF.Cos(_yaw);            
            _front.Y = MathF.Sin(_pitch);            
            _front.Z = MathF.Cos(_pitch) * MathF.Sin(_yaw);

            // We need to make sure the vectors are all normalized, as otherwise we would get some funky results.
            _front = Vector3.Normalize(_front);

            // Calculate both the right and the up vector using cross product.
            // Note that we are calculating the right from the global up; this behaviour might
            // not be what you need for all cameras so keep this in mind if you do not want a FPS camera.
            _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
            _up = Vector3.Normalize(Vector3.Cross(_right, _front));
        }
    }

    public interface IGraphicsObject2D
    {
        public void Create();

        public void Render(Matrix4 view, Matrix4 proj);

        public void Destroy();
    }

    public unsafe class GraphicsObject2D : IGraphicsObject2D
    {
        public GraphicsObject2D(ITransform i_transform)
        {
            // Matrix
            _model = i_transform.GetMatrixPtr;

            // Vertex buffer
            vertexBuffer = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.DynamicDraw);

            // Vertex array buffer
            vertexArray = GL.GenVertexArray();

            GL.BindVertexArray(vertexArray);

            // Element buffer
            elementBuffer = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBuffer);

            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.DynamicDraw);

            // Texture
            texture = new Texture2D("Resources/IMG.png");

            // Shader
            shader = new Texture2DShader("Shaders/Vertex/Texture2DShader.vert", "Shaders/Fragment/Texture2DShader.frag");     

            shader.Use();

            shader.SetInt("texture1", 0);

            int posId = shader.GetAttribLocation("aPosition");

            GL.VertexAttribPointer(posId, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            GL.EnableVertexAttribArray(posId);

            int texId = shader.GetAttribLocation("aTexCoord");

            GL.VertexAttribPointer(texId, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

            GL.EnableVertexAttribArray(texId);             
        }

        public void Create()
        {
            // Vertex buffer
            vertexBuffer = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.DynamicDraw);

            // Vertex array buffer
            vertexArray = GL.GenVertexArray();

            GL.BindVertexArray(vertexArray);

            // Element buffer
            elementBuffer = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBuffer);

            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.DynamicDraw);

            // Texture
            texture = new Texture2D("Resources/IMG.png");

            // Shader
            shader = new Texture2DShader("Shaders/Vertex/Texture2DShader.vert", "Shaders/Fragment/Texture2DShader.frag");   

            shader.Use();

            shader.SetInt("texture1", 0);

            int posId = shader.GetAttribLocation("aPosition");

            GL.VertexAttribPointer(posId, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            GL.EnableVertexAttribArray(posId);

            int texId = shader.GetAttribLocation("aTexCoord");

            GL.VertexAttribPointer(texId, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

            GL.EnableVertexAttribArray(texId);   
        }

        private int vertexArray = 0;

        private int vertexBuffer = 0;

        private int elementBuffer = 0;        

        private float[] _vertices = 
        {
            // FRONT
            -1, -1, 1, 0, 0, // Bottom front left
            1, -1, 1, 1, 0, // Bottom front right
            1, 1, 1, 1, 1, // Top front right
            -1, 1, 1, 0, 1, // Top front left

            // BACK
            -1, -1, -1, 0, 0, // Bottom back left
            1, -1, -1, 1, 0, // Bottom back right
            1, 1, -1, 1, 1, // Top back right
            -1, 1, -1, 0, 1,// Top back left

            // TOP
            -1, 1, 1, 0, 0, // Top front left
            1, 1, 1, 1, 0, // Top front right
            1, 1, -1, 1, 1, // Top back right
            -1, 1, -1, 0, 1, // Top back left

            // BOTTOM
            -1, -1, 1, 0, 0, // Bottom front left
            1, -1, 1, 1, 0, // Bottom front right
            1, -1, -1, 1, 1, // Bottom back right
            -1, -1, -1, 0, 1, // Bottom back left         

            // LEFT
            -1, -1, -1, 0, 0, // Bottom back left
            -1, -1, 1, 1, 0, // Bottom front left
            -1, 1, -1, 0, 1, // Top back left
            -1, 1, 1, 1, 1,// Top front left

            // RIGHT
            1, -1, -1, 0, 0, // Bottom back left
            1, -1, 1, 1, 0, // Bottom front left
            1, 1, -1, 0, 1, // Top back left
            1, 1, 1, 1, 1,// Top front left            
        };

        private uint[] _indices = 
        {
            0, 1, 2,
            0, 2, 3,

            5, 4, 7,
            5, 7, 6,

            3, 2, 6,
            3, 6, 7,

            4, 5, 1,
            4, 1, 0,

            4, 0, 3,
            4, 3, 7,

            1, 5, 6,
            1, 6, 2
        };

        private ITexture2D texture;

        private ITexture2DShader shader;

        private Matrix4* _model;

        public Matrix4 Model => *_model;

        public void Render(Matrix4 view, Matrix4 proj)
        {
            Console.WriteLine(_model->ExtractScale());

            GL.BindVertexArray(vertexArray);

            shader.Use();   

            shader.SetMat4("aProjection", proj);

            shader.SetMat4("aView", view);

            shader.SetMat4("aModel", *_model);    

            texture.Use();                       

            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);   
        }

        public void Destroy()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.DeleteBuffer(vertexBuffer);

            GL.DeleteBuffer(vertexArray);

            GL.DeleteBuffer(elementBuffer);

            texture.Destroy();

            shader.Destroy();
        }
    }

    public interface ITexture2D
    {
        public void Use(TextureUnit unit = TextureUnit.Texture0);

        public void Destroy();
    }

    public struct Texture2D : ITexture2D
    {
        public int Handle;

        public Texture2D(string texturePath)
        {
            // Generate a texture object
            Handle = GL.GenTexture();

            Use();

            // Set the deserialized image coordinate norms to the same as opengl's
            StbImage.stbi_set_flip_vertically_on_load(1);

            // Load the image
            ImageResult image = ImageResult.FromStream(File.OpenRead(texturePath), ColorComponents.RedGreenBlueAlpha);

            // Upload the loaded image to the gl context
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
            
            // Setup texture parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            // Generate mipmaps for the targeted texture
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        public void Use(TextureUnit unit = TextureUnit.Texture0)
        {
            // Activate texture unit
            GL.ActiveTexture(unit);
            // Focus on given texture
            GL.BindTexture(TextureTarget.Texture2D, Handle);
        }

        public void Destroy()
        {
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.DeleteTexture(Handle);
        }
    }

    public interface ITexture2DShader
    {
        public void Use();

        public void SetInt(string uniformName, int value);

        public void SetVec4(string uniformName, Vector4 value);

        public void SetMat4(string uniformName, Matrix4 value);

        public int GetAttribLocation(string attribName);

        public void Destroy();
    }

    public struct Texture2DShader : ITexture2DShader
    {
        public int Handle;

        public Texture2DShader(string vertexPath, string fragmentPath)
        {
            // Find source code of vertex shader
            string vertexShaderSource = File.ReadAllText(vertexPath);

            // Find source code of fragment shader
            string fragementShaderSource = File.ReadAllText(fragmentPath);

            // Create a shader object and get it's id
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            // Appends the source code to the vertex shader
            GL.ShaderSource(vertexShader, vertexShaderSource);

            // Create a shader object and get it's id
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            // Appends the source code to the fragment shader
            GL.ShaderSource(fragmentShader, fragementShaderSource);   

            // Compile the vertex shader
            GL.CompileShader(vertexShader);     

            // Get parameter from vertex shader. In this case its it's compile status
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int vertSuccess);
            // If compilation failed log a message
            if(vertSuccess == 0)
            {
                // Log the info log of the fragment shader
                Console.WriteLine(GL.GetShaderInfoLog(vertexShader));
            }

            // Compile the vertex shader
            GL.CompileShader(fragmentShader);     

            // Get parameter from vertex shader. In this case its it's compile status
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int fragSuccess);
            // If compilation failed log a message
            if(fragSuccess == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                // Log the info log of the fragment shader
                Console.WriteLine(GL.GetShaderInfoLog(fragmentShader));

                Console.ForegroundColor = ConsoleColor.White;
            }        

            // Create a shader object and get it's id
            Handle = GL.CreateProgram();

            // Attach vertex shader to the shader object
            GL.AttachShader(Handle, vertexShader);
            // Attach fragment shader to the shader object
            GL.AttachShader(Handle, fragmentShader);

            // Link shader object to gpu's program query
            GL.LinkProgram(Handle);        

            // Get parameter from shader object. In this case its it's link status
            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int shaderSuccess);
            // If shader object linking failed, log a message
            if(shaderSuccess == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                // Log the info log of the shader object
                Console.WriteLine(GL.GetProgramInfoLog(Handle));

                Console.ForegroundColor = ConsoleColor.White;
            }

            // Detach the shaders from the program,
            // as their compiled data have been
            // transferred to the shader object
            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            // Finally, delete the shaders
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        // Runs the shader program upon call
        public void Use()
        {
            GL.UseProgram(Handle);
        }

        // Set one of the shader's uniforms
        public void SetInt(string uniformName, int value)
        {
            int location = GL.GetUniformLocation(Handle, uniformName);

            GL.Uniform1(location, value);
        }

        public void SetVec4(string uniformName, Vector4 value)
        {
            int location = GL.GetUniformLocation(Handle, uniformName);

            GL.Uniform4(location, value.X, value.Y, value.Z, value.W);
        }

        public void SetMat4(string uniformName, Matrix4 value)
        {
            int location = GL.GetUniformLocation(Handle, uniformName);

            GL.UniformMatrix4(location, true, ref value);
        }

        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }

        // A boolean to indicate if the class has been disposed already
        private bool disposed = false;

        public void Destroy()
        {
            if(!disposed)
            {
                // Delete the shader object
                GL.DeleteProgram(Handle);

                disposed = true;
            }
        }
    }
}