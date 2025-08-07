using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSlot : MonoBehaviour
{
  public SlotUI[] slotUIs;
  public UseItem useItem;

  private ItemData[] itemSlots = new ItemData[2];

  public void AddItem(ItemData newItem)
  {
    if (itemSlots[0] == null)
    {
      itemSlots[0] = newItem;
    }
    else if (itemSlots[1] == null)
    {
      itemSlots[1] = newItem;
    }
    else
    {
      Debug.Log("Slots are full");
      return;
    }
    UpdateUI();
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
    for(int i = 0; i < slotUIs.Length; i++)
    {
      if(i < slotUIs.Length)
        slotUIs[i].SetItem(itemSlots[i]);
    }
  }
}
