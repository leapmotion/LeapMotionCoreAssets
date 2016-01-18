namespace Leap
{
    using System;
    using System.Runtime.InteropServices;
    using System.Collections.Generic;
    
    /**
   * The ToolList class represents a list of Tool objects.
   *
   * Get a ToolList object by calling Frame::tools().
   *
   * \include ToolList_ToolList.txt
   *
   * @since 1.0
   */
    
    public class ToolList : List<Tool>
    {
        
        /**
     * Appends the members of the specified ToolList to this ToolList.
     * @param other A ToolList object containing Tool objects
     * to append to the end of this ToolList.
     * @since 1.0
     */
        public ToolList Append (ToolList other)
        {
            this.InsertRange (this.Count - 1, other);
            return this;
        }
        
        /**
     * Returns a new list containing those tools in the current list that are
     * extended.
     *
     * \include ToolList_extended.txt
     *
     * @returns The list of extended tools from the current list.
     * @since 2.0
     */
        public ToolList Extended ()
        {
            return (ToolList) this.FindAll (delegate (Tool tool) {
                return tool.IsExtended;
            });
        }

        
        
        /**
     * Reports whether the list is empty.
     *
     * \include ToolList_isEmpty.txt
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
     * \include ToolList_leftmost.txt
     *
     * @returns The leftmost tool, or invalid if list is empty.
     * @since 1.0
     */
        public Tool Leftmost {
            get {
                Tool mostest = new Tool();
                float position = float.MaxValue;
                foreach(Tool tool in this){
                    if(tool.TipPosition.x < position){
                        mostest = tool;
                        position = tool.TipPosition.x;
                    }
                }
                return mostest;
            } 
        }
        
        /**
     * The member of the list that is farthest to the right within the standard
     * Leap Motion frame of reference (i.e has the largest X coordinate).
     *
     * \include ToolList_rightmost.txt
     *
     * @returns The rightmost tool, or invalid if list is empty.
     * @since 1.0
     */
        public Tool Rightmost {
            get {
                Tool mostest = new Tool();
                float position = float.MinValue;
                foreach(Tool tool in this){
                    if(tool.TipPosition.x > position){
                        mostest = tool;
                        position = tool.TipPosition.x;
                    }
                }
                return mostest;
            } 
        }
        
        /**
     * The member of the list that is farthest to the front within the standard
     * Leap Motion frame of reference (i.e has the smallest Z coordinate).
     *
     * \include ToolList_frontmost.txt
     *
     * @returns The frontmost tool, or invalid if list is empty.
     * @since 1.0
     */
        public Tool Frontmost {
            get {
                Tool mostest = new Tool();
                float position = float.MaxValue;
                foreach(Tool tool in this){
                    if(tool.TipPosition.z < position){
                        mostest = tool;
                        position = tool.TipPosition.z;
                    }
                }
                return mostest;
            } 
        }
        
    }
    
}
