using UnityEngine;

public class UseItem : MonoBehaviour
{
  public ItemData currItem;
  public SlotUI slotUI;
  [SerializeField] private BoosterItem booster; // 플레이어에 붙은 BoosterItem 참조
  [SerializeField] private ShieldItem shieldItem;
  //[SerializeField] private MissileItem missileItme;

  void Start()
  {
    slotUI.SetItem(currItem);
  }
  public bool Use()
  {
    if (currItem == null) return false;

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

      default:
        return false;
    }

    if (currItem.useSound != null)
      AudioSource.PlayClipAtPoint(currItem.useSound, transform.position);
    currItem = null;
    return true;
  }
}
