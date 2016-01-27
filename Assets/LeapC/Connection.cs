/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/

namespace LeapInternal
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Runtime.InteropServices;

    using Leap;

    public class Connection
    {
        private static Dictionary<int, Connection> connectionDictionary = new Dictionary<int, Connection> ();

        static Connection ()
        {
        }

        public static Connection GetConnection (int connectionKey = 0)
        {
            if (Connection.connectionDictionary.ContainsKey (connectionKey)) {
                Connection conn;
                Connection.connectionDictionary.TryGetValue (connectionKey, out conn);
                return conn;
            } else {
                Connection newConn = new Connection (connectionKey);
                connectionDictionary.Add (connectionKey, newConn);
                return newConn;
            }
        }

        public int ConnectionKey { get; private set; }
        public DistortionDictionary DistortionCache{ get; private set; }

        public CircularObjectBuffer<Frame> Frames{get; set;}
        private ServiceFrameFactory frameFactory = new ServiceFrameFactory ();
        private Queue<Frame> pendingFrames = new Queue<Frame> (); //Holds frames until images and tracked quad are available
        private DeviceList _devices;
        private FailedDeviceList _failedDevices;

        private CircularImageBuffer _irImageCache;
        private ObjectPool<ImageData> _irImageDataCache;
        private CircularImageBuffer _rawImageCache;
        private ObjectPool<ImageData> _rawImageDataCache;
        private CircularObjectBuffer<TrackedQuad> _quads;
        private int _frameBufferLength = 60;
        private int _imageBufferLength = 20;
        private int _quadBufferLength = 20;
        private bool _growImageMemory = false;
        private long _pendingFrameTimeOut = 10; //TODO determine optimum pending timeout value 

        private IntPtr _leapConnection;
        private Thread _polster;
        private bool _isRunning = false;

        //Policy and enabled features
        private UInt64 _cachedPolicies = 0;
        private bool _policiesAreDirty = false;
        private bool _imagesAreEnabled = false;
        private bool _rawImagesAreEnabled = false;
        private bool _trackedQuadsAreEnabled = false;

        //Connection events
        public EventHandler<LeapEventArgs> LeapInit;

        public EventHandler<ConnectionEventArgs> LeapConnection;
        public EventHandler<ConnectionLostEventArgs> LeapConnectionLost;
        public EventHandler<DeviceEventArgs> LeapDevice;
        public EventHandler<DeviceFailureEventArgs> LeapDeviceFailure;
        public EventHandler<DeviceLostEventArgs> LeapDeviceLost;
        public EventHandler<PolicyEventArgs> LeapPolicyChange;
        public EventHandler<FrameEventArgs> LeapFrame;
        public EventHandler<ImageEventArgs> LeapImageComplete;
        public EventHandler<TrackedQuadEventArgs> LeapTrackedQuad;
        public EventHandler<LogEventArgs> LeapLogEvent;
        public EventHandler<SetConfigResponseEventArgs> LeapConfigResponse;
        public EventHandler<ConfigChangeEventArgs> LeapConfigChange;
        public EventHandler<DistortionEventArgs> LeapDistortionChange;

        private bool _disposed = false;
        private bool _needToCheckPendingFrames = false;

        //TODO revisit dispose code
        public void Dispose ()
        { 
            Dispose (true);
            GC.SuppressFinalize (this);
        }
        
        // Protected implementation of Dispose pattern.
        protected virtual void Dispose (bool disposing)
        {
            if (_disposed)
                return; 
            
            if (disposing) {
                Stop ();
            }
            
            _disposed = true;
        }

        private Connection (int connectionKey)
        {
            ConnectionKey = connectionKey;
            _leapConnection = IntPtr.Zero;

            Frames = new CircularObjectBuffer<Frame> (_frameBufferLength);
            _quads = new CircularObjectBuffer<TrackedQuad> (_quadBufferLength);
        }

        public void Start ()
        {
            if (!_isRunning) {
                if(_leapConnection == IntPtr.Zero){
                    eLeapRS result = LeapC.CreateConnection (out _leapConnection);
                    reportAbnormalResults("LeapC CreateConnection call was", result);
                    result = LeapC.OpenConnection (_leapConnection);
                    reportAbnormalResults("LeapC OpenConnection call was", result);
                }
                _isRunning = true;
                _polster = new Thread (new ThreadStart (this.processMessages));
                _polster.IsBackground = true;
                _polster.Start ();
            }
        }

        public void Stop ()
        {
            _isRunning = false;
        }

        //Run in Polster thread, fills in object queues
        private void processMessages ()
        {
            try {
                LeapInit.Dispatch<LeapEventArgs>(this, new LeapEventArgs(LeapEvent.EVENT_INIT));
                while (_isRunning) {
                    if (_leapConnection != IntPtr.Zero) {
                        LEAP_CONNECTION_MESSAGE _msg = new LEAP_CONNECTION_MESSAGE ();
                        eLeapRS result;
                        uint timeout = 1000; //TODO determine optimal timeout value
                        result = LeapC.PollConnection (_leapConnection, timeout, ref _msg);
                        reportAbnormalResults("LeapC PollConnection call was", result);

                        //Logger.Log ("Got Message of type " + Enum.GetName (typeof(eLeapEventType), _msg.type));
                        if (result == eLeapRS.eLeapRS_Success && _msg.type != eLeapEventType.eLeapEventType_None) {
                            switch (_msg.type) {
                            case eLeapEventType.eLeapEventType_Connection:
                                LEAP_CONNECTION_EVENT connection_evt = LeapC.PtrToStruct<LEAP_CONNECTION_EVENT> (_msg.eventStructPtr);
                                handleConnection (ref connection_evt);
                                break;
                            case eLeapEventType.eLeapEventType_ConnectionLost:
                                LEAP_CONNECTION_LOST_EVENT connection_lost_evt = LeapC.PtrToStruct<LEAP_CONNECTION_LOST_EVENT> (_msg.eventStructPtr);
                                handleConnectionLost (ref connection_lost_evt);
                                break;
                            case eLeapEventType.eLeapEventType_Device:
                                LEAP_DEVICE_EVENT device_evt = LeapC.PtrToStruct<LEAP_DEVICE_EVENT> (_msg.eventStructPtr);
                                handleDevice (ref device_evt);
                                break;
                            case eLeapEventType.eLeapEventType_DeviceLost:
                                LEAP_DEVICE_EVENT device_lost_evt = LeapC.PtrToStruct<LEAP_DEVICE_EVENT> (_msg.eventStructPtr);
                                handleLostDevice (ref device_lost_evt);
                                break;
                            case eLeapEventType.eLeapEventType_DeviceFailure:
                                LEAP_DEVICE_FAILURE_EVENT device_failure_evt = LeapC.PtrToStruct<LEAP_DEVICE_FAILURE_EVENT> (_msg.eventStructPtr);
                                handleFailedDevice (ref device_failure_evt);
                                break;
                            case eLeapEventType.eLeapEventType_Tracking:
                                LEAP_TRACKING_EVENT tracking_evt = LeapC.PtrToStruct<LEAP_TRACKING_EVENT> (_msg.eventStructPtr);
                                enqueueFrame (ref tracking_evt);
                                _needToCheckPendingFrames = true;
                                break;
                            case eLeapEventType.eLeapEventType_Image:
                                LEAP_IMAGE_EVENT image_evt = LeapC.PtrToStruct<LEAP_IMAGE_EVENT> (_msg.eventStructPtr);
                                startImage (ref image_evt);
                                break;
                            case eLeapEventType.eLeapEventType_ImageComplete:
                                LEAP_IMAGE_COMPLETE_EVENT image_complete_evt = LeapC.PtrToStruct<LEAP_IMAGE_COMPLETE_EVENT> (_msg.eventStructPtr);
                                completeImage (ref image_complete_evt);
                                _needToCheckPendingFrames = true;
                                break;
                            case eLeapEventType.eLeapEventType_TrackedQuad:
                                LEAP_TRACKED_QUAD_EVENT quad_evt = LeapC.PtrToStruct<LEAP_TRACKED_QUAD_EVENT> (_msg.eventStructPtr); 
                                handleQuadMessage (ref quad_evt);
                                _needToCheckPendingFrames = true;
                                break;
                            case eLeapEventType.eLeapEventType_LogEvent:
                                LEAP_LOG_EVENT log_evt = LeapC.PtrToStruct<LEAP_LOG_EVENT> (_msg.eventStructPtr);
                                reportLogMessage (ref log_evt);
                                break;
                            case eLeapEventType.eLeapEventType_PolicyChange:
                                LEAP_POLICY_EVENT policy_evt = LeapC.PtrToStruct<LEAP_POLICY_EVENT> (_msg.eventStructPtr);
                                handlePolicyChange (ref policy_evt);
                                break;
                            case eLeapEventType.eLeapEventType_ConfigChange:
                                LEAP_CONFIG_CHANGE_EVENT config_change_evt = LeapC.PtrToStruct<LEAP_CONFIG_CHANGE_EVENT> (_msg.eventStructPtr);
                                handleConfigChange (ref config_change_evt);
                                break;
                            case eLeapEventType.eLeapEventType_ConfigResponse:
                                LEAP_CONFIG_RESPONSE_EVENT config_response_evt = LeapC.PtrToStruct<LEAP_CONFIG_RESPONSE_EVENT> (_msg.eventStructPtr);
                                handleConfigResponse (ref config_response_evt);
                                break;
                            default:
                                //discard unknown message types
                                Logger.Log ("Unhandled message type " + Enum.GetName (typeof(eLeapEventType), _msg.type));
                                break;
                            } //switch on _msg.type
                        } // if valid _msg.type
                    } // if have connection handle
                    //Update policy flags if needed
                    if (_policiesAreDirty) {
                        UInt64 setFlags = _cachedPolicies;
                        UInt64 clearFlags = ~_cachedPolicies; //inverse of desired policies
                        UInt64 priorFlags;
                        eLeapRS result = LeapC.SetPolicyFlags (_leapConnection, setFlags, clearFlags, out priorFlags);
                        if (result == eLeapRS.eLeapRS_Success)
                            _policiesAreDirty = false;
                        else
                            reportAbnormalResults("LeapC SetPolicyFlags call was", result);
                        }
                    if(_needToCheckPendingFrames == true){
                        checkPendingFrames ();
                        _needToCheckPendingFrames = false;
                    }
                } //forever
            } catch (Exception e) {
                Logger.Log ("Exception: " + e);
            }
        }

        private void checkPendingFrames ()
        {
            if (pendingFrames.Count > 0) {
                Frame pending = pendingFrames.Peek ();
                if (isFrameReady (pending) || (pending.Timestamp < LeapC.GetNow () - _pendingFrameTimeOut)) { //is ready or too late to wait
                    pendingFrames.Dequeue ();
                    Frames.Put (pending);
                    this.LeapFrame.Dispatch<FrameEventArgs> (this, new FrameEventArgs (pending));
                    checkPendingFrames (); //check the next frame if this one was ready
                }
            }
        }

        private bool isFrameReady (Frame frame)
        {
            if ((!_imagesAreEnabled || frame.Images.Count == 2) &&
                (!_rawImagesAreEnabled || frame.RawImages.Count == 2) &&
                (!_trackedQuadsAreEnabled || frame.TrackedQuad.IsValid)){
                return true;
            }

            return false;
        }

        private void enqueueFrame (ref LEAP_TRACKING_EVENT trackingMsg)
        {
            Frame newFrame = frameFactory.makeFrame (ref trackingMsg);
            if (_imagesAreEnabled) {
                Image left;
                Image right;
                if (_irImageCache.GetImagesForFrame (newFrame.Id, out left, out right)) {
                    newFrame.Images.Add (left);
                    newFrame.Images.Add (right);
                }
            }
            if (_rawImagesAreEnabled) {
                Image left;
                Image right;
                if (_rawImageCache.GetImagesForFrame (newFrame.Id, out left, out right)) {
                    newFrame.RawImages.Add (left);
                    newFrame.RawImages.Add (right);
                }
            }
            if (_trackedQuadsAreEnabled)
                newFrame.TrackedQuad = this.findTrackQuadForFrame (newFrame.Id);

            pendingFrames.Enqueue (newFrame);
        }

        private void enableIRImages ()
        {
            //Create image buffers if images turned on
            if (_irImageDataCache == null) {
                _irImageDataCache = new ObjectPool<ImageData> (_imageBufferLength, _growImageMemory);
                _irImageCache = new CircularImageBuffer (_imageBufferLength);
            }
            if (DistortionCache == null) {
                DistortionCache = new DistortionDictionary ();
            }
            _imagesAreEnabled = true;
        }
        private void enableRawImages ()
        {
            //Create image buffers if images turned on
            if (_rawImageDataCache == null) {
                _rawImageDataCache = new ObjectPool<ImageData> (_imageBufferLength, _growImageMemory);

                _rawImageCache = new CircularImageBuffer (_imageBufferLength);
            }
            if (DistortionCache == null) {
                DistortionCache = new DistortionDictionary ();
            }
            _rawImagesAreEnabled = true;
        }

        private void startImage (ref LEAP_IMAGE_EVENT imageMsg)
        {
            if(_imagesAreEnabled || _rawImagesAreEnabled){
                ImageData newImageData = _irImageDataCache.CheckOut ();
                newImageData.poolIndex = imageMsg.image.index;
                if (newImageData.pixelBuffer == null || (ulong)newImageData.pixelBuffer.Length != imageMsg.image_size) {
                    newImageData.pixelBuffer = new byte[imageMsg.image_size];
                }
                eLeapRS result = LeapC.SetImageBuffer (ref imageMsg.image, newImageData.getPinnedHandle (), imageMsg.image_size);
                reportAbnormalResults("LeapC SetImageBuffer call was", result);
            }
            //If image policies have been turned off, then discard the images
            eLeapRS discardResult = LeapC.SetImageBuffer (ref imageMsg.image, IntPtr.Zero, 0);
            reportAbnormalResults("LeapC SetImageBuffer(0,0,0) call was", discardResult);
        }

        private void completeImage (ref LEAP_IMAGE_COMPLETE_EVENT imageMsg)
        {
            LEAP_IMAGE_PROPERTIES props = LeapC.PtrToStruct<LEAP_IMAGE_PROPERTIES> (imageMsg.properties);
            ImageData pendingImageData = _irImageDataCache.FindByPoolIndex (imageMsg.image.index);
            if (pendingImageData != null) {
                pendingImageData.unPinHandle (); //Done with pin for unmanaged code

                DistortionData distData;
                if (!DistortionCache.TryGetValue (imageMsg.matrix_version, out distData)){//if fails, then create new entry
                    distData = new DistortionData ();
                    distData.version = imageMsg.matrix_version;
                    distData.width = 64; //fixed value for now
                    distData.height = 64; //fixed value for now
                    distData.data = new float[(int)(2 * distData.width * distData.height)]; //2 float values per map point
                    LEAP_DISTORTION_MATRIX matrix = LeapC.PtrToStruct<LEAP_DISTORTION_MATRIX> (imageMsg.distortionMatrix);
                    Array.Copy (matrix.matrix_data, distData.data, matrix.matrix_data.Length);
                    DistortionCache.Add ((UInt64)imageMsg.matrix_version, distData);
                }

                //Signal distortion data change if necessary
                if ((props.perspective == eLeapPerspectiveType.eLeapPerspectiveType_stereo_left) && (imageMsg.matrix_version != DistortionCache.CurrentLeftMatrix) ||
                    (props.perspective == eLeapPerspectiveType.eLeapPerspectiveType_stereo_right) && (imageMsg.matrix_version != DistortionCache.CurrentRightMatrix)) { //then the distortion matrix has changed
                    DistortionCache.DistortionChange = true;
                    this.LeapDistortionChange.Dispatch<DistortionEventArgs>(this, new DistortionEventArgs());
                } else {
                    DistortionCache.DistortionChange = false; // clear old change flag
                }
                if (props.perspective == eLeapPerspectiveType.eLeapPerspectiveType_stereo_left) {
                    DistortionCache.CurrentLeftMatrix = imageMsg.matrix_version;
                } else {
                    DistortionCache.CurrentRightMatrix = imageMsg.matrix_version;
                }

                Image newImage = frameFactory.makeImage (ref imageMsg, pendingImageData, distData);
                if(props.type == eLeapImageType.eLeapImageType_Default){
                    _irImageCache.Put (newImage);
                    for(int f = 0; f < pendingFrames.Count; f++){
                        Frame frame = pendingFrames.Dequeue();
                        if (frame.Id == newImage.Id)
                            frame.Images.Add (newImage);
                        pendingFrames.Enqueue (frame);
                    }
                } else {
                    _rawImageCache.Put (newImage);
                    for(int f = 0; f < pendingFrames.Count; f++){
                        Frame frame = pendingFrames.Dequeue();
                        if (frame.Id == newImage.Id)
                            frame.RawImages.Add (newImage);
                        pendingFrames.Enqueue (frame);
                    }
                }

                this.LeapImageComplete.Dispatch<ImageEventArgs> (this, new ImageEventArgs (newImage));
            }
        }

        private void handleQuadMessage(ref LEAP_TRACKED_QUAD_EVENT quad_evt){
            TrackedQuad quad = frameFactory.makeQuad (ref quad_evt);
            _quads.Put(quad);

            //TODO rework pending frame lookup -- if frame leaves queue because of timeout, it will never get its quads or images
            for(int f = 0; f < pendingFrames.Count; f++){
                Frame frame = pendingFrames.Dequeue();
                if (frame.Id == quad.Id)
                    frame.TrackedQuad = quad;
                
                pendingFrames.Enqueue (frame);
            }
        }

        private void handleConnection (ref LEAP_CONNECTION_EVENT connectionMsg)
        {
            Logger.Log ("Update Connection Message");
            Logger.LogStruct (connectionMsg);
            //TODO update connection on CONNECTION_EVENT
            this.LeapConnection.Dispatch<ConnectionEventArgs> (this, new ConnectionEventArgs()); //TODO Meaningful Connection event args
        }

        private void handleConnectionLost (ref LEAP_CONNECTION_LOST_EVENT connectionMsg)
        {
            Logger.Log ("Update Connection Message");
            Logger.LogStruct (connectionMsg);
            //TODO update connection on CONNECtiON_LOST_EVENT
            this.LeapConnectionLost.Dispatch<ConnectionLostEventArgs> (this, new ConnectionLostEventArgs()); //TODO Meaningful ConnectionLost event args
        }

        private void handleDevice (ref LEAP_DEVICE_EVENT deviceMsg)
        {
            Logger.Log ("Device Message");
            Logger.LogStruct (deviceMsg);
            if (_devices == null)
                this.initializeDeviceList ();
            this.LeapDevice.Dispatch<DeviceEventArgs> (this, new DeviceEventArgs()); //TODO Meaningful Device event args
        }
        private void handleLostDevice (ref LEAP_DEVICE_EVENT deviceMsg)
        {
            Logger.Log ("Lost Device Message");
        }

        private void handleFailedDevice (ref LEAP_DEVICE_FAILURE_EVENT deviceMsg)
        {
            Logger.Log ("Failed Device Message");
            Logger.LogStruct (deviceMsg);
            //TODO Check validity of existing devices
            this.LeapDeviceFailure.Dispatch<DeviceFailureEventArgs> (this, new DeviceFailureEventArgs()); //TODO Meaningful Device Failure event args

        }

        private void handleConfigChange (ref LEAP_CONFIG_CHANGE_EVENT configEvent)
        {
            Logger.Log ("Config change >>>>>>>>>>>>>>>>>>>>>");
            Logger.LogStruct (configEvent);
            this.LeapConfigChange.Dispatch<ConfigChangeEventArgs>(this, new ConfigChangeEventArgs()); //TODO Meaningful config change Failure event args
        }
        
        private void handleConfigResponse (ref LEAP_CONFIG_RESPONSE_EVENT configEvent)
        {
            Logger.Log ("Config response >>>>>>>>>>>>>>>>>>>>>");
            
            Logger.LogStruct (configEvent);
            
            Logger.LogStruct (configEvent.value);
            this.LeapConfigResponse.Dispatch<SetConfigResponseEventArgs>(this, new SetConfigResponseEventArgs()); //TODO Meaningful config response Failure event args
        }

        private void initializeDeviceList ()
        {
            //Get device count
            UInt32 deviceCount = 0;
            eLeapRS result = LeapC.GetDeviceCount (_leapConnection, out deviceCount);
            if (deviceCount > 0) {
                _devices = new DeviceList ();
                UInt32 validDeviceHandles = deviceCount;
                LEAP_DEVICE_REF[] deviceRefList = new LEAP_DEVICE_REF[deviceCount];
                result = LeapC.GetDeviceList (_leapConnection, deviceRefList, out validDeviceHandles);
                if (result == eLeapRS.eLeapRS_Success) {
                    for (int d = 0; d < validDeviceHandles; d++) {
                        IntPtr device;
                        if (deviceRefList [d].handle != IntPtr.Zero) {
                            LeapC.OpenDevice (deviceRefList [d], out device);
                            LEAP_DEVICE_INFO deviceInfo;
                            int defaultLength = 14;
                            deviceInfo.serial_length = (uint)defaultLength;
                            deviceInfo.serial = Marshal.AllocCoTaskMem (defaultLength);
                            deviceInfo.size = 0;
                            deviceInfo.baseline = 0;
                            deviceInfo.caps = 0;
                            deviceInfo.h_fov = 0;
                            deviceInfo.range = 0;
                            deviceInfo.status = 0;
                            deviceInfo.type = eLeapDeviceType.eLeapDeviceType_Peripheral;
                            deviceInfo.v_fov = 0;
                            deviceInfo.size = (uint)Marshal.SizeOf (deviceInfo);
                            result = LeapC.GetDeviceInfo (device, out deviceInfo);
                            while (result == eLeapRS.eLeapRS_InsufficientBuffer) {
                                deviceInfo.serial = Marshal.AllocCoTaskMem ((int)deviceInfo.serial_length + 10); //TODO modify when length bug is fixed
                                deviceInfo.size = (uint)Marshal.SizeOf (deviceInfo);
                                result = LeapC.GetDeviceInfo (device, out deviceInfo);
                            }
                            Device apiDevice = new Device (deviceRefList [d].handle,
                                                           deviceInfo.h_fov, //radians
                                                           deviceInfo.v_fov, //radians
                                                           deviceInfo.range / 1000, //to mm 
                                                           deviceInfo.baseline / 1000, //to mm 
                                                           (deviceInfo.caps == (UInt32)eLeapDeviceCaps.eLeapDeviceCaps_Embedded),
                                                           (deviceInfo.status == (UInt32)eLeapDeviceStatus.eLeapDeviceStatus_Streaming),
                                                           Marshal.PtrToStringAnsi (deviceInfo.serial));
                            _devices.Add (apiDevice);
                        }
                    }
                }
            }
            Logger.Log ("Device Count: " + _devices.Count);
        }

        private void reportLogMessage (ref LEAP_LOG_EVENT logMsg)
        {
            Logger.LogStruct (logMsg);
            this.LeapLogEvent.Dispatch<LogEventArgs>(this, new LogEventArgs (publicSeverity(logMsg.severity), logMsg.timestamp, logMsg.message));
        }

        private MessageSeverity publicSeverity(eLeapLogSeverity leapCSeverity){
            switch(leapCSeverity){
                case eLeapLogSeverity.eLeapLogSeverity_Unknown:
                    return MessageSeverity.MESSAGE_UNKNOWN;
                case eLeapLogSeverity.eLeapLogSeverity_Information:
                    return MessageSeverity.MESSAGE_INFORMATION;
                case eLeapLogSeverity.eLeapLogSeverity_Warning:
                    return MessageSeverity.MESSAGE_WARNING;
                case eLeapLogSeverity.eLeapLogSeverity_Critical:
                    return MessageSeverity.MESSAGE_CRITICAL;
                default:
                    return MessageSeverity.MESSAGE_UNKNOWN;
            }
        }

        private void handlePolicyChange (ref LEAP_POLICY_EVENT policyMsg)
        {
            this.LeapPolicyChange.Dispatch<PolicyEventArgs> (this, new PolicyEventArgs (policyMsg.current_policy, _cachedPolicies));

            _cachedPolicies = policyMsg.current_policy;

            if ((policyMsg.current_policy & (UInt64)eLeapPolicyFlag.eLeapPolicyFlag_Images) 
                == (UInt64)eLeapPolicyFlag.eLeapPolicyFlag_Images)
                enableIRImages ();

            if ((policyMsg.current_policy & (UInt64)eLeapPolicyFlag.eLeapPolicyFlag_RawImages) 
                == (UInt64)eLeapPolicyFlag.eLeapPolicyFlag_RawImages)
                enableRawImages ();

            //TODO Handle other (non-image) policy changes; handle policy disable
        }

        public void SetPolicy (Controller.PolicyFlag policy)
        {
            UInt64 setFlags = (ulong)flagForPolicy (policy);
            _cachedPolicies = _cachedPolicies | setFlags;
            _policiesAreDirty = true;
            setFlags = _cachedPolicies;
            UInt64 clearFlags = ~_cachedPolicies; //inverse of desired policies
            UInt64 priorFlags;

            eLeapRS result = LeapC.SetPolicyFlags (_leapConnection, setFlags, clearFlags, out priorFlags);
            reportAbnormalResults("LeapC SetPolicyFlags call was", result);
        }
        
        public void ClearPolicy (Controller.PolicyFlag policy)
        {
            UInt64 clearFlags = (ulong)flagForPolicy (policy);
            _cachedPolicies = _cachedPolicies & ~clearFlags;
            _policiesAreDirty = true; //request occurs in message loop
        }
        
        private eLeapPolicyFlag flagForPolicy (Controller.PolicyFlag singlePolicy)
        {
            switch (singlePolicy) {
            case Controller.PolicyFlag.POLICY_BACKGROUND_FRAMES:
                return eLeapPolicyFlag.eLeapPolicyFlag_BackgroundFrames;
            case Controller.PolicyFlag.POLICY_OPTIMIZE_HMD:
                return eLeapPolicyFlag.eLeapPolicyFlag_OptimizeHMD;
            case Controller.PolicyFlag.POLICY_IMAGES:
                return eLeapPolicyFlag.eLeapPolicyFlag_Images;
            case Controller.PolicyFlag.POLICY_RAW_IMAGES:
                return eLeapPolicyFlag.eLeapPolicyFlag_RawImages;
            case Controller.PolicyFlag.POLICY_DEFAULT:
                return 0;
            default:
                return 0;
            }
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
            UInt64 policyToCheck = (ulong)flagForPolicy (policy);
            
            UInt64 setFlags = 0;
            UInt64 clearFlags = 0;
            UInt64 priorFlags;
            eLeapRS result = LeapC.SetPolicyFlags (_leapConnection, setFlags, clearFlags, out priorFlags);
            if (result == eLeapRS.eLeapRS_Success) {
                return (priorFlags & policyToCheck) == policyToCheck;
            } else {
                reportAbnormalResults("LeapC SetPolicyFlags call was", result);
                return (_cachedPolicies & policyToCheck) == policyToCheck;
            }
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
     * Reports whether your application has a connection to the Leap Motion
     * daemon/service. Can be true even if the Leap Motion hardware is not available.
     * @since 1.2
     */
        public bool IsServiceConnected {
            get {
                if (_leapConnection == IntPtr.Zero)
                    return false;
                
                LEAP_CONNECTION_INFO pInfo;
                eLeapRS result = LeapC.GetConnectionInfo (_leapConnection, out pInfo);
                reportAbnormalResults("LeapC GetConnectionInfo call was", result);

                if (pInfo.status == eLeapConnectionStatus.eLeapConnectionStatus_Connected)
                    return true;
                
                return false;
            }
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
                return IsServiceConnected && Devices.Count > 0;
            } 
        }

        public bool GetLatestImagePair (out Image left, out Image right)
        {
            if (!_imagesAreEnabled) {
                left = null;
                right = null;
                return false;
            }
            return _irImageCache.GetLatestImages (out left, out right);
        }

        public bool GetLatestRawImagePair (out Image left, out Image right)
        {
            if (!_rawImagesAreEnabled) {
                left = null;
                right = null;
                return false;
            }
            return _rawImageCache.GetLatestImages (out left, out right);
        }

        public bool GetFrameImagePair (long frameId, out Image left, out Image right)
        {
            if (!_imagesAreEnabled) {
                left = null;
                right = null;
                return false;
            }
            return _irImageCache.GetImagesForFrame (frameId, out left, out right);
        }

        private TrackedQuad findTrackQuadForFrame (long frameId)
        {
            TrackedQuad quad = null;
            for (int q = 0; q < _quads.Count; q++) {
                quad = _quads.Get (q);
                if (quad.Id == frameId)
                    return quad;
                if (quad.Id < frameId)
                    break;
            }
            return quad; //null
        }

        public TrackedQuad GetLatestQuad ()
        {
            return _quads.Get (0);
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
                if (_devices == null) {
                    _devices = new DeviceList ();
                }

                return _devices;
            } 
        }

        public FailedDeviceList FailedDevices {
            get {
                if (_failedDevices == null) {
                    _failedDevices = new FailedDeviceList ();
                }
                
                return _failedDevices;
            } 
        }

        public bool IsPaused {
            get {
                return false; //TODO implement IsPaused
            }
        }

        public void SetPaused (bool newState)
        {
            //TODO implement pausing
        }

        private eLeapRS _lastResult;
        private void reportAbnormalResults(string context, eLeapRS result){
            if(result != eLeapRS.eLeapRS_Success &&
               result != _lastResult){
                string msg = context + " " + result;
                Logger.Log (msg);
                this.LeapLogEvent.Dispatch<LogEventArgs>(this, 
                      new LogEventArgs(MessageSeverity.MESSAGE_CRITICAL,
                                       LeapC.GetNow(),
                                       msg)
                );
            }
            _lastResult = result;
        }
    }
}
