namespace Leap
{
    using System;
    using System.Runtime.InteropServices;

    /**
   * The Pointable class reports the physical characteristics of a detected finger or tool.
   *
   * Both fingers and tools are classified as Pointable objects. Use the Pointable::isFinger()
   * function to determine whether a Pointable object represents a finger. Use the
   * Pointable::isTool() function to determine whether a Pointable object represents a tool.
   * The Leap Motion software classifies a detected entity as a tool when it is thinner, straighter, and longer
   * than a typical finger.
   *
   * \include Pointable_Get_Basic.txt
   *
   * To provide touch emulation, the Leap Motion software associates a floating touch
   * plane that adapts to the user's finger movement and hand posture. The Leap Motion
   * interprets purposeful movements toward this plane as potential touch points.
   * The Pointable class reports
   * touch state with the touchZone and touchDistance values.
   *
   * Note that Pointable objects can be invalid, which means that they do not contain
   * valid tracking data and do not correspond to a physical entity. Invalid Pointable
   * objects can be the result of asking for a Pointable object using an ID from an
   * earlier frame when no Pointable objects with that ID exist in the current frame.
   * A Pointable object created from the Pointable constructor is also invalid.
   * Test for validity with the Pointable::isValid() function.
   *
   * @since 1.0
   */

    public class Pointable
    {
        int _id = 0;
        int _handID = 0;
        Vector _tipPosition;
        Vector _tipVelocity;
        Vector _direction;
        float _width = 0;
        float _length = 0;
        bool _isTool = false;
        bool _isFinger = false;
        bool _isExtended = false;
        bool _isValid = false;
        Pointable.Zone _zone = Pointable.Zone.ZONE_NONE;
        float _touchDistance = float.PositiveInfinity;
        Vector _stabilizedTipPosition;
        float _timeVisible = 0;

        /**
     * Constructs a Pointable object.
     *
     * An uninitialized pointable is considered invalid.
     * Get valid Pointable objects from a Frame or a Hand object.
     *
     * \include Pointable_Pointable.txt
     *
     * @since 1.0
     */
        public Pointable ()
        {
             _tipPosition = Vector.Zero;
             _tipVelocity = Vector.Zero;
             _direction = Vector.Zero;
             _stabilizedTipPosition = Vector.Zero;

        }

        //Note: Add a new constructor when tools are added to LeapC
        public Pointable(int handId,
                         int fingerId,
                         float timeVisible,
                         Vector tipPosition,
                         Vector tipVelocity,
                         Vector direction,
                         Vector stabilizedTipPosition,
                         float width,
                         float length,
                         bool isExtended,
                         Finger.FingerType type)
        {
             _id = (handId * 10) + fingerId;
             _handID = handId;
             _tipPosition = tipPosition;
             _tipVelocity = tipVelocity;
             _direction = direction;
             _width = width;
             _length = length;
             _isTool = false;
             _isFinger = true;
             _isExtended = isExtended;
             _isValid = false;
             _zone = Pointable.Zone.ZONE_NONE; //not implemented
            _touchDistance = 1.0f; //not implemented
             _stabilizedTipPosition = stabilizedTipPosition;
             _timeVisible = timeVisible;
        }

        /**
     * Compare Pointable object equality.
     *
     * \include Pointable_operator_equals.txt
     *
     * Two Pointable objects are equal if and only if both Pointable objects represent the
     * exact same physical entities in the same frame and both Pointable objects are valid.
     * @since 1.0
     */
        public bool Equals (Pointable other)
        {
            return this.IsValid && 
                other.IsValid &&
                this.Id == other.Id; //Fox - && this.Hand.Id == other.Hand.Id && this.Frame.Id == other.Frame.Id;
        }

