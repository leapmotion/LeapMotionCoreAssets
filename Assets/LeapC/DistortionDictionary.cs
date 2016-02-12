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
    //TODO ensure thread safety
    public class DistortionDictionary : Dictionary<UInt64, DistortionData>{
        public UInt64 CurrentMatrix = 0;

        public bool DistortionChange = false;

        public DistortionData GetMatrix(UInt64 version){
            DistortionData matrix;
            this.TryGetValue(version, out matrix);
            return matrix;
        }

        public bool VersionExists(UInt64 version){
            return this.ContainsKey(version);
        }

    }
}

