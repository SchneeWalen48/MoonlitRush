using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemCollector : MonoBehaviour
{
  public ItemSlot itemSlot;

  [Tooltip("PickupItem에 itemData없을때 사용하는 Scriptable Object")]
  public ItemData defaultBooster;
  public ItemData defaultShield;

  void OnTriggerEnter(Collider other)
  {
    if (!other.CompareTag("ItemBooster") || !other.CompareTag("ItemShield") || !other.CompareTag("ItemMissile")) return;

    var pick = other.GetComponent<PickupItem>();
    ItemData data = pick != null && pick.itemData != null ? pick.itemData : (other.CompareTag("ItemBooster") ? defaultBooster : defaultShield);

    if(data == null || itemSlot == null) return;

    if(!itemSlot.AddItem(data)) return;

    Vector3 pos = other.transform.position;
    Quaternion rot = other.transform.rotation;

    if(pick != null && pick.itemboxPrefab)
    {
      Destroy(other.gameObject);
      StartCoroutine(BoxRespawnCoroutine(pick.itemboxPrefab, pos, rot, pick.respawnDelay, other.tag));
    }
    else
    {
      StartCoroutine(DisEnableCoroutine(other.gameObject, 3f));
    }
  }

  IEnumerator BoxRespawnCoroutine(GameObject prefab, Vector3 pos, Quaternion rot, float delay, string tagName)
  {
    yield return new WaitForSeconds(delay);
    var go = Object.Instantiate(prefab, pos, rot);
    go.tag = tagName;
  }

  IEnumerator DisEnableCoroutine(GameObject go, float delay)
  {
    go.SetActive(false);
    yield return new WaitForSeconds(delay);
    go.SetActive(true);
  }
}