        /**
     * A string containing a brief, human readable description of the Pointable object.
     *
     * @returns A description of the Pointable object as a string.
     * @since 1.0
     */
        public override string ToString ()
        {
            return "Pointable " + this.Id + (IsFinger ? " is a finger." : " is a tool.");
        }

/**
     * A unique ID assigned to this Pointable object, whose value remains the
     * same across consecutive frames while the tracked finger or tool remains
     * visible. If tracking is lost (for example, when a finger is occluded by
     * another finger or when it is withdrawn from the Leap Motion Controller field of view), the
     * Leap Motion software may assign a new ID when it detects the entity in a future frame.
     *
     * \include Pointable_id.txt
     *
     * Use the ID value with the Frame::pointable() function to find this
     * Pointable object in future frames.
     *
     * IDs should be from 1 to 100 (inclusive). If more than 100 objects are tracked
     * an IDs of -1 will be used until an ID in the defined range is available.
     *
     * @returns The ID assigned to this Pointable object.
     * @since 1.0
     */
        public int Id {
            get {
                return _id;
            } 
        }

/**
     * The Hand associated with a finger.
     *
     * \include Pointable_hand.txt
     *
     * Not that in version 2+, tools are not associated with hands. For
     * tools, this function always returns an invalid Hand object.
     *
     * @returns The associated Hand object, if available; otherwise,
     * an invalid Hand object is returned.
     * @since 1.0
     */
      //Fox  - removed along with backref to Frame
        //public Hand Hand {
        //    get {
        //        return _frame.Hand(_handID);
        //    } 
        //}

