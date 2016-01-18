using System;
using System.Collections.Generic;
using Leap;

namespace LeapInternal
{
    //TODO ensure thread safety
    public class DistortionDictionary : Dictionary<UInt64, DistortionData>{
        public UInt64 CurrentRightMatrix = 0;
        public UInt64 CurrentLeftMatrix = 0;

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

