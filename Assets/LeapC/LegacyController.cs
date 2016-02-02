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
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Runtime.InteropServices;

    using LeapInternal;

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

    public class LegacyController : Controller
    {
        List<Listener> _listeners = new List<Listener>();

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
        public LegacyController (Listener listener) : base()
        {
            this.AddListener (listener);
        }

        public LegacyController() : base(){}
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

        protected override void OnInit(object sender, LeapEventArgs eventArgs){
            //            foreach(Listener listener in _listeners){
            //                listener.OnInit(this);
            //            }
        }
        protected override void OnConnect(object sender, LeapEventArgs eventArgs){
            //            foreach(Listener listener in _listeners){
            //                listener.OnConnect(this);
            //            }
        }
        protected override void OnDisconnect(object sender, LeapEventArgs eventArgs){
            //            foreach(Listener listener in _listeners){
            //                listener.OnDisconnect(this);
            //            }
        }
        protected override void OnExit(object sender, LeapEventArgs eventArgs){
            //            foreach(Listener listener in _listeners){
            //                listener.OnExit(this);
            //            }
        }
        protected override void OnFrame(object sender, FrameEventArgs eventArgs){

            for(int l = 0; l < _listeners.Count; l++){
                if(_listeners[l].HasMethod("OnFrame")){
                    _listeners[l].OnFrame(this);
                }
            }
        }
        protected override void OnFocusGained(object sender, LeapEventArgs eventArgs){
            //            foreach(Listener listener in _listeners){
            //                listener.OnFocusLost(this);
            //            }
        }
        protected override void OnFocusLost(object sender, LeapEventArgs eventArgs){
            //            foreach(Listener listener in _listeners){
            //                listener.OnFocusLost(this);
            //            }
        }
        protected override void OnServiceConnect(object sender, LeapEventArgs eventArgs){
            //            foreach(Listener listener in _listeners){
            //                listener.OnServiceConnect(this);
            //            }
        }
        protected override void OnServiceDisconnect(object sender, LeapEventArgs eventArgs){
            //            foreach(Listener listener in _listeners){
            //                listener.OnServiceDisconnect(this);
            //            }
        }
        protected override void OnDevice(object sender, DeviceEventArgs eventArgs){
            //            foreach(Listener listener in _listeners){
            //                listener.OnDeviceChange(this);
            //            }
        }
        protected override void OnDeviceLost(object sender, DeviceEventArgs eventArgs){
            //            foreach(Listener listener in _listeners){
            //                listener.OnDeviceChange(this);
            //            }
        }
        protected override void OnImages(object sender, ImageEventArgs eventArgs){
            try {
                for(int l = 0; l < _listeners.Count; l++){
                    if(_listeners[l].HasMethod("OnImages"))
                        _listeners[l].OnImages(this);
                }
            } catch (Exception e){
                Logger.Log("Failed to dispatch OnImage event: " + e.Message);
            }
        }
        protected override void OnServiceChange(object sender, LeapEventArgs eventArgs){
            //            foreach(Listener listener in _listeners){
            //                listener.OnServiceChange(this);
            //            }
        }
        protected override void OnDeviceFailure(object sender, DeviceFailureEventArgs eventArgs){
            //            foreach(Listener listener in _listeners){
            //                listener.OnDeviceFailure(this);
            //            }
        }
        protected override void OnLogEvent(object sender, LogEventArgs eventArgs){
            try {
                LogEventArgs args = eventArgs as LogEventArgs;
                for(int l = 0; l < _listeners.Count; l++){
                    if((_listeners != null) && _listeners[l].HasMethod("OnLogMessage"))
                        _listeners[l].OnLogMessage(this, args.message); //TODO use severity and timestamp in Log event
                }
            } catch (Exception e){
                Logger.Log(e.Message);
            }
        }
        
        protected override void OnPolicyChange(object sender, PolicyEventArgs eventArgs){
            try {
                for(int l = 0; l < _listeners.Count; l++){
                    if(_listeners[l].HasMethod("OnPolicyChange"))
                        _listeners[l].OnPolicyChange(this);
                }
            } catch (Exception e){
                Logger.Log("Failed to dispatch OnPolicyChange event: " + e.Message);
            }
        }
        protected override void OnConfigChange(object sender, ConfigChangeEventArgs eventArgs){}
        protected override void OnDistortionChange(object sender, DistortionEventArgs eventArgs){}
        protected override void OnTrackedQuad(object sender, TrackedQuadEventArgs eventArgs){}

    }

}
