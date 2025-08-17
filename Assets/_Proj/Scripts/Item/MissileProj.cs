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
  private GameObject explosionFx;
  [SerializeField] private bool debugTestMode = false; // 씬 둔 테스트용이면 체크

  void Start()
  {
    rb = GetComponent<Rigidbody>();

    if (debugTestMode)
    {
      // 제자리 고정, Init() 안 불려도 충돌 살아있음
      if (rb != null)
      {
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
      }
      me = null; // 자기 자신 판정 없앰
    }
  }
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
      Vector3 targetPos = target.transform.position + Vector3.up * 2f;
      Vector3 dir = (targetPos - transform.position).normalized;
      rb.velocity = dir * speed;
    }
    // 타겟 없으면 직선
    else
    {
      rb.velocity = transform.forward * speed;
    }

    if(rb.velocity.sqrMagnitude > 0.1f)
    {
      transform.rotation = Quaternion.LookRotation(rb.velocity.normalized, Vector3.up);
    }
  }

  void OnCollisionEnter(Collision collision)
  {
    print("충돌 " + collision.gameObject.name);

    // 미사일 맞았을 때
    var car = collision.gameObject.GetComponent<CarController>();
    //var aiCar = collision.gameObject.GetComponent<AICarController>();

    if (collision.gameObject == me) return;

    if (car != null)
    {
      // CarController에 있는 충돌 효과 발동
      car.StartCoroutine(car.HitByMissileCoroutine());
    }

    //if(aiCar != null)
    //{
    //  aiCar.StartCoroutine(aiCar.HitByMissileCoroutine());
    //}

    if (explosionFx != null)
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
