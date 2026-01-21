using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSlot : MonoBehaviour
{
  public SlotUI[] slotUis; // slot오브젝트에 SlotUI컴포넌트 붙이고 할당
  public UseItem useItem; // 플레이어에 UseItem컴포넌트 붙이고 할당

  private ItemData[] itemSlots = new ItemData[2]; // 실제 아이템 데이터 저장용 슬롯 2칸

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
    return false;
  }

  // 가장 먼저 획득한 아이템 사용 (FIFO)
  public void UseFirstItem()
  {
    if (itemSlots[0] == null) return;
    useItem.currItem = itemSlots[0];
    useItem.Use();

    // 슬롯 당김 (FIFO)
    itemSlots[0] = itemSlots[1];
    itemSlots[1] = null;

    UpdateUI();
  }

  // 슬롯 UI 동기화
  private void UpdateUI()
  {
    for(int i = 0; i < slotUis.Length; i++)
    {
      if(i < slotUis.Length)
        slotUis[i].SetItem(itemSlots[i]);
    }
  }
}