        public int HandId{
            get{
                return _handID;
            }
        }
/**
     * The tip position in millimeters from the Leap Motion origin.
     *
     * \include Pointable_tipPosition.txt
     *
     * @returns The Vector containing the coordinates of the tip position.
     * @since 1.0
     */
        public Vector TipPosition {
            get {
                return _tipPosition;
            } 
        }

/**
     * The rate of change of the tip position in millimeters/second.
     *
     * \include Pointable_tipVelocity.txt
     *
     * @returns The Vector containing the coordinates of the tip velocity.
     * @since 1.0
     */
        public Vector TipVelocity {
            get {
                return _tipVelocity;
            } 
        }

/**
     * The direction in which this finger or tool is pointing.
     *
     * \include Pointable_direction.txt
     *
     * The direction is expressed as a unit vector pointing in the same
     * direction as the tip.
     *
     * \image html images/Leap_Finger_Model.png
     *
     * @returns The Vector pointing in the same direction as the tip of this
     * Pointable object.
     * @since 1.0
     */
        public Vector Direction {
            get {
                return _direction;
            } 
        }

/**
     * The estimated width of the finger or tool in millimeters.
     *
     * \include Pointable_width.txt
     *
     * @returns The estimated width of this Pointable object.
     * @since 1.0
     */
        public float Width {
            get {
                return _width;
            } 
        }

/**
     * The estimated length of the finger or tool in millimeters.
     *
     * \include Pointable_length.txt
     *
     * @returns The estimated length of this Pointable object.
     * @since 1.0
     */
        public float Length {
            get {
                return _length;
            } 
        }

/**
     * Whether or not this Pointable is classified as a tool.
     *
     * \include Pointable_Conversion.txt
     *
     * @returns True, if this Pointable is classified as a tool.
     * @since 1.0
     */
        public bool IsTool {
            get {
                return _isTool;
            } 
        }

/**
     * Whether or not this Pointable is classified as a finger.
     *
     * \include Pointable_Conversion.txt
     *
     * @returns True, if this Pointable is classified as a finger.
     * @since 1.0
     */
        public bool IsFinger {
            get {
                return _isFinger;
            } 
        }

/**
     * Whether or not this Pointable is in an extended posture.
     *
     * A finger is considered extended if it is extended straight from the hand as if
     * pointing. A finger is not extended when it is bent down and curled towards the
     * palm.  Tools are always extended.
     *
     * \include Finger_isExtended.txt
     *
     * @returns True, if the pointable is extended.
     * @since 2.0
     */
        public bool IsExtended {
            get {
                return _isExtended;
            } 
        }

/**
     * Reports whether this is a valid Pointable object.
     *
     * \include Pointable_isValid.txt
     *
     * @returns True, if this Pointable object contains valid tracking data.
     * @since 1.0
     */
        public bool IsValid {
            get {
                return _isValid;
            } 
        }

/**
     * The current touch zone of this Pointable object.
     *
     * The Leap Motion software computes the touch zone based on a floating touch
     * plane that adapts to the user's finger movement and hand posture. The Leap
     * Motion software interprets purposeful movements toward this plane as potential touch
     * points. When a Pointable moves close to the adaptive touch plane, it enters the
     * "hovering" zone. When a Pointable reaches or passes through the plane, it enters
     * the "touching" zone.
     *
     * The possible states are present in the Zone enum of this class:
     *
     * **Zone.NONE** -- The Pointable is outside the hovering zone.
     *
     * **Zone.HOVERING** -- The Pointable is close to, but not touching the touch plane.
     *
     * **Zone.TOUCHING** -- The Pointable has penetrated the touch plane.
     *
     * The touchDistance value provides a normalized indication of the distance to
     * the touch plane when the Pointable is in the hovering or touching zones.
     *
     * \include Pointable_touchZone.txt
     *
     * @returns The touch zone of this Pointable
     * @since 1.0
     */
        public Pointable.Zone TouchZone {
            get {
                //TODO deprecate touch zone and touch distance
                return _zone; // Not implemented
            } 
        }

/**
     * A value proportional to the distance between this Pointable object and the
     * adaptive touch plane.
     *
     * \image html images/Leap_Touch_Plane.png
     *
     * The touch distance is a value in the range [-1, 1]. The value 1.0 indicates the
     * Pointable is at the far edge of the hovering zone. The value 0 indicates the
     * Pointable is just entering the touching zone. A value of -1.0 indicates the
     * Pointable is firmly within the touching zone. Values in between are
     * proportional to the distance from the plane. Thus, the touchDistance of 0.5
     * indicates that the Pointable is halfway into the hovering zone.
     *
     * \include Pointable_touchDistance.txt
     *
     * You can use the touchDistance value to modulate visual feedback given to the
     * user as their fingers close in on a touch target, such as a button.
     *
     * @returns The normalized touch distance of this Pointable object.
     * @since 1.0
     */
        public float TouchDistance {
            get {
                return _touchDistance; // Not implemented
            } 
        }

/**
     * The stabilized tip position of this Pointable.
     *
     * Smoothing and stabilization is performed in order to make
     * this value more suitable for interaction with 2D content. The stabilized
     * position lags behind the tip position by a variable amount, depending
     * primarily on the speed of movement.
     *
     * \include Pointable_stabilizedTipPosition.txt
     *
     * @returns A modified tip position of this Pointable object
     * with some additional smoothing and stabilization applied.
     * @since 1.0
     */
        public Vector StabilizedTipPosition {
            get {
                return _stabilizedTipPosition;
            } 
        }

/**
     * The duration of time this Pointable has been visible to the Leap Motion Controller.
     *
     * \include Pointable_timeVisible.txt
     *
     * @returns The duration (in seconds) that this Pointable has been tracked.
     * @since 1.0
     */
        public float TimeVisible {
            get {
                return _timeVisible;
            } 
        }

/**
     * The Frame associated with this Pointable object.
     *
     * \include Pointable_frame.txt
     *
     * @returns The associated Frame object, if available; otherwise,
     * an invalid Frame object is returned.
     * @since 1.0
     */
      //Fox - removed along with backref to Frame
        //public Frame Frame {
        //        get {
        //            return (_frame != null) ? _frame : new Frame ();
        //        } 
        //}

/**
     * Returns an invalid Pointable object.
     *
     * You can use the instance returned by this function in comparisons testing
     * whether a given Pointable instance is valid or invalid. (You can also use the
     * Pointable::isValid() function.)
     *
     * \include Pointable_invalid.txt
     *
     * @returns The invalid Pointable instance.
     * @since 1.0
     */
        public static Pointable Invalid {
            get {
                return new Pointable ();
            } 
        }

        /**
       * Defines the values for reporting the state of a Pointable object in relation to
       * an adaptive touch plane.
       * @since 1.0
       */
        public enum Zone
        {
            /**
         * The Pointable object is too far from the plane to be
         * considered hovering or touching.
         * @since 1.0
         */
            ZONE_NONE = 0,
            /**
         * The Pointable object is close to, but not touching
         * the plane.
         * @since 1.0
         */
            ZONE_HOVERING = 1,
            /**
         * The Pointable has penetrated the plane.
         * @since 1.0
         */
            ZONE_TOUCHING = 2,
            ZONENONE = ZONE_NONE,
            ZONEHOVERING = ZONE_HOVERING,
            ZONETOUCHING = ZONE_TOUCHING
        }

    }

}
