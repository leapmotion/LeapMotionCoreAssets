using UnityEngine;
using System.Collections;
using VRWidgets;

public class ExponentialSmoothing {
  float alpha;
  float value = float.MinValue;

  public ExponentialSmoothing(float alpha)
  {
    this.alpha = alpha;
  }

  public float Calculate(float value) 
  {
    this.value = (this.value == float.MinValue) ? value : alpha * value + (1 - alpha) * this.value;
    return this.value;
  }

  public float Value()
  {
    return this.value;
  }
}

public class ScrollDemoViewer : ScrollViewerBase
{
  public ScrollContentBase content;

  public GameObject cursor = null;
  public GameObject incIndicator = null;
  public GameObject decIndicator = null;

  protected Limits cursor_boundaries_ = new Limits();

  private ExponentialSmoothing velocity_ = new ExponentialSmoothing(0.5f);
  private float previous_percent_ = -1.0f;

  private void SetRenderers(GameObject game_object, bool enabled)
  {
    Renderer[] renderers = game_object.GetComponentsInChildren<Renderer>();
    foreach (Renderer renderer in renderers)
    {
      renderer.enabled = enabled;
    }
  }

  public void SetBloomGain(GameObject game_object, float gain)
  {
    Renderer[] renderers = game_object.GetComponentsInChildren<Renderer>();
    foreach (Renderer renderer in renderers)
    {
      renderer.material.SetFloat("_Gain", gain);
    }
  }

  public void SetColor(GameObject game_object, Color color)
  {
    Renderer[] renderers = game_object.GetComponentsInChildren<Renderer>();
    foreach (Renderer renderer in renderers)
    {
      renderer.material.color = color;
    }
  }

  public override void ScrollActive()
  {
    SetBloomGain(scrollWindowFrame, 7.0f);
    SetBloomGain(cursor, 10.0f);
  }

  public override void ScrollInactive()
  {
    SetBloomGain(scrollWindowFrame, 5.0f);
    SetBloomGain(cursor, 5.0f);
  }
  
  public void UpdateCursors(float percent)
  {
    percent = Mathf.Clamp(percent, 0.0f, 1.0f);

    if (previous_percent_ < 0.0f)
    {
      previous_percent_ = percent;
    }
    else
    {
      velocity_.Calculate(percent - previous_percent_);
      float cursor_height = cursor_boundaries_.t - cursor_boundaries_.b;
      float upper_limit = boundaries_.t - cursor_height / 2.0f;
      float lower_limit = boundaries_.b + cursor_height / 2.0f;

      if (cursor != null)
      {
        Vector3 local_position = cursor.transform.localPosition;
        local_position.y = (upper_limit - lower_limit) * percent + lower_limit;
        cursor.transform.localPosition = local_position;
      }

      SetRenderers(incIndicator, false);
      SetRenderers(decIndicator, false);

      previous_percent_ = percent;
    }
  }

  public override void Awake()
  {
    base.Awake();
    cursor_boundaries_.GetLimits(cursor, gameObject);
    SetBloomGain(incIndicator, 1.0f);
    SetBloomGain(decIndicator, 1.0f);
  }

  void LateUpdate()
  {
    UpdateCursors(content.GetPercent());
  }
}
