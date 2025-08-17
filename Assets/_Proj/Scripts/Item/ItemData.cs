using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Item/Item Data")]
public class ItemData : ScriptableObject
{
  public string itemName;
  public string description;
  public ItemType type;

  public Sprite icon;
  public GameObject fxPrefab; // 아이템 사용 시 나오는 효과 (아이템 효과 X)
  // 미사일의 경우, 폭발 시 나오는 효과

  public float duration; // Maintain duration
  public float power; // Booster <- Add Speed, Missile <- Add Damage

  public AudioClip useSound;
}
