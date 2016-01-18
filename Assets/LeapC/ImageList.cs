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
    
    public class ImageList : List<Image>, IDisposable {
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
                // Free any managed objects here.
                //
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
        }
        
        
        /**
     * Appends the members of the specified ImageList to this ImageList.
     * @param other A ImageList object containing Image objects
     * to append to the end of this ImageList.
     * @since 1.0
     */
        public ImageList Append(ImageList other) {
            this.InsertRange(this.Count - 1, other);
            return this;
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
        
    }
    
}
