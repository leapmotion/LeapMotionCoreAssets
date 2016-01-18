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

        public bool GetLatestImages(out Image left, out Image right){
            bool giveUp = false;
            for(int i = 0; i < this.Count; i ++){
                Image firstImage = this.Get (i);
                for(int j = i; j < this.Count; j++){
                    Image nextImage = this.Get (j);
                    if(firstImage.SequenceId == nextImage.SequenceId){
                        left = (firstImage.Id == 0) ? firstImage : nextImage;
                        right = (nextImage.Id == 0) ? firstImage : nextImage;
                        return true;
                    }
                    if(nextImage.SequenceId > firstImage.SequenceId){
                        giveUp = true;
                        break;
                    }
                }
                if(giveUp == true)
                    break;
            }
            left = new Image();
            right = new Image();
            return false;
        }

        public bool GetImagesForFrame(long frameId, out Image left, out Image right){
            bool giveUp = false;
            for(int i = 0; i < this.Count; i ++){
                Image firstImage = this.Get (i);
                if(firstImage.SequenceId == frameId){
                    for(int j = i; j < this.Count; j++){
                        Image nextImage = this.Get (j);
                        if(firstImage.SequenceId == nextImage.SequenceId){
                            left = (firstImage.Id == 0) ? firstImage : nextImage;
                            right = (nextImage.Id == 0) ? firstImage : nextImage;
                            return true;
                        }
                        if(nextImage.SequenceId > firstImage.SequenceId){
                            giveUp = true;
                            break;
                        }
                    }
                    if(giveUp == true)
                        break;
                }
            }
            left = new Image();
            right = new Image();
            return false;
        }
    }
}

