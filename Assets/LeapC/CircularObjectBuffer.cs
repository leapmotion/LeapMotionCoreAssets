using System;
using System.Threading;

namespace LeapInternal
{
    //TODO ensure thread safety

    /** 
     * A Limited capacity, circular LIFO buffer that wraps around 
     * when full. Supports indexing to get older items. Array-backed.
     * *
     * Unlike many collections, objects are never removed, just overwritten when
     * the buffer cycles back to their array location.
     * 
     * Object types used must have default parameterless constructor. It should be obvious that
     * such default objects are invalid. I.e. for Leap API objects, the IsValid property should be false.
     */
    public class CircularObjectBuffer<T> where T : new()
    {
        private T[] array;
        private int current = 0;
        public int Count{get; private set;}
        public int Capacity{get; private set;}
        public bool IsEmpty{get; private set;}

        public CircularObjectBuffer(int capacity)
        {
            this.Capacity = capacity;
            this.array = new T[this.Capacity];
            this.current = 0;
            this.Count = 0;
            this.IsEmpty = true;
        }

        /** Put an item at the head of the list. Once full, this will overwrite the oldest item. */
        public virtual void Put(T item){
            if(!IsEmpty){
                current++;
                if(current >= Capacity){
                    current = 0;
                }
            }
            if(Count < Capacity)
                Count++;

            lock(array){
                array[current] = item;
            }
            IsEmpty = false;
        }

        /** Get the item indexed backward from the head of the list */
        public T Get(int index = 0){
            if(IsEmpty || (index > Count - 1))
                return new T(); //default(T);
            int effectiveIndex = current - index;
            if(effectiveIndex < 0)
                effectiveIndex += Capacity;
            return array[effectiveIndex];
        }

        /** Increase  */
        public void Resize(int newCapacity){
            if(newCapacity <= Capacity){
                return;
            }
            
            T[] newArray = new T[newCapacity];
            int j = 0;
            for(int i = Count - 1; i >= 0; i--){
                newArray[j++] = this.Get (i);
            }
            this.array = newArray;
            this.Capacity = newCapacity;
        }
    }
}

