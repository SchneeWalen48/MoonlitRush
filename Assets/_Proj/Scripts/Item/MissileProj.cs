using System.Reflection;
using Unity.Android.Types;
using UnityEngine;

public class MissileProj : MonoBehaviour
{
  private Rigidbody rb;
  private GameObject me;
  private CarController target;
  public float speed;
  public float lifeTime;
  public float detectRadius = 30f;

  public GameObject explosionFx;

  public void Init(float power, float duration, GameObject shooter)
  {
    rb = GetComponent<Rigidbody>();
    speed = power; // ItemData에서 덮어씀
    me = shooter; // ItemData에서 덮어씀
    lifeTime = duration;
    if (rb != null) rb.velocity = transform.forward * speed;

    Collider myCol = GetComponent<Collider>();
    Collider shooterCol = shooter.GetComponent<Collider>();

    if(myCol != null && shooterCol != null)
    {
      Physics.IgnoreCollision(myCol, shooterCol);
    }
    Destroy(gameObject, lifeTime);
  }

  void FixedUpdate()
  {
    if (rb == null) return;

    if(target == null)
    {
      Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius);
      foreach(var hit in hits)
      {
        CarController car = hit.GetComponent<CarController>();
        if(car != null && hit.gameObject != me)
        {
          target = car;
          break;
        }
      }
    }

    if (target != null)
    {
      Vector3 dir = (target.transform.position - transform.position).normalized;
      rb.velocity = dir * speed;
      transform.forward = rb.velocity;
    }
    else
    {
      rb.velocity = transform.forward * speed;
    }
  }

  void OnCollisionEnter(Collision collision)
  {
    CarController car = collision.gameObject.GetComponent<CarController>();
    if (car != null)
    {

      car.StartCoroutine(car.HitByMissileCoroutine());
    }
    if(explosionFx != null)
    {
      GameObject fx = Instantiate(explosionFx, transform.position, Quaternion.identity);
      Destroy(fx, 2f);
    }

    Destroy(gameObject);

  }
}
