using System;

namespace LeapInternal
{
    //TODO ensure thread safety
    public class ObjectPool<T> where T : PooledObject, new()
    {
        private T[] pool; //the pooled objects
        private UInt64 age = 0;
        private bool _preallocate = false;
        public int InUse{get; private set;}
        public bool Growable{get; set;}
        private const double _growRate = 1.5;

        /**
         * If Growable is true, then Capacity is only the **current** 
         * size of the underlying memory store. 
         */
        public int Capacity{
            get{
                return pool.Length;
            } 
        }

        public ObjectPool(int initialCapacity, bool preallocate = false, bool growable = false)
        {
            this.pool = new T[initialCapacity];
//            _preallocate = preallocate; 
//            if(_preallocate){
//                for(uint p = 0; p < initialCapacity; p++){
//                    addItem(p);
//                }
//            }
            this.Growable = growable;
            this.InUse = 0;
        }

        private void addItem(uint index){
            this.pool[index] = new T();
            this.pool[index].poolIndex = index;
            this.pool[index].age = 0;
        }

        public T CheckOut(){
            UInt64 eldest = UInt64.MaxValue;
            uint indexToUse = 0;
            bool freeObjectFound = false;
            for(uint p = 0; p < Capacity; p++){
                if(this.pool[p] == null || this.pool[p].age == 0){
                    indexToUse = p;
                    freeObjectFound = true;
                    InUse++;
                    break;
                }
                if(this.pool[p].age < eldest){
                    eldest = this.pool[p].age;
                    indexToUse = p;
                }
            }
            if(!freeObjectFound){
                if(Growable){
                    indexToUse = (uint)pool.Length;
                    expand();
                    InUse++;
                }
            } //else recycle existing object

            if(this.pool[indexToUse] == null)
                this.pool[indexToUse] = new T(); // if non-preallocated
            
            this.pool[indexToUse].poolIndex = indexToUse;
            this.pool[indexToUse].age = ++age;

            return this.pool[indexToUse];
        }

        public void CheckIn(T pooledObject){
            pooledObject.age = 0;
            InUse--;
        }

        public T FindByPoolIndex(UInt64 index){
            for(int e = 0; e < this.pool.Length; e++){
                T item = this.pool[e];
                if(item != null && item.poolIndex == index && item.age > 0)
                    return item;
            }
            return null;
        }

        private void expand(){
            int newSize = (int)Math.Floor(Capacity * _growRate);
            T[] newPool = new T[newSize];
            uint m = 0;
            for(; m < this.pool.Length; m++){
                newPool[m] = this.pool[m];
            }
            this.pool = newPool;
//            if(_preallocate){
//                for(; m < this.pool.Length; m++){
//                    addItem(m);
//                }
//            }
        }
    }
}

