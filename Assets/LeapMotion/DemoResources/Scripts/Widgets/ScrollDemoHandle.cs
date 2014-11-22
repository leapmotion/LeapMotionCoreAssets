using UnityEngine;
using System.Collections;
using VRWidgets;

public class ScrollDemoHandle : ScrollHandleBase 
{
  public float maxDistanceViewer = 0.1f;

  public override void Update()
  {
    base.Update();
    Vector3 content_position = content.transform.localPosition;
    content_position.z = Mathf.Min(transform.localPosition.z, maxDistanceViewer);
    content.transform.localPosition = content_position;

    Vector3 viewer_position = viewer.scrollWindow.transform.localPosition;
    viewer_position.z = Mathf.Min(transform.localPosition.z, maxDistanceViewer);
    viewer.scrollWindow.transform.localPosition = viewer_position;
  }
}
