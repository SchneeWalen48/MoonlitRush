using System;
using System.Collections;
using UnityEngine;
using UnityEngine.WSA;

public class BoosterItem : MonoBehaviour
{
  private bool isBoost;

  public void Activate(ItemData data)
  {
    if (isBoost) return;
    StartCoroutine(BoostCoroutine(data.duration, data.power, data.fxPrefab));
  }

  IEnumerator BoostCoroutine(float duration, float power, GameObject fx)
  {
    isBoost = true;

    var controller = GetComponent<CarController>();
    if (controller != null)
      //controller.ApplyBoost(power);

    if (fx != null)
      Instantiate(fx, transform.position, Quaternion.identity, transform);

    yield return new WaitForSeconds(duration);

    if (controller != null)
      //controller.ResetSpeed();

    isBoost = false;
  }
}