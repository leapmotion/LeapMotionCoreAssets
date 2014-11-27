/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Leap;

public struct LMDevice
{
  public static int PERIPERAL_WIDTH = 640;
  public static int PERIPERAL_HEIGHT = 240;
  public static int DRAGONFLY_WIDTH = 608;
  public static int DRAGONFLY_HEIGHT = 540;

  public int width;
  public int height;
  public int pixels;
  public LM_DEVICE type;

  public LMDevice (LM_DEVICE device = LM_DEVICE.INVALID)
  {
    type = device;
    switch (type)
    {
      case LM_DEVICE.PERIPHERAL:
        width = PERIPERAL_WIDTH;
        height = PERIPERAL_HEIGHT;
        break;
      case LM_DEVICE.DRAGONFLY:
        width = DRAGONFLY_WIDTH;
        height = DRAGONFLY_HEIGHT;
        break;
      default:
        width = 0;
        height = 0;
        break;
    }
    this.pixels = width * height;
  }
}

public enum LM_DEVICE
{
  INVALID = -1,
  PERIPHERAL = 0,
  DRAGONFLY = 1
}

// To use the LeapImageRetriever you must be on version 2.1+
// and enable "Allow Images" in the Leap Motion settings.
public class LeapImageRetriever : MonoBehaviour
{
  public const string IR_NORMAL_SHADER = "LeapMotion/LeapIRDistorted";
  public const string IR_UNDISTORT_SHADER = "LeapMotion/LeapIRUndistorted";
  public const string RGB_NORMAL_SHADER = "LeapMotion/LeapRGBDistorted";
  public const string RGB_UNDISTORT_SHADER = "LeapMotion/LeapRGBUndistorted";

  public const int DEFAULT_DISTORTION_WIDTH = 64;
  public const int DEFAULT_DISTORTION_HEIGHT = 64;
  public const int IMAGE_WARNING_WAIT = 10;

  public int imageIndex = 0;
  public Color imageColor = Color.white;
  public float gammaCorrection = 1.0f;
  public bool undistortImage = true;
  public bool blackIsTransparent = true;

  protected Controller leap_controller_;
  private LMDevice attached_device_ = new LMDevice();

  // Main texture.
  protected Texture2D main_texture_;
  protected Color32[] image_pixels_;
  protected int image_misses_ = 0;

  // Distortion textures.
  protected Texture2D distortionX_;
  protected Texture2D distortionY_;
  protected Color32[] dist_pixelsX_;
  protected Color32[] dist_pixelsY_;

  private LM_DEVICE GetDevice(int width, int height)
  {
    if (width == LMDevice.PERIPERAL_WIDTH && height == LMDevice.PERIPERAL_HEIGHT)
    {
      return LM_DEVICE.PERIPHERAL;
    }
    else if (width == LMDevice.DRAGONFLY_WIDTH && height == LMDevice.DRAGONFLY_HEIGHT)
    {
      return LM_DEVICE.DRAGONFLY;
    }
    return LM_DEVICE.PERIPHERAL;
  }

  protected void SetShader() 
  {
    DestroyImmediate(renderer.material);
    if (undistortImage)
    {
      switch (attached_device_.type)
      {
        case LM_DEVICE.PERIPHERAL:
          renderer.material = new Material(Shader.Find(IR_UNDISTORT_SHADER));
          break;
        case LM_DEVICE.DRAGONFLY:
          renderer.material = new Material(Shader.Find(RGB_UNDISTORT_SHADER));
          break;
        default:
          renderer.material = new Material(Shader.Find(IR_UNDISTORT_SHADER));
          break;
      }
    }
    else
    {
      switch (attached_device_.type)
      {
        case LM_DEVICE.PERIPHERAL:
          renderer.material = new Material(Shader.Find(IR_NORMAL_SHADER));
          break;
        case LM_DEVICE.DRAGONFLY:
          renderer.material = new Material(Shader.Find(RGB_NORMAL_SHADER));
          break;
        default:
          renderer.material = new Material(Shader.Find(IR_NORMAL_SHADER));
          break;
      }
    }
  }

