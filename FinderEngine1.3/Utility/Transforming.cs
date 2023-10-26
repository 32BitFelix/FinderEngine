using OpenTK.Mathematics;

using SuperArray;

//TODO: Learn quaternions...
namespace Transforming
{
    // Struct to contain the info of the world space
    public struct WorldSpaceInfo
    {
        // Constructor
        public WorldSpaceInfo(Vector3 i_worldBound)
        {
            worldBound = i_worldBound;
        }

        // Vector3 to indicate the world's bounds        
        public readonly Vector3 worldBound;

        // Method to return the default value of the world space info
        public static WorldSpaceInfo Default()
        {
            WorldSpaceInfo nInfo = new WorldSpaceInfo((200, 200, 200));

            return nInfo;
        }
    }

    // System to handle all transforms in the scene
    public unsafe class TransformSystem : IDisposable
    {
        // Constructor
        public TransformSystem(WorldSpaceInfo? i_info)
        {
            // If "i_info" is null, set "info" to the default of WorldSpaceInfo
            // If "i_info" is not null, set "info" to "i_info" 
            info = i_info == null ? WorldSpaceInfo.Default() : (WorldSpaceInfo)i_info;

            // Get the adress of "info"
            fixed(WorldSpaceInfo* ptr = & info)
            {
                // Set "pm_info" to the adress of "info"
                pm_info = ptr;
            }
        }

        // The world space information
        private WorldSpaceInfo info;

        // The master pointer of the worldspaceinfo "info"
        private WorldSpaceInfo* pm_info;

        // SuperArray to hold the transforms
        private readonly SuperArray<Transform> transforms = new SuperArray<Transform>(999);

        // Create a transform
        public ITransform CreateTransform(Vector3 scale, Vector3 rotation, Vector3 translation, int identifier)
        {
            // Check if a transform with thta id exists already
            foreach(ITransformID t in transforms)
            {
                if(t.ID == identifier) throw new ArgumentException("Transform with ID: " + identifier.ToString() + " exists already");
            }

            // Create the new transform
            Transform nTransform = new Transform(scale, rotation, translation, pm_info, identifier);

            // Add the new transform to the "transforms" superarray
            transforms.Add(nTransform);

            // Return the new transform
            return (ITransform)nTransform;
        }        

        // Get a transform with an identifier
        public Transform GetTransform(int identifier)
        {
            // Find the transform with the given id
            // and return it            
            foreach(ITransformID t in transforms)
            {
                if(t.ID == identifier) return t.obj;
            }

            // If transform wasn't found
            throw new IndexOutOfRangeException();
        }

        // Remove a trasform that matches the identifier
        public void RemoveTransform(int identifier)
        {
            // Find the transform with the given id
            // and remove it
            foreach(ITransformID t in transforms)
            {
                if(t.ID == identifier)
                {
                    t.obj.DisposeAsync();

                    transforms.Remove(identifier);
                }
            }

            // If transform wasn't found...
            throw new IndexOutOfRangeException();
        }

        // Bool to indicate if the object has been disposed
        private bool disposed;

        // Method for disposal
        public void Dispose()
        {
            if(!disposed)
            {
                disposed = true;

                // Dereferencing pointer
                pm_info = null;

                // Dispose all transforms
                foreach(IAsyncDisposable t in transforms)
                {
                    t.DisposeAsync();
                }                
            }

        }
    }

    // Interface of the transform 
    public unsafe interface ITransform
    {
        public Vector3 Scale { get; set; }

        public Vector3 Rotation { get; set; }        

        public Vector3 Position { get; set; }

        public Matrix4* GetMatrixPtr { get; }
    }

    public interface ITransformID
    {
        public int ID { get; }

        public Transform obj { get; }
    }

