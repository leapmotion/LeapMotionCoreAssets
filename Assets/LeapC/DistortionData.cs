/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
using System;
using System.Collections.Generic;
using Leap;

namespace LeapInternal
{
    public class DistortionData{
        public UInt64 version{get; set;}
        public float width{get; set;}
        public float height{get; set;}
        public float[] data{get; set;}
    }
}

