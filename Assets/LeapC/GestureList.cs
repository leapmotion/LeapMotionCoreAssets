namespace Leap {
    
    using System;
    using System.Runtime.InteropServices;
    using System.Collections.Generic;
    
    /**
   * The GestureList class represents a list of Gesture objects.
   *
   * Get a GestureList object by calling Controller::gestures().
   * @since 1.0
   */
    
    public class GestureList : List<Gesture> {
        
        ~GestureList() {
        }
        
        
        /**
     * Constructs an empty list of gestures.
     * @since 1.0
     */
        public GestureList() {
        }
        
        
        /**
     * Appends the members of the specified GestureList to this GestureList.
     * @param other A GestureList object containing Gesture objects
     * to append to the end of this GestureList.
     * @since 1.0
     */
        public GestureList Append(GestureList other) {
            this.InsertRange(this.Count - 1, other);
            return this;
        }
        
        /**
     * Reports whether the list is empty.
     *
     * \include GestureList_isEmpty.txt
     *
     * @returns True, if the list has no members.
     * @since 1.0
     */  public bool IsEmpty {
            get {
                return this.Count == 0;
            } 
        }
        
    }
    
}
