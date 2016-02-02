namespace Leap {

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LeapInternal;

  /**
   * The Config class provides access to Leap Motion system configuration information.
   *
   * @since 1.0
   */

public class Config {
        private Connection _connection;
        private Dictionary<string, ConfigValue> _configDictionary = new Dictionary<string, ConfigValue>();

  public Config(int connectionKey) {
            _connection = Connection.GetConnection(connectionKey);
            _connection.LeapConfigChange += handleConfigChange;
            _connection.LeapConfigResponse += handleConfigResponse;
             _configDictionary.Add("tracking_hand_enabled", new ConfigValue());
            _configDictionary.Add("robust_mode_enabled", new ConfigValue());
            _configDictionary.Add("force_robust_mode", new ConfigValue());
            _configDictionary.Add("images_mode", new ConfigValue());
            _configDictionary.Add("image_processing_auto_flip", new ConfigValue());
            _configDictionary.Add("interaction_box_auto", new ConfigValue());
            _configDictionary.Add("low_resource_mode_enabled", new ConfigValue());
            _configDictionary.Add("avoid_poor_performance", new ConfigValue());
            _configDictionary.Add("power_saving_adapter", new ConfigValue());
            _configDictionary.Add("power_saving_battery", new ConfigValue());

//            if(_connection.IsServiceConnected)
//                refreshCachedValues();
  }

        private void refreshCachedValues(){
            foreach(string key in _configDictionary.Keys){
                _connection.GetConfigValue(key);
            }
        }

        private void handleConfigChange(object sender, ConfigChangeEventArgs eventArgs){
            if(_configDictionary.ContainsKey(eventArgs.ConfigKey)){
                _configDictionary[eventArgs.ConfigKey].pending = false;
            }
        }
        private void handleConfigResponse(object sender, SetConfigResponseEventArgs eventArgs){
            _configDictionary[eventArgs.ConfigKey] = eventArgs.Value;
            _configDictionary[eventArgs.ConfigKey].pending = false;
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
  }


//
//
//        tracking_hand_enabled
//        robust_mode_enabled
//        force_robust_mode
//        images_mode arg
//        image_processing_auto_flip
//        interaction_box_auto
//        tracking_quad_enabled
//        low_resource_mode_enabled
//        avoid_poor_performance
//        power_saving_adapter
//        power_saving_battery

//        public bool HandTrackingEnabled{
//            
//        }
//                RobustModeEnabled
//                RobustModeOn
//                ImagesEnabled
//                AutoOrientationEnabled
//                AutoInterActionBoxHeightEnabled
//                QuadTrackingEnabled
//                LowResourceModeOn
//                AvoidPoorPerformance
//                ReducePowerUsageAdapter
//                ReducePowerUsageBattery


}
    public class ConfigValue{
        public ConfigValue(){
            pending = true;
        }
        public Config.ValueType type{get; set;}
        public float floatValue{get; set;}
        public Int32 intValue{get; set;}
        public bool boolValue{get; set;}
        public string stringValue{get; set;}
        public bool pending{get; set;}
    }
}
