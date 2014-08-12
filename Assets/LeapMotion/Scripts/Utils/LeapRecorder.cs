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
  PlayingBack = 2
}

public class LeapRecorder {

  public int startTime;
  public float speed;
  public bool loop;
  public int delay;

  private RecorderState state_ = RecorderState.Idling;
  private List<byte[]> frames_;
  private float frame_index_;
  
  public LeapRecorder() {
    Reset();
  }
  
  public void Reset() {
    frames_ = new List<byte[]>();
    state_ = RecorderState.Idling;
    speed = 1.0f;
    startTime = 0;
    frame_index_ = 0;
    delay = 0;
  }
  
  public RecorderState GetState() { return state_; }
  public int GetStartTime() { return startTime; }
  public float GetSpeed() { return speed; }
  public bool GetLoop() { return loop; }
  public float GetLoopDelay() { return delay; }
  public int GetIndex() { return (int)frame_index_; }
  
  public void SetState(RecorderState new_state) { state_ = new_state; }
  public void SetStartTime(int new_start_time) { startTime = new_start_time; }
  public void SetSpeed(float new_speed) { speed = new_speed; }
  public void SetLoop(bool new_loop) { loop = new_loop; }
  public void SetLoopDelay(int new_delay) { delay = new_delay; }
  public void SetIndex(int new_index) { 
    if (new_index >= frames_.Count) {
      frame_index_ = frames_.Count - 1;
    }
    else {
      frame_index_ = new_index; 
    }
  }
  
  public void AddFrame(Frame frame) {
    frames_.Add(frame.Serialize);
  }
  
  public Frame GetFrame() {
    Frame frame = new Frame();
    if (frames_.Count > 0) {
      if (startTime > 0) {
        startTime--;
      }
      else {
        if (frame_index_ >= frames_.Count) {
          return frame;
        }
        frame.Deserialize(frames_[(int)frame_index_]);
        frame_index_ += speed;
        if (loop) {
          if (frame_index_ >= frames_.Count) {
            frame_index_ = 0;
            startTime = delay;
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
    return File.Exists(path);
  }
  
  public void Load(byte[] data, int start_time = 0, float new_speed = 1.0f, bool new_loop = true, int new_delay = 0) {
    speed = new_speed;
    startTime = start_time;
    loop = new_loop;
    delay = new_delay;
    
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
