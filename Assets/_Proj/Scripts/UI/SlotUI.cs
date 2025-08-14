using UnityEngine;
using UnityEngine.UI;

public class SlotUI : MonoBehaviour
{
  public Image icon;
  public GameObject emptyState; // 빈 슬롯 표시용

  private ItemData curr;

  public void SetItem(ItemData data)
  {
    curr = data;

    if (curr != null)
    {
      if (data != null && data.icon != null)
      {
        icon.sprite = data.icon;
        icon.enabled = true;
      }
      else
      {
        icon.enabled = false;
      }
    }

    if(emptyState != null)
    {
      emptyState.SetActive(data == null);
    }
  }

  public ItemData current => curr;
}
