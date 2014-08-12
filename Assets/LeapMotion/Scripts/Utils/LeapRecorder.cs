using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using Leap;

public enum RecorderMode {
  Off = 0,
  Record = 1,
  Playback = 2
}

public enum RecorderState {
  Idling = 0,
  Recording = 1,
  Playbacking = 2
}

public class LeapRecorder {

  private RecorderState state_ = RecorderState.Idling;
  private List<byte[]> frames_;
  
  private float frame_index_;
  private int start_time_;
  private float speed_;
  private bool loop_;
  private int delay_;
  
  public LeapRecorder() {
    Reset();
  }
  
	public void Reset() {
    frames_ = new List<byte[]>();
    state_ = RecorderState.Idling;
    speed_ = 1.0f;
    start_time_ = 0;
    frame_index_ = 0;
    delay_ = 0;
  }
	
  public RecorderState GetState() { return state_; }
  public int GetStartTime() { return start_time_; }
  public float GetSpeed() { return speed_; }
  public bool GetLoop() { return loop_; }
  public float GetLoopDelay() { return delay_; }
  public int GetIndex() { return (int)frame_index_; }
  
  public void SetState(RecorderState state) { state_ = state; }
  public void SetStartTime(int start_time) { start_time_ = start_time; }
  public void SetSpeed(float speed) { speed_ = speed; }
  public void SetLoop(bool loop) { loop_ = loop; }
  public void SetLoopDelay(int delay) { delay_ = delay; }
  public void SetIndex(int index) { 
    if (index > frames_.Count - 1) {
      frame_index_ = frames_.Count - 1;
    } else {
      frame_index_ = index; 
    }
  }
  
	public void AddFrame(Frame frame) {
    frames_.Add(frame.Serialize);
	}
  
  public Frame GetFrame() {
    Frame frame = new Frame();
    if (frames_.Count > 0) {
      if (start_time_ > 0) {
        start_time_--;
      } else {
        if (frame_index_ > frames_.Count - 1) {
          return frame;
        }
        frame.Deserialize(frames_[(int)frame_index_]);
        frame_index_ += speed_;
        if (loop_) {
          if (frame_index_ > frames_.Count - 1) {
            frame_index_ = 0;
            start_time_ = delay_;
          }
        }
      }
    }
    return frame;
  }
  
  public List<Frame> GetFrames() {
    List<Frame> frames = new List<Frame>();
    for (int i = 0; i < frames_.Count; ++i) {
      Frame frame = new Frame();
      frame.Deserialize(frames_[i]);
      frames.Add(frame);
    }
    return frames;
  }
  
  public int GetFramesCount() {
    return frames_.Count;
  }
  
  public bool Save(string path) {
    if (File.Exists(@path)) {
      File.Delete(@path);
    }
    FileStream stream = new FileStream(path, FileMode.Append, FileAccess.Write);
    for (int i = 0; i < frames_.Count; ++i) {
      byte[] frame_size = new byte[4];
      frame_size = System.BitConverter.GetBytes(frames_[i].Length);
      stream.Write(frame_size, 0, frame_size.Length);
      stream.Write(frames_[i], 0, frames_[i].Length);
    }
    stream.Close();
    if (File.Exists(path)) {
      return true;
    } else {
      return false;
    }
  }
  
  public void Load(byte[] data, int start_time = 0, float speed = 1.0f, bool loop = true, int delay = 0) {
    speed_ = speed;
    start_time_ = start_time;
    loop_ = loop;
    delay_ = delay;
    
    frame_index_ = 0;
    frames_.Clear();
    int i = 0;
    while (i < data.Length) {
      byte[] frame_size = new byte[4];
      Array.Copy(data, i, frame_size, 0, frame_size.Length);
      i += frame_size.Length;
      byte[] frame = new byte[System.BitConverter.ToUInt32(frame_size, 0)];
      Array.Copy(data, i, frame, 0, frame.Length);
      i += frame.Length;
      frames_.Add(frame);
    }
  }
}
