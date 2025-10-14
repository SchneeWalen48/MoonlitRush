using System.Collections;
using UnityEngine;

public class ItemCollector : MonoBehaviour
{
  public ItemSlot itemSlots;

  [Tooltip("PickupItem에 itemData없을때 사용하는 Scriptable Object")]
  public ItemData defaultBooster;
  public ItemData defaultShield;
  public ItemData defaultMissile;

  void OnTriggerEnter(Collider other)
  {
    //Debug.Log("Tag: " + other.tag);

    if (!other.CompareTag("ItemBooster") && !other.CompareTag("ItemShield") && !other.CompareTag("ItemMissile")) return;

    var pick = other.GetComponent<ItemBox>();
    if (pick == null) return;

    bool added = false;
    if(pick.itemData != null && itemSlots != null)
    {
        added = itemSlots.AddItem(pick.itemData);
    }

    StartCoroutine(BoxRespawnCoroutine(other.gameObject, pick));
  }

  IEnumerator BoxRespawnCoroutine(GameObject box, ItemBox pick)
  {
    Vector3 pos = box.transform.position;
    Quaternion rot = box.transform.rotation;

    box.SetActive(false);

    yield return new WaitForSeconds(pick.respawnDelay);

    box.transform.position = pos;
    box.transform.rotation = rot;
    pick.ResetVisual();
    box.SetActive(true);
  }
}
