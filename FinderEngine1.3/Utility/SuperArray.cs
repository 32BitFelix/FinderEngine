using System.Collections;
using System.Collections.Generic;

namespace SuperArray;

// Homebrew Enumerator struct that handles all types. Feel free to steal
public struct SuperArray<T> : IEnumerable
{
    // Array that holds the data
    private T[] array;

    public int Size => array.Length;

    public SuperArray(int size)
    {
        array = new T[size];

        isDirty = false;
    }

    // Boolean to indicate if the array has been modified
    // (Works only manually as of now)
    public bool isDirty;

    // Adds new data to the array if it has enough space
    public void Add(T value)
    {
        // loop until i reaches the length of the array that stores the data
        for(int i = 0; i < array.Length; i++)
        {
            // if free space has been found...
            if(array[i] == null)
            {
                // put the overloaded value into the free space
                array[i] = value;

                // inicate that the array has been modified
                isDirty = true;

                // end the loop
                break;
            }
        }
    }

    // Clears a space of the array that has the same index as the overloaded integer
    public void Remove(int pos)
    {
        // clear space with the type's default value i.e null (in most cases)
        array[pos] = default;

        for(int i = pos + 1; i < array.Length; i++)
        {
            if(array[i] == null)
            {
                array[i - 1] = default;

                break;
            } 

            array[i - 1] = array[i];
        }
    }

    public T this[int index]
    {
        get
        {
            if (index >= array.Length)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                return array[index];
            }
        }
    }

    // Returns a private enumerator class for enumerating purposes
    IEnumerator IEnumerable.GetEnumerator()
    {
        // return an enumerator which is implemented by ComponentEnum 
        return (IEnumerator) new ComponentEnum<T>(array);
    }

    internal struct ComponentEnum<T> : IEnumerator
    {
        // Initializer of the class
        public ComponentEnum(T[] i_arrayToSearchThrough)
        {
            arrayToSearchThrough = i_arrayToSearchThrough;
        }

        // Reference to the upper classe's array
        private T[] arrayToSearchThrough;

        // Current position of iteration
        private int position = -1;

        // Moves the position up by one and returns a boolean
        public bool MoveNext()
        {
            // move position up by one
            position++;

            // return true, if the position is lower than the array's length
            return position < arrayToSearchThrough.Length && arrayToSearchThrough[position] != null;
        }

        // Resets the position back to -1
        public void Reset()
        {
            position = -1;
        }

        // Returns the current array element of the iteration
        object IEnumerator.Current
        {
            get => Current;
        }

        // safety precaution for IEnumerator.Current
        private T Current
        {
            get
            {
                try
                {
                    return arrayToSearchThrough[position];
                }
                catch(IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}