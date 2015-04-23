//#define OVERRIDE_MANTIS

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

// To use the LeapImageRetriever you must be on version 2.1+
// and enable "Allow Images" in the Leap Motion settings.
public class LeapImageRetriever : MonoBehaviour {
    public enum EYE {
        LEFT = 0,
        RIGHT = 1
    }

    public enum SYNC_MODE {
        SYNC_WITH_HANDS,
        LOW_LATENCY
    }

    public EYE eye = (EYE)(-1);
    [Tooltip ("Should the image match the tracked hand, or should it be displayed as fast as possible")]
    public SYNC_MODE syncMode = SYNC_MODE.SYNC_WITH_HANDS;
    public float gammaCorrection = 1.0f;
    public bool rescaleController = true;
    public bool undistortImage = true;

    public const int IMAGE_WARNING_WAIT = 10;
    protected int image_misses_ = 0;
    protected Controller _controller;

    //Information about the current format the retriever is configured for.  Used to detect changes in format
    private int _currentWidth = 0;
    private int _currentHeight = 0;
    private Image.FormatType _currentFormat = (Image.FormatType)(-1);
    private string _enabledMaterialKeyword = null;

    //ImageList to use during rendering.  Can either be updated in OnPreRender or in Update
    private ImageList _imageList;

    //Holders for Image Based Materials
    private static List<LeapImageBasedMaterial> registeredImageBasedMaterials = new List<LeapImageBasedMaterial>();
    private static List<LeapImageBasedMaterial> imageBasedMaterialsToInit = new List<LeapImageBasedMaterial>();

    // Main texture.
    protected Texture2D _main_texture = null;

    // Distortion textures.
    protected bool shouldRecalculateDistortion = false;
    protected Texture2D distortion_ = null;
    protected Color32[] dist_pixels_;

    public static void registerImageBasedMaterial(LeapImageBasedMaterial imageBasedMaterial) {
        registeredImageBasedMaterials.Add(imageBasedMaterial);
        imageBasedMaterialsToInit.Add(imageBasedMaterial);
    }

    public static void unregisterImageBasedMaterial(LeapImageBasedMaterial imageBasedMaterial) {
        registeredImageBasedMaterials.Remove(imageBasedMaterial);
    }

    private void initImageBasedMaterial(LeapImageBasedMaterial imageBasedMaterial) {
        Material material = imageBasedMaterial.GetComponent<Renderer>().material;

        if (_enabledMaterialKeyword != null) {
            material.DisableKeyword(_enabledMaterialKeyword);
        }

        switch(_currentFormat){
            case Image.FormatType.INFRARED:
                _enabledMaterialKeyword = "LEAP_FORMAT_IR";
                break;
            case (Image.FormatType)4:
                _enabledMaterialKeyword = "LEAP_FORMAT_RGB";
                break;
            default:
                _enabledMaterialKeyword = null;
                Debug.LogWarning("Unexpected format type " + _currentFormat);
                break;
        }

        if (_enabledMaterialKeyword != null) {
            material.EnableKeyword(_enabledMaterialKeyword);
        }

        imageBasedMaterial.GetComponent<Renderer>().material.SetFloat("_LeapGammaCorrectionExponent", 1.0f / gammaCorrection);
    }

    private void updateImageBasedMaterial(LeapImageBasedMaterial imageBasedMaterial, ref Image image) {
        imageBasedMaterial.GetComponent<Renderer>().material.SetTexture("_LeapTexture", _main_texture);

        Vector4 projection = new Vector4();
        projection.x = GetComponent<Camera>().projectionMatrix[0, 2];
        projection.z = GetComponent<Camera>().projectionMatrix[0, 0];
        projection.w = GetComponent<Camera>().projectionMatrix[1, 1];
        imageBasedMaterial.GetComponent<Renderer>().material.SetVector("_LeapProjection", projection);

        if (distortion_ == null) {
            initDistortion(ref image);
            loadDistortion(ref image);
            shouldRecalculateDistortion = false;
        }

        //Only recalculate distortion if a recalculate is requested AND there is at least one hand in the scene
        //This is to get around the fact that we can't know if a device has been flipped
        if (shouldRecalculateDistortion && _controller.Frame().Hands.Count != 0) {
            loadDistortion(ref image);
            shouldRecalculateDistortion = false;
        }

        imageBasedMaterial.GetComponent<Renderer>().material.SetTexture("_LeapDistortion", distortion_);
    }

    protected TextureFormat getTextureFormat(ref Image image) {
        switch (image.Format) {
            case Image.FormatType.INFRARED:
                return TextureFormat.Alpha8;
            case (Image.FormatType)4:
                return TextureFormat.RGBA32;
            default:
                throw new System.Exception("Unexpected image format!");
        }
    }

    protected int bytesPerPixel(TextureFormat format) {
        switch (format) {
            case TextureFormat.Alpha8: return 1;
            case TextureFormat.RGBA32:
            case TextureFormat.BGRA32:
            case TextureFormat.ARGB32: return 4;
            default: throw new System.Exception("Unexpected texture format " + format);
        }
    }

    protected int totalBytes(Texture2D texture) {
        return texture.width * texture.height * bytesPerPixel(texture.format);
    }

