using UnityEngine;

public class ItemSlot : MonoBehaviour
{
  public SlotUI[] slotUIs;
  public UseItem useItem; // 플레이어에 붙은 UseItem

  private ItemData[] itemSlots = new ItemData[2];

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
      UpdateUI();
      return true;
    }
    return false;
  }
  public void UpdateUI()
  {
    for(int i = 0; i < slotUIs.Length; i++)
    {
      ItemData data = (i < itemSlots.Length) ? itemSlots[i] : null;
      slotUIs[i].SetItem(data);
    }
  }

  public void UseFirstItem()
  {
    UseSlot(0);
  }

  public void UseSlot(int idx)
  {
    if (idx < 0 || idx > 1) return;
    if (itemSlots[idx] == null) return;
    if (useItem == null) return;

    useItem.currItem = itemSlots[idx];
    bool used = useItem.Use();
    if (!used) return;

    if (idx == 0)
    {
      itemSlots[0] = itemSlots[1];
      itemSlots[1] = null;
    }
    else
    {
      itemSlots[1] = null;
    }

    UpdateUI();
  }
}
