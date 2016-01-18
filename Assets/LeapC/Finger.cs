namespace Leap
{
    using System;
    using System.Runtime.InteropServices;

    /**
   * The Finger class represents a tracked finger.
   *
   * Fingers are Pointable objects that the Leap Motion software has classified as a finger.
   * Get valid Finger objects from a Frame or a Hand object.
   *
   * Fingers may be permanently associated to a hand. In this case the angular order of the finger IDs
   * will be invariant. As fingers move in and out of view it is possible for the guessed ID
   * of a finger to be incorrect. Consequently, it may be necessary for finger IDs to be
   * exchanged. All tracked properties, such as velocity, will remain continuous in the API.
   * However, quantities that are derived from the API output (such as a history of positions)
   * will be discontinuous unless they have a corresponding ID exchange.
   *
   * Note that Finger objects can be invalid, which means that they do not contain
   * valid tracking data and do not correspond to a physical finger. Invalid Finger
   * objects can be the result of asking for a Finger object using an ID from an
   * earlier frame when no Finger objects with that ID exist in the current frame.
   * A Finger object created from the Finger constructor is also invalid.
   * Test for validity with the Finger::isValid() function.
   * @since 1.0
   */

    public class Finger : Pointable
    {
        Bone[] _bones = new Bone[4];
        FingerType _type = FingerType.TYPE_UNKNOWN;

        /**
     * Constructs a Finger object.
     *
     * An uninitialized finger is considered invalid.
     * Get valid Finger objects from a Frame or a Hand object.
     * @since 1.0
     */
        public Finger ()
        {
        }

        public Finger (Frame frame, 
                           int handId, 
                           int fingerId,
                           float timeVisible,
                           Vector tipPosition,
                           Vector tipVelocity,
                           Vector direction,
                           Vector stabilizedTipPosition,
                           float width,
                           float length,
                           bool isExtended,
                           Finger.FingerType type,
                           Bone metacarpal,
                           Bone proximal,
                           Bone intermediate,
                           Bone distal) 
            : base( frame, 
                    handId,
                    fingerId,
                    timeVisible,
                    tipPosition,
                    tipVelocity,
                    direction,
                    stabilizedTipPosition,
                    width,
                    length,
                    isExtended,
                    type)
        {
            _type = type;
            _bones [0] = metacarpal;
            _bones [1] = proximal;
            _bones [2] = intermediate;
            _bones [3] = distal;
        }
        /**
     * If the specified Pointable object represents a finger, creates a copy
     * of it as a Finger object; otherwise, creates an invalid Finger object.
     *
     * \include Finger_Finger.txt
     *
     * @since 1.0
     */
        public Finger (Pointable pointable)
        {

        }

        /**
     * Deprecated as of version 2.0
     * Use 'bone' method instead.
     */
        public Vector JointPosition (Finger.FingerJoint jointIx)
        {
            switch (jointIx){
                case FingerJoint.JOINT_MCP:
                    return _bones[0].NextJoint;
                case FingerJoint.JOINT_PIP:
                    return _bones[1].NextJoint;
                case FingerJoint.JOINT_DIP:
                    return _bones[2].NextJoint;
                case FingerJoint.JOINT_TIP:
                    return _bones[3].NextJoint;
            }
            return Vector.Zero;
        }

        /**
     * The bone at a given bone index on this finger.
     *
     * \include Bone_iteration.txt
     *
     * @param boneIx An index value from the Bone::Type enumeration identifying the
     * bone of interest.
     * @returns The Bone that has the specified bone type.
     * @since 2.0
     */
        public Bone Bone (Bone.BoneType boneIx)
        {
            return _bones[(int)boneIx];
        }

        /**
     * A string containing a brief, human readable description of the Finger object.
     *
     * \include Finger_toString.txt
     *
     * @returns A description of the Finger object as a string.
     * @since 1.0
     */
        public override string ToString ()
        {
            return Enum.GetName(typeof(FingerType), Type) + " id:" + Id;
        }

/**
     * The name of this finger.
     *
     * \include Finger_type.txt
     *
     * @returns The anatomical type of this finger as a member of the Finger::Type
     * enumeration.
     * @since 2.0
     */
        public Finger.FingerType Type {
            get {
                return _type;
            } 
        }

/**
     * Returns an invalid Finger object.
     *
     * You can use the instance returned by this function in comparisons testing
     * whether a given Finger instance is valid or invalid. (You can also use the
     * Finger::isValid() function.)
     *
     * \include Finger_invalid.txt
     *
     * @returns The invalid Finger instance.
     * @since 1.0
     */
        public new static Finger Invalid {
            get {
                return new Finger ();
            } 
        }

        /**
       * Deprecated as of version 2.0
       */
        public enum FingerJoint
        {
            JOINT_MCP = 0,
            JOINT_PIP = 1,
            JOINT_DIP = 2,
            JOINT_TIP = 3
        }

        /**
       * Enumerates the names of the fingers.
       *
       * Members of this enumeration are returned by Finger::type() to identify a
       * Finger object.
       * @since 2.0
       */
        public enum FingerType
        {
            TYPE_THUMB = 0,
            TYPE_INDEX = 1,
            TYPE_MIDDLE = 2,
            TYPE_RING = 3,
            /** The pinky or little finger 
  */
            TYPE_PINKY = 4,
            TYPE_UNKNOWN = -1
        }

    }

}
