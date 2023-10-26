using SuperArray;

namespace Entities
{
    public class EntitySystem : IDisposable
    {
        public EntitySystem()
        {

        }

        private readonly SuperArray<Entity> entities = new SuperArray<Entity>(999);

        public Entity CreateEntity(string name)
        {
            Entity nEntity = new Entity(name);

            entities.Add(nEntity);

            return nEntity;
        }

        public Entity GetEntityByName(string name)
        {
            foreach(Entity e in entities)
            {
                if(e.name == name) return e;
            }

            throw new IndexOutOfRangeException();
        }

        public Entity GetEntityByHash(int identifier)
        {
            foreach(Entity e in entities)
            {
                if(e.GetHashCode() == identifier) return e;
            }

            throw new IndexOutOfRangeException();
        }

        public void RemoveEntityByName(string name)
        {
            for(int i = 0; i < entities.Size; i++)
            {
                if(entities[i].name == name)
                {
                    entities.Remove(i);

                    return;
                } 
            }

            throw new IndexOutOfRangeException();
        }

        public void RemoveEntityByHash(int hash)
        {
            for(int i = 0; i < entities.Size; i++)
            {
                if(entities[i].GetHashCode() == hash)
                {
                    entities.Remove(i);

                    return;
                } 
            }

            throw new IndexOutOfRangeException();            
        }

        private bool disposed;

        public void Dispose()
        {
            if(!disposed)
            {
                disposed = true;

                for(int i = 0; i < entities.Size; i++)
                {
                    entities.Remove(i);
                }
            }
        }
    }

    public struct Entity
    {
        public Entity(string i_name)
        {
            name = i_name;
        }

        public string name;
    }
}