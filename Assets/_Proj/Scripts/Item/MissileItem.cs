using UnityEngine;

public class MissileItem : MonoBehaviour
{
  public Transform shotPoint;
  public GameObject missilePrefab; // Rigidbody + MissileProj 컴포넌트 가진 오브젝트

  public void Activate(ItemData data)
  {
    if (missilePrefab == null)
    {
      print("미사일 할당 안 됨");
      return;
    }
    
    if (data.fxPrefab == null)
    {
      Debug.LogError("fxPrefab비어있음");
      return;
    }

    GameObject missile = Instantiate(missilePrefab, shotPoint.position, shotPoint.rotation);
    missile.transform.localRotation = missilePrefab.transform.localRotation;
    Debug.Log(missile);
    TrailRenderer trail = missile.GetComponentInChildren<TrailRenderer>();
    if (trail != null) { trail.Clear(); }

    MissileProj proj = missile.GetComponent<MissileProj>();
    Debug.Log("Proj?" + (proj!=null));
    if(proj != null)
    {
      proj.Init(data.power, data.duration, gameObject, data.fxPrefab);
    }
  }
}
