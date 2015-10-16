/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Leap;

// To use the LeapImageRetriever you must be on version 2.1+
// and enable "Allow Images" in the Leap Motion settings.
public class LeapImageRetriever : MonoBehaviour {
  public const string IR_SHADER_VARIANT_NAME = "LEAP_FORMAT_IR";
  public const string RGB_SHADER_VARIANT_NAME = "LEAP_FORMAT_RGB";
  public const int IMAGE_WARNING_WAIT = 10;

  public static event Action<CameraParams> OnValidCameraParams;
  public static event Action OnLeftPreRender;
  public static event Action OnRightPreRender;

  public enum EYE {
    LEFT = 0,
    RIGHT = 1,
    LEFT_TO_RIGHT = 2,
    RIGHT_TO_LEFT = 3
  }

  public enum SYNC_MODE {
    SYNC_WITH_HANDS,
    LOW_LATENCY
  }

  public struct CameraParams {
    public readonly Matrix4x4 ProjectionMatrix;
    public readonly int Width;
    public readonly int Height;

    public CameraParams(Camera camera) {
      ProjectionMatrix = camera.projectionMatrix;
      Width = camera.pixelWidth;
      Height = camera.pixelHeight;
    }
  }

  public EYE retrievedEye = EYE.LEFT;
  
  [Tooltip ("Should the image match the tracked hand, or should it be displayed as fast as possible")]
  public SYNC_MODE syncMode = SYNC_MODE.SYNC_WITH_HANDS;

  public float gammaCorrection = 1.0f;

  private int _missedImages = 0;
  private Controller _controller;
  private int frameEye = 0;

  //ImageList to use during rendering.  Can either be updated in OnPreRender or in Update
  private ImageList _imageList;
  private Camera _cachedCamera;

  // Intermediate arrays
  private EyeTextureData[] _eyeTextureData = new EyeTextureData[2]; // left and right data
  private byte[] _mainTextureIntermediateArray = null;
  private Color32[] _distortionIntermediateArray = null;

  //Used to recalculate the distortion every time a hand enters the frame.  Used because there is no way to tell if the device has flipped (which changes the distortion)
  private bool _forceDistortionRecalc = false;

  private bool _hasFiredCameraParams = false;

  private class EyeTextureData {
    public Texture2D mainTexture = null;
    public Texture2D distortion = null;
    public Image.FormatType formatType = Image.FormatType.INFRARED;
  }

  private void resetGlobalShaderVariants(EyeTextureData eyeTextureData) {
    switch (eyeTextureData.formatType) {
      case Image.FormatType.INFRARED:
        Shader.DisableKeyword(RGB_SHADER_VARIANT_NAME);
        Shader.EnableKeyword(IR_SHADER_VARIANT_NAME);
        break;
      case (Image.FormatType)4:
        Shader.DisableKeyword(IR_SHADER_VARIANT_NAME);
        Shader.EnableKeyword(RGB_SHADER_VARIANT_NAME);
        break;
      default:
        Debug.LogWarning("Unexpected format type " + eyeTextureData.formatType);
        break;
    }
  }

  private void updateGlobalShaderProperties(EyeTextureData eyeTextureData) {
    Shader.SetGlobalTexture("_LeapGlobalTexture", eyeTextureData.mainTexture);
    Shader.SetGlobalTexture("_LeapGlobalDistortion", eyeTextureData.distortion);

    Vector4 projection = new Vector4();
    projection.x = _cachedCamera.projectionMatrix[0, 2];
    projection.y = 0f;
    projection.z = _cachedCamera.projectionMatrix[0, 0];
    projection.w = _cachedCamera.projectionMatrix[1, 1];
    Shader.SetGlobalVector("_LeapGlobalProjection", projection);

    Shader.SetGlobalFloat("_LeapGlobalGammaCorrectionExponent", 1.0f / gammaCorrection);

    // Set camera parameters
    Shader.SetGlobalFloat("_LeapGlobalVirtualCameraV", _cachedCamera.fieldOfView);
    Shader.SetGlobalFloat("_LeapGlobalVirtualCameraH", Mathf.Rad2Deg * Mathf.Atan(Mathf.Tan(Mathf.Deg2Rad * _cachedCamera.fieldOfView / 2f) * _cachedCamera.aspect) * 2f);
    Shader.SetGlobalMatrix("_LeapGlobalInverseView", _cachedCamera.worldToCameraMatrix.inverse);
  }

