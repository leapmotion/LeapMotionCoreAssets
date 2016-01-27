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

