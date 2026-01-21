using UnityEngine;

// 아이템박스에 부착
public class ItemBox : MonoBehaviour 
{
  public ItemData itemData;
  public float respawnDelay = 3f;
  public GameObject itemboxPrefab;
  
  private Vector3 initScale;

  void Awake()
  {
    initScale = transform.localScale;
  }

  public void ResetVisual()
  {
    var rend = GetComponent<Renderer>();
    transform.localScale = initScale;
  }
}
