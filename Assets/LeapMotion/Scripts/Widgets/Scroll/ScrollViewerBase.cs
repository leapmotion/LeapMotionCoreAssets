using UnityEngine;
using System.Collections;

namespace VRWidgets
{
  public abstract class ScrollViewerBase : MonoBehaviour
  {
    public GameObject scrollWindow;
    public GameObject scrollWindowFrame;

    protected Limits boundaries_ = new Limits();

    public abstract void ScrollActive();
    public abstract void ScrollInactive();

    // Use this for initialization
    public virtual void Awake()
    {
      if (scrollWindowFrame != null)
      {
        boundaries_.GetLimits(scrollWindowFrame, gameObject);
        scrollWindow.transform.localPosition = new Vector3((boundaries_.r + boundaries_.l) / 2.0f, (boundaries_.t + boundaries_.b) / 2.0f, 0.0f);
        scrollWindow.transform.localScale = new Vector3((boundaries_.r - boundaries_.l), (boundaries_.t - boundaries_.b), 0.0f);
      }
      else
      {
        boundaries_.GetLimits(scrollWindow, gameObject);
      }
    }
  }
}

