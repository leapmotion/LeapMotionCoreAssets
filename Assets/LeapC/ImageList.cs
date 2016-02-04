/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
namespace Leap {
    
    using System;
    using System.Runtime.InteropServices;
    using System.Collections.Generic;
    
    /**
   * The ImageList class represents a list of Image objects.
   *
   * Get a ImageList object by calling Controller::images().
   * @since 1.0
   */
    
    public class ImageList {
        public const int MAX_IMAGES = 4;

        private Image[] _images;

        /**
     * Constructs an empty list of images.
     * @since 1.0
     */
        public ImageList() {
            _images = new Image[MAX_IMAGES];
        }
        
        public Image this[int i]{
            get{
                switch(i){
                case 0:
                    if(_images[0] != null)
                        return _images[0];
                    if(_images[2] != null)
                        return _images[2];
                    throw new IndexOutOfRangeException("ImageList is empty");
                case 1:
                    if(_images[1] != null)
                        return _images[1];
                    if(_images[3] != null)
                        return _images[3];
                    throw new IndexOutOfRangeException("ImageList is empty");
                case 2:
                    if(_images[2] != null)
                        return _images[2];
                    throw new IndexOutOfRangeException();
                case 3:
                    if(_images[3] != null)
                        return _images[3];
                    throw new IndexOutOfRangeException();
                    default:
                    throw new IndexOutOfRangeException();
                }
            }
            set{
                if( i < 0 || i > MAX_IMAGES - 1)
                    throw new IndexOutOfRangeException();

                if(value == null)
                    _images[i] = value;
                
                if(value.Perspective == Image.PerspectiveType.STEREO_LEFT){
                    if(value.Type == Image.ImageType.DEFAULT){
                        IRLeft = value;
                    } else {
                        RawLeft = value;
                    }
                } else { 
                    if(value.Type == Image.ImageType.DEFAULT){
                        IRRight = value;
                    } else {
                        RawRight = value;
                    }
                }
            }
        }

        public Image IRLeft{
            get{
                return _images[0];
            } 
            set{
                if(value != null && (value.Perspective != Image.PerspectiveType.STEREO_LEFT || value.Type != Image.ImageType.DEFAULT))
                    throw new ArgumentException("Wrong image type or perspective.");
                _images[0] = value;
            }
        }
        public Image IRRight{
            get{
                return _images[1];
            } 
            set{
                if(value != null && (value.Perspective != Image.PerspectiveType.STEREO_RIGHT || value.Type != Image.ImageType.DEFAULT))
                    throw new ArgumentException("Wrong image type or perspective.");
                _images[1] = value;
            }
        }
        public Image RawLeft{
            get{
                return _images[2];
            } 
            set{
                if(value != null && (value.Perspective != Image.PerspectiveType.STEREO_LEFT || value.Type != Image.ImageType.RAW))
                    throw new ArgumentException("Wrong image type or perspective.");
                _images[2] = value;
            }
        }
        public Image RawRight{
            get{
                return _images[3];
            } 
            set{
                if(value != null && (value.Perspective != Image.PerspectiveType.STEREO_RIGHT || value.Type != Image.ImageType.RAW))
                    throw new ArgumentException("Wrong image type or perspective.");
                _images[3] = value;
            }
        }
        public int Count{
            get{
                int count = 0;
                for(int i = 0; i < MAX_IMAGES; i++){
                    if((_images != null) && (_images[i] != null) && _images[i].IsValid) count++;
                }
                return count;
            }
        }

        public bool Add(Image image){
            if(image == null)
                return false;
            
            if(image.Type == Image.ImageType.DEFAULT){
                if(image.Perspective == Image.PerspectiveType.STEREO_LEFT){ IRLeft = image; return true;}
                if(image.Perspective == Image.PerspectiveType.STEREO_RIGHT){ IRRight = image; return true;}
            } else if (image.Type == Image.ImageType.RAW){
                if(image.Perspective == Image.PerspectiveType.STEREO_LEFT){ RawLeft = image; return true;}
                if(image.Perspective == Image.PerspectiveType.STEREO_RIGHT){ RawRight = image; return true;}
            }
            return false;
        }
        public ImageList RawImages{
            get{
                ImageList raw = new ImageList();
                raw.RawLeft = this.RawLeft;
                raw.RawRight = this.RawRight;
                return raw;
            }
        }
        public ImageList IRImages{
            get{
                ImageList ir = new ImageList();
                ir.IRLeft = this.IRLeft;
                ir.IRRight = this.IRRight;
                return ir;
            }
        }

        /**
     * Reports whether the list is empty.
     *
     * \include ImageList_isEmpty.txt
     *
     * @returns True, if the list has no members.
     * @since 1.0
     */  public bool IsEmpty {
            get {
                return this.Count == 0;
            } 
        }

        public IEnumerable<Image> AllImages 
        { 
            get{
                for(int i = 0; i < MAX_IMAGES; i++){
                    if(_images[i] != null && _images[i].IsValid) yield return _images[i];
                }
            }
        }

    }

}
