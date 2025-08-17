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

        //플레이어용
    //var controller = GetComponent<CarController>();
    //if (controller != null && controller.boostApplyer != null)
    //{
    //  controller.boostApplyer.ApplyBoost(duration, 1.1f, power);

    //  Rigidbody rb = controller.GetComponent<Rigidbody>();
    //  Vector3 lv = controller.transform.InverseTransformDirection(rb.velocity);
    //  lv.z = Mathf.Max(lv.z, 25f);
    //  rb.velocity = controller.transform.TransformDirection(lv);
    //}

        // AI용 AICarController 체크
        var aiController = GetComponent<AICarController>();
        if (aiController != null && aiController.boostApplyer != null)
        {
            aiController.boostApplyer.ApplyBoost(duration, 1.1f, power);

            Rigidbody rb = aiController.GetComponent<Rigidbody>();
            Vector3 lv = aiController.transform.InverseTransformDirection(rb.velocity);
            lv.z = Mathf.Max(lv.z, 25f);
            rb.velocity = aiController.transform.TransformDirection(lv);
        }


        if (fx != null)
      Instantiate(fx, transform.position, Quaternion.identity, transform);

    yield return new WaitForSeconds(duration);

    isBoost = false;
  }
}