namespace Leap {

using System;
using System.Runtime.InteropServices;


public class BugReport{

  public BugReport() {
  }

  public bool BeginRecording() {
    return false;
  }

  public void EndRecording() {
  }

  public bool IsActive {
    get {
      return false;
    } 
  }

  public float Progress {
    get {
      return 0;
    } 
  }

  public float Duration {
    get {
      return 0;
    } 
  }

}

}
