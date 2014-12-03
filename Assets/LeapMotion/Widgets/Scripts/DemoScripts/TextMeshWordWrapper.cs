using UnityEngine;
using System.Collections;

[RequireComponent(typeof(TextMesh))]
public class TextMeshWordWrapper : MonoBehaviour 
{
  public TextAsset textAsset;
  public float maxWidth;

  TextMesh text_mesh_;
	
	void Awake () {
    if (textAsset == null)
      return;

    string text = textAsset.text;
    text = text.Replace("\\n", System.Environment.NewLine);

    textAsset = null; // Required otherwise the clone will instantiate other clones
    GameObject clone = Instantiate(gameObject) as GameObject;
    TextMesh clone_text_mesh = clone.GetComponent<TextMesh>();

    string[] parts = text.Split(' ');
    text = "";
    string line = "";
    for (int i = 0; i < parts.Length; ++i)
    {
      clone_text_mesh.text = line + parts[i];
      if (clone_text_mesh.renderer.bounds.extents.x > maxWidth)
      {
        text += line.TrimEnd() + System.Environment.NewLine;
        line = "";
      }
      line += parts[i] + " ";
    }
    text += line.TrimEnd();

    text_mesh_ = GetComponent<TextMesh>();
    text_mesh_.text = text;

    Destroy(clone);
	}
}
