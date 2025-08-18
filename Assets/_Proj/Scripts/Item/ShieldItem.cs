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
    if(fx != null)
    {
      fxPrefab = Instantiate(fx, transform.position, Quaternion.identity, transform);
    }

    //TODO : 무적 처리

    yield return new WaitForSeconds(duration);

    if (fx != null)
    {
      Destroy(fxPrefab);
    }

    isShield = false;
  }
}
