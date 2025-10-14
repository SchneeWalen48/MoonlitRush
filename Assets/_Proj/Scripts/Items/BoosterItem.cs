using System.Collections;
using UnityEngine;

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
    if (controller != null && controller.boostApplyer != null)
    {
      controller.boostApplyer.ApplyBoost(duration, 1.1f, power);

      Rigidbody rb = controller.GetComponent<Rigidbody>();
      Vector3 lv = controller.transform.InverseTransformDirection(rb.velocity);
      lv.z = Mathf.Max(lv.z, 25f);
      rb.velocity = controller.transform.TransformDirection(lv);
    }

    if (fx != null)
      Instantiate(fx, transform.position, Quaternion.identity, transform);

    yield return new WaitForSeconds(duration);

    isBoost = false;
  }
}