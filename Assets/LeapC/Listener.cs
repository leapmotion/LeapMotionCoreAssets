namespace Leap {

using System;
using System.Runtime.InteropServices;

  /**
   * The Listener class defines a set of callback functions that you can
   * override in a subclass to respond to events dispatched by the Controller object.
   *
   * To handle Leap Motion events, create an instance of a Listener subclass and assign
   * it to the Controller instance. The Controller calls the relevant Listener
   * callback function when an event occurs, passing in a reference to itself.
   * You do not have to implement callbacks for events you do not want to handle.
   *
   * The Controller object calls these Listener functions from a thread created
   * by the Leap Motion library, not the thread used to create or set the Listener instance.
   * @since 1.0
   */
    //TODO implement Listener
public class Listener : IDisposable {

  ~Listener() {
    Dispose();
  }

  public virtual void Dispose() {
  }

    /**
     * Constructs a Listener object.
     * @since 1.0
     */
  public Listener() {
  }

    /**
     * Called once, when this Listener object is newly added to a Controller.
     *
     * \include Listener_onInit.txt
     *
     * @param controller The Controller object invoking this callback function.
     * @since 1.0
     */
  public virtual void OnInit(Controller arg0) {
  }

    /**
     * Called when the Controller object connects to the Leap Motion software and
     * the Leap Motion hardware device is plugged in,
     * or when this Listener object is added to a Controller that is already connected.
     *
     * When this callback is invoked, Controller::isServiceConnected is true,
     * Controller::devices() is not empty, and, for at least one of the Device objects in the list,
     * Device::isStreaming() is true.
     *
     * \include Listener_onConnect.txt
     *
     * @param controller The Controller object invoking this callback function.
     * @since 1.0
     */
  public virtual void OnConnect(Controller arg0) {
  }

    /**
     * Called when the Controller object disconnects from the Leap Motion software or
     * the Leap Motion hardware is unplugged.
     * The controller can disconnect when the Leap Motion device is unplugged, the
     * user shuts the Leap Motion software down, or the Leap Motion software encounters an
     * unrecoverable error.
     *
     * \include Listener_onDisconnect.txt
     *
     * Note: When you launch a Leap-enabled application in a debugger, the
     * Leap Motion library does not disconnect from the application. This is to allow
     * you to step through code without losing the connection because of time outs.
     *
     * @param controller The Controller object invoking this callback function.
     * @since 1.0
     */
  public virtual void OnDisconnect(Controller arg0) {
  }

    /**
     * Called when this Listener object is removed from the Controller
     * or the Controller instance is destroyed.
     *
     * \include Listener_onExit.txt
     *
     * @param controller The Controller object invoking this callback function.
     * @since 1.0
     */
  public virtual void OnExit(Controller arg0) {
  }

    /**
     * Called when a new frame of hand and finger tracking data is available.
     * Access the new frame data using the Controller::frame() function.
     *
     * \include Listener_onFrame.txt
     *
     * Note, the Controller skips any pending onFrame events while your
     * onFrame handler executes. If your implementation takes too long to return,
     * one or more frames can be skipped. The Controller still inserts the skipped
     * frames into the frame history. You can access recent frames by setting
     * the history parameter when calling the Controller::frame() function.
     * You can determine if any pending onFrame events were skipped by comparing
     * the ID of the most recent frame with the ID of the last received frame.
     *
     * @param controller The Controller object invoking this callback function.
     * @since 1.0
     */
  public virtual void OnFrame(Controller arg0) {
  }

    /**
     * Called when this application becomes the foreground application.
     *
     * Only the foreground application receives tracking data from the Leap
     * Motion Controller. This function is only called when the controller
     * object is in a connected state.
     *
     * \include Listener_onFocusGained.txt
     *
     * @param controller The Controller object invoking this callback function.
     * @since 1.0
     */
  public virtual void OnFocusGained(Controller arg0) {
  }

    /**
     * Called when this application loses the foreground focus.
     *
     * Only the foreground application receives tracking data from the Leap
     * Motion Controller. This function is only called when the controller
     * object is in a connected state.
     *
     * \include Listener_onFocusLost.txt
     *
     * @param controller The Controller object invoking this callback function.
     * @since 1.0
     */
  public virtual void OnFocusLost(Controller arg0) {
  }

    /**
     * Called when the Leap Motion daemon/service connects to your application Controller.
     *
     * \include Listener_onServiceConnect.txt
     *
     * @param controller The Controller object invoking this callback function.
     * @since 1.2
     */
  public virtual void OnServiceConnect(Controller arg0) {
  }

    /**
     * Called if the Leap Motion daemon/service disconnects from your application Controller.
     *
     * Normally, this callback is not invoked. It is only called if some external event
     * or problem shuts down the service or otherwise interrupts the connection.
     *
     * \include Listener_onServiceDisconnect.txt
     *
     * @param controller The Controller object invoking this callback function.
     * @since 1.2
     */
  public virtual void OnServiceDisconnect(Controller arg0) {
  }

    /**
     * Called when a Leap Motion controller is plugged in, unplugged, or the device changes state.
     *
     * State changes include entering or leaving robust mode and low resource mode.
     * Note that there is no direct way to query whether the device is in these modes,
     * although you can use Controller::isLightingBad() to check if there are environmental
     * IR lighting problems.
     *
     * \include Listener_onDeviceChange.txt
     *
     * @param controller The Controller object invoking this callback function.
     * @since 1.2
     */
  public virtual void OnDeviceChange(Controller arg0) {
  }

    /**
     * Called when new images are available.
     * Access the new frame data using the Controller::images() function.
     *
     * \include Listener_onImages.txt
     *
     * @param controller The Controller object invoking this callback function.
     * @since 2.2.1
     */
  public virtual void OnImages(Controller arg0) {
  }

   /**
    * Called when the Leap Motion service is paused or resumed or when a 
    * controller policy is changed.
    *
    * The service can change states because the computer user changes settings
    * in the Leap Motion Control Panel application or because an application
    * connected to the service triggers a change. Any application can pause or
    * unpause the service, but only runtime policy changes you make apply to your
    * own application.
    *
    * \include Listener_onServiceChange.txt
    *
    * You can query the pause state of the controller with Controller::isPaused().
    * You can check the state of those policies you are interested in with
    * Controller::isPolicySet().
    *
    * @param controller The Controller object invoking this callback function.
    * @since banana
    */
  public virtual void OnServiceChange(Controller arg0) {
  }

   /**
    * Called when a Leap Motion controller device is plugged into the client
    * computer, but fails to operate properly.
    *
    * Get the list containing all failed devices using Controller::failedDevices().
    * The members of this list provide the device pnpID and reason for failure.
    *
    * \include Listener_onDeviceFailure.txt
    *
    * @param controller The Controller object invoking this callback function.
    * @since banana
    */
  public virtual void OnDeviceFailure(Controller arg0) {
  }

   /**
    * Called when the results of a device diagnostic check are available.
    * 
    * \include Listener_onDiagnosticEvent.txt
    *
    * Request a device diagnostic check using Controller::requestDiagnostic().
    *
    * @param controller The Controller object invoking this callback function.
    * @param msg The diagnostic results.
    * @since banana
    */
  public virtual void OnLogMessage(Controller arg0, string msg) {
  }


}

}
