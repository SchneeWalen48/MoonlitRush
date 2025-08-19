using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class CarController : MonoBehaviour
{
  public float currSpeed;
  #region References
  [Header("References")]
  [SerializeField] private Rigidbody rb;
  [SerializeField] private Transform[] rayPoints;
  [SerializeField] private LayerMask drivable;
  [SerializeField] private Transform accelPoint;
  [SerializeField] private GameObject[] tires = new GameObject[4];
  [SerializeField] private GameObject[] frontTireParents = new GameObject[2];
  [SerializeField] private TrailRenderer[] skidMarks = new TrailRenderer[2];
  [SerializeField] private ParticleSystem[] skidFxs = new ParticleSystem[2];
  [SerializeField] private AudioSource engineSound, skidSound;
  public BoostApplyer boostApplyer;
  #endregion

  #region Suspension
  [Header("Suspension Settings")]
  [SerializeField] private float springStiffness = 30000f;
  [SerializeField] private float damperStiffness = 3000f;
  [SerializeField] private float restLen = 1f;
  [SerializeField] private float springTravel = 0.5f;
  [SerializeField] private float wheelRadius = 0.33f;
  #endregion
  
  int[] wheelIsGrounded = new int[4];
  bool isGrounded = false;
  public bool isInvincible { get; set; }

  [Header("Reverse")]
  [SerializeField] private float reverseMaxSpeed = 5f;
  [SerializeField] private float reverseAccel = 1f;

  [Header("Car Settings")]
  public float acceleration = 25f;
  [SerializeField] private float maxSpeed = 100f;
  [SerializeField] private float deceleration = 10f;
  [SerializeField] private float steerForce = 15f;
  [SerializeField] private AnimationCurve turningCurve;
  [SerializeField] private float dragCoefficient;

  #region Gear Settings
  [Header("Gear")]
  [SerializeField, Range(1, 5)] private int maxGears = 5;
  [SerializeField] private float[] gearsPercents = new float[] { 0.18f, 0.36f, 0.56f, 0.78f, 1 };
  //[SerializeField] private float[] gearAccelMultipliers = new float[] { 1.8f, 1.5f, 1.25f, 1f, 0.8f };
  [SerializeField] private float holdTopSpeed = 1f; // 자동 변속 전 기어 별 최고 속도에서 유지하는 시간(s)
  [SerializeField, Range(0.01f, 0.2f)] private float dropBeforeShiftPercent = 0.1f; // 변속 전 기어 별 최고 속도에서 잠깐 속도 줄이는 비율(%)[실제 기어 변속 하듯이 <- 수동 변속기 클러치 떼는 순간 속도 살짝 줄어드는 느낌]

  private int currGear = 1; // 현재 기어 단
  private bool isHoldingTop = false; // 기어 최고 속도에서 속도 유지했는지
  private float holdTimer = 0f;
  private bool didDropBeforeShift = false; // 변속 전 속도 떨어뜨렸는지
  #endregion

  #region Drfit Settings
  [Header("Drift")]
  [SerializeField] private float driftDragMultiplier = 2f;
  [SerializeField] private float driftTransitionSpeed = 5f;

  private Vector3 currCarLocalVel = Vector3.zero;
  private float carVelRatio = 0;
  private float currDragCoefficient;

  bool readyToReverse = false;
  float moveInput = 0;
  float steerInput = 0;
  bool isDrifting = false;
  #endregion

  #region Airbourne
  [Header("Airbourne Settings")]
  [SerializeField, Range(0, 1)] private float airGravity = 0.4f;
  [SerializeField] private float airGravityDuration = 0.3f; // 결국 이것이 airTimer
  [SerializeField] private float lvTorqueStrength = 8f;
  [SerializeField] private float lvTorqueDamping = 0.6f;
  [SerializeField] private float maxLvTorque = 200f;

  private float airTimer = 0f;
  private bool isAir = true;
  #endregion

  #region Barrel Roll
  

  #endregion

  [Header("Weight Feel (Minimal)")]
  [SerializeField] private float baseDownforce = 300f;
  [SerializeField] private float downforcePerMS = 0.6f; // 속도(m/s)당 추가 눌림
  [SerializeField] private float maxDownforce = 1000f;  // 과접지 방지 캡

  [Header("Visuals")]
  [SerializeField] private float tireRotSpeed = 3000f;
  [SerializeField] private float maxSteeringAngle = 30f;
  [SerializeField] private float minSideSkidVel = 10f;

  [Header("Audio")]
  [SerializeField]
  [Range(0, 1)] private float minPitch = 1f;
  [SerializeField]
  [Range(1, 5)] private float maxPitch = 5f;

  bool isBarrelRolling = false;
  public float barrelRollTorque = 100f;
  public float barrelRollDuration = 1.5f;
  void Awake()
  {
    rb = GetComponent<Rigidbody>();
  }

  void Update()
  {
    GetPlayerInput();
    currSpeed = rb.velocity.magnitude;
  }

  void FixedUpdate()
  {
    Suspension();
    GroundCheck();

    if (!isGrounded && isAir)
    {
      airTimer = airGravityDuration;
    }
    isAir = isGrounded;
 
    CalculateCarVelocity(); 
    ApplyDownforce();
    Movement();
    Visuals();
    ApplyGearHoldAndCap();
    GearLogic();
    ApplyReverseSpeed();
    Airbourne();
    //EngineSound();
  }

  #region Movement
  void Movement()
  {
    if (isGrounded)
    {
      if (Mathf.Abs(moveInput) > 0.01f)
      {
        Acceleration();
        readyToReverse = false;
      }
      else if (moveInput < -0.01f)
      {
        if (currCarLocalVel.z > 0.1f) { Deceleration(); }
        else { Acceleration(); readyToReverse = true; }
      }
      else
      {
        if(!isHoldingTop)
        Deceleration();
      }

      Turn();
      SidewaysDrag();
    }
  }

  void Acceleration()
  {
    float accelPower = (moveInput >= 0f) ? acceleration : reverseAccel;

    Vector3 force = accelPower * Mathf.Abs(moveInput) * Mathf.Sign(moveInput) * transform.forward;
    // 후륜 구동 : 뒷바퀴 1 (index 2)
    if (tires.Length > 2)
    {
      rb.AddForceAtPosition(acceleration * moveInput * transform.forward, tires[2].transform.position, ForceMode.Acceleration);
    }

    // 뒷바퀴 2 (index 3)
    if (tires.Length > 3)
    {
      rb.AddForceAtPosition(acceleration * moveInput * transform.forward, tires[3].transform.position, ForceMode.Acceleration);
    }
  }

  void Deceleration()
  {
    Vector3 lv = transform.InverseTransformDirection(rb.velocity);
    float dir = Mathf.Sign(lv.z);
    Vector3 brake = -dir * deceleration * transform.forward;
    rb.AddForceAtPosition(brake, accelPoint.position, ForceMode.Acceleration);
  }

  void ApplyReverseSpeed()
  {
    if (currCarLocalVel.z < 0)
    {
      if (Mathf.Abs(currCarLocalVel.z) > reverseMaxSpeed)
      {
        Vector3 lv = transform.InverseTransformDirection(rb.velocity);
        lv.z = -reverseMaxSpeed;
        rb.velocity = transform.TransformDirection(lv);
      }
    }
  }

  void Turn()
  {
    float currSteerStrength = isDrifting ? steerForce * 1.5f : steerForce;
    rb.AddTorque(currSteerStrength * steerInput * turningCurve.Evaluate(Mathf.Abs(carVelRatio)) * Mathf.Sign(currCarLocalVel.z) * transform.up, ForceMode.Acceleration);
  }

  void SidewaysDrag()
  {
    float currSidewaysSpeed = currCarLocalVel.x;

    float targetDrag = isDrifting ? dragCoefficient / driftDragMultiplier : dragCoefficient;
    currDragCoefficient = Mathf.Lerp(currDragCoefficient, targetDrag, Time.deltaTime * driftTransitionSpeed);

    float dragMagnitude = -currSidewaysSpeed * currDragCoefficient;
    Vector3 dragForce = transform.right * dragMagnitude;
    rb.AddForceAtPosition(dragForce, rb.worldCenterOfMass, ForceMode.Acceleration);
  }
  void ApplyDownforce()
  {
    if (!isGrounded) return; // 공중에선 X
    float v = rb.velocity.magnitude;  // m/s
    float down = Mathf.Min(baseDownforce + downforcePerMS * v, maxDownforce);
    rb.AddForce(-transform.up * down, ForceMode.Force);
  }
  #endregion

  #region Airbourne
  void Airbourne()
  {
    if (airTimer > 0f)
    {
      Vector3 deltaF = rb.mass * (airGravity - 1f) * Physics.gravity;
      rb.AddForce(deltaF, ForceMode.Force);
      airTimer -= Time.fixedDeltaTime;
    }

    Vector3 up = transform.up;
    Vector3 toUpAxis = Vector3.Cross(up, Vector3.up);
    float sinAngle = toUpAxis.magnitude;
    if (sinAngle > 1e-4f)
    {
      Vector3 torqueDir = toUpAxis.normalized;
      float angle = Mathf.Asin(Mathf.Clamp(sinAngle, -1f, 1f));

      Vector3 corrective = torqueDir * (lvTorqueStrength * angle) - rb.angularVelocity * lvTorqueDamping;

      corrective = Vector3.ClampMagnitude(corrective, maxLvTorque);

      rb.AddTorque(corrective, ForceMode.Acceleration);
    }
  }
  #endregion
 
  #region Visuals
  void Visuals()
  {
    TireVisuals();
    VFX();
  }
  void TireVisuals()
  {
    float steeringAngle = maxSteeringAngle * steerInput;
    for (int i = 0; i < tires.Length; i++)
    {
      if (i < 2)
      {
        tires[i].transform.Rotate(Vector3.right, tireRotSpeed * carVelRatio * Time.deltaTime, Space.Self);

        frontTireParents[i].transform.localEulerAngles = new Vector3(frontTireParents[i].transform.localEulerAngles.x, steeringAngle, frontTireParents[i].transform.localEulerAngles.z);
      }
      else
      {
        tires[i].transform.Rotate(Vector3.right, tireRotSpeed * carVelRatio * Time.deltaTime, Space.Self);
      }
    }
  }

  void VFX()
  {
    bool allowFwdFx = (currCarLocalVel.z > 0.1f || moveInput > 0.01f) && currCarLocalVel.z >= 0f;

    bool doSkid = (isGrounded && allowFwdFx) && (isDrifting || Mathf.Abs(currCarLocalVel.x) > minSideSkidVel);

    ToggleSkidMarks(doSkid);
    ToggleSkidSmokes(doSkid);

    if(boostApplyer != null && boostApplyer.fx != null)
    {
      boostApplyer.fx.SetEmission(allowFwdFx);
    }
  }

  void ToggleSkidMarks(bool toggle)
  {
    foreach (var skidMark in skidMarks)
    {
      skidMark.emitting = toggle;
    }
  }

  void ToggleSkidSmokes(bool toggle)
  {
    foreach (var smoke in skidFxs)
    {
      if (toggle)
      {
        smoke.Play();
      }
      else
      {
        smoke.Stop();
      }
    }
  }
  void SetTirePosition(GameObject tire, Vector3 targetPos)
  {
    tire.transform.position = targetPos;
  }
  #endregion

  #region Audio
  //void EngineSound()
  //{
  //  engineSound.pitch = Mathf.Lerp(minPitch, maxPitch, Mathf.Abs(carVelRatio));
  //}
  //void ToggleSkidSound(bool toggle)
  //{
  //  skidSound.mute = !toggle;
  //}
  #endregion


  #region Car Status Check
  void GroundCheck()
  {
    int tempGroundedWheels = 0;

    for (int i = 0; i < wheelIsGrounded.Length; i++)
    {
      tempGroundedWheels += wheelIsGrounded[i];
    }
    if (tempGroundedWheels > 1)
    {
      isGrounded = true;
    }
    else
    {
      isGrounded = false;
    }
    print(tempGroundedWheels);
  }

  void CalculateCarVelocity()
  {
    currCarLocalVel = transform.InverseTransformDirection(rb.velocity);
    carVelRatio = currCarLocalVel.z / maxSpeed;
  }
  #endregion

  void GetPlayerInput()
  {
    float rawInput = Input.GetAxis("Vertical");
    steerInput = Input.GetAxis("Horizontal");

    isDrifting = Mathf.Abs(currCarLocalVel.z) > 1f && Mathf.Abs(steerInput) > 0.1f && Input.GetKey(KeyCode.LeftShift);

    if (rawInput < -0.01f && Mathf.Abs(currCarLocalVel.z) < 0.1f && !readyToReverse)
    {
      readyToReverse = true;
      moveInput = 0;
    }
    else
    {
      if (rawInput > 0.01f)
      {
        readyToReverse = false;
      }
      moveInput = rawInput;
    }
  }

  #region Gear Shift
  void GearLogic()
  {
    // 후진하거나 정지할때 기어 변속 중지
    if (currCarLocalVel.z <= 0)
    {
      currGear = 1; // 기어는 항상 1단으로 유지
      isHoldingTop = false;
      holdTimer = 0f;
      didDropBeforeShift = false;
      return;
    }
    int max = Mathf.Clamp(maxGears, 1, 5); // 5단 기어
    if (gearsPercents.Length != max) Array.Resize(ref gearsPercents, max);
    gearsPercents[max - 1] = 1f;

    float fwdSpeed = Mathf.Max(0f, currCarLocalVel.z);
    float speedRatio = Mathf.Clamp01(fwdSpeed / Mathf.Max(0.01f, maxSpeed));

    float currTop = gearsPercents[Mathf.Clamp(currGear - 1, 0, max - 1)];

    if (!isHoldingTop)
    {
      if (speedRatio >= currTop && currGear < max)
      {
        isHoldingTop = true;
        holdTimer = holdTopSpeed;
        didDropBeforeShift = false;
      }
    }
    else
    {
      holdTimer -= Time.deltaTime;
      if (holdTimer <= 0f)
      {
        if (!didDropBeforeShift)
        {
          DropForwardSpeedByPercent(dropBeforeShiftPercent);
          didDropBeforeShift = true;
        }
        currGear = Mathf.Min(currGear + 1, max);
        isHoldingTop = false;
      }
    }
  }

  void DropForwardSpeedByPercent(float percent)
  {
    percent = Mathf.Clamp01(percent);
    Vector3 lv = transform.InverseTransformDirection(rb.velocity);
    if (lv.z > 0f)
    {
      lv.z *= (1f - percent);
      rb.velocity = transform.TransformDirection(lv);
    }
  }

  void ApplyGearHoldAndCap()
  {
    int max = Mathf.Clamp(maxGears, 1, 5);
    float currTop = gearsPercents[Mathf.Clamp(currGear - 1, 0, max - 1)];
    float gearTopSpeed = maxSpeed * currTop;

    Vector3 lv = transform.InverseTransformDirection(rb.velocity);
    if (lv.z > gearTopSpeed)
    {
      lv.z = gearTopSpeed;
      rb.velocity = transform.TransformDirection(lv);
    }
  }
  #endregion

  #region Suspension
  void Suspension()
  {

    for (int i = 0; i < rayPoints.Length; i++)
    {
      RaycastHit hit;
      float maxDistance = restLen;

      if (Physics.Raycast(rayPoints[i].position, -rayPoints[i].up, out hit, maxDistance + wheelRadius, drivable))
      {
        wheelIsGrounded[i] = 1;

        float currSpringLen = hit.distance - wheelRadius;
        float springCompression = (restLen - currSpringLen) / springTravel;

        float springVel = Vector3.Dot(rb.GetPointVelocity(rayPoints[i].position), rayPoints[i].up);
        float dampForce = damperStiffness * springVel;

        float springForce = springStiffness * springCompression;

        float netForce = springForce - dampForce;

        rb.AddForceAtPosition(netForce * rayPoints[i].up, rayPoints[i].position);

        SetTirePosition(tires[i], hit.point + rayPoints[i].up * wheelRadius / 2);
        Debug.DrawLine(rayPoints[i].position, hit.point, Color.red);
      }
      else
      {
        wheelIsGrounded[i] = 0;

        SetTirePosition(tires[i], rayPoints[i].position - rayPoints[i].up * (restLen + springTravel) * 0.9f);
        Debug.DrawLine(rayPoints[i].position, rayPoints[i].position + (wheelRadius + maxDistance) * -rayPoints[i].up, Color.green);
      }
    }
  }
  #endregion

  public IEnumerator HitByMissileCoroutine()
  {
    if (isInvincible) yield break;

    rb.velocity *= 0.3f;

    rb.AddForce(Vector3.up * 8f, ForceMode.VelocityChange);

    float originDrag = rb.drag;
    rb.drag = 0.5f;

    yield return new WaitForSeconds(2f); // 2초간 공중 상태

    rb.drag = originDrag;
  }
  #region Trigger
  void OnTriggerEnter(Collider other)
  {
    Vector3 lv = transform.InverseTransformDirection(rb.velocity);
    if (lv.z <= 0.01f)
      return;
    currGear = 5;
    
    if (currCarLocalVel.z > 0.1f)
    {
      if (other.CompareTag("SpeedUp"))
      {
        Debug.Log($"감지 : {other.tag}");
        if (boostApplyer != null)
        {
          boostApplyer.ApplyBoost(2f, 1.1f, 1.5f); // 시간, 크기, 속도
        };
        lv.z = Mathf.Max(lv.z, 27f); // 부스터 목표 속도 (m/s)
        rb.velocity = transform.TransformDirection(lv);
      }

      if (other.CompareTag("Barrel"))
      {
        Debug.Log($"감지 : {other.tag}");
        if (!isBarrelRolling)
          StartCoroutine(BarrelRollCoroutine());
      }

      if (other.CompareTag("BoostPad"))
      {
        Debug.Log($"감지 : {other.tag}");
        if(boostApplyer != null)
        {
          boostApplyer.ApplyBoost(2f, 1.1f, 2f);
          lv.z = (Mathf.Max(lv.z, 20f));
          rb.velocity = (transform.TransformDirection(lv));
        }
      }
    }
  }
  #endregion

  #region Barrel Roll Coroutine
  IEnumerator BarrelRollCoroutine()
  {
    Vector3 lv = transform.InverseTransformDirection(rb.velocity);
    if (boostApplyer != null)
      boostApplyer.ApplyBoost(3, 1.1f, 2f);
    lv.z = Mathf.Max(lv.z, 30f);
    rb.velocity = transform.TransformDirection(lv);

    yield return new WaitForSeconds(0.4f);
    yield return new WaitUntil(() => !isGrounded);


    isBarrelRolling = true;
    float timer = barrelRollDuration;
    float rollDir = Mathf.Sign(steerInput);

    while (timer > 0)
    {
      rb.AddTorque(transform.forward * barrelRollTorque, ForceMode.Acceleration);
      timer -= Time.deltaTime;
      yield return null;
    }

    isBarrelRolling = false;

    moveInput = 1f;
  }
  #endregion
}