  private TextureFormat getTextureFormat(Image image) {
    switch (image.Format) {
      case Image.FormatType.INFRARED:
        return TextureFormat.Alpha8;
      case (Image.FormatType)4:
        return TextureFormat.RGBA32;
      default:
        throw new System.Exception("Unexpected image format!");
    }
  }

  private int bytesPerPixel(TextureFormat format) {
    switch (format) {
      case TextureFormat.Alpha8:
        return 1;
      case TextureFormat.RGBA32:
      case TextureFormat.BGRA32:
      case TextureFormat.ARGB32:
        return 4;
      default:
        throw new System.Exception("Unexpected texture format " + format);
    }
  }

  private int totalBytes(Texture2D texture) {
    return texture.width * texture.height * bytesPerPixel(texture.format);
  }

  private void ensureMainTextureUpdated(Image image, EyeTextureData textureData) {
    int width = image.Width;
    int height = image.Height;

    if (textureData.mainTexture == null || textureData.mainTexture.width != width || textureData.mainTexture.height != height) {
      TextureFormat format = getTextureFormat(image);

      if (textureData.mainTexture != null) {
        DestroyImmediate(textureData.mainTexture);
      }

      textureData.mainTexture = new Texture2D(image.Width, image.Height, format, false, true);
      textureData.mainTexture.wrapMode = TextureWrapMode.Clamp;
      textureData.mainTexture.filterMode = FilterMode.Bilinear;
      textureData.formatType = image.Format;
      _mainTextureIntermediateArray = new byte[width * height * bytesPerPixel(format)];

      _forceDistortionRecalc = true;
      resetGlobalShaderVariants(textureData);
    }

    Marshal.Copy(image.DataPointer(), _mainTextureIntermediateArray, 0, _mainTextureIntermediateArray.Length);
    textureData.mainTexture.LoadRawTextureData(_mainTextureIntermediateArray);
    textureData.mainTexture.Apply();
  }

  private void encodeFloat(float value, out byte byte0, out byte byte1) {
    // The distortion range is -0.6 to +1.7. Normalize to range [0..1).
    value = (value + 0.6f) / 2.3f;
    float enc_0 = value;
    float enc_1 = value * 255.0f;

    enc_0 = enc_0 - (int)enc_0;
    enc_1 = enc_1 - (int)enc_1;

    enc_0 -= 1.0f / 255.0f * enc_1;

    byte0 = (byte)(enc_0 * 256.0f);
    byte1 = (byte)(enc_1 * 256.0f);
  }

  private void ensureDistortionUpdated(Image image, EyeTextureData textureData) {
    int width = image.DistortionWidth / 2;
    int height = image.DistortionHeight;

    if (textureData.distortion == null || textureData.distortion.width != width || textureData.distortion.height != height || textureData.formatType != image.Format) {
      if (textureData.distortion != null) {
        DestroyImmediate(textureData.distortion);
      }

      _distortionIntermediateArray = new Color32[width * height];
      textureData.distortion = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
      textureData.distortion.wrapMode = TextureWrapMode.Clamp;

      textureData.formatType = image.Format;

      _forceDistortionRecalc = true;
    }

    if (_forceDistortionRecalc) {
      float[] distortionData = image.Distortion;

      // Move distortion data to distortion texture
      for (int i = 0; i < distortionData.Length; i += 2) {
        byte b0, b1, b2, b3;
        encodeFloat(distortionData[i], out b0, out b1);
        encodeFloat(distortionData[i + 1], out b2, out b3);
        _distortionIntermediateArray[i / 2] = new Color32(b0, b1, b2, b3);
      }

      textureData.distortion.SetPixels32(_distortionIntermediateArray);
      textureData.distortion.Apply();
    }
  }

#if UNITY_EDITOR
  void Reset() {
    string lowercaseName = gameObject.name.ToLower();
    if (lowercaseName.Contains("right")) {
      retrievedEye = EYE.RIGHT;
    } else if (lowercaseName.Contains("left")) {
      retrievedEye = EYE.LEFT;
    } else {
      retrievedEye = EYE.LEFT_TO_RIGHT;
    }
  }

