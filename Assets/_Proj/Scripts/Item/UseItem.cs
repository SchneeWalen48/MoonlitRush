using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UseItem : MonoBehaviour
{
  public ItemData currItem;
  public SlotUI slotUI;

  void Start()
  {
    slotUI.SetItem(currItem);
  }
  public void Use()
  {
    if (currItem == null) return;

    switch (currItem.type)
    {
      case ItemType.Booster:
        GetComponent<BoosterItem>()?.Activate(currItem);
        break;
      //case ItemType.Missile:
      //  GetComponent<MissileItem>()?.Activate(currItem);
      //  break;
      case ItemType.Shield:
        GetComponent<ShieldItem>()?.Activate(currItem);
        break;
    }

    if (currItem.useSound != null)
      AudioSource.PlayClipAtPoint(currItem.useSound, transform.position);
    slotUI.SetItem(null); // 사용 후 슬롯 비우기
  }
}
