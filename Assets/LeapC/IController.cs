/******************************************************************************\
* Copyright (C) 2012-2015 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
using System;

namespace Leap {
    interface IController :IDisposable{
         Frame Frame(int history = 0);
         bool AddListener(Listener listener);
         bool RemoveListener(Listener listener);
         void SetPolicy(Controller.PolicyFlag policy);
         void ClearPolicy(Controller.PolicyFlag policy);
         bool IsPolicySet(Controller.PolicyFlag policy);
         void EnableGesture(Gesture.GestureType type, bool enable);
         void EnableGesture(Gesture.GestureType type);
         bool IsGestureEnabled(Gesture.GestureType type);
         long Now();
         bool IsConnected {get;}
         bool IsServiceConnected {get;}
         bool HasFocus {get;}
         Config Config {get;}
         ImageList Images {get;}
         DeviceList Devices {get;}
         TrackedQuad TrackedQuad {get;}
         BugReport BugReport {get;}
//         bool GetDistortionData(IntPtr deviceHandle, ref float[] buffer, out int width, out int height);
    }
}