  void OnValidate() {
    foreach (var retriever in FindObjectsOfType<LeapImageRetriever>()) {
      bool anyChange = false;

      if (retriever.gammaCorrection != gammaCorrection) {
        retriever.gammaCorrection = gammaCorrection;
        anyChange = true;
      }

      if (retriever.syncMode != syncMode) {
        retriever.syncMode = syncMode;
        anyChange = true;
      }

      if (anyChange) {
        UnityEditor.EditorUtility.SetDirty(retriever);
      }
    }
  }
#endif

  void Start() {
    if (HandController.Main == null) {
      Debug.LogWarning("Could not find Main Hand Controller.  Cannot use LeapImageRetriever if there is no Main Hand Controller!");
      enabled = false;
      return;
    }

    float gamma = 1f;
    if (QualitySettings.activeColorSpace != ColorSpace.Linear) {
      gamma = -Mathf.Log10(Mathf.GammaToLinearSpace(0.1f));
    }
    Shader.SetGlobalFloat("_LeapGlobalColorSpaceGamma", gamma);

    _eyeTextureData[0] = new EyeTextureData();
    _eyeTextureData[1] = new EyeTextureData();

    _cachedCamera = GetComponent<Camera>();

    _controller = HandController.Main.GetLeapController();
    _controller.SetPolicy(Controller.PolicyFlag.POLICY_IMAGES);
  }

  void Update() {
    Frame frame = _controller.Frame();

    _forceDistortionRecalc = false;
    
    //Force a distortion recalculation if a hand has entered the frame when previously there were no hands
    if (frame.Hands.Count != 0 && _controller.Frame(1).Hands.Count == 0) {
      _forceDistortionRecalc = true;
    }

    if (syncMode == SYNC_MODE.SYNC_WITH_HANDS) {
      _imageList = frame.Images;
    }
  }

  void OnPreCull() {
    frameEye = 0;
  }

  void OnPreRender() {
    if (syncMode == SYNC_MODE.LOW_LATENCY) {
      _imageList = _controller.Images;
    }

    if (!_hasFiredCameraParams && OnValidCameraParams != null) {
      CameraParams cameraParams = new CameraParams(_cachedCamera);
      OnValidCameraParams(cameraParams);
      _hasFiredCameraParams = true;
    }

    int imageEye = frameEye;
    switch (retrievedEye) {
      case EYE.LEFT:
        imageEye = 0;
        break;
      case EYE.RIGHT:
        imageEye = 1;
        break;
      case EYE.RIGHT_TO_LEFT:
        imageEye = 1 - imageEye;
        break;
      default:
        break;
    }

    bool isLeft = imageEye == 0;
    if (isLeft) {
      if (OnLeftPreRender != null) {
        OnLeftPreRender();
      }
    } else {
      if (OnRightPreRender != null) {
        OnRightPreRender();
      }
    }

    Image referenceImage = _imageList[imageEye];
    EyeTextureData eyeTextureData = _eyeTextureData[imageEye];

    if (referenceImage.Width == 0 || referenceImage.Height == 0) {
      _missedImages++;
      if (_missedImages == IMAGE_WARNING_WAIT) {
        Debug.LogWarning("Can't find any images. " +
          "Make sure you enabled 'Allow Images' in the Leap Motion Settings, " +
          "you are on tracking version 2.1+ and " +
          "your Leap Motion device is plugged in.");
      }
      return;
    }

    ensureMainTextureUpdated(referenceImage, eyeTextureData);
    ensureDistortionUpdated(referenceImage, eyeTextureData);

    updateGlobalShaderProperties(eyeTextureData);

    frameEye++;
  }

  private void forceReInit() {
    _forceDistortionRecalc = true;

    
  }

  /// <returns>The time at which the current image was recorded, in microseconds</returns>
  public long ImageNow() {
    if (_imageList == null) {
      Debug.LogWarning("Images have not been initialized -> defaulting to LeapNow");
      return LeapNow();
    }

    if (_imageList.IsEmpty) {
      Debug.LogWarning("ImageNow has no images -> defaulting to LeapNow");
      return LeapNow();
    }
    return _imageList[0].Timestamp;
  }

  /// <returns>The current time using the same clock as GetImageNow</returns>
  public long LeapNow() {
    return _controller.Now();
  }
}
