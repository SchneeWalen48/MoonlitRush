using UnityEngine;

public class BoosterItem : MonoBehaviour
{
  [SerializeField] private BoostApplyer boostApplyer;

  public bool isPickup = false;
  public ItemData pickupData;
  public string playerTag = "Player";
  
  //Player가 아이템 사용 시 호출
  public void Activate(ItemData data)
  {
    if (isPickup || data == null) return;
    if (boostApplyer == null)
    {
      boostApplyer = GetComponent<BoostApplyer>();
    }

    if (boostApplyer != null)
    {
      float capMul = Mathf.Max(1f, data.power);
      float accelMul = Mathf.Max(1f, data.power);

      boostApplyer.ApplyBoost(
        duration: data.duration,
        sizeMul: 1f,
        speedMul: data.power,
        capMul: capMul,
        accelMul: accelMul);
    }
    if (data.fxPrefab != null) {
      Instantiate(data.fxPrefab, transform.position, Quaternion.identity, transform);
    }
  }

  // 플레이어가 아이템
  void OnTriggerEnter(Collider other)
  {
    
  }
}