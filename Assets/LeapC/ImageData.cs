namespace LeapInternal
{
    using System;
    using System.Runtime.InteropServices;
    using Leap;

    public class ImageData : PooledObject
    {
        public bool isComplete = false;
        public byte[] pixelBuffer;
        //private IntPtr dataPtr;
        private GCHandle _bufferHandle;
        public UInt64 index;
        public Int64 frame_id;
        public Int64 timestamp;

        public Image.FormatType type;
        public Image.PerspectiveType perspective;
        public UInt32 bpp;
        public UInt32 width;
        public UInt32 height;
        public float RayOffsetX;
        public float RayOffsetY;
        public float RayScaleX;
        public float RayScaleY;
        public int DistortionSize;
        public UInt64 DistortionMatrixKey;
        public DistortionData DistortionData;

        public ImageData(){}
        public ImageData(UInt64 bufferLength, UInt64 index){
            pixelBuffer = new byte[bufferLength];
            for(int p = 0; p < (int)bufferLength; p++){
                pixelBuffer[p] = 0x77;
            }
            this.index = index;
        }

        public void CompleteImageData(Image.FormatType type,
                                      Image.PerspectiveType perspective,
                                      UInt32 bpp, 
                                      UInt32 width, 
                                      UInt32 height,
                                      Int64 timestamp,
                                      Int64 frame_id,
                                      float x_offset,
                                      float y_offset,
                                      float x_scale,
                                      float y_scale,
                                      DistortionData distortionData,
                                      int distortion_size,
                                      UInt64 distortion_matrix_version){
            this.type = type; 
            this.perspective = perspective;
            this.bpp = bpp;
            this.width = width;
            this.height = height;
            this.timestamp = timestamp;
            this.frame_id = frame_id;
            this.RayOffsetX = x_offset;
            this.RayOffsetY = y_offset;
            this.RayScaleX = x_scale;
            this.RayScaleY = y_offset;
            this.DistortionData = distortionData;
            this.DistortionSize = distortion_size;
            this.DistortionMatrixKey = distortion_matrix_version;
            isComplete = true;
        }

        public IntPtr getPinnedHandle(){
            if(pixelBuffer == null)
                return IntPtr.Zero;

            _bufferHandle = GCHandle.Alloc(pixelBuffer, GCHandleType.Pinned);
            return _bufferHandle.AddrOfPinnedObject();;
        }

        public void unPinHandle(){
            _bufferHandle.Free();
        }

        public ImageData Copy(){
            ImageData copy = new ImageData();
            copy.pixelBuffer = new byte[pixelBuffer.Length];
            copy.index = this.index;
            copy.type = this.type; 
            copy.perspective = this.perspective; 
            copy.bpp = this.bpp;
            copy.width = this.width;
            copy.height = this.height;
            copy.timestamp = this.timestamp;
            copy.frame_id = this.frame_id;
            copy.RayOffsetX = this.RayOffsetX;
            copy.RayOffsetY = this.RayOffsetY;
            copy.RayScaleX = this.RayScaleX;
            copy.RayScaleY = this.RayScaleY;
            copy.DistortionData = this.DistortionData;
            copy.DistortionMatrixKey = this.DistortionMatrixKey;
            copy.isComplete = this.isComplete;

            return copy;
        }
    }



}