  protected void SetRenderer(ref Image image)
  {
    renderer.material.mainTexture = main_texture_;
    renderer.material.SetColor("_Color", imageColor);
    renderer.material.SetInt("_DeviceType", Convert.ToInt32(attached_device_.type));
    renderer.material.SetFloat("_GammaCorrection", gammaCorrection);
    renderer.material.SetInt("_BlackIsTransparent", blackIsTransparent ? 1 : 0);

    renderer.material.SetTexture("_DistortX", distortionX_);
    renderer.material.SetTexture("_DistortY", distortionY_);
    renderer.material.SetFloat("_RayOffsetX", image.RayOffsetX);
    renderer.material.SetFloat("_RayOffsetY", image.RayOffsetY);
    renderer.material.SetFloat("_RayScaleX", image.RayScaleX);
    renderer.material.SetFloat("_RayScaleY", image.RayScaleY);
  }

  protected bool InitiateTexture(ref Image image)
  {
    int width = image.Width;
    int height = image.Height;

    attached_device_ = new LMDevice(GetDevice(width, height));
    if (attached_device_.width == 0 || attached_device_.height == 0)
    {
      attached_device_ = new LMDevice();
      Debug.LogWarning("No data in the image texture.");
      return false;
    }
    else
    {
      switch (attached_device_.type)
      {
        case LM_DEVICE.PERIPHERAL:
          main_texture_ = new Texture2D(attached_device_.width, attached_device_.height, TextureFormat.Alpha8, false);
          break;
        case LM_DEVICE.DRAGONFLY:
          main_texture_ = new Texture2D(attached_device_.width, attached_device_.height, TextureFormat.RGBA32, false);
          break;
        default:
          main_texture_ = new Texture2D(attached_device_.width, attached_device_.height, TextureFormat.Alpha8, false);
          break;
      }
      main_texture_.wrapMode = TextureWrapMode.Clamp;
      image_pixels_ = new Color32[attached_device_.pixels];
    }
    return true;
  }

  protected bool InitiateDistortion(ref Image image)
  {
    int width = image.DistortionWidth / 2;
    int height = image.DistortionHeight;

    if (width == 0 || height == 0)
    {
      Debug.LogWarning("No data in image distortion");
      return false;
    }
    else
    {
      dist_pixelsX_ = new Color32[width * height];
      dist_pixelsY_ = new Color32[width * height];
      DestroyImmediate(distortionX_);
      DestroyImmediate(distortionY_);
      distortionX_ = new Texture2D(width, height, TextureFormat.RGBA32, false);
      distortionY_ = new Texture2D(width, height, TextureFormat.RGBA32, false);
      distortionX_.wrapMode = TextureWrapMode.Clamp;
      distortionY_.wrapMode = TextureWrapMode.Clamp;
    }

    return true;
  }

  protected bool InitiatePassthrough(ref Image image)
  {
    if (!InitiateTexture(ref image))
      return false;

    if (!InitiateDistortion(ref image))
      return false;
    
    SetShader();
    SetRenderer(ref image);

    return true;
  }

  protected void LoadMainTexture(ref Image image)
  {
    byte[] image_data = image.Data;
    switch (attached_device_.type)
    {
      case LM_DEVICE.PERIPHERAL:
        for (int i = 0; i < image_data.Length; ++i)
          image_pixels_[i].a = image_data[i];
        break;
      case LM_DEVICE.DRAGONFLY:
        int image_index = 0;
        for (int i = 0; i < image_data.Length; image_index++)
        {
          image_pixels_[image_index].r = image_data[i++];
          image_pixels_[image_index].g = image_data[i++];
          image_pixels_[image_index].b = image_data[i++];
          image_pixels_[image_index].a = image_data[i++];
        }
        gammaCorrection = Mathf.Max(gammaCorrection, 1.7f);
        break;
      default:
        for (int i = 0; i < image_data.Length; ++i)
          image_pixels_[i].a = image_data[i];
        break;
    }

    main_texture_.SetPixels32(image_pixels_);
    main_texture_.Apply();
  }