    public unsafe struct Transform : ITransform, ITransformID, IAsyncDisposable
    {
        // Constructor
        public Transform(Vector3 i_scale, Vector3 i_rotation, Vector3 i_translation, WorldSpaceInfo* i_p_info, int i_ID)
        {
            // Disposal
            disposed = false;

            // Identifier
            _ID = i_ID;

            // Scale
            _model = Matrix4.CreateScale(i_scale);

            // Rotation
            _model *= Matrix4.CreateRotationX(i_rotation.X);

            _model *= Matrix4.CreateRotationY(i_rotation.Y);

            _model *= Matrix4.CreateRotationZ(i_rotation.Z);

            // Translation
            _model *= Matrix4.CreateTranslation(i_translation);

            // World Space Info
            p_info = i_p_info;        
        }

        // Callback to this object
        public Transform obj => this;

        // Getter of the id;
        public int ID => _ID;

        // Unique Identifier of the transform
        private readonly int _ID;

        // A pointer pointing to the
        // Scene's worldspace info.
        private WorldSpaceInfo* p_info;

        // Pointer to the child transform
        private Transform* child = null
            , parent = null;

        // the model matrix of the transform
        private Matrix4 _model;

        public Matrix4* GetMatrixPtr 
        {
            get
            {
                fixed(Matrix4* ptr = &_model)
                {
                    return ptr;
                }               
            }
        }

        // Returns or sets the translation component of the matrix
        public Vector3 Position
        {
            set
            {
                // Get the world bounds
                Vector3 bounds = p_info->worldBound;

                // Multiply the new position value with the scale 
                Vector3 nValue = value * _model.ExtractScale();

                // Check if the x coordinates exceed the world bounds
                if((nValue.X < -bounds.X) || (nValue.X > bounds.X)) return;

                // Check if the y coordinates exceed the world bounds
                if((nValue.Y < -bounds.Y) || (nValue.Y > bounds.Y)) return;

                // Check if the z coordinates exceed the world bounds
                if((nValue.Z < -bounds.Z) || (nValue.Z > bounds.Z)) return;

                // Clear translation in matrix
                _model.ClearTranslation();

                // Add new translation
                _model *= Matrix4.CreateTranslation(value);

                // Set the new position of the child
                child->Position = child->Position + value;
            }

            // Get the translation in matrix
            get => _model.ExtractTranslation();
        }

        // Returns or sets the scale of the matrix
        public Vector3 Scale
        {
            set
            {
                // Get the world bounds
                Vector3 bounds = p_info->worldBound;

                // Multiply the new scale with the position
                Vector3 nValue = value * _model.ExtractTranslation();

                // Check if the x coordinates exceed the world bounds
                if((nValue.X < -bounds.X) || (nValue.X > bounds.X)) return;

                // Check if the y coordinates exceed the world bounds
                if((nValue.Y < -bounds.Y) || (nValue.Y > bounds.Y)) return;

                // Check if the z coordinates exceed the world bounds
                if((nValue.Z < -bounds.Z) || (nValue.Z > bounds.Z)) return;

                // Clear scale in matrix
                _model.ClearScale();

                // Add new Scale
                _model *= Matrix4.CreateScale(value);         

                // Set the new scale of the child
                child->Scale = child->Scale + value;
            }

            // Get the scale of the matrix
            get => _model.ExtractScale();
        }

        // Returns or sets the rotation of the matrix
        public Vector3 Rotation
        {
            set
            {
                // Clear rotation in matrix
                _model.ClearRotation();

                // Add new x rotation
                _model *= Matrix4.CreateRotationX(value.X);

                // Add new y rotation
                _model *= Matrix4.CreateRotationY(value.Y);

                // Add new z rotation
                _model *= Matrix4.CreateRotationZ(value.Z);

                // Set the new rotation of the child
                child->Rotation = child->Rotation + value;
            }

            // Get the rotation of the matrix
            get => _model.ExtractRotation().ToEulerAngles();
        }

        // bool to indicate if the object has been disposed
        private bool disposed;

        // Method for disposal
        public ValueTask DisposeAsync()
        {
            if(!disposed)
            {
                disposed = true;

                // Dereference the pointers
                child = null;

                parent = null;

                p_info = null;
            }

            return new ValueTask();
        }
    }
}