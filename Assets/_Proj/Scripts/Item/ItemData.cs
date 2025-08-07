using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Item/Item Data")]
public class ItemData : ScriptableObject
{
  public string itemName;
  public string description;
  public ItemType type;

  public Sprite icon;
  public GameObject fxPrefab;

  public float duration; // Maintain duration
  public float power; // Booster <- Add Speed, Missile <- Add Damage

  public AudioClip useSound;
}