  protected bool LoadDistortion(ref Image image)
  {
    if (image.DistortionWidth == 0 || image.DistortionHeight == 0)
    {
      Debug.LogWarning("No data in the distortion texture.");
      return false;
    }

    if (undistortImage)
    {
      float[] distortion_data = image.Distortion;
      int num_distortion_floats = 2 * distortionX_.width * distortionX_.height;

      // Move distortion data to distortion x textures.
      for (int i = 0; i < num_distortion_floats; i += 2)
      {
        // The distortion range is -0.6 to +1.7. Normalize to range [0..1).
        float dval = (distortion_data[i] + 0.6f) / 2.3f;
        float enc_x = dval;
        float enc_y = dval * 255.0f;
        float enc_z = dval * 65025.0f;
        float enc_w = dval * 160581375.0f;

        enc_x = enc_x - (int)enc_x;
        enc_y = enc_y - (int)enc_y;
        enc_z = enc_z - (int)enc_z;
        enc_w = enc_w - (int)enc_w;

        enc_x -= 1.0f / 255.0f * enc_y;
        enc_y -= 1.0f / 255.0f * enc_z;
        enc_z -= 1.0f / 255.0f * enc_w;

        int index = i >> 1;
        dist_pixelsX_[index].r = (byte)(256 * enc_x);
        dist_pixelsX_[index].g = (byte)(256 * enc_y);
        dist_pixelsX_[index].b = (byte)(256 * enc_z);
        dist_pixelsX_[index].a = (byte)(256 * enc_w);
      }
      distortionX_.SetPixels32(dist_pixelsX_);
      distortionX_.Apply();

      // Move distortion data to distortion y textures.
      for (int i = 1; i < num_distortion_floats; i += 2)
      {
        // The distortion range is -0.6 to +1.7. Normalize to range [0..1).
        float dval = (distortion_data[i] + 0.6f) / 2.3f;
        float enc_x = dval;
        float enc_y = dval * 255.0f;
        float enc_z = dval * 65025.0f;
        float enc_w = dval * 160581375.0f;

        enc_x = enc_x - (int)enc_x;
        enc_y = enc_y - (int)enc_y;
        enc_z = enc_z - (int)enc_z;
        enc_w = enc_w - (int)enc_w;

        enc_x -= 1.0f / 255.0f * enc_y;
        enc_y -= 1.0f / 255.0f * enc_z;
        enc_z -= 1.0f / 255.0f * enc_w;

        int index = i >> 1;
        dist_pixelsY_[index].r = (byte)(256 * enc_x);
        dist_pixelsY_[index].g = (byte)(256 * enc_y);
        dist_pixelsY_[index].b = (byte)(256 * enc_z);
        dist_pixelsY_[index].a = (byte)(256 * enc_w);
      }
      distortionY_.SetPixels32(dist_pixelsY_);
      distortionY_.Apply();
    }
      
    return true;
  }

  void Start()
  {
    leap_controller_ = new Controller();
    leap_controller_.SetPolicyFlags(Controller.PolicyFlag.POLICY_IMAGES);
  }

  void Update()
  {

    Frame frame = leap_controller_.Frame();

    if (frame.Images.Count == 0)
    {
      image_misses_++;
      if (image_misses_ == IMAGE_WARNING_WAIT)
      {
        Debug.LogWarning("Can't find any images. " +
                          "Make sure you enabled 'Allow Images' in the Leap Motion Settings, " +
                          "you are on tracking version 2.1+ and " +
                          "your Leap Motion device is plugged in.");
      }
      return;
    }

    // Check main texture dimensions.
    Image image = frame.Images[imageIndex];

    if (attached_device_.width != image.Width || attached_device_.height != image.Height)
    {
      if (!InitiatePassthrough(ref image))
        return;
    }

    LoadMainTexture(ref image);
    LoadDistortion(ref image);
  }
}