    protected void initTexture(ref Image image, ref Texture2D texture) {
        TextureFormat format = getTextureFormat(ref image);
        texture = new Texture2D(image.Width, image.Height, format, false, true);
        texture.wrapMode = TextureWrapMode.Clamp;
    }

    protected void loadTexture(ref Image sourceImage, ref Texture2D destTexture) {
        byte[] data = sourceImage.Data;

        int epxected = totalBytes(destTexture);
        if (data.Length != epxected) {
            Debug.LogWarning("Expected " + epxected + " bytes but recieved " + data.Length);
            return;
        }

        destTexture.LoadRawTextureData(data);
        destTexture.Apply();
    }

    protected bool initDistortion(ref Image image) {
        int width = image.DistortionWidth / 2;
        int height = image.DistortionHeight;

        if (width == 0 || height == 0) {
            Debug.LogWarning("No data in image distortion");
            return false;
        } else {
            dist_pixels_ = new Color32[width * height];
            DestroyImmediate(distortion_);
            distortion_ = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            distortion_.wrapMode = TextureWrapMode.Clamp;
        }

        return true;
    }

    protected bool loadDistortion(ref Image image) {
        if (image.DistortionWidth == 0 || image.DistortionHeight == 0) {
            Debug.LogWarning("No data in the distortion texture.");
            return false;
        }

        float[] distortion_data = image.Distortion;
        int num_distortion_floats = 2 * distortion_.width * distortion_.height;

        // Move distortion data to distortion texture
        for (int i = 0; i < num_distortion_floats; i += 2) {
            // The distortion range is -0.6 to +1.7. Normalize to range [0..1).
            float dvalX = (distortion_data[i] + 0.6f) / 2.3f;
            float enc_x = dvalX;
            float enc_y = dvalX * 255.0f;

            enc_x = enc_x - (int)enc_x;
            enc_y = enc_y - (int)enc_y;

            enc_x -= 1.0f / 255.0f * enc_y;

            float dvalY = (distortion_data[i + 1] + 0.6f) / 2.3f;
            float enc_z = dvalY;
            float enc_w = dvalY * 255.0f;

            enc_z = enc_z - (int)enc_z;
            enc_w = enc_w - (int)enc_w;

            enc_z -= 1.0f / 255.0f * enc_w;

            int index = i >> 1;
            Color32 color = new Color32((byte)(enc_x * 256.0f),
                                        (byte)(enc_y * 256.0f),
                                        (byte)(enc_z * 256.0f),
                                        (byte)(enc_w * 256.0f));
            dist_pixels_[index] = color;
        }
        distortion_.SetPixels32(dist_pixels_);
        distortion_.Apply();

        return true;
    }

    void Start() {
        HandController handController = FindObjectOfType<HandController>();
        if (handController == null) {
            Debug.LogWarning("Cannot use LeapImageRetriever if there is no HandController in the scene!");
            enabled = false;
            return;
        }

        _controller = handController.GetLeapController();
        _controller.SetPolicy(Controller.PolicyFlag.POLICY_IMAGES);
    }

    void Update() {
        Frame frame = _controller.Frame();

        if (frame.Hands.Count == 0) {
            shouldRecalculateDistortion = true;
        }

        if (syncMode == SYNC_MODE.SYNC_WITH_HANDS) {
            _imageList = frame.Images;
        }
    }

    void OnPreRender() {
        if (syncMode == SYNC_MODE.LOW_LATENCY) {
            _imageList = _controller.Images;
        }

        // Check main texture dimensions.
        Image referenceImage = _imageList[(int)eye];

        if (referenceImage.Width == 0 || referenceImage.Height == 0) {
            image_misses_++;
            if (image_misses_ == IMAGE_WARNING_WAIT) {
                Debug.LogWarning("Can't find any images. " +
                                  "Make sure you enabled 'Allow Images' in the Leap Motion Settings, " +
                                  "you are on tracking version 2.1+ and " +
                                  "your Leap Motion device is plugged in.");
            }
        }

        if (referenceImage.Height != _currentHeight || referenceImage.Width != _currentWidth || referenceImage.Format != _currentFormat) {
            initTexture(ref referenceImage, ref _main_texture);

            _currentHeight = referenceImage.Height;
            _currentWidth = referenceImage.Width;
            _currentFormat = referenceImage.Format;

            imageBasedMaterialsToInit.Clear();
            imageBasedMaterialsToInit.AddRange(registeredImageBasedMaterials);
        }

        loadTexture(ref referenceImage, ref _main_texture);

        for (int i = imageBasedMaterialsToInit.Count - 1; i >= 0; i--) {
            LeapImageBasedMaterial material = imageBasedMaterialsToInit[i];
            initImageBasedMaterial(material);
            imageBasedMaterialsToInit.RemoveAt(i);
        }

        foreach (LeapImageBasedMaterial material in registeredImageBasedMaterials) {
            if (material.imageMode == LeapImageBasedMaterial.ImageMode.STEREO ||
               (material.imageMode == LeapImageBasedMaterial.ImageMode.LEFT_ONLY && eye == EYE.LEFT) ||
               (material.imageMode == LeapImageBasedMaterial.ImageMode.RIGHT_ONLY && eye == EYE.RIGHT)) {
                updateImageBasedMaterial(material, ref referenceImage);
            }
        }
    }
}
