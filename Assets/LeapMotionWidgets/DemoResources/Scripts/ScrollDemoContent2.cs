using UnityEngine;
using System.Collections;
using VRWidgets;

public struct HSVColor
{
  public float h;
  public float s;
  public float v;
  public float a;

  public HSVColor(float h, float s, float v, float a = 1.0f)
  {
    this.h = h;
    this.s = s;
    this.v = v;
    this.a = a;
  }

  public HSVColor(Color color)
  {
    HSVColor temp_hsv = FromRGB(color);
    this.h = temp_hsv.h;
    this.s = temp_hsv.s;
    this.v = temp_hsv.v;
    this.a = temp_hsv.a;
  }

  public static HSVColor FromRGB(Color rgb)
  {
    HSVColor hsv = new HSVColor(0.0f, 0.0f, 0.0f, rgb.a);

    float r = rgb.r;
    float g = rgb.g;
    float b = rgb.b;

    float max = Mathf.Max(r, g, b);
    if (max <= 0)
      return hsv;

    float min = Mathf.Min(r, g, b);
    float diff = max - min;

    if (max > min)
    {
      if (g == max)
      {
        hsv.h = (b - r) / diff * 60.0f + 60.0f * 2;
      }
      else if (b == max)
      {
        hsv.h = (r - g) / diff * 60.0f + 60.0f * 4;
      } 
      else if (b > g)
      {
        hsv.h = (g - b) / diff * 60.0f + 60.0f * 6;
      }
      else
      {
        hsv.h = (g - b) / diff * 60.0f + 60.0f * 0;
      }
      if (hsv.h < 0)
      {
        hsv.h = hsv.h + 360.0f;
      }
    }
    hsv.h *= 1.0f / 360.0f;
    hsv.s = (diff / max) * 1.0f;
    hsv.v = max;

    return hsv;
  }

  public static Color ToRGB(HSVColor hsv)
  {
    float r = hsv.h;
    float g = hsv.h;
    float b = hsv.h;
    if (hsv.s != 0)
    {
      float max = hsv.v;
      float diff = hsv.v * hsv.s;
      float min = hsv.v - diff;

      float h = hsv.h * 360.0f;

      if (h < 60.0f * 1)
      {
        r = max;
        g = h * diff / 60.0f + min;
        b = min;
      }
      else if (h < 60.0f * 2)
      {
        r = -(h - 120.0f) * diff / 60.0f + min;
        g = max;
        b = min;
      }
      else if (h < 60.0f * 3)
      {
        r = min;
        g = max;
        b = +(h - 120.0f) * diff / 60.0f + min;
      }
      else if (h < 60.0f * 4)
      {
        r = min;
        g = -(h - 240.0f) * diff / 60.0f + min;
        b = max;
      }
      else if (h < 60.0f * 5)
      {
        r = +(h - 240.0f) * diff / 60.0f + min;
        g = min;
        b = max;  
      }
      else if (h <= 60.0f * 6)
      {
        r = max;
        g = min;
        b = -(h - 360.0f) * diff / 60.0f + min;
      }
      else 
      {
        r = 0;
        g = 0;
        b = 0;
      }
    }
    return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), hsv.a);
  }
}

public class ScrollDemoContent2 : ScrollContentBase 
{
	// Use this for initialization
	public override void Start () {
    base.Start();
    Renderer[] renderers = GetComponentsInChildren<Renderer>();
    float increment = 1.0f / 16.0f;
    float value = 0.0f;
    foreach (Renderer renderer in renderers)
    {
      if (renderer.transform.name == "Quad")
      {
        Color color = HSVColor.ToRGB(new HSVColor(value, 1.0f, 1.0f));
        renderer.material.color = color;
        value += increment;
      }
    }
	}
}
