using UnityEngine;
using System.Collections;

namespace VRWidgets
{
  public class Limits
  {
    public float t = float.MinValue;
    public float b = float.MaxValue;
    public float r = float.MinValue;
    public float l = float.MaxValue;

    public Limits()
    {
    }

    public void GetLimits(GameObject target, GameObject reference = null)
    {
      if (reference == null)
        reference = target;

      MeshFilter[] mesh_filters = target.GetComponentsInChildren<MeshFilter>();
      foreach (MeshFilter mesh_filter in mesh_filters)
      {
        Vector3[] vertices = mesh_filter.mesh.vertices;
        for (int i = 0; i < vertices.Length; ++i)
        {
          Vector3 verticeTransformed = reference.transform.InverseTransformPoint(mesh_filter.transform.TransformPoint(vertices[i]));
          t = Mathf.Max(verticeTransformed.y, t);
          b = Mathf.Min(verticeTransformed.y, b);
          r = Mathf.Max(verticeTransformed.x, r);
          l = Mathf.Min(verticeTransformed.x, l);
        }
      }
    }
  }
}

