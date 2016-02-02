using System;
using Leap;

namespace LeapInternal
{
    /** 
     * A CircularObjectBuffer specialized for image retrieval.
     */
    public class CircularImageBuffer: CircularObjectBuffer<Image>
    {
        public CircularImageBuffer(int capacity):base(capacity){}

        private Image _latestIRLeft;
        private Image _latestIRRight;
        private Image _latestRawLeft;
        private Image _latestRawRight;

        public override void Put (Image item)
        {
            base.Put (item);
            if(item.Type == Image.ImageType.DEFAULT){
                if(item.Perspective == Image.PerspectiveType.STEREO_LEFT) _latestIRLeft = item;
                if(item.Perspective == Image.PerspectiveType.STEREO_RIGHT) _latestIRRight = item;
//                System.IO.File.WriteAllBytes(("default" + item.SequenceId + ".raw"), item.Data);
            } else if (item.Type == Image.ImageType.RAW){
                if(item.Perspective == Image.PerspectiveType.STEREO_LEFT) _latestRawLeft = item;
                if(item.Perspective == Image.PerspectiveType.STEREO_RIGHT) _latestRawRight = item;
//                System.IO.File.WriteAllBytes(("raw" + item.SequenceId + ".raw"), item.Data);
            }
        }

        public ImageList GetLatestImages(){
            ImageList latest = new ImageList();
            latest.IRLeft = _latestIRLeft;
            latest.IRRight = _latestIRRight;
            latest.RawLeft = _latestRawLeft;
            latest.RawRight = _latestRawRight;
            return latest;
        }

        public void GetLatestImages(ImageList receiver){
            if(receiver == null)
                receiver = new ImageList();
            receiver.IRLeft = _latestIRLeft;
            receiver.IRRight = _latestIRRight;
            receiver.RawLeft = _latestRawLeft;
            receiver.RawRight = _latestRawRight;
        }

        public void GetLatestImages(out Image irLeft, out Image irRight, out Image rawLeft, out Image rawRight){
            irLeft = _latestIRLeft;
            irRight = _latestIRRight;
            rawLeft = _latestRawLeft;
            rawRight = _latestRawRight;
        }

        public int GetImagesForFrame(long frameId, ImageList receiver){
            if( receiver == null)
                receiver = new ImageList();
            int foundCount = 0;
            for(int i = 0; i < this.Count; i ++){
                Image image = this.Get (i);
                if(image.SequenceId == frameId){ 
                    receiver.Add (image);
                    foundCount++;
                } else if(image.SequenceId < frameId) break;
            }
            return foundCount;
        }
    }
}

