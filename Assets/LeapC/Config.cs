namespace Leap {

using System;
using System.Runtime.InteropServices;
using LeapInternal;

  /**
   * The Config class provides access to Leap Motion system configuration information.
   *
   * You can get and set gesture configuration parameters using the Config object
   * obtained from a connected Controller object. The key strings required to
   * identify a configuration parameter include:
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
   * After setting a configuration value, you must call the Config::save() method
   * to commit the changes. You can save after the Controller has connected to
   * the Leap Motion service/daemon. In other words, after the Controller
   * has dispatched the serviceConnected or connected events or
   * Controller::isConnected is true. The configuration value changes are
   * not persistent; your application needs to set the values every time it runs.
   *
   * @see CircleGesture
   * @see KeyTapGesture
   * @see ScreenTapGesture
   * @see SwipeGesture
   * @since 1.0
   */

public class Config {
        private Connection _connection;

  public Config(int connectionKey) {
            _connection = Connection.GetConnection(connectionKey);
  }

    /**
     * Reports the natural data type for the value related to the specified key.
     *
     * \include Config_type.txt
     *
     * @param key The key for the looking up the value in the configuration dictionary.
     * @returns The native data type of the value, that is, the type that does not
     * require a data conversion.
     * @since 1.0
     */
  public Config.ValueType Type(string key) {
    return Config.ValueType.TYPE_UNKNOWN;
  }

    /**
     * Gets the boolean representation for the specified key.
     *
     * \include Config_getBool.txt
     *
     * @since 1.0
     */
  public bool GetBool(string key) {
    return false;
  }

    /** Sets the boolean representation for the specified key.
     *
     * \include Config_setBool.txt
     *
     * @returns true on success, false on failure.
     * @since 1.0
     */
  public bool SetBool(string key, bool value) {
    return false;
  }

    /**
     * Gets the 32-bit integer representation for the specified key.
     *
     * \include Config_getInt32.txt
     *
     * @since 1.0
     */
  public int GetInt32(string key) {
    return 0;
  }

    /** Sets the 32-bit integer representation for the specified key.
     *
     * \include Config_setInt32.txt
     *
     * @returns true on success, false on failure.
     * @since 1.0
     */
  public bool SetInt32(string key, int value) {
    return false;
  }

    /**
     * Gets the floating point representation for the specified key.
     *
     * \include Config_getFloat.txt
     *
     * @since 1.0
     */
  public float GetFloat(string key) {
    return 0;
  }

    /** Sets the floating point representation for the specified key.
     *
     * \include Config_setFloat.txt
     *
     * @returns true on success, false on failure.
     * @since 1.0
     */
  public bool SetFloat(string key, float value) {
    return false;
  }

    /**
     * Gets the string representation for the specified key.
     *
     * \include Config_getString.txt
     *
     * @since 1.0
     */
  public string GetString(string key) {
    return "Not implemented";
  }

    /** Sets the string representation for the specified key.
     *
     * \include Config_setString.txt
     *
     * @returns true on success, false on failure.
     * @since 1.0
     */
  public bool SetString(string key, string value) {
    return false;
  }

    /**
     * Saves the current state of the config.
     *
     * Call ``save()`` after making a set of configuration changes. The
     * ``save()`` function transfers the configuration changes to the Leap Motion
     * service. You can save after the Controller has connected to
     * the Leap Motion service/daemon. In other words, after the Controller
     * has dispatched the serviceConnected or connected events or
     * Controller::isConnected is true. The configuration value changes are not persistent; your
     * application must set the values every time it runs.
     *
     * \include Config_save.txt
     *
     * @returns true on success, false on failure.
     * @since 1.0
     */
  public bool Save() {
    return false;
  }

      /**
       * Enumerates the possible data types for configuration values.
       *
       * The Config::type() function returns an item from the ValueType enumeration.
       * @since 1.0
       */
  public enum ValueType {
        /**
         * The data type is unknown.
         * @since 1.0
         */
    TYPE_UNKNOWN = 0,
        /**
         * A boolean value.
         * @since 1.0
         */
    TYPE_BOOLEAN = 1,
        /**
         * A 32-bit integer.
         * @since 1.0
         */
    TYPE_INT32 = 2,
        /**
         * A floating-point number.
         * @since 1.0
         */
    TYPE_FLOAT = 6,
        /**
         * A string of characters.
         * @since 1.0
         */
    TYPE_STRING = 8,
    TYPEUNKNOWN = TYPE_UNKNOWN,
    TYPEBOOLEAN = TYPE_BOOLEAN,
    TYPEINT32 = TYPE_INT32,
    TYPEFLOAT = TYPE_FLOAT,
    TYPESTRING = TYPE_STRING
  }

}

}
