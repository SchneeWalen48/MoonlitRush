using UnityEngine;
using UnityEngine.UI;

public class SlotUI : MonoBehaviour
{
  public Image icon;

  public void SetItem(ItemData data)
  {
    if(data != null && data.icon != null)
    {
      icon.sprite = data.icon;
      icon.enabled = true;
    }
    else
    {
      icon.sprite = null; // 아이콘 초기화
      icon.enabled = false;
    }
  }
}
