using UnityEngine;

// 아이템박스에 부착
public class ItemBox : MonoBehaviour 
{
  public ItemData itemData;
  public float respawnDelay = 3f;
  public GameObject itemboxPrefab;

  private Color initColour;
  private Vector3 initScale;

  void Awake()
  {
    initColour = GetComponent<Renderer>().material.color;
    initScale = transform.localScale;
  }

  public void ResetVisual()
  {
    var rend = GetComponent<Renderer>();
    if(rend != null) rend.material.color = initColour;
    transform.localScale = initScale;
  }
}
