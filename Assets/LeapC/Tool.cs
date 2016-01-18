namespace Leap {

using System;
using System.Runtime.InteropServices;

  /**
   * The Tool class represents a tracked tool.
   *
   * Tools are Pointable objects that the Leap Motion software has classified as a tool.
   *
   * Get valid Tool objects from a Frame object.
   *
   * \image html images/Leap_Tool.png
   *
   * Note that Tool objects can be invalid, which means that they do not contain
   * valid tracking data and do not correspond to a physical tool. Invalid Tool
   * objects can be the result of asking for a Tool object using an ID from an
   * earlier frame when no Tool objects with that ID exist in the current frame.
   * A Tool object created from the Tool constructor is also invalid.
   * Test for validity with the Tool::isValid() function.
   * @since 1.0
   */

public class Tool : Pointable {

    /**
     * Constructs a Tool object.
     *
     * An uninitialized tool is considered invalid.
     * Get valid Tool objects from a Frame object.
     *
     * \include Tool_Tool.txt
     *
     * @since 1.0
     */
  public Tool() {
  }

    /**
     * If the specified Pointable object represents a tool, creates a copy
     * of it as a Tool object; otherwise, creates an invalid Tool object.
     *
     * \include Tool_Tool_copy.txt
     *
     * @since 1.0
     */
  public Tool(Pointable arg0) {
    
  }

    /**
     * A string containing a brief, human readable description of the Tool object.
     *
     * @returns A description of the Tool object as a string.
     * @since 1.0
     */
  public override string ToString() {
    return "Tools not supported.";
  }

/**
     * Returns an invalid Tool object.
     *
     * You can use the instance returned by this function in comparisons testing
     * whether a given Tool instance is valid or invalid. (You can also use the
     * Tool::isValid() function.)
     *
     * \include Tool_invalid.txt
     *
     * @returns The invalid Tool instance.
     * @since 1.0
     */  public new static Tool Invalid {
    get {
      return new Tool();
    } 
  }

}

}
