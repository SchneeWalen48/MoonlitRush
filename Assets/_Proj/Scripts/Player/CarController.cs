using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class CarController : MonoBehaviour
{
  public float currSpeed;

  #region References
  [Header("References")]
  public CarStats stats;
  [SerializeField] private Rigidbody rb;
  [SerializeField] private Transform[] rayPoints;
  [SerializeField] private LayerMask drivable;
  [SerializeField] private Transform accelPoint;
  [SerializeField] private GameObject[] tires = new GameObject[4];
  [SerializeField] private GameObject[] frontTireParents = new GameObject[2];
  [SerializeField] private TrailRenderer[] skidMarks = new TrailRenderer[2];
  [SerializeField] private ParticleSystem[] skidFxs = new ParticleSystem[2];
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
  public bool isFinished;
  public bool isInvincible { get; set; }

  [Header("Reverse")]
  [SerializeField] private float reverseMaxSpeed = 5f;
  [SerializeField] private float reverseAccel = 1f;
  private float brakePow = 3f;

  [Header("Car Settings")]
  public float acceleration = 25f;
  private float maxSpeed = 100f;
  private float deceleration = 10f;
  private float steerForce = 15f;
  private AnimationCurve turningCurve;
  private AnimationCurve accelCurve;
  private float dragCoefficient = 100f;
  private float decelLerpSpeed = 7f;   // 클수록 빨리 따라감
  [Range(0, 1)] private float coastFactor = 0.6f; // 페달 off일 때 제동 비율
  private float minSpeedForFullDecel = 6f; // 저속에서는 감속 줄이기(m/s)
  private float currDecelMag = 0f; // 현재 적용 중인 감속 크기(스무딩 값)


  // 부스트 효과
  [SerializeField] private float extraDecayPerSec = 10f; // 초당 얼마나 줄일지(m/s)
  private float extraFwd = 0f; // 현재 오버레이 전진 속도(m/s)

  #region Gear Settings
  [Header("Gear")]
  [SerializeField, Range(1, 5)] private int maxGears = 5;
  [SerializeField] private float[] gearsPercents = new float[] { 0.18f, 0.36f, 0.56f, 0.78f, 1 };
  //[SerializeField] private float[] gearAccelMultipliers = new float[] { 1.8f, 1.5f, 1.25f, 1f, 0.8f };
  private float holdTopSpeed = 1f; // 자동 변속 전 기어 별 최고 속도에서 유지하는 시간(s)
  [SerializeField, Min(0)] private float dropBeforeShiftAmount = 1f; // 변속 전 기어 별 최고 속도에서 잠깐 속도 줄이는 속도(m/s)[실제 기어 변속 하듯이 <- 수동 변속기 클러치 떼는 순간 속도 살짝 줄어드는 느낌]

  private int currGear = 1; // 현재 기어 단
  private bool isHoldingTop = false; // 기어 최고 속도에서 속도 유지했는지
  private float holdTimer = 0f;
  private bool didDropBeforeShift = false; // 변속 전 속도 떨어뜨렸는지
  [SerializeField] private float downshift = 0.3f; // 이전 기어 최고속도의 30% 밑으로 떨어지면 다운시프트
  [SerializeField] private float gearBypass = 0.7f; // 부스트 시 잠깐 기어로직 중단
  private float gearBypassEndTime = 0f;
  private bool gearBypassed => Time.time < gearBypassEndTime;
  #endregion

  #region Drfit Settings
  [Header("Drift")]
  [SerializeField] private float driftDragMultiplier = 2f;
  [SerializeField] private float driftTransitionSpeed = 5f;

  private Vector3 currCarLocalVel = Vector3.zero;
  private float carVelRatio = 0;
  private float currDragCoefficient;

  bool readyToReverse = false;
  [HideInInspector] public float moveInput = 0;
  [HideInInspector] public float steerInput = 0;
  bool isDrifting = false;
  #endregion

  #region Airbourne
  [Header("Airbourne Settings")]
  private float airGravity = 0.6f;
  private float airGravityDuration = 0.3f; // 결국 이것이 airTimer
  [SerializeField] private float lvTorqueStrength = 8f;
  [SerializeField] private float lvTorqueDamping = 0.6f;
  [SerializeField] private float maxLvTorque = 200f;

  private float airTimer = 0f;
  private bool isAir = true;
  #endregion

  #region Barrel Roll
  bool isBarrelRolling = false;
  public float barrelRollTorque = 100f;
  public float barrelRollDuration = 1.5f;
  #endregion

  [Header("Weight Feel (Minimal)")]
  private float baseDownforce = 300f;
  private float downforcePerMS = 0.6f; // 속도(m/s)당 추가 눌림
  [SerializeField] private float maxDownforce = 1000f;  // 과접지 방지 캡

  [Header("Visuals")]
  [SerializeField] private float tireRotSpeed = 3000f;
  [SerializeField] private float maxSteeringAngle = 30f;
  [SerializeField] private float minSideSkidVel = 10f;

  #region Audio Settings
  [Header("Audio")]
  [SerializeField] private AudioSource engineSound;
  [SerializeField]
  [Range(0, 1)] private float minPitch = 1f;
  [SerializeField]
  [Range(1, 5)] private float maxPitch = 5f;
  // ================== GEAR ==================
  [SerializeField] private AudioClip gearShiftSound;
  [SerializeField, Range(0, 1)] private float shiftSoundVol = 0.7f;
  [SerializeField] private bool usePitchDip = true;
  [SerializeField, Range(0.3f, 1)] private float shiftPitchDiip = 0.75f; // 속도 내려갈때 피치 비율
  [SerializeField] private float shiftPitchDipTime = 0.06f;
  [SerializeField] private float shiftPitchRiseTime = 0.12f;
  private bool shiftingPitch = false;
  // ================== Skid ==================
  [SerializeField] private AudioSource skidSound;
  [SerializeField] float skidStartSlip = 2.0f; // 이 이상이면 소리 시작(m/s)
  [SerializeField] float skidFullSlip = 7f; // 이 이상이면 최대 볼륨
  [SerializeField] float skidFadeSpeed = 10f; // 볼륨/피치 램프 속도
  [SerializeField] float skidMinVol = 0f, skidMaxVol = 0.9f;
  [SerializeField] float skidMinPitch = 0.9f, skidMaxPitch = 1.2f;
  #endregion

  void Awake()
  {
    rb = GetComponent<Rigidbody>();
    ApplyStats(stats);
  }

  void Update()
  {
    if(isFinished) return;
    GetPlayerInput();
    currSpeed = rb.velocity.magnitude;
    EngineSound();
  }

  void FixedUpdate()
  {
    if (Time.frameCount % 60 == 0)
    {
      Debug.Log($"[{name}] lv.z={currCarLocalVel.z:F2}, gear={currGear}, " +
                $"Accel={acceleration}, RL={tires[2].name}, RR={tires[3].name}");
    }
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
    float slip = CalcLateralSpeed();
    bool shouldSkid = isGrounded && (slip > skidStartSlip) && Mathf.Abs(currCarLocalVel.z) > 2f;
    UpdatedSkidSound(shouldSkid, slip);
    EndBooster();
  }

  #region Stats
  void ApplyStats(CarStats s)
  {
    rb.mass = s.mass;
    rb.angularDrag = s.angularDrag;

    acceleration = s.acceleration;
    maxSpeed = s.maxSpeed;
    deceleration = s.deceleration;
    steerForce = s.steerForce;
    turningCurve = s.turningCurve;
    decelLerpSpeed = s.decelLerpSpeed;
    coastFactor = s.coastFactor;
    minSpeedForFullDecel = s.minSpeedForFullDecel;
    currDecelMag = s.currDecelMag;

    holdTopSpeed = s.holdTopSpeed;

    baseDownforce = s.baseDownforce;
    downforcePerMS = s.downforcePerMS;

    airGravity = s.airGravity;
    airGravityDuration = s.airGravityDuration;

    accelCurve = s.accelCurve;

    brakePow = s.brakePow;
  }
  #endregion

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
        if (!isHoldingTop)
          Deceleration();
      }

      Turn();
      SidewaysDrag();
    }
  }

  void Acceleration()
  {
    currDecelMag = 0f;
    float accelPower = (moveInput >= 0f) ? acceleration : reverseAccel;

    float speedRatio = Mathf.Clamp01(MathF.Abs(currCarLocalVel.z) / maxSpeed);
    float curveMulti = accelCurve.Evaluate(speedRatio);
    Vector3 force = accelPower * Mathf.Abs(moveInput) * Mathf.Sign(moveInput) * curveMulti * transform.forward;

    // 후륜 구동 : 뒷바퀴 idx 2부터
    for (int i = 2; i < tires.Length; i++)
    {
      rb.AddForceAtPosition(acceleration * moveInput * transform.forward, tires[i].transform.position, ForceMode.Acceleration);
    }
  }

  void Deceleration()
  {
    Vector3 lv = transform.InverseTransformDirection(rb.velocity);
    float dir = Mathf.Sign(lv.z);
    float speedAbs = Mathf.Abs(lv.z);

    // 페달 off vs 브레이크 판단
    bool isCoast = Mathf.Abs(moveInput) <= 0.01f;
    bool isBrake = (!isCoast) && (
      (moveInput < -0.01f && lv.z > 0.1f) ||   //전진 중 후진 입력
      (moveInput > 0.01f && lv.z < -0.1f)      // 후진 중 전진 입력
       );

    // 기본 제동 크기
    float baseDecel = deceleration * brakePow;

    // 저속 구간에서는 과도한 감속 방지(속도 비례)
    float lowSpeedScale = Mathf.InverseLerp(0f, minSpeedForFullDecel, speedAbs);

    // 목표 감속 크기: 브레이크일 땐 100%, 코스트일 땐 coastFactor
    float targetMag = baseDecel * Mathf.Lerp(coastFactor, 1f, isBrake ? 1f : 0f);

    // 저속일수록 더 부드럽게 줄이기
    targetMag *= Mathf.Clamp01(lowSpeedScale);

    // 스무딩 (FixedUpdate 기준으로는 MoveTowards가 안정적)
    currDecelMag = Mathf.MoveTowards(currDecelMag, targetMag, decelLerpSpeed * Time.fixedDeltaTime);

    // 진행 반대 방향으로 Center of Mass에 힘 적용 -> Pitch/Yaw 부작용 최소
    Vector3 brake = -dir * currDecelMag * transform.forward;
    rb.AddForce(brake, ForceMode.Acceleration);
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

    if (boostApplyer != null && boostApplyer.fx != null)
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
  void EngineSound()
  {
    if (engineSound == null || shiftingPitch) return;
    float t = Mathf.InverseLerp(0f, maxSpeed, Mathf.Abs(currCarLocalVel.z));
    engineSound.pitch = Mathf.Lerp(minPitch, maxPitch, t);
  }
  IEnumerator ShiftAudioBlip()
  {
    if (engineSound == null) yield break;
    shiftingPitch = true;
    float start = engineSound.pitch;
    float target = MathF.Max(0.1f, start * shiftPitchDiip);

    float t = 0f;
    while (t < shiftPitchDipTime)
    {
      t += Time.deltaTime;
      engineSound.pitch = Mathf.Lerp(start, target, t / shiftPitchDipTime);
      yield return null;
    }

    t = 0f;
    while (t < shiftPitchRiseTime)
    {
      t += Time.deltaTime;
      engineSound.pitch = Mathf.Lerp(target, start, t / shiftPitchRiseTime);
      yield return null;
    }
    shiftingPitch = false;
  }

  float CalcLateralSpeed()
  {
    Vector3 vPlanar = Vector3.ProjectOnPlane(rb.velocity, transform.up);
    Vector3 vFwd = Vector3.Project(vPlanar, transform.forward);
    Vector3 vSide = vPlanar - vFwd;
    return vSide.magnitude; // m/s
  }
  void UpdatedSkidSound(bool on, float slip)
  {
    if (skidSound == null) return;

    float t = on ? Mathf.InverseLerp(skidStartSlip, skidFullSlip, slip) : 0f;
    float targetVol = Mathf.Lerp(skidMinVol, skidMaxVol, t);
    float targetPitch = Mathf.Lerp(skidMinPitch, skidMaxPitch, t);

    if (!skidSound.isPlaying && skidSound.volume > 0.01f) skidSound.Play();
    if (skidSound.isPlaying && skidSound.volume < 0.01f) skidSound.Pause();
  }
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

    if (gearBypassed) return;

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
      else if (currGear > 1)
      {
        // 이전 기어 상한에서 downshift값만큼 떨어지면 내림
        float prevTop = gearsPercents[Mathf.Clamp(currGear - 2, 0, max - 1)];
        if (speedRatio < prevTop * downshift)
        {
          currGear--;
          print($"기어 내림 {currGear}");
        }
      }
    }
    else
    {
      holdTimer -= Time.deltaTime;
      if (holdTimer <= 0f)
      {
        if (!didDropBeforeShift)
        {
          DropForwardSpeedByAmount(dropBeforeShiftAmount);
          didDropBeforeShift = true;
        }
        currGear = Mathf.Min(currGear + 1, max);
        print($"기어 올림 {currGear}");
        isHoldingTop = false;

        if (engineSound != null)
        {
          if (gearShiftSound != null) engineSound.PlayOneShot(gearShiftSound, shiftSoundVol);
          if (usePitchDip) StartCoroutine(ShiftAudioBlip());
        }
      }
    }
  }

  void DropForwardSpeedByAmount(float amount)
  {
    amount = Mathf.Max(0, amount);
    Vector3 lv = transform.InverseTransformDirection(rb.velocity);
    if (lv.z > 0f)
    {
      lv.z = Mathf.Max(0f, lv.z - amount);
      rb.velocity = transform.TransformDirection(lv);
    }
  }

  void ApplyGearHoldAndCap()
  {
    if (gearBypassed && currGear < maxGears)
      currGear = maxGears;

    int max = Mathf.Clamp(maxGears, 1, 5);
    float currTop = gearsPercents[Mathf.Clamp(currGear - 1, 0, max - 1)];
    float gearTopSpeed = maxSpeed * currTop;

    Vector3 lv = transform.InverseTransformDirection(rb.velocity);
    if (lv.z > gearTopSpeed && extraFwd <= 0f)
    {
      lv.z = gearTopSpeed;
      rb.velocity = transform.TransformDirection(lv);
    }
  }

  public void EnterGearBypass(float duration, bool topGear = true)
  {
    gearBypassEndTime = Time.time + Mathf.Max(0.05f, duration);
    isHoldingTop = false;
    holdTimer = 0f;
    didDropBeforeShift = true;

    if (topGear)
      currGear = maxGears; // 잠시 동안 기어 5단 고정
  }

  void EndBooster()
  {
    if (extraFwd > 0f)
    {
      Vector3 lv = transform.InverseTransformDirection(rb.velocity);
      lv.z += extraFwd;
      rb.velocity = transform.TransformDirection(lv);

      // 자연스럽게 감속(maxSpeed 캡)
      extraFwd = Mathf.Max(0f, extraFwd - extraDecayPerSec * Time.fixedDeltaTime);
    }
    else
    {
      Vector3 lv = transform.InverseTransformDirection(rb.velocity);
      if (lv.z > maxSpeed)
      {
        lv.z = Mathf.Max(maxSpeed, lv.z - Time.fixedDeltaTime * deceleration);
        rb.velocity = transform.TransformDirection(lv);
      }
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

    EnterGearBypass(gearBypass, true);

    if (currCarLocalVel.z > 0.1f)
    {
      if (other.CompareTag("SpeedUp"))
      {
        Debug.Log($"감지 : {other.tag}");
        if (boostApplyer != null)
        {
          boostApplyer.ApplyBoost(2f, 1.1f, 1.5f); // 시간, 크기, 속도
        }
        ;
        rb.AddForce(transform.forward * acceleration * 30f, ForceMode.Acceleration); // 슬로프 탈 때 속도 감속 강제 보정

        float targetBoostSpeed = maxSpeed * 1.25f; // 내 차 최고속도의 125%
        ApplyTransientOverdrive(add: maxSpeed * 0.12f, minFwdIfLower: maxSpeed * 0.5f);
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
        if (boostApplyer != null)
        {
          boostApplyer.ApplyBoost(2f, 1.1f, 2f);

          rb.AddForce(transform.forward * acceleration * 50f, ForceMode.Acceleration); // 물리적으로 앞으로 밀기

          float targetBoostSpeed = maxSpeed * 1.5f; // pad 밟으면 최소 보장 속도
          StartCoroutine(BoostPadCoroutine(targetBoostSpeed, 1.5f));
        }
      }
      if (other.CompareTag("Goal"))
      {
        if (isFinished) return;

        StartCoroutine(SmoothStop(2f));
        FinalCount.Instance.FinishPlayer();
      }
    }
  }
  public IEnumerator SmoothStop(float duration = 1.5f)
  {
    isFinished = true;

    // 엑셀은 즉시 막고, 핸들은 계속 살아 있게 둠
    moveInput = 0;

    float timer = 0f;
    Vector3 initVel = rb.velocity;
    Vector3 initAngularVel = rb.angularVelocity;

    while (timer < duration)
    {
      float t = timer / duration;

      rb.velocity = Vector3.Lerp(initVel, Vector3.zero, t);
      rb.angularVelocity = Vector3.Lerp(initAngularVel, Vector3.zero, t);

      timer += Time.deltaTime;
      yield return null;
    }

    rb.velocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;
  }
  #endregion

  #region Booster Effects
  // 부스트 효과
  public void ApplyTransientOverdrive(float add, float minFwdIfLower = 0f)
  {
    // 현재 전진 속도가 너무 낮으면 최소 보장(minFwdIfLower)
    if (minFwdIfLower > 0f)
    {
      Vector3 lv = transform.InverseTransformDirection(rb.velocity);
      lv.z = Mathf.Max(lv.z, minFwdIfLower);
      rb.velocity = transform.TransformDirection(lv);
    }

    // 오버레이 속도: 기존 것보다 더 큰 값으로 갱신(스택 대신 최댓값 권장). add : 추가로 붙여줄 속도
    extraFwd = Mathf.Max(extraFwd, add);
  }

  IEnumerator BoostPadCoroutine(float targetSpeed, float duration)
  {
    float timer = duration;
    while (timer > 0f)
    {
      Vector3 lv = transform.InverseTransformDirection(rb.velocity);
      lv.z = Mathf.Max(lv.z, targetSpeed);
      rb.velocity = transform.TransformDirection(lv);

      timer -= Time.fixedDeltaTime;
      yield return new WaitForFixedUpdate();
    }
  }

  #endregion

  #region Barrel Roll Coroutine
  IEnumerator BarrelRollCoroutine()
  {
    Vector3 lv = transform.InverseTransformDirection(rb.velocity);
    if (boostApplyer != null)
      boostApplyer.ApplyBoost(3, 1.1f, 2f);
    rb.AddForce(transform.forward * acceleration * 30f, ForceMode.Acceleration);
    lv.z = Mathf.Max(lv.z, 30f);
    rb.velocity = transform.TransformDirection(lv);

    ApplyTransientOverdrive(add: maxSpeed * 0.15f, minFwdIfLower: maxSpeed * 0.55f);
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

  void OnCollisionEnter(Collision collision)
  {
    if (collision.gameObject.CompareTag("Wall"))
    {
      Rigidbody rb = GetComponent<Rigidbody>();
      rb.velocity *= 0.5f;
      rb.velocity = Vector3.Reflect(rb.velocity, collision.contacts[0].normal) * 0.3f;
    }
  }
}