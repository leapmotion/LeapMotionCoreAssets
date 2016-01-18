/******************************************************************************\
* Copyright (C) 2012-2015 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/

namespace Leap {

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
    using LeapInternal;

  /**
   * The DeviceList class represents a list of Device objects.
   *
   * Get a DeviceList object by calling Controller::devices().
   * @since 1.0
   */

public class DeviceList : List<Device> {

  ~DeviceList() {
  }


    /**
     * Constructs an empty list of devices.
     * @since 1.0
     */
  public DeviceList() {
  }

        public Device FindDeviceByHandle(IntPtr deviceHandle){
            for( int d = 0; d < this.Count; d++){
                if(this[d].UsesHandle(deviceHandle))
                    return this[d];
            }
            return new Device();
        }

        public Device FindActiveDevice(){
            if(Count == 1)
                return this[0];

            for( int d = 0; d < this.Count; d++){
                if(this[d].IsStreaming)
                    return this[d];
            }
            return new Device();
        }

    /**
     * Appends the members of the specified DeviceList to this DeviceList.
     * @param other A DeviceList object containing Device objects
     * to append to the end of this DeviceList.
     * @since 1.0
     */
//  public DeviceList Append(DeviceList other) {
//            this.InsertRange(this.Count - 1, other);
//    return this;
//  }

/**
     * Reports whether the list is empty.
     *
     * \include DeviceList_isEmpty.txt
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
