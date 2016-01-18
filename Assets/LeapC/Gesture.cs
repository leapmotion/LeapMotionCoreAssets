namespace Leap {

using System;
using System.Runtime.InteropServices;

  /**
   * The Gesture class represents a recognized movement by the user.
   *
   * The Leap Motion Controller watches the activity within its field of view for certain movement
   * patterns typical of a user gesture or command. For example, a movement from side to
   * side with the hand can indicate a swipe gesture, while a finger poking forward
   * can indicate a screen tap gesture.
   *
   * When the Leap Motion software recognizes a gesture, it assigns an ID and adds a
   * Gesture object to the frame gesture list. For continuous gestures, which
   * occur over many frames, the Leap Motion software updates the gesture by adding
   * a Gesture object having the same ID and updated properties in each
   * subsequent frame.
   *
   * **Important:** Recognition for each type of gesture must be enabled using the
   * Controller::enableGesture() function; otherwise **no gestures are recognized or
   * reported**.
   *
   * \include Gesture_Feature_enable.txt
   *
   * Subclasses of Gesture define the properties for the specific movement patterns
   * recognized by the Leap Motion software.
   *
   * The Gesture subclasses include:
   *
   * **CircleGesture** -- A circular movement by a finger.
   *
   * **SwipeGesture** -- A straight line movement by the hand with fingers extended.
   *
   * **ScreenTapGesture** -- A forward tapping movement by a finger.
   *
   * **KeyTapGesture** -- A downward tapping movement by a finger.
   *
   * Circle and swipe gestures are continuous and these objects can have a
   * state of start, update, and stop.
   *
   * The screen tap gesture is a discrete gesture. The Leap Motion software only creates a single
   * ScreenTapGesture object for each tap and it always has a stop state.
   *
   * Get valid Gesture instances from a Frame object. You can get a list of gestures
   * with the Frame::gestures() method. You can get a list of gestures since a
   * specified frame with the `Frame::gestures(const Frame&)` method. You can also
   * use the `Frame::gesture()` method to find a gesture in the current frame using
   * an ID value obtained in a previous frame.
   *
   * Gesture objects can be invalid. For example, when you get a gesture by ID
   * using `Frame::gesture()`, and there is no gesture with that ID in the current
   * frame, then `gesture()` returns an Invalid Gesture object (rather than a null
   * value). Always check object validity in situations where a gesture might be
   * invalid.
   *
   * The following keys can be used with the Config class to configure the gesture
   * recognizer:
   *
   * \table
   * ====================================  ========== ============= =======
   * Key string                            Value type Default value Units
   * ====================================  ========== ============= =======
   * Gesture.Circle.MinRadius              float      5.0           mm
   * Gesture.Circle.MinArc                 float      1.5 * pi      radians
   * Gesture.Swipe.MinLength               float      150           mm
   * Gesture.Swipe.MinVelocity             float      1000          mm/s
   * Gesture.KeyTap.MinDownVelocity        float      50            mm/s
   * Gesture.KeyTap.HistorySeconds         float      0.1           s
   * Gesture.KeyTap.MinDistance            float      3.0           mm
   * Gesture.ScreenTap.MinForwardVelocity  float      50            mm/s
   * Gesture.ScreenTap.HistorySeconds      float      0.1           s
   * Gesture.ScreenTap.MinDistance         float      5.0           mm
   * ====================================  ========== ============= =======
   * \endtable
   *
   * @since 1.0
   */

public class Gesture{

    /**
     * Constructs a new Gesture object.
     *
     * An uninitialized Gesture object is considered invalid. Get valid instances
     * of the Gesture class, which will be one of the Gesture subclasses, from a
     * Frame object.
     * @since 1.0
     */
  public Gesture() {
  }

    /**
     * Constructs a new copy of an Gesture object.
     *
     * \include Gesture_Gesture_copy.txt
     *
     * @since 1.0
     */
  public Gesture(Gesture rhs) {
    
  }

    /**
     * Compare Gesture object equality.
     *
     * \include Gesture_operator_equals.txt
     *
     * Two Gestures are equal if they represent the same snapshot of the same
     * recognized movement.
     * @since 1.0
     */
  public bool Equals(Gesture rhs) {
    return false;
  }

    /**
     * A string containing a brief, human-readable description of this
     * Gesture.
     *
     * \include Gesture_toString.txt
     *
     * @since 1.0
     */
  public override string ToString() {
    return "Gestures are not supported.";
  }

/**
     * The gesture type.
     *
     * \include Gesture_type.txt
     *
     * @returns Gesture::Type A value from the Gesture::Type enumeration.
     * @since 1.0
     */  public Gesture.GestureType Type {
    get {
      return Gesture.GestureType.TYPE_INVALID;
    } 
  }

/**
     * The gesture state.
     *
     * Recognized movements occur over time and have a beginning, a middle,
     * and an end. The 'state()' attribute reports where in that sequence this
     * Gesture object falls.
     *
     * \include Gesture_state.txt
     *
     * @returns Gesture::State A value from the Gesture::State enumeration.
     * @since 1.0
     */  public Gesture.GestureState State {
    get {
                return Gesture.GestureState.STATE_INVALID;
    } 
  }

/**
     * The gesture ID.
     *
     * All Gesture objects belonging to the same recognized movement share the
     * same ID value. Use the ID value with the Frame::gesture() method to
     * find updates related to this Gesture object in subsequent frames.
     *
     * \include Gesture_id.txt
     *
     * @returns int32_t the ID of this Gesture.
     * @since 1.0
     */  public int Id {
    get {
      return 0;
    } 
  }

/**
     * The elapsed duration of the recognized movement up to the
     * frame containing this Gesture object, in microseconds.
     *
     * \include Gesture_duration.txt
     *
     * The duration reported for the first Gesture in the sequence (with the
     * STATE_START state) will typically be a small positive number since
     * the movement must progress far enough for the Leap Motion software to recognize it as
     * an intentional gesture.
     *
     * @return int64_t the elapsed duration in microseconds.
     * @since 1.0
     */  public long Duration {
    get {
      return 0;
    } 
  }

/**
     * The elapsed duration in seconds.
     *
     * \include Gesture_durationSeconds.txt
     *
     * @see duration()
     * @return float the elapsed duration in seconds.
     * @since 1.0
     */  public float DurationSeconds {
    get {
      return 0;
    } 
  }

/**
     * The Frame containing this Gesture instance.
     *
     * \include Gesture_frame.txt
     _
     * @return Frame The parent Frame object.
     * @since 1.0
     */  public Frame Frame {
    get {
      return new Frame();
    } 
  }

/**
     * The list of hands associated with this Gesture, if any.
     *
     * \include Gesture_hands.txt
     *
     * If no hands are related to this gesture, the list is empty.
     *
     * @return HandList the list of related Hand objects.
     * @since 1.0
     */  public HandList Hands {
    get {
      return new HandList();
    } 
  }

/**
     * The list of fingers and tools associated with this Gesture, if any.
     *
     * If no Pointable objects are related to this gesture, the list is empty.
     *
     * \include Gesture_pointables.txt
     *
     * @return PointableList the list of related Pointable objects.
     * @since 1.0
     */  public PointableList Pointables {
    get {
      return new PointableList();
    } 
  }

/**
     * Reports whether this Gesture instance represents a valid Gesture.
     *
     * An invalid Gesture object does not represent a snapshot of a recognized
     * movement. Invalid Gesture objects are returned when a valid object cannot
     * be provided. For example, when you get an gesture by ID
     * using Frame::gesture(), and there is no gesture with that ID in the current
     * frame, then gesture() returns an Invalid Gesture object (rather than a null
     * value). Always check object validity in situations where an gesture might be
     * invalid.
     *
     * \include Gesture_isValid.txt
     *
     * @returns bool True, if this is a valid Gesture instance; false, otherwise.
     * @since 1.0
     */  public bool IsValid {
    get {
      return false;
    } 
  }

/**
     * Returns an invalid Gesture object.
     *
     * You can use the instance returned by this function in comparisons testing
     * whether a given Gesture instance is valid or invalid. (You can also use the
     * Gesture::isValid() function.)
     *
     * \include Gesture_invalid.txt
     *
     * @returns The invalid Gesture instance.
     * @since 1.0
     */  public static Gesture Invalid {
    get {
      return new Gesture();
    } 
  }

      /**
       * The supported types of gestures.
       * @since 1.0
       */
  public enum GestureType {
        /**
         * An invalid type.
         * @since 1.0
         */
    TYPE_INVALID = -1,
        /**
         * A straight line movement by the hand with fingers extended.
         * @since 1.0
         */
    TYPE_SWIPE = 1,
        /**
         * A circular movement by a finger.
         * @since 1.0
         */
    TYPE_CIRCLE = 4,
        /**
         * A forward tapping movement by a finger.
         * @since 1.0
         */
    TYPE_SCREEN_TAP = 5,
        /**
         * A downward tapping movement by a finger.
         * @since 1.0
         */
    TYPE_KEY_TAP = 6,
    TYPEINVALID = TYPE_INVALID,
    TYPESWIPE = TYPE_SWIPE,
    TYPECIRCLE = TYPE_CIRCLE,
    TYPESCREENTAP = TYPE_SCREEN_TAP,
    TYPEKEYTAP = TYPE_KEY_TAP
  }

      /**
       * The possible gesture states.
       * @since 1.0
       */
  public enum GestureState {
        /**
         * An invalid state
         * @since 1.0
         */
    STATE_INVALID = -1,
        /**
         * The gesture is starting. Just enough has happened to recognize it.
         * @since 1.0
         */
    STATE_START = 1,
        /**
         * The gesture is in progress. (Note: not all gestures have updates).
         * @since 1.0
         */
    STATE_UPDATE = 2,
        /**
         * The gesture has completed or stopped.
         * @since 1.0
         */
    STATE_STOP = 3,
    STATEINVALID = STATE_INVALID,
    STATESTART = STATE_START,
    STATEUPDATE = STATE_UPDATE,
    STATESTOP = STATE_STOP
  }

}

}
