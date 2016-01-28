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
    
    public class ImageList : IDisposable {
        public const int MAX_IMAGES = 4;

        private Image[] _images;

        // TODO: revisit dispose code
        // Dispose() is called explicitly by CoreAssets, so adding the stub implementation for now.
        bool _disposed = false;
        public void Dispose(){
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing){
            if (_disposed)
              return;

            // cleanup
            if (disposing) {
                _images = null;
            }

            // Free any unmanaged objects here.
            //
            _disposed = true;
        }
        
        ~ImageList() {
            Dispose(false);
        }
        
        
        /**
     * Constructs an empty list of images.
     * @since 1.0
     */
        public ImageList() {
            _images = new Image[MAX_IMAGES];
        }
        
        
        public Image IRLeft{
            get{
                return _images[0];
            } 
            set{
                _images[0] = value;
            }
        }
        public Image IRRight{
            get{
                return _images[1];
            } 
            set{
                _images[1] = value;
            }
        }
        public Image RawLeft{
            get{
                return _images[2];
            } 
            set{
                _images[2] = value;
            }
        }
        public Image RawRight{
            get{
                return _images[3];
            } 
            set{
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
