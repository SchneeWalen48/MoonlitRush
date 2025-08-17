using System.Reflection;
using Unity.Android.Types;
using UnityEngine;

public class MissileProj : MonoBehaviour
{
  private Rigidbody rb;
  private GameObject me;
  private Transform target;

  public float speed;
  public float lifeTime;
  public float detectRadius = 30f;
  public GameObject explosionFx;

  public void Init(float power, float duration, GameObject shooter, GameObject fxPrefab)
  {
    rb = GetComponent<Rigidbody>();
    speed = power; // ItemData에서 덮어씀
    me = shooter; // ItemData에서 덮어씀
    lifeTime = duration;
    explosionFx = fxPrefab;

    Collider myCol = GetComponent<Collider>();
    Collider shooterCol = shooter.GetComponent<Collider>();

    if(myCol != null && shooterCol != null)
      Physics.IgnoreCollision(myCol, shooterCol);
    
    if(rb!= null)
      rb.velocity = transform.forward * speed;

    Destroy(gameObject, lifeTime);
  }

  void FixedUpdate()
  {
    if (rb == null) return;

    // 가장 가까운 ai 탐색
    if (target == null)
    {
      GameObject[] aiPlayers = GameObject.FindGameObjectsWithTag("AIPlayer");
      float minDist = float.MaxValue;

      foreach (var hit in aiPlayers)
      {
        float dist = Vector3.Distance(transform.position, hit.transform.position);
        if (dist < detectRadius && dist < minDist)
        {
          target = hit.transform;
          minDist = dist;
        }
      }
    }

    // 타깃 발견 시 추적
    if (target != null)
    {
      Vector3 dir = (target.position - transform.position).normalized;
      rb.velocity = dir * speed;
      transform.forward = rb.velocity;
    }
    // 타겟 없으면 직선
    else
    {
      rb.velocity = transform.forward * speed;
    }
  }

  void OnCollisionEnter(Collision collision)
  {
    print("충돌 " + collision.gameObject.name);
    if (collision.gameObject == me) return;

    if(explosionFx != null)
    {
      print("explosion fx");
      GameObject fx = Instantiate(explosionFx, transform.position, Quaternion.identity);
      Destroy(fx, 2f);
    }
    else
    {
      print("explosion fx is null");
    }

      Destroy(gameObject);
  }
}
