using UnityEngine;
using System.Collections;

public class RecordingControls : MonoBehaviour {
  [Multiline]
  public string header;
  private string footer = "";
  public GUIText displayGui;

  public KeyCode beginRecordingKey = KeyCode.R;
  public KeyCode endRecordingKey = KeyCode.R;
  public KeyCode beginPlaybackKey = KeyCode.P;
  public KeyCode pausePlaybackKey = KeyCode.P;
  public KeyCode stopPlaybackKey = KeyCode.S;

  private HandController _controller;

  void Start() {
    _controller = FindObjectOfType<HandController>();
  }

  void Update() {
    if (displayGui != null) displayGui.text = header + "\n";

    switch (_controller.GetLeapRecorder().state) {
      case RecorderState.Recording:
        allowEndRecording();
        break;
      case RecorderState.Playing:
        allowPausePlayback();
        allowStopPlayback();
        break;
      case RecorderState.Paused:
        allowBeginPlayback();
        allowStopPlayback();
        break;
      case RecorderState.Stopped:
        allowBeginRecording();
        allowBeginPlayback();
        break;
    }

    if (displayGui != null) displayGui.text += footer;
  }

  private void allowBeginRecording() {
    if (displayGui != null) displayGui.text += beginRecordingKey + " - Begin Recording\n";
    if (Input.GetKeyDown(beginRecordingKey)) {
      _controller.ResetRecording();
      _controller.Record();
      footer = "";
    }
  }

  private void allowBeginPlayback() {
    if (displayGui != null) displayGui.text += beginPlaybackKey + " - Begin Playback\n";
    if (Input.GetKeyDown(beginPlaybackKey)) {
      _controller.PlayRecording();
    }
  }

  private void allowEndRecording() {
    if (displayGui != null) displayGui.text += endRecordingKey + " - End Recording\n";
    if (Input.GetKeyDown(endRecordingKey)) {
      string savedPath = _controller.FinishAndSaveRecording();
      footer = "Recording saved to:\n" + savedPath;
    }
  }

  private void allowPausePlayback() {
    if (displayGui != null) displayGui.text += pausePlaybackKey + " - Pause Playback\n";
    if (Input.GetKeyDown(pausePlaybackKey)) {
      _controller.PauseRecording();
    }
  }

  private void allowStopPlayback() {
    if (displayGui != null) displayGui.text += stopPlaybackKey + " - Stop Playback\n";
    if (Input.GetKeyDown(stopPlaybackKey)) {
      _controller.StopRecording();
    }
  }
}
