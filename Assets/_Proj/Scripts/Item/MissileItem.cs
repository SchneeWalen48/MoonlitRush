using UnityEngine;

public class MissileItem : MonoBehaviour
{
  public Transform firePoint;
  public GameObject missilePrefab;

  public void Activate(ItemData data)
  {
    if (missilePrefab == null) return;
    Debug.Log("Activate 호출됨 by " + gameObject.name + "at" + Time.time);
    
    if (data.fxPrefab == null)
    {
      Debug.LogError("fxPrefab비어있음");
      return;
    }

    GameObject missile = Instantiate(missilePrefab, firePoint.position, firePoint.rotation);
    Debug.Log(missile);
    Rigidbody rb = missile.GetComponent<Rigidbody>();
    Debug.Log("rigidbody" + (rb != null));

    MissileProj proj = missile.GetComponent<MissileProj>();
    Debug.Log("Proj?" + (proj!=null));
    if(proj != null)
    {
      proj.Init(data.power, data.duration, gameObject);
    }
  }
}
