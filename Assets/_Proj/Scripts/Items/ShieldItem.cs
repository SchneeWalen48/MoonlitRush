using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ShieldItem : MonoBehaviour
{
  private bool isShield;
  private GameObject fxPrefab;

  public void Activate(ItemData data)
  {
    if (isShield) return;

    StartCoroutine(ShieldCoroutine(data.duration, data.fxPrefab ));
  }

  IEnumerator ShieldCoroutine(float duration, GameObject fx)
  {
    isShield = true;

    var controller = GetComponent<CarController>();
    // 무적 상태 부여
    if (controller != null)
      controller.isInvincible = true;

    if(fx != null)
    {
      fxPrefab = Instantiate(fx, transform.position, Quaternion.identity, transform);
    }

    yield return new WaitForSeconds(duration);

    if(controller != null)
    {
      controller.isInvincible = false;
    }

    if (fxPrefab != null)
    {
      Destroy(fxPrefab);
    }

    isShield = false;
  }
}
