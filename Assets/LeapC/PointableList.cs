/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/

namespace Leap
{
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Collections.Generic;
    
    /**
   * The PointableList class represents a list of Pointable objects.
   *
   * Get a PointableList object by calling Frame::pointables().
   *
   * \include PointableList_PointableList.txt
   *
   * @since 1.0
   */
    
    public class PointableList : List<Pointable>
    {
        /**
     * Appends the members of the specified PointableList to this PointableList.
     * @param other A PointableList object containing Pointable objects
     * to append to the end of this PointableList.
     * @since 1.0
     */
        public PointableList Append (PointableList other)
        {
            this.InsertRange (this.Count - 1, other);
            return this;
        }

        /**
     * Appends the members of the specified FingerList to this PointableList.
     * @param other A FingerList object containing Finger objects
     * to append to the end of this PointableList.
     * @since 1.0
     */
        public PointableList Append(FingerList other) {
            foreach(Finger finger in other){
                this.Add(finger as Pointable);
            }
            return this;
        }
        
        /**
     * Appends the members of the specified ToolList to this PointableList.
     * @param other A ToolList object containing Tool objects
     * to append to the end of this PointableList.
     * @since 1.0
     */
        public PointableList Append(ToolList other) {
            foreach(Tool tool in other){
                this.Add(tool as Pointable);
            }
            return this;
        }

     /**
     * Returns a new list containing those pointables in the current list that are
     * extended.
     *
     * \include PointableList_extended.txt
     *
     * @returns The list of extended pointables from the current list.
     * @since 2.0
     */
        public PointableList Extended ()
        {
            return (PointableList) this.FindAll (delegate (Pointable pointable) {
                return pointable.IsExtended;
            });
        }
        
        

        
        /**
     * Reports whether the list is empty.
     *
     * \include PointableList_isEmpty.txt
     *
     * @returns True, if the list has no members.
     * @since 1.0
     */
        public bool IsEmpty {
            get {
                return this.Count == 0;
            } 
        }
        
        /**
     * The member of the list that is farthest to the left within the standard
     * Leap Motion frame of reference (i.e has the smallest X coordinate).
     *
     * \include PointableList_leftmost.txt
     *
     * @returns The leftmost pointable, or invalid if list is empty.
     * @since 1.0
     */
        public Pointable Leftmost {
            get {
                Pointable mostest = new Pointable();
                float position = float.MaxValue;
                foreach(Pointable pointable in this){
                    if(pointable.TipPosition.x < position){
                        mostest = pointable;
                        position = pointable.TipPosition.x;
                    }
                }
                return mostest;
            } 
        }
        
        /**
     * The member of the list that is farthest to the right within the standard
     * Leap Motion frame of reference (i.e has the largest X coordinate).
     *
     * \include PointableList_rightmost.txt
     *
     * @returns The rightmost pointable, or invalid if list is empty.
     * @since 1.0
     */
        public Pointable Rightmost {
            get {
                Pointable mostest = new Pointable();
                float position = float.MinValue;
                foreach(Pointable pointable in this){
                    if(pointable.TipPosition.x > position){
                        mostest = pointable;
                        position = pointable.TipPosition.x;
                    }
                }
                return mostest;
            } 
        }
        
        /**
     * The member of the list that is farthest to the front within the standard
     * Leap Motion frame of reference (i.e has the smallest Z coordinate).
     *
     * \include PointableList_frontmost.txt
     *
     * @returns The frontmost pointable, or invalid if list is empty.
     * @since 1.0
     */
        public Pointable Frontmost {
            get {
                Pointable mostest = new Pointable();
                float position = float.MaxValue;
                foreach(Pointable pointable in this){
                    if(pointable.TipPosition.z < position){
                        mostest = pointable;
                        position = pointable.TipPosition.z;
                    }
                }
                return mostest;
            } 
        }
/* IEnumerable implementation -- using List<T> for now
        public int Count{
            get{
                return _count;
            }
        }
        private Pointable _operator_get(int index) {
            Pointable ret = new Pointable();
            return ret;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }

        public IEnumerator<Pointable> GetEnumerator() {
            return new PointableListEnumerator(this);
        }

        public Pointable this[int index] {
            get { return _operator_get(index); }
        }

        private class PointableListEnumerator : System.Collections.Generic.IEnumerator<Pointable> {
            private PointableList _list;
            private int _index;
            public PointableListEnumerator(PointableList list) {
                _list = list;
                _index = -1;
            }
            public void Reset() {
                _index = -1;
            }
            public Pointable Current {
                get {
                    return _list._operator_get(_index);
                }
            }
            object System.Collections.IEnumerator.Current {
                get {
                    return this.Current;
                }
            }
            public bool MoveNext() {
                _index++;
                return (_index < _list.Count);
            }
            public void Dispose() {
                //No cleanup needed
            }
        }
*/
    }
}
