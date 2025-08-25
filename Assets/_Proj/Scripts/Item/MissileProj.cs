using System.Collections.Generic;
using UnityEngine;

public enum Team { Player, AI }
public class MissileProj : MonoBehaviour
{
  private Rigidbody rb;
  private GameObject me;
  private Transform target;

  public float speed;
  public float lifeTime;
  public float detectRadius = 30f;
  private GameObject explosionFx;

  private Vector3 launchFwd = Vector3.zero;
  [SerializeField] bool inheritShootervelocity = true;
  [SerializeField] float inheritDecayPerSec = 0f;
  Vector3 inheritedVel;

  [SerializeField] private bool debugTestMode = false; // 씬에 둔 테스트용이면 체크


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
  public void Init(float power, float duration, GameObject shooter, GameObject fxPrefab, Transform fwdBasis = null)
  {
    if(rb==null) rb = GetComponent<Rigidbody>();
    if (rb != null) rb.isKinematic = false;
    rb = GetComponent<Rigidbody>();
    speed = power; // ItemData에서 덮어씀
    me = shooter; // ItemData에서 덮어씀
    lifeTime = duration;
    explosionFx = fxPrefab;

    var myCols = GetComponentsInChildren<Collider>();
    var shootCols = shooter.GetComponentsInChildren<Collider>();
    foreach (var mc in myCols)
      foreach (var sc in shootCols)
        Physics.IgnoreCollision(mc, sc);

    Transform basis = fwdBasis != null ? fwdBasis : shooter.transform;
    launchFwd = basis.TransformDirection(Vector3.forward).normalized;

    var shooterRb = shooter.GetComponentInParent<Rigidbody>();
    inheritedVel = (inheritShootervelocity && shooterRb != null) ? shooterRb.velocity : Vector3.zero;

    if(rb!= null)
      rb.velocity = inheritedVel + launchFwd * speed;

    var lookDir = (rb != null && rb.velocity.sqrMagnitude > 0.01f) ? rb.velocity : launchFwd;
    transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);

    Destroy(gameObject, lifeTime);
  }

  void FixedUpdate()
  {
    if (rb == null) return;

        //Debug.Log($"미사일 속도: {rb.velocity.magnitude}");

        Vector3 fwd = (rb.velocity.sqrMagnitude > 0.01f) ? rb.velocity.normalized 
      : (launchFwd != Vector3.zero ? launchFwd : transform.forward);

    // 가장 가까운 대상 탐색
    // 주체 Player => AI탐색
    // 주체 AI => AI + Player 탐색
    if (target == null)
    {
      List<GameObject> racers = new List<GameObject>();

      bool shooterIsPlayer = (me != null && me.CompareTag("Player"));
      bool shooterIsAI = (me != null && me.CompareTag("AIPlayer"));

      if (shooterIsPlayer)
      {
        racers.AddRange(GameObject.FindGameObjectsWithTag("AIPlayer"));
      }
      else if (shooterIsAI) {
        var allAIs = GameObject.FindGameObjectsWithTag("AIPlayer");
        foreach (var ai in allAIs)
        {
          if (me != null &&( ai == me || ai.transform.IsChildOf(me.transform))) continue; // 자신 제외
          racers.Add(ai);
        }

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null) racers.Add(playerGO);
      }
      else
      {
        racers.AddRange(GameObject.FindGameObjectsWithTag("AIPlayer"));
      }
      float minDist = float.MaxValue;

      float cosFov = Mathf.Cos(75f * Mathf.Deg2Rad);

      foreach (var racer in racers)
      {
        if(racer == null) continue;

        if (me != null && (racer == me || racer.transform.IsChildOf(me.transform))) continue;
        float dist = Vector3.Distance(transform.position, racer.transform.position);
        if (dist > detectRadius) continue;
        
        Vector3 toTarget = (racer.transform.position - transform.position).normalized;
        float dot = Vector3.Dot(fwd, toTarget);

        // 전방 75도 이내만 탐지
        if (dot > Mathf.Cos(75f * Mathf.Deg2Rad) && dist < minDist)
        {
          target = racer.transform;
          minDist = dist;
        }
      }
    }
    Vector3 baseVel = inheritedVel;

    // 타깃 발견 시 추적
    if (target != null)
    {
      Vector3 targetPos = target.position + Vector3.up;
      Vector3 dir = (targetPos - transform.position).normalized;
      rb.velocity = baseVel + dir * speed;
      transform.rotation = Quaternion.LookRotation(rb.velocity, Vector3.up);
    }
    // 타겟 없으면 직선
    else
    {
      rb.velocity = baseVel + launchFwd * speed;

      if (rb.velocity.sqrMagnitude > 0.1f)
      {
        transform.rotation = Quaternion.LookRotation(rb.velocity, Vector3.up);
      }
    }
  }

  void OnCollisionEnter(Collision collision)
  {
    if (me != null && (collision.transform == me.transform || collision.transform.IsChildOf(me.transform))) return;

    print("충돌 " + collision.gameObject.name);

    // 미사일 맞았을 때
    var car = collision.gameObject.GetComponentInParent<CarController>();
    var aiCar = collision.gameObject.GetComponentInParent<AICarController>();

    if (collision.gameObject == me) return;

    if (car != null)
    {
      // CarController에 있는 충돌 효과 발동
      car.StartCoroutine(car.HitByMissileCoroutine());
    }

    if (aiCar != null)
    {
      aiCar.StartCoroutine(aiCar.HitByMissileCoroutine());
    }

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
