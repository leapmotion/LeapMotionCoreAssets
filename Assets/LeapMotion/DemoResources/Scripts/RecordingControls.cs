using UnityEngine;
using System.Collections;

public class RecordingControls : MonoBehaviour {
  [Multiline]
  public string header;
  public GUIText controlsGui;
  public GUIText recordingGui;

  public KeyCode beginRecordingKey = KeyCode.R;
  public KeyCode endRecordingKey = KeyCode.R;
  public KeyCode beginPlaybackKey = KeyCode.P;
  public KeyCode pausePlaybackKey = KeyCode.P;
  public KeyCode stopPlaybackKey = KeyCode.S;

  void Update() {
    if (controlsGui != null) controlsGui.text = header + "\n";

    switch (HandController.Main.GetLeapRecorder().state) {
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
    if (controlsGui != null) controlsGui.text += beginRecordingKey + " - Begin Recording\n";
    if (Input.GetKeyDown(beginRecordingKey)) {
      HandController.Main.ResetRecording();
      HandController.Main.Record();
      recordingGui.text = "";
    }
  }

  private void allowBeginPlayback() {
    if (controlsGui != null) controlsGui.text += beginPlaybackKey + " - Begin Playback\n";
    if (Input.GetKeyDown(beginPlaybackKey)) {
      HandController.Main.PlayRecording();
    }
  }

  private void allowEndRecording() {
    if (controlsGui != null) controlsGui.text += endRecordingKey + " - End Recording\n";
    if (Input.GetKeyDown(endRecordingKey)) {
      string savedPath = HandController.Main.FinishAndSaveRecording();
      recordingGui.text = "Recording saved to:\n" + savedPath;
    }
  }

  private void allowPausePlayback() {
    if (controlsGui != null) controlsGui.text += pausePlaybackKey + " - Pause Playback\n";
    if (Input.GetKeyDown(pausePlaybackKey)) {
      HandController.Main.PauseRecording();
    }
  }

  private void allowStopPlayback() {
    if (controlsGui != null) controlsGui.text += stopPlaybackKey + " - Stop Playback\n";
    if (Input.GetKeyDown(stopPlaybackKey)) {
      HandController.Main.StopRecording();
    }
  }
}
