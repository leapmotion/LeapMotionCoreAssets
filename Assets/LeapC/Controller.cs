/******************************************************************************\
* Copyright (C) 2012-2015 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/

namespace Leap
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Runtime.InteropServices;

    using LeapInternal;

    using System.IO; //debugging

    /**
   * The Controller class is your main interface to the Leap Motion Controller.
   *
   * Create an instance of this Controller class to access frames of tracking
   * data and configuration information. Frame data can be polled at any time
   * using the Controller::frame() function. Call frame() or frame(0) to get the
   * most recent frame. Set the history parameter to a positive integer to access
   * previous frames. A controller stores up to 60 frames in its frame history.
   *
   * Polling is an appropriate strategy for applications which already have an
   * intrinsic update loop, such as a game. You can also add an instance of a
   * subclass of Leap::Listener to the controller to handle events as they occur.
   * The Controller dispatches events to the listener upon initialization and exiting,
   * on connection changes, when the application gains and loses the OS input focus,
   * and when a new frame of tracking data is available.
   * When these events occur, the controller object invokes the appropriate
   * callback function defined in your subclass of Listener.
   *
   * To access frames of tracking data as they become available:
   *
   * 1. Implement a subclass of the Listener class and override the
   *    Listener::onFrame() function.
   * 2. In your Listener::onFrame() function, call the Controller::frame()
   *    function to access the newest frame of tracking data.
   * 3. To start receiving frames, create a Controller object and add an instance
   *    of the Listener subclass to the Controller::addListener() function.
   *
   * When an instance of a Listener subclass is added to a Controller object,
   * it calls the Listener::onInit() function when the listener is ready for use.
   * When a connection is established between the controller and the Leap Motion software,
   * the controller calls the Listener::onConnect() function. At this point, your
   * application will start receiving frames of data. The controller calls the
   * Listener::onFrame() function each time a new frame is available. If the
   * controller loses its connection with the Leap Motion software or device for any
   * reason, it calls the Listener::onDisconnect() function. If the listener is
   * removed from the controller or the controller is destroyed, it calls the
   * Listener::onExit() function. At that point, unless the listener is added to
   * another controller again, it will no longer receive frames of tracking data.
   *
   * The Controller object is multithreaded and calls the Listener functions on
   * its own thread, not on an application thread.
   * @since 1.0
   */

    public class Controller : IController
    {
        Connection _connection;
        SynchronizationContext _synchronizationContext;
        List<Listener> _listeners = new List<Listener>();
        ImageList _images;

        bool _disposed = false;

        //TODO revisit dispose code
        public void Dispose()
        { 
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return; 
            
            if (disposing) {
                // Free any other managed objects here.
                Logger.Log ("Disposing managed");
            }
            
            // Free any unmanaged objects here.
            //
            Logger.Log ("Disposing rest");
            _disposed = true;
        }
        /**
     * Constructs a Controller object.
     *
     * When creating a Controller object, you may optionally pass in a
     * reference to an instance of a subclass of Leap::Listener. Alternatively,
     * you may add a listener using the Controller::addListener() function.
     *
     * @since 1.0
     */
        public Controller ():this(0){}

        public void handleAllEvents(eLeapEventType type, object obj){
            Logger.Log ("Controller received event of " + type);
        } 

        public Controller(int connectionKey){
            Logger.Log ("Creating controler with connectionKey: " + connectionKey);
            _synchronizationContext = SynchronizationContext.Current;
            _connection = Connection.GetConnection(connectionKey);
            _connection.AddLeapCEventHandler(eLeapEventType.eLeapEventType_Tracking, new LeapCEventHandler(dispatchOnFrame));
            _connection.AddLeapCEventHandler(eLeapEventType.eLeapEventType_ImageComplete, new LeapCEventHandler(dispatchOnImages));
        }
        /**
     * Constructs a Controller object.
     *
     * When creating a Controller object, you may optionally pass in a
     * reference to an instance of a subclass of Leap::Listener. Alternatively,
     * you may add a listener using the Controller::addListener() function.
     *
     * \include Controller_Controller.txt
     *
     * @param listener An instance of Leap::Listener implementing the callback
     * functions for the Leap Motion events you want to handle in your application.
     * @since 1.0
     */
        public Controller (Listener listener) :this()
        {
            this.AddListener (listener);
        }


        /**
     * Reports whether your application has a connection to the Leap Motion
     * daemon/service. Can be true even if the Leap Motion hardware is not available.
     * @since 1.2
     */
        public bool IsServiceConnected {
            get {
                return _connection.IsServiceConnected;
            }
        }
        /**
     * This function has been deprecated. Use setPolicy() and clearPolicy() instead.
     * @deprecated 2.1.6
     */
        //public void SetPolicyFlags(Controller.PolicyFlag flags) {}
        
        /**
     * Requests setting a policy.
     *
     * A request to change a policy is subject to user approval and a policy
     * can be changed by the user at any time (using the Leap Motion settings dialog).
     * The desired policy flags must be set every time an application runs.
     *
     * Policy changes are completed asynchronously and, because they are subject
     * to user approval or system compatibility checks, may not complete successfully. Call
     * Controller::isPolicySet() after a suitable interval to test whether
     * the change was accepted.
     *
     * \include Controller_setPolicy.txt
     *
     * @param policy A PolicyFlag value indicating the policy to request.
     * @since 2.1.6
     */
        public void SetPolicy (Controller.PolicyFlag policy)
        {
            _connection.SetPolicy(policy);
        }
        
        /**
     * Requests clearing a policy.
     *
     * Policy changes are completed asynchronously and, because they are subject
     * to user approval or system compatibility checks, may not complete successfully. Call
     * Controller::isPolicySet() after a suitable interval to test whether
     * the change was accepted.
     *
     * \include Controller_clearPolicy.txt
     *
     * @param flags A PolicyFlag value indicating the policy to request.
     * @since 2.1.6
     */
        public void ClearPolicy (Controller.PolicyFlag policy)
        {
            _connection.ClearPolicy(policy);
        }

        /**
     * Gets the active setting for a specific policy.
     *
     * Keep in mind that setting a policy flag is asynchronous, so changes are
     * not effective immediately after calling setPolicyFlag(). In addition, a
     * policy request can be declined by the user. You should always set the
     * policy flags required by your application at startup and check that the
     * policy change request was successful after an appropriate interval.
     *
     * If the controller object is not connected to the Leap Motion software, then the default
     * state for the selected policy is returned.
     *
     * \include Controller_isPolicySet.txt
     *
     * @param flags A PolicyFlag value indicating the policy to query.
     * @returns A boolean indicating whether the specified policy has been set.
     * @since 2.1.6
     */
        public bool IsPolicySet (Controller.PolicyFlag policy)
        {
            return _connection.IsPolicySet(policy);
        }


        /**
     * Returns a frame of tracking data from the Leap Motion software. Use the optional
     * history parameter to specify which frame to retrieve. Call frame() or
     * frame(0) to access the most recent frame; call frame(1) to access the
     * previous frame, and so on. If you use a history value greater than the
     * number of stored frames, then the controller returns an invalid frame.
     *
     * \include Controller_Frame_1.txt
     *
     * You can call this function in your Listener implementation to get frames at the
     * Leap Motion frame rate:
     *
     * \include Controller_Listener_onFrame.txt
     * 
     * @param history The age of the frame to return, counting backwards from
     * the most recent frame (0) into the past and up to the maximum age (59).
     * @returns The specified frame; or, if no history parameter is specified,
     * the newest frame. If a frame is not available at the specified history
     * position, an invalid Frame is returned.
     * @since 1.0
     */
        public Frame Frame (int history)
        {
            Frame frame = _connection.Frames.Get (history);
            frame.controller = this;
            return frame;
        }

        /**
     * Returns a frame of tracking data from the Leap Motion software. Use the optional
     * history parameter to specify which frame to retrieve. Call frame() or
     * frame(0) to access the most recent frame; call frame(1) to access the
     * previous frame, and so on. If you use a history value greater than the
     * number of stored frames, then the controller returns an invalid frame.
     *
     * \include Controller_Frame_1.txt
     *
     * You can call this function in your Listener implementation to get frames at the
     * Leap Motion frame rate:
     *
     * \include Controller_Listener_onFrame.txt
     * 
     * @param history The age of the frame to return, counting backwards from
     * the most recent frame (0) into the past and up to the maximum age (59).
     * @returns The specified frame; or, if no history parameter is specified,
     * the newest frame. If a frame is not available at the specified history
     * position, an invalid Frame is returned.
     * @since 1.0
     */
        public Frame Frame ()
        {
            return Frame (0);
        }

        /**
     * Enables or disables reporting of a specified gesture type.
     *
     * By default, all gesture types are disabled. When disabled, gestures of the
     * disabled type are never reported and will not appear in the frame
     * gesture list.
     *
     * \include Controller_enableGesture.txt
     *
     * As a performance optimization, only enable recognition for the types
     * of movements that you use in your application.
     *
     * @param type The type of gesture to enable or disable. Must be a
     * member of the Gesture::Type enumeration.
     * @param enable True, to enable the specified gesture type; False,
     * to disable.
     * @see Controller::isGestureEnabled()
     * @since 1.0
     */
        public void EnableGesture (Gesture.GestureType type, bool enable)
        {
            throw new NotImplementedException ("Gestures are not implemented in this interface.");
        }

        /**
     * Enables or disables reporting of a specified gesture type.
     *
     * By default, all gesture types are disabled. When disabled, gestures of the
     * disabled type are never reported and will not appear in the frame
     * gesture list.
     *
     * \include Controller_enableGesture.txt
     *
     * As a performance optimization, only enable recognition for the types
     * of movements that you use in your application.
     *
     * @param type The type of gesture to enable or disable. Must be a
     * member of the Gesture::Type enumeration.
     * @param enable True, to enable the specified gesture type; False,
     * to disable.
     * @see Controller::isGestureEnabled()
     * @since 1.0
     */
        public void EnableGesture (Gesture.GestureType type)
        {
            EnableGesture (type, true);
        }

        /**
     * Reports whether the specified gesture type is enabled.
     *
     * \include Controller_isGestureEnabled.txt
     *
     * @param type The type of gesture to check; a member of the Gesture::Type enumeration.
     * @returns True, if the specified type is enabled; false, otherwise.
     * @see Controller::enableGesture()
     * @since 1.0
     */
        public bool IsGestureEnabled (Gesture.GestureType type)
        {
            throw new NotImplementedException ("Gestures are not implemented in this interface.");
        }

        /**
     * Returns a timestamp value as close as possible to the current time.
     * Values are in microseconds, as with all the other timestamp values.
     *
     * @since 2.2.7
     *
     */
        public long Now ()
        {
            return LeapC.GetNow ();
        }

        /**
     * Pauses or resumes the Leap Motion service.
     *
     * When the service is paused no applications receive tracking data and the
     * service itself uses minimal CPU time.
     *
     * Before changing the state of the service, you must set the
     * POLICY_ALLOW_PAUSE_RESUME using the Controller::setPolicy() function.
     * Policies must be set every time the application is run.
     *
     * \include Controller_setPaused.txt
     *
     * @param pause Set true to pause the service; false to resume.
     * @since 2.4.0
     */
        public void SetPaused(bool pause) {
            _connection.SetPaused(pause);
        }
        
        /**
     * Reports whether the Leap Motion service is currently paused.
     *
     * \include Controller_isPaused.txt
     *
     * @returns True, if the service is paused; false, otherwise.
     * @since 2.4.0
     */
        public bool IsPaused() {
            return _connection.IsPaused;
        }

/**
     * Reports whether this Controller is connected to the Leap Motion service and
     * the Leap Motion hardware is plugged in.
     *
     * When you first create a Controller object, isConnected() returns false.
     * After the controller finishes initializing and connects to the Leap Motion
     * software and if the Leap Motion hardware is plugged in, isConnected() returns true.
     *
     * You can either handle the onConnect event using a Listener instance or
     * poll the isConnected() function if you need to wait for your
     * application to be connected to the Leap Motion software before performing some other
     * operation.
     *
     * \include Controller_isConnected.txt
     * @returns True, if connected; false otherwise.
     * @since 1.0
     */
        public bool IsConnected {
            get {
                //TODO Check that a device is streaming, not just present.  -- need to update devices in PollConnection first.
                return IsServiceConnected && Devices.Count > 0;
            } 
        }

/**
     * Reports whether this application is the focused, foreground application.
     *
     * By default, your application only receives tracking information from
     * the Leap Motion controller when it has the operating system input focus.
     * To receive tracking data when your application is in the background,
     * the background frames policy flag must be set.
     *
     * \include Controller_hasFocus.txt
     *
     * @returns True, if application has focus; false otherwise.
     *
     * @see Controller::setPolicyFlags()
     * @since 1.0
     */
        public bool HasFocus {
            get {
                return false;
            } 
        }

/**
     * This function has been deprecated. Use isPolicySet() instead.
     * @deprecated 2.1.6
     */
        public Controller.PolicyFlag PolicyFlags {
            get {
                return 0;
            } 
        }

        //TODO Add Config interface when available from LeapC
/**
     * Returns a Config object, which you can use to query the Leap Motion system for
     * configuration information.
     *
     * \include Controller_config.txt
     *
     * @returns The Controller's Config object.
     * @since 1.0
     */  
        public Config Config {
            get {
                return new Config (this._connection.ConnectionKey);
            } 
        }

/**
     * The most recent set of images from the Leap Motion cameras.
     *
     * \include Controller_images.txt
     *
     * Depending on timing and the current processing frame rate, the images
     * obtained with this function can be newer than images obtained from
     * the current frame of tracking data.
     *
     * @return An ImageList object containing the most recent camera images.
     * @since 2.2.1
     */
        public ImageList Images {
            get {
                if (_images == null)
                    _images = new ImageList ();

                Image left;
                Image right;
                bool found = _connection.GetLatestImagePair(out left, out right);
                if(found){
                    _images.Clear();
                    
                    _images.Add(left);
                    _images.Add(right);
                }
                return _images;
            } 
        }

        //TODO Get RawImages from LeapC
        public ImageList RawImages{
            get{
                return this.Images;
            }
        }

        public void GetImagesForFrame(long frameId, ref ImageList images) {
            Image left;
            Image right;
            if(_connection.GetFrameImagePair(frameId, out left, out right)){
                if(left.IsValid)
                    images.Add(left);
                if(right.IsValid)
                    images.Add(right);
            }
        }

/**
     * The list of currently attached and recognized Leap Motion controller devices.
     *
     * The Device objects in the list describe information such as the range and
     * tracking volume.
     *
     * \include Controller_devices.txt
     *
     * Currently, the Leap Motion Controller only allows a single active device at a time,
     * however there may be multiple devices physically attached and listed here.  Any active
     * device(s) are guaranteed to be listed first, however order is not determined beyond that.
     *
     * @returns The list of Leap Motion controllers.
     * @since 1.0
     */
        public DeviceList Devices {
            get {
                return _connection.Devices;
            } 
        }

        /**
    * A list of any Leap Motion hardware devices that are physically connected to
    * the client computer, but are not functioning correctly. The list contains
    * FailedDevice objects containing the pnpID and the reason for failure. No
    * other device information is available.
    *
    * \include Controller_failedDevices.txt
    *
    * @since 2.4.0
    */
        public FailedDeviceList FailedDevices() {
            return _connection.FailedDevices;
        }

/**
     * Note: This class is an experimental API for internal use only. It may be
     * removed without warning.
     *
     * Returns information about the currently detected quad in the scene.
     *
     * \include Controller_trackedQuad.txt
     * If no quad is being tracked, then an invalid TrackedQuad is returned.
     * @since 2.2.6
     **/
        public TrackedQuad TrackedQuad {
            get {

                return _connection.GetLatestQuad();
            } 
        }

        public TrackedQuad GetTrackedQuadForFrame(long id){
            return _connection.GetFrameQuad(id);
        }

        public BugReport BugReport {
            get {
                throw new NotImplementedException ("BugReport not implemented in this interface.");
            } 
        }


      /**
       * The supported controller policies.
       *
       * The supported policy flags are:
       *
       * **POLICY_BACKGROUND_FRAMES** -- requests that your application receives frames
       *   when it is not the foreground application for user input.
       *
       *   The background frames policy determines whether an application
       *   receives frames of tracking data while in the background. By
       *   default, the Leap Motion  software only sends tracking data to the foreground application.
       *   Only applications that need this ability should request the background
       *   frames policy. The "Allow Background Apps" checkbox must be enabled in the
       *   Leap Motion Control Panel or this policy will be denied.
       *
       * **POLICY_IMAGES** -- request that your application receives images from the
       *   device cameras. The "Allow Images" checkbox must be enabled in the
       *   Leap Motion Control Panel or this policy will be denied.
       *
       *   The images policy determines whether an application receives image data from
       *   the Leap Motion sensors which each frame of data. By default, this data is
       *   not sent. Only applications that use the image data should request this policy.
       *
       *
       * **POLICY_OPTIMIZE_HMD** -- request that the tracking be optimized for head-mounted
       *   tracking.
       *
       *   The optimize HMD policy improves tracking in situations where the Leap
       *   Motion hardware is attached to a head-mounted display. This policy is
       *   not granted for devices that cannot be mounted to an HMD, such as
       *   Leap Motion controllers embedded in a laptop or keyboard.
       *
       * Some policies can be denied if the user has disabled the feature on
       * their Leap Motion control panel.
       *
       * @since 1.0
       */
        public enum PolicyFlag
        {
            /**
         * The default policy.
         * @since 1.0
         */
            POLICY_DEFAULT = 0,
            /**
         * Receive background frames.
         * @since 1.0
         */
            POLICY_BACKGROUND_FRAMES = (1 << 0),
            /**
         * Receive raw images from sensor cameras.
         * @since 2.1.0
         */
            POLICY_IMAGES = (1 << 1),
            /**
         * Optimize the tracking for head-mounted device.
         * @since 2.1.2
         */
            POLICY_OPTIMIZE_HMD = (1 << 2),
            /**
        * Allow pausing and unpausing of the Leap Motion service.
        * @since 2.4.0
        */
            POLICY_ALLOW_PAUSE_RESUME = (1 << 3),
        }

        public void RequestDiagnostic(){
            //TODO implement request diagnostic
            //Are we still doing this?
        }
        // Listener Dispatch
        //TODO Add listener interface and/or delegates
    /**
     * Adds a listener to this Controller.
     *
     * The Controller dispatches Leap Motion events to each associated listener. The
     * order in which listener callback functions are invoked is arbitrary. If
     * you pass a listener to the Controller's constructor function, it is
     * automatically added to the list and can be removed with the
     * Controller::removeListener() function.
     *
     * \include Controller_addListener.txt
     *
     * The Controller does not keep a strong reference to the Listener instance.
     * Ensure that you maintain a reference until the listener is removed from
     * the controller.
     *
     * @param listener A subclass of Leap::Listener implementing the callback
     * functions for the Leap Motion events you want to handle in your application.
     * @returns Whether or not the listener was successfully added to the list
     * of listeners.
     * @since 1.0
     */
        public bool AddListener (Listener listener)
        {
            if (!_listeners.Contains (listener)) {
                _listeners.Add (listener);
                return true;
            }
            return false; //if already added
        }
        
        /**
     * Remove a listener from the list of listeners that will receive Leap Motion
     * events. A listener must be removed if its lifetime is shorter than the
     * controller to which it is listening.
     *
     * \include Controller_removeListener.txt
     *
     * @param listener The listener to remove.
     * @returns Whether or not the listener was successfully removed from the
     * list of listeners.
     * @since 1.0
     */
        public bool RemoveListener (Listener listener)
        {
            return _listeners.Remove (listener);
        }

        enum NotificationType{
            OnInit,
            OnFrame
        }


//        void dispatchMessage(NotificationType notification){
//            syncContext.Post(notifyListeners, notification);
//        }
//        SendOrPostCallback notifyListeners = delegate(object notificationType){
//            NotificationType notificationType = (NotificationType)notificationType; 
//            switch(notificationType){
//                case NotificationType.OnFrame:
//                    //dispatchOnFrame();
//                    break;
//            }
//        };
        void dispatchOnInit(){
//            foreach(Listener listener in _listeners){
//                listener.OnInit(this);
//            }
        }
        void dispatchOnConnect(){
//            foreach(Listener listener in _listeners){
//                listener.OnConnect(this);
//            }
        }
        void dispatchOnDisconnect(){
//            foreach(Listener listener in _listeners){
//                listener.OnDisconnect(this);
//            }
        }
        void dispatchOnExit(){
//            foreach(Listener listener in _listeners){
//                listener.OnExit(this);
//            }
        }
        void dispatchOnFrame(eLeapEventType type, object data){
                for(int l = 0; l < _listeners.Count; l++){
                    if(HasMethod(_listeners[l], "OnFrame")){
                        _listeners[l].OnFrame(this);
                    }
                }
        }
        void dispatchOnFocusGained(){
//            foreach(Listener listener in _listeners){
//                listener.OnFocusLost(this);
//            }
        }
        void dispatchOnFocusLost(){
//            foreach(Listener listener in _listeners){
//                listener.OnFocusLost(this);
//            }
        }
        void dispatchOnServiceConnect(){
//            foreach(Listener listener in _listeners){
//                listener.OnServiceConnect(this);
//            }
        }
        void dispatchOnServiceDisconnect(){
//            foreach(Listener listener in _listeners){
//                listener.OnServiceDisconnect(this);
//            }
        }
        void dispatchOnDeviceChange(){
//            foreach(Listener listener in _listeners){
//                listener.OnDeviceChange(this);
//            }
        }
        void dispatchOnImages(eLeapEventType type, object data){
            try {
                for(int l = 0; l < _listeners.Count; l++){
                    if(HasMethod(_listeners[l], "OnImages"))
                        _listeners[l].OnImages(this);
                }
            } catch (Exception e){
                Logger.Log("Failed to dispatch OnImage event: " + e.Message);
            }
        }
        void dispatchOnServiceChange(){
//            foreach(Listener listener in _listeners){
//                listener.OnServiceChange(this);
//            }
        }
        void dispatchOnDeviceFailure(){
//            foreach(Listener listener in _listeners){
//                listener.OnDeviceFailure(this);
//            }
        }
        void dispatchOnLogEvent(string msg){
            try {
                for(int l = 0; l < _listeners.Count; l++){
                    if((_listeners != null) && HasMethod(_listeners[l], "OnLogMessage"))
                        _listeners[l].OnLogMessage(this, msg);
                }
            } catch (Exception e){
                Logger.Log(e.Message);
            }
        }
        private bool HasMethod(this object objectToCheck, string methodName)
        {
            var type = objectToCheck.GetType();
            return type.GetMethod(methodName) != null;
        }
    }

}