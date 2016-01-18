/******************************************************************************\
* Copyright (C) 2012-2015 Leap Motion, Inc. All rights reserved.               *
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

    using System.IO; //needed for temporary debugging


    public class Connection
    {
        private static Dictionary<int, Connection> connectionDictionary = new Dictionary<int, Connection>();
        static Connection(){}
        public static Connection GetConnection(int connectionKey = 0){
            if(Connection.connectionDictionary.ContainsKey(connectionKey)){
                Connection conn;
                Connection.connectionDictionary.TryGetValue(connectionKey, out conn);
                return conn;
            } else {
                Connection newConn = new Connection(connectionKey);
                connectionDictionary.Add(connectionKey, newConn);
                return newConn;
            }
        }

        public int ConnectionKey {get; private set;}
        public CircularObjectBuffer<Frame> Frames;
        private Queue<Frame> pendingFrames = new Queue<Frame>(); //Holds frames until images and tracked quad are available

//        private Object _threadLock = new Object();
        private CircularImageBuffer _irImageCache;
        private ObjectPool<ImageData> _irImageDataCache;
        private CircularObjectBuffer<TrackedQuad> _quads;

        private int _frameBufferLength = 60;
        private int _imageBufferLength = 20;
        private int _quadBufferLength = 20;
        private bool _preallocateImageMemory = false;
        private bool _growImageMemory = false;

        private IntPtr _leapConnection;
        private Thread Polster;

        //Policy and enabled features
        private UInt64 _cachedPolicies = 0;
        private bool _policiesAreDirty = false;
        private bool _imagesAreEnabled = false;
        private bool _rawImagesAreEnabled = false;
        private bool _trackedQuadsAreEnabled = false;

        //event state
        private LeapCEventHandler[] _eventDelegates = new LeapCEventHandler[Enum.GetNames(typeof(eLeapEventType)).Length];

        DeviceList _devices;
        FailedDeviceList _failedDevices;
        public DistortionDictionary DistortionCache{get; private set;}

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
                Logger.Log ("Connection Disposing managed");
            }
            
            // Free any unmanaged objects here.
            //
            Logger.Log ("Connection Disposing rest");
            _disposed = true;
        }

        private Connection (int connectionKey)
        {

            Logger.Log ("Creating Connection");
            ConnectionKey = connectionKey;
            _leapConnection = IntPtr.Zero;

            Frames = new CircularObjectBuffer<Frame> (_frameBufferLength);
            _quads = new CircularObjectBuffer<TrackedQuad> (_quadBufferLength);
            try {
                eLeapRS result = LeapC.CreateConnection (out _leapConnection);
                if(result != eLeapRS.eLeapRS_Success)
                    Logger.Log ("LeapC CreateConnection call was " + result);
                result = LeapC.OpenConnection (_leapConnection);
                if(result != eLeapRS.eLeapRS_Success)
                    Logger.Log ("LeapC OpenConnection call was " + result);
            } catch (Exception e) {
                Logger.Log (e.Message);
            }
            Polster = new Thread (new ThreadStart (this.processMessages));
            Polster.IsBackground = true;
            Polster.Start ();
        }

        //Run in Polster thread, fills in object queues
        private void processMessages ()
        {
            try {
                while(!_disposed) {
                        if (_leapConnection != IntPtr.Zero) {
                            LEAP_CONNECTION_MESSAGE _msg = new LEAP_CONNECTION_MESSAGE ();
                            eLeapRS result;
                            uint timeout = 1000; //TODO determine optimal timeout value
                                result = LeapC.PollConnection (_leapConnection, timeout, ref _msg);
                                if(result != eLeapRS.eLeapRS_Success)
                                    Logger.Log ("LeapC SetPolicyFlags call was " + result);

                                //Logger.Log ("Got Message of type " + Enum.GetName (typeof(eLeapEventType), _msg.type));
                                if (result == eLeapRS.eLeapRS_Success && _msg.type != eLeapEventType.eLeapEventType_None) {
                                    switch (_msg.type) {
                                    case eLeapEventType.eLeapEventType_Connection:
                                        LEAP_CONNECTION_EVENT connection_evt = LeapC.PtrToStruct<LEAP_CONNECTION_EVENT>(_msg.eventStructPtr);
                                        updateConnection (ref connection_evt);
                                        break;
                                    case eLeapEventType.eLeapEventType_ConnectionLost:
                                        LEAP_CONNECTION_LOST_EVENT connection_lost_evt = LeapC.PtrToStruct<LEAP_CONNECTION_LOST_EVENT>(_msg.eventStructPtr);
                                        updateConnection (ref connection_lost_evt);
                                        break;
                                    case eLeapEventType.eLeapEventType_Device:
                                        LEAP_DEVICE_EVENT device_evt = LeapC.PtrToStruct<LEAP_DEVICE_EVENT>(_msg.eventStructPtr);
                                        updateDevices (ref device_evt);
                                        break;
                                    case eLeapEventType.eLeapEventType_DeviceFailure:
                                        LEAP_DEVICE_FAILURE_EVENT device_failure_evt = LeapC.PtrToStruct<LEAP_DEVICE_FAILURE_EVENT>(_msg.eventStructPtr);
                                        updateDevices (ref device_failure_evt);
                                        break;
                                    case eLeapEventType.eLeapEventType_Tracking:
                                        LEAP_TRACKING_EVENT tracking_evt = LeapC.PtrToStruct<LEAP_TRACKING_EVENT>(_msg.eventStructPtr);
                                        pushFrame (ref tracking_evt);
                                        break;
                                    case eLeapEventType.eLeapEventType_Image:
                                        LEAP_IMAGE_EVENT image_evt = LeapC.PtrToStruct<LEAP_IMAGE_EVENT>(_msg.eventStructPtr);
                                        startImage (ref image_evt);
                                        break;
                                    case eLeapEventType.eLeapEventType_ImageComplete:
                                        LEAP_IMAGE_COMPLETE_EVENT image_complete_evt = LeapC.PtrToStruct<LEAP_IMAGE_COMPLETE_EVENT>(_msg.eventStructPtr);
                                        completeImage (ref image_complete_evt);
                                        break;
                                    case eLeapEventType.eLeapEventType_TrackedQuad:
                                        LEAP_TRACKED_QUAD_EVENT quad_evt = LeapC.PtrToStruct<LEAP_TRACKED_QUAD_EVENT>(_msg.eventStructPtr); 
                                        makeQuad (ref quad_evt);
                                        break;
                                    case eLeapEventType.eLeapEventType_LogEvent:
                                        LEAP_LOG_EVENT log_evt = LeapC.PtrToStruct<LEAP_LOG_EVENT>(_msg.eventStructPtr);
                                        reportLogMessage (ref log_evt);
                                        break;
                                    case eLeapEventType.eLeapEventType_PolicyChange:
                                        LEAP_POLICY_EVENT policy_evt = LeapC.PtrToStruct<LEAP_POLICY_EVENT>(_msg.eventStructPtr);
                                        handlePolicyChange(ref policy_evt);
                                        break;
                                    default:
                                        //discard None and unknown message types
                                        Logger.Log ("Unhandled message type " + Enum.GetName (typeof(eLeapEventType), _msg.type));
                                        break;
                                    } //switch on _msg.type
                                } // if valid _msg.type
                        } // if have connection handle
                    //Update policy flags if needed
                    if(_policiesAreDirty){
                        UInt64 setFlags = _cachedPolicies;
                        UInt64 clearFlags = ~_cachedPolicies; //inverse of desired policies
                        UInt64 priorFlags;
                        eLeapRS result = LeapC.SetPolicyFlags (_leapConnection, setFlags, clearFlags, out priorFlags);
                        if(result == eLeapRS.eLeapRS_Success)
                            _policiesAreDirty = false;
                        else
                            Logger.Log ("LeapC SetPolicyFlags call result: " + result);
                    }
                    checkPendingFrames();
                } //forever
            }

            catch (Exception e) {
                Logger.Log ("Exception: " + e);
            }
        }

        private void checkPendingFrames(){
            for(int p = 0; p < pendingFrames.Count; p++){
                Frame pending = pendingFrames.Dequeue();
                if(isFrameReady(pending)){
                    Frames.Put(pending);
                    this.DistpatchLeapCEvent(eLeapEventType.eLeapEventType_Tracking, new FrameEventArgs(pending));
                }
                else{
                    pendingFrames.Enqueue(pending);
                    Logger.Log("Requeued pending frame " + pending.Id);
                }
            }
        }

        private bool isFrameReady(Frame frame){
            //TODO need better check for TrackedQuad
            if( (!_imagesAreEnabled || frame.Images.Count == 2) &&
                (!_rawImagesAreEnabled || frame.RawImages.Count == 2) &&
                (!_trackedQuadsAreEnabled || frame.TrackedQuad.IsValid))
                return true;

            return true;//disabled feature
        }

        private void pushFrame (ref LEAP_TRACKING_EVENT trackingMsg){
            Frame newFrame = makeFrame(ref trackingMsg);
            if(isFrameReady(newFrame)){
                Frames.Put(newFrame);
                this.DistpatchLeapCEvent(eLeapEventType.eLeapEventType_Tracking, new FrameEventArgs(newFrame));
            }
            else{
                pendingFrames.Enqueue (newFrame);
                Logger.Log("Enqueued pending frame " + newFrame.Id);
            }
        }

        public Frame makeFrame (ref LEAP_TRACKING_EVENT trackingMsg)
        {
            Frame newFrame = new Leap.Frame ((long)trackingMsg.info.frame_id, 
                                             (long)trackingMsg.info.timestamp, 
                                             trackingMsg.framerate,
                                             new InteractionBox(trackingMsg.interaction_box_center.ToLeapVector(), 
                                                                trackingMsg.interaction_box_size.ToLeapVector()));

            int handStructSize = Marshal.SizeOf (typeof(LEAP_HAND));
            int pHandArrayOffset = 0;
            for (int h = 0; h < trackingMsg.nHands; h++) {
                //TODO verify pointer arithmetic is valid on both 32 and 64 bit platforms
                LEAP_HAND hand = LeapC.PtrToStruct<LEAP_HAND>(new IntPtr (trackingMsg.pHands.ToInt64 () + pHandArrayOffset));
                pHandArrayOffset += handStructSize;

                Hand newHand = makeHand(ref hand, newFrame);
                newFrame.AddHand (newHand);
            }
            return newFrame;
        }

        private void startImage (ref LEAP_IMAGE_EVENT imageMsg)
        {
            ImageData newImageData = _irImageDataCache.CheckOut();
            newImageData.poolIndex = imageMsg.image.index;
            if(newImageData.pixelBuffer == null || (ulong)newImageData.pixelBuffer.Length != imageMsg.image_size){
                newImageData.pixelBuffer = new byte[imageMsg.image_size];
            }
            eLeapRS result = LeapC.SetImageBuffer(ref imageMsg.image, newImageData.getPinnedHandle(), imageMsg.image_size);
            if(result != eLeapRS.eLeapRS_Success)
                Logger.Log ("LeapC SetImageBuffer call was " + result);
        }

        private void completeImage (ref LEAP_IMAGE_COMPLETE_EVENT imageMsg)
        {
            LEAP_IMAGE_PROPERTIES props = LeapC.PtrToStruct<LEAP_IMAGE_PROPERTIES>(imageMsg.properties);
            ImageData pendingImageData = _irImageDataCache.FindByPoolIndex(imageMsg.image.index);
            if(pendingImageData != null){
                Image.FormatType apiImageType;
                switch (props.type){
                    case eLeapImageType.eLeapImageType_IR:
                        apiImageType = Image.FormatType.INFRARED;
                        break;
                    default:
                        apiImageType = Image.FormatType.INFRARED;
                        break;
                }
                Image.PerspectiveType apiPerspectiveType;
                switch (props.perspective){
                    case eLeapPerspectiveType.eLeapPerspectiveType_stereo_left:
                        apiPerspectiveType = Image.PerspectiveType.STEREO_LEFT;
                        break;
                    case eLeapPerspectiveType.eLeapPerspectiveType_stereo_right:
                        apiPerspectiveType = Image.PerspectiveType.STEREO_RIGHT;
                        break;
                    case eLeapPerspectiveType.eLeapPerspectiveType_mono:
                        apiPerspectiveType = Image.PerspectiveType.MONO;
                        break;
                    default:
                        apiPerspectiveType = Image.PerspectiveType.INVALID;
                        break;
                }

                if(!DistortionCache.VersionExists(imageMsg.matrix_version)){ //then create new entry
                    DistortionData distData = new DistortionData();
                    distData.version = imageMsg.matrix_version;
                    distData.width = 64; //fixed value for now
                    distData.height = 64; //fixed value for now
                    distData.data = new float[(int)(2 * distData.width * distData.height)]; 
                    LEAP_DISTORTION_MATRIX matrix = LeapC.PtrToStruct<LEAP_DISTORTION_MATRIX>(imageMsg.distortionMatrix);
                    Array.Copy (matrix.matrix_data, distData.data, matrix.matrix_data.Length);
                    DistortionCache.Add((UInt64)imageMsg.matrix_version, distData);
                }

                if((apiPerspectiveType == Image.PerspectiveType.STEREO_LEFT)  && (imageMsg.matrix_version != DistortionCache.CurrentLeftMatrix) ||
                   (apiPerspectiveType == Image.PerspectiveType.STEREO_RIGHT) && (imageMsg.matrix_version != DistortionCache.CurrentRightMatrix)){ //then the distortion matrix has changed
                    DistortionCache.DistortionChange = true;
                    //TODO raise distortion change event (after defining one)
                } else {
                    DistortionCache.DistortionChange = false; // clear old change
                }
                if(apiPerspectiveType == Image.PerspectiveType.STEREO_LEFT){
                    DistortionCache.CurrentLeftMatrix = imageMsg.matrix_version;
                } else {
                    DistortionCache.CurrentRightMatrix = imageMsg.matrix_version;
                }

                pendingImageData.CompleteImageData(apiImageType,
                                                   apiPerspectiveType,
                                                   props.bpp,
                                                   props.width,
                                                   props.height,
                                                   imageMsg.info.timestamp,
                                                   this.ConnectionKey,
                                                   imageMsg.info.frame_id,
                                                   .5f,
                                                   .5f,
                                                   .5f/LeapC.DistortionSize,
                                                   .5f/LeapC.DistortionSize,
                                                   LeapC.DistortionSize,
                                                   imageMsg.matrix_version);
                pendingImageData.unPinHandle(); //Done with pin for unmanaged code
                Image newImage = new Image(pendingImageData); //Create the public API object
                _irImageCache.Put(newImage);
                this.DistpatchLeapCEvent(eLeapEventType.eLeapEventType_ImageComplete, new ImageEventArgs(newImage));
            }
        }

        private void makeQuad (ref LEAP_TRACKED_QUAD_EVENT quadMsg)
        {
            Logger.Log (" ################ TrackedQuad ############################ ");
            this.DistpatchLeapCEvent(eLeapEventType.eLeapEventType_TrackedQuad, null); //TODO TrackedQuad event args
        }

        private void updateConnection (ref LEAP_CONNECTION_EVENT connectionMsg)
        {
            Logger.Log ("Update Connection Message");
            Logger.LogStruct (connectionMsg);
            //TODO update connection on CONNECtiON_EVENT
            this.DistpatchLeapCEvent(eLeapEventType.eLeapEventType_Connection, null); //TODO Connection event args
        }

        private void updateConnection (ref LEAP_CONNECTION_LOST_EVENT connectionMsg)
        {
            Logger.Log ("Update Connection Message");
            Logger.LogStruct (connectionMsg);
            //TODO update connection on CONNECtiON_LOST_EVENT
            this.DistpatchLeapCEvent(eLeapEventType.eLeapEventType_ConnectionLost, null); //TODO ConnectionLost event args
        }

        private void updateDevices (ref LEAP_DEVICE_EVENT deviceMsg)
        {
            Logger.Log ("Update Devices Message");
            Logger.LogStruct (deviceMsg);
            if(_devices == null)
                this.initializeDeviceList();
            this.DistpatchLeapCEvent(eLeapEventType.eLeapEventType_Device, null); //TODO Device event args
        }

        private void updateDevices (ref LEAP_DEVICE_FAILURE_EVENT deviceMsg)
        {
            Logger.Log ("Update Devices Message");
            Logger.LogStruct (deviceMsg);
            //TODO Check validity of existing devices
            this.DistpatchLeapCEvent(eLeapEventType.eLeapEventType_DeviceFailure, null); //TODO Device Failure event args

        }

        private void reportLogMessage (ref LEAP_LOG_EVENT logMsg)
        {
            Logger.LogStruct (logMsg);
            this.DistpatchLeapCEvent(eLeapEventType.eLeapEventType_LogEvent, new LogEventArgs(ref logMsg));
        }

        private void handlePolicyChange(ref LEAP_POLICY_EVENT policyMsg){
            this.DistpatchLeapCEvent(eLeapEventType.eLeapEventType_PolicyChange, 
                                     new PolicyEventArgs(policyMsg.current_policy, _cachedPolicies));

            _cachedPolicies = policyMsg.current_policy;

            //Create image buffer if images turned on
            if( (policyMsg.current_policy & (UInt64)eLeapPolicyFlag.eLeapPolicyFlag_Images) 
                == (UInt64)eLeapPolicyFlag.eLeapPolicyFlag_Images){
                if(_irImageDataCache == null){
                    _irImageDataCache = new ObjectPool<ImageData>(_imageBufferLength, _preallocateImageMemory, _growImageMemory);
                    _irImageCache = new CircularImageBuffer(_imageBufferLength);
                }
                if(DistortionCache == null){
                    DistortionCache = new DistortionDictionary();
                }
                _imagesAreEnabled = true;
            }

            //TODO Handle other (non-image) policy changes; handle policy disable
        }

        public Hand makeHand(ref LEAP_HAND hand, Frame owningFrame){
            LEAP_BONE arm = LeapC.PtrToStruct<LEAP_BONE>(hand.arm);
            Arm newArm = makeArm (ref arm);
            LEAP_PALM palm = LeapC.PtrToStruct<LEAP_PALM>(hand.palm);
            
            Hand newHand = new Hand ((int)hand.id,
                                     hand.confidence,
                                     hand.grab_strength,
                                     hand.pinch_strength,
                                     palm.width,
                                     hand.type == eLeapHandType.eLeapHandType_Left,
                                     hand.visible_time,
                                     newArm,
                                     new PointableList (),
                                     new FingerList (),
                                     new Vector (palm.position.x, palm.position.y, palm.position.z),
                                     new Vector (palm.stabilized_position.x, palm.stabilized_position.y, palm.stabilized_position.z),
                                     new Vector (palm.velocity.x, palm.velocity.y, palm.velocity.z),
                                     new Vector (palm.normal.x, palm.normal.y, palm.normal.z),
                                     new Vector (palm.direction.x, palm.direction.y, palm.direction.z),
                                     newArm.NextJoint //wrist position
                                     );
            LEAP_DIGIT thumbDigit = LeapC.PtrToStruct<LEAP_DIGIT>(hand.thumb);
            Finger thumb = makeFinger (owningFrame, ref hand, ref thumbDigit, Finger.FingerType.TYPE_THUMB);
            newHand.Pointables.Add ((Pointable)thumb);
            newHand.Fingers.Add (thumb);
            LEAP_DIGIT indexDigit = LeapC.PtrToStruct<LEAP_DIGIT>(hand.index);
            Finger index = makeFinger (owningFrame, ref hand, ref indexDigit, Finger.FingerType.TYPE_INDEX);
            newHand.Fingers.Add (index);
            newHand.Pointables.Add ((Pointable)index);
            LEAP_DIGIT middleDigit = LeapC.PtrToStruct<LEAP_DIGIT>(hand.middle);
            Finger middle = makeFinger (owningFrame, ref hand, ref middleDigit, Finger.FingerType.TYPE_MIDDLE);
            newHand.Fingers.Add (middle);
            newHand.Pointables.Add ((Pointable)middle);
            LEAP_DIGIT ringDigit = LeapC.PtrToStruct<LEAP_DIGIT>(hand.ring);
            Finger ring = makeFinger (owningFrame, ref hand, ref ringDigit, Finger.FingerType.TYPE_RING);
            newHand.Fingers.Add (ring);
            newHand.Pointables.Add ((Pointable)ring);
            LEAP_DIGIT pinkyDigit = LeapC.PtrToStruct<LEAP_DIGIT>(hand.pinky);
            Finger pinky = makeFinger (owningFrame, ref hand, ref pinkyDigit, Finger.FingerType.TYPE_PINKY);
            newHand.Fingers.Add (pinky);
            newHand.Pointables.Add ((Pointable)pinky);

            return newHand;
        }

        public Finger makeFinger (Frame owner, ref LEAP_HAND hand, ref LEAP_DIGIT digit, Finger.FingerType type)
        {
            Bone metacarpal = makeBone (ref digit.metacarpal, Bone.BoneType.TYPE_METACARPAL);
            Bone proximal = makeBone (ref digit.proximal, Bone.BoneType.TYPE_PROXIMAL);
            Bone intermediate = makeBone (ref digit.intermediate, Bone.BoneType.TYPE_INTERMEDIATE);
            Bone distal = makeBone (ref digit.distal, Bone.BoneType.TYPE_DISTAL);
            return new Finger (owner,
                               (int)hand.id,
                               (int)digit.finger_id,
                               hand.visible_time,
                               distal.NextJoint,
                               new Vector (digit.tip_velocity.x, digit.tip_velocity.y, digit.tip_velocity.z),
                               intermediate.Direction,
                               new Vector (digit.stabilized_tip_position.x, digit.stabilized_tip_position.y, digit.stabilized_tip_position.z),
                               intermediate.Width,
                               proximal.Length + intermediate.Length + (distal.Length * 0.77f), //0.77 is used in platform code for this calculation
                               digit.is_extended,
                               type,
                               metacarpal,
                               proximal,
                               intermediate,
                               distal
            );
        }

        public Bone makeBone (ref LEAP_BONE bone, Bone.BoneType type)
        {
            Vector prevJoint = new Vector (bone.prev_joint.x, bone.prev_joint.y, bone.prev_joint.z);
            Vector nextJoint = new Vector (bone.next_joint.x, bone.next_joint.y, bone.next_joint.z);
            Vector center = (nextJoint + prevJoint) * .5f;
            float length = (nextJoint - prevJoint).Magnitude;
            Vector direction = (nextJoint - prevJoint) / length;
            Matrix basis = new Matrix (bone.basis.x_basis.x,
                                       bone.basis.x_basis.y,
                                       bone.basis.x_basis.z,
                                       bone.basis.y_basis.x,
                                       bone.basis.y_basis.y,
                                       bone.basis.y_basis.z,
                                       bone.basis.z_basis.x,
                                       bone.basis.z_basis.y,
                                       bone.basis.z_basis.z);
            return new Bone (prevJoint, nextJoint, center, direction, length, bone.width, type, basis);
        }

        public Arm makeArm (ref LEAP_BONE bone)
        {
            Vector prevJoint = new Vector (bone.prev_joint.x, bone.prev_joint.y, bone.prev_joint.z);
            Vector nextJoint = new Vector (bone.next_joint.x, bone.next_joint.y, bone.next_joint.z);
            Vector center = (nextJoint + prevJoint) * .5f;
            float length = (nextJoint - prevJoint).Magnitude;
            Vector direction = Vector.Zero;
            if(length > 0)
                direction = (nextJoint - prevJoint) / length;
            Matrix basis = new Matrix (bone.basis.x_basis.x,
                                       bone.basis.x_basis.y,
                                       bone.basis.x_basis.z,
                                       bone.basis.y_basis.x,
                                       bone.basis.y_basis.y,
                                       bone.basis.y_basis.z,
                                       bone.basis.z_basis.x,
                                       bone.basis.z_basis.y,
                                       bone.basis.z_basis.z);
            return new Arm (prevJoint, nextJoint, center, direction, length, bone.width, Bone.BoneType.TYPE_METACARPAL, basis); //Bone type ignored for arms
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
                if(result == eLeapRS.eLeapRS_Success){
                    for (int d = 0; d < validDeviceHandles; d++) {
                        IntPtr device;
                        if(deviceRefList[d].handle != IntPtr.Zero){
                            LeapC.OpenDevice (deviceRefList [d], out device);
                            LEAP_DEVICE_INFO deviceInfo;
                            int defaultLength = 14;
                            deviceInfo.serial_length = (uint)defaultLength;
                            deviceInfo.serial = Marshal.AllocCoTaskMem(defaultLength);
                            deviceInfo.size = 0;
                            deviceInfo.baseline = 0;
                            deviceInfo.caps = 0;
                            deviceInfo.h_fov = 0;
                            deviceInfo.range = 0;
                            deviceInfo.status = 0;
                            deviceInfo.type = eLeapDeviceType.eLeapDeviceType_Peripheral;
                            deviceInfo.v_fov = 0;
                            deviceInfo.size =(uint) Marshal.SizeOf(deviceInfo);
                            result = LeapC.GetDeviceInfo (device, out deviceInfo);
                            while (result == eLeapRS.eLeapRS_InsufficientBuffer) {
                                deviceInfo.serial = Marshal.AllocCoTaskMem((int)deviceInfo.serial_length + 10); //TODO modify when length bug is fixed
                                deviceInfo.size = (uint) Marshal.SizeOf(deviceInfo);
                                result = LeapC.GetDeviceInfo (device, out deviceInfo);
                            }
                            Logger.LogStruct(deviceInfo, "Initialize device list");
                            Device apiDevice = new Device (deviceRefList[d].handle,
                                                           deviceInfo.h_fov, //radians
                                                           deviceInfo.v_fov, //radians
                                                           deviceInfo.range/1000, //to mm 
                                                           deviceInfo.baseline/1000, //to mm 
                                                           (deviceInfo.caps == (UInt32)eLeapDeviceCaps.eLeapDeviceCaps_Embedded),
                                                           (deviceInfo.status == (UInt32)eLeapDeviceStatus.eLeapDeviceStatus_Streaming),
                                                           Marshal.PtrToStringAnsi(deviceInfo.serial));
                            _devices.Add (apiDevice);
                        }
                    }
                }
            }
            Logger.Log ("Device Count: " + _devices.Count);
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
                if(result != eLeapRS.eLeapRS_Success)
                    Logger.Log ("LeapC GetConnectionInfo call was " + result);

                if (pInfo.status == eLeapConnectionStatus.eLeapConnectionStatus_Connected)
                    return true;
                
                return false;
            }
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
            if(result != eLeapRS.eLeapRS_Success)
                Logger.Log ("LeapC SetPolicyFlags call was " + result);
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
            if(result == eLeapRS.eLeapRS_Success){
                return (priorFlags & policyToCheck) == policyToCheck;
            } else {
                Logger.Log ("LeapC SetPolicyFlags call was " + result);
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


        public bool GetLatestImagePair(out Image left, out Image right){
            if(!_imagesAreEnabled){
                left = null;
                right = null;
                return false;
            }
            return _irImageCache.GetLatestImages(out left, out right);
        }

        public bool GetFrameImagePair(long frameId, out Image left, out Image right){
            if(!_imagesAreEnabled){
                left = null;
                right = null;
                return false;
            }
            return _irImageCache.GetImagesForFrame(frameId, out left, out right);
        }

        public TrackedQuad GetLatestQuad(){
            return _quads.Get(0);

        }
        public TrackedQuad GetFrameQuad(long frameID){
            return _quads.Get(0); //TODO look up quad by frame id
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
                if(_devices == null){
                    _devices = new DeviceList ();
                }

                return _devices;
            } 
        }
        public FailedDeviceList FailedDevices {
            get {
                if(_failedDevices == null){
                    _failedDevices = new FailedDeviceList ();
                }
                
                return _failedDevices;
            } 
        }

        public bool IsPaused{
            get{
                return false; //TODO implement IsPaused
            }
        }

        public void SetPaused(bool newState){
            //TODO implement pausing
        }

        public void AddLeapCEventHandler(eLeapEventType type, LeapCEventHandler handler){
            _eventDelegates[indexFor(type)] += handler;
        }

        public void RemoveLeapCEventHandler(eLeapEventType type, LeapCEventHandler handler){
            _eventDelegates[indexFor(type)] -= handler;
        }

        public void DistpatchLeapCEvent(eLeapEventType type, EventArgs args){
            if(_eventDelegates[indexFor(type)] != null)
                _eventDelegates[indexFor(type)].Invoke(type, args);
        }
        private int indexFor(Enum enumItem){
            return Array.IndexOf(Enum.GetValues(enumItem.GetType()), enumItem);
        }
        private eLeapEventType itemFor(int ordinal){
            int[] values = (int[])Enum.GetValues(typeof(eLeapEventType));
            return (eLeapEventType)values[ordinal];
        }

    }

    public class FrameEventArgs : EventArgs{
        public FrameEventArgs(Frame frame){
            this.frame = frame;
        }
        public Frame frame{get; set;}
    }
    public class ImageEventArgs : EventArgs{
        public ImageEventArgs(Image image){
            this.image = image;
        }
        public Image image{get; set;}
    }
    public class LogEventArgs : EventArgs{
        public LogEventArgs(ref LEAP_LOG_EVENT log){
            this.severity = log.severity;
            this.message = log.message;
            this.timestamp = this.timestamp;
        }
        public eLeapLogSeverity severity{get; set;}
        public Int64 timestamp{get; set;}
        public string message{get; set;}
    }
    public class PolicyEventArgs : EventArgs{
        public PolicyEventArgs(UInt64 currentPolicies, UInt64 oldPolicies){
            this.currentPolicies = currentPolicies;
            this.oldPolicies = oldPolicies;
        }
        public UInt64 currentPolicies{get; set;}
        public UInt64 oldPolicies{get; set;}
    }

}
