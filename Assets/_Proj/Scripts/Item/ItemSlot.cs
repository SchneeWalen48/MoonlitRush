using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSlot : MonoBehaviour
{
  public SlotUI[] slotUis; // slot오브젝트에 SlotUI컴포넌트 붙이고 할당
  public UseItem useItem; // 플레이어에 UseItem컴포넌트 붙이고 할당

  private ItemData[] itemSlots = new ItemData[2];

  void Update()
  {
    if (Input.GetKeyDown(KeyCode.LeftControl))
    {
      UseFirstItem();
    }
  }

  public bool AddItem(ItemData newItem)
  {
    if (newItem == null) return false;
    if (itemSlots[0] == null)
    {
      itemSlots[0] = newItem;
      UpdateUI();
      return true;
    }
    if (itemSlots[1] == null)
    {
      itemSlots[1] = newItem;
      UpdateUI() ;
      return true;
    }
    Debug.Log("Slots are full");
    return false;
  }

  public void UseFirstItem()
  {
    if (itemSlots[0] == null) return;
    useItem.currItem = itemSlots[0];
    useItem.Use();

    itemSlots[0] = itemSlots[1];
    itemSlots[1] = null;

    UpdateUI();
  }

  private void UpdateUI()
  {
    for(int i = 0; i < slotUis.Length; i++)
    {
      if(i < slotUis.Length)
        slotUis[i].SetItem(itemSlots[i]);
    }
  }
}
