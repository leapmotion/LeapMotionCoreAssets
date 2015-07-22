using UnityEngine;
using System.Collections;

public class RecordingControls : MonoBehaviour {
  [Multiline]
  public string header;
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
    displayGui.text = header + "\n";

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
  }

  private void allowBeginRecording() {
    displayGui.text += beginRecordingKey + " - Begin Recording\n";
    if (Input.GetKeyDown(beginRecordingKey)) {
      _controller.ResetRecording();
      _controller.Record();
    }
  }

  private void allowBeginPlayback() {
    displayGui.text += beginPlaybackKey + " - Begin Playback\n";
    if (Input.GetKeyDown(beginPlaybackKey)) {
      _controller.PlayRecording();
    }
  }

  private void allowEndRecording() {
    displayGui.text += endRecordingKey + " - End Recording\n";
    if (Input.GetKeyDown(endRecordingKey)) {
      string savedPath = _controller.FinishAndSaveRecording();
      Debug.Log("Recording saved to: " + savedPath);
    }
  }

  private void allowPausePlayback() {
    displayGui.text += pausePlaybackKey + " - Pause Playback\n";
    if (Input.GetKeyDown(pausePlaybackKey)) {
      _controller.PauseRecording();
    }
  }

  private void allowStopPlayback() {
    displayGui.text += stopPlaybackKey + " - Stop Playback\n";
    if (Input.GetKeyDown(stopPlaybackKey)) {
      _controller.StopRecording();
    }
  }
}
