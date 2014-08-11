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

public class LeapRecorder {

  private List<byte[]> frames_;
  
  public LeapRecorder() {
    Reset();
  }
	
	public void Record(Frame frame) {
    frames_.Add(frame.Serialize);
	}
  
	public void Reset() {
    frames_ = new List<byte[]>();
  }
  
  public Frame GetFrame(int index) {
    Frame frame = new Frame();
    frame.Deserialize(frames_[index]);
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
  
  public bool Load(string path) {
    if (!File.Exists(path)) {
      return false;
    }
    frames_.Clear();
    FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
    int stream_increment = 0;
    for (int stream_index = 0; stream_index < stream.Length; stream_index += stream_increment) {
      byte[] frame_size = new byte[4];
      stream.Read(frame_size, 0, frame_size.Length);
      byte[] frame = new byte[System.BitConverter.ToUInt32(frame_size, 0)];
      stream.Read(frame, 0, frame.Length);
      frames_.Add(frame);
      stream_index += frame_size.Length;
      stream_index += frame.Length;
    }
    stream.Close();
    if (File.Exists(path)) {
      return true;
    } else {
      return false;
    }
  }
  
  public void Load(byte[] data) {
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
