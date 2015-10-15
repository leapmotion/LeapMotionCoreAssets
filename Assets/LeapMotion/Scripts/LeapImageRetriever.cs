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

      if (SystemInfo.graphicsDeviceVersion.ToLower().Contains("direct3d")) {
        for (int i = 0; i < 4; i++) {
          ProjectionMatrix[1, i] = -ProjectionMatrix[1, i];
        }
        // Scale and bias from OpenGL -> D3D depth range
        for (int i = 0; i < 4; i++) {
          ProjectionMatrix[2, i] = ProjectionMatrix[2, i] * 0.5f + ProjectionMatrix[3, i] * 0.5f;
        }
      }

      Width = camera.pixelWidth;
      Height = camera.pixelHeight;
    }
  }

  public EyeType eyeType;

  [Tooltip("Should the image match the tracked hand, or should it be displayed as fast as possible")]
  public SYNC_MODE syncMode = SYNC_MODE.SYNC_WITH_HANDS;

  public float gammaCorrection = 1.0f;

  private int _missedImages = 0;
  private Controller _controller;
  private int frameEye = 0;

  //ImageList to use during rendering.  Can either be updated in OnPreRender or in Update
  private ImageList _imageList;
  private ImageList _rawImageList;
  private Camera _cachedCamera;

  private EyeTextureData[] _eyeTextureData = new EyeTextureData[2]; // left and right data

  private bool _hasFiredCameraParams = false;

  private class LeapTextureData {
    public Texture2D texture = null;
    public byte[] intermediateArray = null;
    public Image.FormatType formatType = Image.FormatType.INFRARED;

    public bool CheckStale(Image image) {
      if (texture == null || intermediateArray == null) {
        return true;
      }

      if (image.Width != texture.width || image.Height != texture.height) {
        return true;
      }

      if (texture.format != getTextureFormat(image)) {
        return true;
      }

      return false;
    }

    public void Reconstruct(Image image) {
      int width = image.Width;
      int height = image.Height;

      TextureFormat format = getTextureFormat(image);

      if (texture != null) {
        DestroyImmediate(texture);
      }

      texture = new Texture2D(image.Width, image.Height, format, false, true);
      texture.wrapMode = TextureWrapMode.Clamp;
      texture.filterMode = FilterMode.Bilinear;
      intermediateArray = new byte[width * height * bytesPerPixel(format)];

      formatType = image.Format;
    }

    public void UpdateTexture(Image image) {
      Marshal.Copy(image.DataPointer(), intermediateArray, 0, intermediateArray.Length);
      texture.LoadRawTextureData(intermediateArray);
      texture.Apply();
    }

    private TextureFormat getTextureFormat(Image image) {
      switch (image.Format) {
        case Image.FormatType.INFRARED:
          return TextureFormat.Alpha8;
        case (Image.FormatType)4:
        case Image.FormatType.IBRG:
          return TextureFormat.RGBA32;
        default:
          throw new System.Exception("Unexpected image format " + image.Format + "!");
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
  }

  private class LeapDistortionData {
    public Texture2D texture = null;

    public bool CheckStale() {
      return texture == null;
    }

    public void Reconstruct(Image image) {
      int width = image.DistortionWidth / 2;
      int height = image.DistortionHeight;

      if (texture != null) {
        DestroyImmediate(texture);
      }

      Color32[] colorArray = new Color32[width * height];
      texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
      texture.filterMode = FilterMode.Bilinear;
      texture.wrapMode = TextureWrapMode.Clamp;

      float[] distortionData = image.Distortion;

      // Move distortion data to distortion texture
      for (int i = 0; i < distortionData.Length; i += 2) {
        byte b0, b1, b2, b3;
        encodeFloat(distortionData[i], out b0, out b1);
        encodeFloat(distortionData[i + 1], out b2, out b3);
        colorArray[i / 2] = new Color32(b0, b1, b2, b3);
      }

      texture.SetPixels32(colorArray);
      texture.Apply();
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
  }

  private class EyeTextureData {
    public LeapTextureData mainTexture = new LeapTextureData();
    public LeapTextureData rawTexture = new LeapTextureData();
    public LeapDistortionData distortion = new LeapDistortionData();

    public bool CheckStale(Image mainImage, Image rawImage) {
      if (mainTexture.CheckStale(mainImage) || distortion.CheckStale()) {
        return true;
      }

      //Only need to check with the raw texture if we are in color mode
      if (rawImage.Format == Image.FormatType.IBRG) {
        return rawTexture.CheckStale(rawImage);
      }

      return false;
    }

    public void Reconstruct(Image mainImage, Image rawImage) {
      mainTexture.Reconstruct(mainImage);
      distortion.Reconstruct(mainImage);

      switch (rawImage.Format) {
        case Image.FormatType.INFRARED:
          Shader.DisableKeyword(RGB_SHADER_VARIANT_NAME);
          Shader.EnableKeyword(IR_SHADER_VARIANT_NAME);
          break;
        case (Image.FormatType)4:
          Shader.DisableKeyword(IR_SHADER_VARIANT_NAME);
          Shader.EnableKeyword(RGB_SHADER_VARIANT_NAME);
          rawTexture.Reconstruct(rawImage); //Only need raw texture for Dragonfly
          break;
        default:
          Debug.LogWarning("Unexpected format type " + rawTexture.formatType);
          break;
      }
    }

    public void UpdateTextures(Image mainImage, Image rawImage) {
      mainTexture.UpdateTexture(mainImage);
      rawTexture.UpdateTexture(rawImage);
    }
  }

  private void updateGlobalShaderProperties(EyeTextureData eyeTextureData) {
    Shader.SetGlobalTexture("_LeapGlobalMainTexture", eyeTextureData.mainTexture.texture);
    Shader.SetGlobalTexture("_LeapGlobalDistortion", eyeTextureData.distortion.texture);

    if (eyeTextureData.rawTexture.texture != null) {
      Shader.SetGlobalTexture("_LeapGlobalRawTexture", eyeTextureData.rawTexture.texture);
    }

    Vector4 projection = new Vector4();
    projection.x = _cachedCamera.projectionMatrix[0, 2];
    projection.y = 0f;
    projection.z = _cachedCamera.projectionMatrix[0, 0];
    projection.w = _cachedCamera.projectionMatrix[1, 1];
    Shader.SetGlobalVector("_LeapGlobalProjection", projection);

    Shader.SetGlobalFloat("_LeapGlobalGammaCorrectionExponent", 1.0f / gammaCorrection);
  }

  void Reset() {
    eyeType = new EyeType(gameObject.name);
  }

  void Start() {
    if (HandController.Main == null) {
      Debug.LogWarning("Cannot use LeapImageRetriever if there is no main HandController!");
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
    if (_controller == null) {
      return;
    }

    Frame frame = _controller.Frame();

    if (syncMode == SYNC_MODE.SYNC_WITH_HANDS) {
      _imageList = frame.Images;
      _rawImageList = frame.RawImages;
    }
  }

  void OnPreCull() {
    frameEye = 0;
  }

  void OnPreRender() {
    eyeType.BeginCamera();

    if (syncMode == SYNC_MODE.LOW_LATENCY) {
      _imageList = _controller.Images;
      _rawImageList = _controller.RawImages;
    }

    if (_imageList == null || _imageList.Count == 0 || _rawImageList == null || _rawImageList.Count == 0) {
      _missedImages++;
      if (_missedImages == IMAGE_WARNING_WAIT) {
        Debug.LogWarning("Can't find any images. " +
          "Make sure you enabled 'Allow Images' in the Leap Motion Settings, " +
          "you are on tracking version 2.1+ and " +
          "your Leap Motion device is plugged in.");
      }
      return;
    }

    if (!_hasFiredCameraParams && OnValidCameraParams != null) {
      CameraParams cameraParams = new CameraParams(_cachedCamera);
      OnValidCameraParams(cameraParams);
      _hasFiredCameraParams = true;
    }

    int imageIndex;

    if (eyeType.IsLeftEye) {
      imageIndex = 0;
      if (OnLeftPreRender != null) OnLeftPreRender();
    } else {
      imageIndex = 1;
      if (OnRightPreRender != null) OnRightPreRender();
    }

    Image referenceImage = _imageList[imageIndex];
    Image referenceRawImage = _rawImageList[imageIndex];
    EyeTextureData eyeTextureData = _eyeTextureData[imageIndex];

    if (eyeTextureData.CheckStale(referenceImage, referenceRawImage)) {
      eyeTextureData.Reconstruct(referenceImage, referenceRawImage);
    }

    eyeTextureData.UpdateTextures(referenceImage, referenceRawImage);

    updateGlobalShaderProperties(eyeTextureData);

    frameEye++;
  }
}
