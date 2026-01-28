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
  [SerializeField] private Transform accelPoint; // 가속 기준점
  [SerializeField] private GameObject[] tires = new GameObject[4]; // 타이어 메쉬
  [SerializeField] private GameObject[] frontTireParents = new GameObject[2]; // 앞바퀴 회전 부모(조향 시각 연출)
  [SerializeField] private TrailRenderer[] skidMarks = new TrailRenderer[2];
  [SerializeField] private ParticleSystem[] skidFxs = new ParticleSystem[2];
  public BoostApplyer boostApplyer;
  #endregion

  #region Suspension
  [Header("Suspension Settings")]
  [SerializeField] private float springStiffness = 30000f; // 스프링 강성 –> 높을수록 단단한 서스펜션
  [SerializeField] private float damperStiffness = 3000f; // 감쇠 계수 –> 흔들림 억제
  [SerializeField] private float restLen = 1f; // 정지 시 휠 기준 길이

  [SerializeField] private float springTravel = 0.5f; // 서스펜션 이동 허용 범위
  [SerializeField] private float wheelRadius = 0.33f; // 휠 반지름
  #endregion

  int[] wheelIsGrounded = new int[4];
  bool isGrounded = false;
  public bool isFinished;
  bool hardLocked = false;
  bool isFinishing = false;

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
  // 피치 파라미터
  [SerializeField, Range(0.1f, 4f)] private float minPitch = 0.5f;
  [SerializeField, Range(0.1f, 4f)] private float basePitch = 1.0f;  // 평상시
  [SerializeField, Range(0.1f, 4f)] private float maxPitch = 3.0f;  // 변속 시 피크

  // 변속 피치 연출
  [SerializeField] private float shiftBurstUpTime = 0.06f;  // 3까지 올라가는 시간
  [SerializeField] private float shiftBurstHoldTime = 0.05f;  // 정점 유지 시간
  [SerializeField] private float shiftBurstDownTime = 0.12f;  // 1로 내려오는 시간
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
    // 엔진 오디오 시작값 강제: 인트로 전에는 항상 꺼둠
    if (engineSound == null) engineSound = GetComponent<AudioSource>();
    if (engineSound != null)
    {
      engineSound.loop = true;
      engineSound.playOnAwake = false; // 씬 시작 즉시 재생 방지
      engineSound.mute = true;         // 인트로 중엔 항상 뮤트
      engineSound.Stop();              // 혹시 재생 중이면 정지
    }
    if (engineSound) engineSound.pitch = basePitch;
  }



  void Update()
  {
    if (hardLocked) return;
    GetPlayerInput();
    currSpeed = rb.velocity.magnitude;
    EngineSound();
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
    accelCurve = s.accelCurve;

    decelLerpSpeed = s.decelLerpSpeed;
    coastFactor = s.coastFactor;
    minSpeedForFullDecel = s.minSpeedForFullDecel;
    currDecelMag = s.currDecelMag;

    holdTopSpeed = s.holdTopSpeed;

    baseDownforce = s.baseDownforce;
    downforcePerMS = s.downforcePerMS;

    airGravity = s.airGravity;
    airGravityDuration = s.airGravityDuration;


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
        Vector3 lv = transform.InverseTransformDirection(rb.velocity); // 설정된 최대 후진 속도를 초과하면 강제로 클램프
        lv.z = -reverseMaxSpeed; // 월드 속도를 로컬로 변환 후 Z만 제한
        rb.velocity = transform.TransformDirection(lv); // 다시 월드 좌표로 변환하여 적용
      }
    }
  }

  void Turn()
  {
    // 드리프트 중에는 조향력을 증가시켜 슬라이드 상태에서도 방향 전환 가능하게 만듦
    float currSteerStrength = isDrifting ? steerForce * 1.5f : steerForce;

    // 조향 토크 계산
    rb.AddTorque(currSteerStrength * steerInput * turningCurve.Evaluate(Mathf.Abs(carVelRatio)) * Mathf.Sign(currCarLocalVel.z) * transform.up, ForceMode.Acceleration);
  }

  void SidewaysDrag()
  {
    float currSidewaysSpeed = currCarLocalVel.x; // 로컬 X축 속도 = 측면 미끄러짐 크기

    // 드리프트 중에는 횡방향 드래그를 줄여 미끄러짐을 허용하고, 일반 주행에선 강하게 억제
    float targetDrag = isDrifting ? dragCoefficient / driftDragMultiplier : dragCoefficient;

     // 드리프트 진입/해제 시 급격한 변화 방지용 스무딩
    currDragCoefficient = Mathf.Lerp(currDragCoefficient, targetDrag, Time.deltaTime * driftTransitionSpeed); 

    // 측면 속도에 반대 방향으로 힘 적용
    float dragMagnitude = -currSidewaysSpeed * currDragCoefficient;
    Vector3 dragForce = transform.right * dragMagnitude;
    rb.AddForceAtPosition(dragForce, rb.worldCenterOfMass, ForceMode.Acceleration); // 질량 중심에 힘을 가해 불필요한 회전 토크 방지
  }
  
  // 접지력 강화를 위한 다운포스 적용
  void ApplyDownforce()
  {
    if (!isGrounded) return; // 공중에선 X
    float v = rb.velocity.magnitude;  // m/s
    float down = Mathf.Min(baseDownforce + downforcePerMS * v, maxDownforce); // 속도에 비례해 증가하는 다운포스
    rb.AddForce(-transform.up * down, ForceMode.Force); // 차량 아래 방향으로 힘 적용
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
    float sinAngle = toUpAxis.magnitude; // sin(각도) 크기 (두 벡터가 얼마나 기울어졌는지)

    // 거의 수직일 경우 불필요한 연산/토크 방지
    if (sinAngle > 1e-4f)
    {
      Vector3 torqueDir = toUpAxis.normalized; // 회전 방향(정규화된 회전 축)
      float angle = Mathf.Asin(Mathf.Clamp(sinAngle, -1f, 1f)); // 실제 기울어진 각도(rad)

      Vector3 corrective = torqueDir * (lvTorqueStrength * angle) - rb.angularVelocity * lvTorqueDamping; // 기울어진 각도에 비례한 복원 토크 + 현재 각속도에 반비례한 감쇠 토크

      corrective = Vector3.ClampMagnitude(corrective, maxLvTorque); // 과도한 회전 방지

      rb.AddTorque(corrective, ForceMode.Acceleration); // 공중에서도 자연스럽게 차체가 세워지도록 토크 적용
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
    if (engineSound == null) return;

    if (shiftingPitch) return;

    engineSound.pitch = basePitch;
  }
  IEnumerator ShiftPitchBurst()
  {
    if (engineSound == null) yield break;
    shiftingPitch = true;

    float start = Mathf.Max(minPitch, engineSound.pitch);
    float peak = Mathf.Max(basePitch, maxPitch);

    // ↑ 올라가기
    float t = 0f;
    while (t < shiftBurstUpTime)
    {
      t += Time.deltaTime;
      engineSound.pitch = Mathf.Lerp(start, peak, t / Mathf.Max(0.0001f, shiftBurstUpTime));
      yield return null;
    }
    engineSound.pitch = peak;

    // 정점 유지
    if (shiftBurstHoldTime > 0f)
      yield return new WaitForSeconds(shiftBurstHoldTime);

    // ↓ 내려오기 (basePitch=1.0으로)
    t = 0f;
    while (t < shiftBurstDownTime)
    {
      t += Time.deltaTime;
      engineSound.pitch = Mathf.Lerp(peak, basePitch, t / Mathf.Max(0.0001f, shiftBurstDownTime));
      yield return null;
    }
    engineSound.pitch = basePitch;

    shiftingPitch = false;
  }

  public void SetEngineMute(bool mute, bool stopIfMuting = true, bool restartIfUnmuted = true)
  {
    if (!engineSound) return;
    engineSound.mute = mute;

    if (mute && stopIfMuting && engineSound.isPlaying)
      engineSound.Stop();

    // 인트로 끝나고 뮤트 해제 시 재생 보장
    if (!mute && restartIfUnmuted && engineSound.clip != null && !engineSound.isPlaying)
      engineSound.Play();
  }

  public void BeginFinishSequence(float duration = 1.5f, bool hardLockAfter = true)
  {
    if (isFinishing) return;
    StartCoroutine(FinishRoutine(duration, hardLockAfter));
  }

  IEnumerator FinishRoutine(float duration, bool hardLockAfter)
  {
    isFinishing = true;
    isFinished = true;   // 조작 차단 의도 플래그
    moveInput = 0f;     // 엑셀 입력 즉시 차단

    Vector3 v0 = rb.velocity;
    Vector3 w0 = rb.angularVelocity;

    float vol0 = (engineSound ? engineSound.volume : 0f);
    float pitch0 = (engineSound ? engineSound.pitch : basePitch);

    float t = 0f;
    while (t < duration)
    {
      float k = t / Mathf.Max(0.0001f, duration);

      // 물리 감속
      rb.velocity = Vector3.Lerp(v0, Vector3.zero, k);
      rb.angularVelocity = Vector3.Lerp(w0, Vector3.zero, k);

      // 엔진음도 같이 가라앉기(피치는 minPitch쪽으로, 볼륨은 0으로)
      if (engineSound)
      {
        engineSound.pitch = Mathf.Lerp(pitch0, minPitch, k);
        engineSound.volume = Mathf.Lerp(vol0, 0f, k);
        if (!engineSound.isPlaying) engineSound.Play();
      }

      t += Time.unscaledDeltaTime;
      yield return null;
    }

    // 최종 정지
    rb.velocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;

    // 엔진음 정리
    if (engineSound)
    {
      engineSound.Stop();
      engineSound.pitch = basePitch; // 다음 레이스 대비 복구
      engineSound.volume = vol0;
    }

    isFinishing = false;

    if (hardLockAfter)
    {
      // 이후 입력/업데이트 완전 차단
      hardLocked = true;
      rb.isKinematic = true;
      enabled = false; // CarController 비활성
    }
  }
  float CalcLateralSpeed()
  {
    Vector3 vPlanar = Vector3.ProjectOnPlane(rb.velocity, transform.up); // 차량 Up 기준 평면 성분만 추출
    Vector3 vFwd = Vector3.Project(vPlanar, transform.forward); // 전방 성분 분리
    Vector3 vSide = vPlanar - vFwd; // 남은 성분 = 횡방향 미끄러짐
    return vSide.magnitude; // m/s 단위 횡속
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
    float speedRatio = Mathf.Clamp01(fwdSpeed / Mathf.Max(0.01f, maxSpeed)); // 기어별 최고 속도 비율 계산
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
        }
      }
    }
    else
    {
      holdTimer -= Time.deltaTime;
      if (holdTimer <= 0f)
      {
        // 실제 변속 느낌을 위한 순간 감속
        if (!didDropBeforeShift)
        {
          DropForwardSpeedByAmount(dropBeforeShiftAmount);
          didDropBeforeShift = true;
        }
        currGear = Mathf.Min(currGear + 1, max);
        isHoldingTop = false;
        if (engineSound != null)
        {
          StartCoroutine(ShiftPitchBurst());
        }
      }
    }
  }

  // 변속 연출을 위한 순간 감속
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

  // 부스트 종료 후 부스트 잔여 속도(extraFwd)를 점진적으로 정리
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
    // 각 휠(레이 포인트)별로 독립적인 서스펜션 계산
    for (int i = 0; i < rayPoints.Length; i++)
    {
      RaycastHit hit;
      float maxDistance = restLen;

      // 휠 기준 아래 방향으로 레이캐스트. wheelRadius를 더해 실제 타이어 접촉 시점을 보정
      if (Physics.Raycast(rayPoints[i].position, -rayPoints[i].up, out hit, maxDistance + wheelRadius, drivable))
      {
        wheelIsGrounded[i] = 1;

        float currSpringLen = hit.distance - wheelRadius;
        float springCompression = (restLen - currSpringLen) / springTravel;

        // 해당 휠 위치에서의 상대 속도(스프링 방향
        float springVel = Vector3.Dot(rb.GetPointVelocity(rayPoints[i].position), rayPoints[i].up);
        float dampForce = damperStiffness * springVel; // 감쇠력: 스프링 속도에 비례 (진동 억제)

        float springForce = springStiffness * springCompression; // 반발력: 압축량에 비례

        float netForce = springForce - dampForce; // 최종 스프링 힘 (반발 - 감쇠)

        rb.AddForceAtPosition(netForce * rayPoints[i].up, rayPoints[i].position);

        SetTirePosition(tires[i], hit.point + rayPoints[i].up * wheelRadius / 2);
        //Debug.DrawLine(rayPoints[i].position, hit.point, Color.red);
      }
      else
      {
        wheelIsGrounded[i] = 0; // 비접지 상태

        // 공중에 뜬 타이어는 최대 스트로크 위치로 내려서 연출
        SetTirePosition(tires[i], rayPoints[i].position - rayPoints[i].up * (restLen + springTravel) * 0.9f);
        //Debug.DrawLine(rayPoints[i].position, rayPoints[i].position + (wheelRadius + maxDistance) * -rayPoints[i].up, Color.green);
      }
    }
  }
  #endregion

  public IEnumerator HitByMissileCoroutine()
  {
    if (isInvincible) yield break;

    rb.velocity *= 0.3f; // 속도 대폭 감소

    rb.AddForce(Vector3.up * 8f, ForceMode.VelocityChange); // 위쪽으로 순간 힘을 주어 피격 반동 연출

    // 공중에서 과도한 회전을 줄이기 위해 드래그 증가
    float originDrag = rb.drag;
    rb.drag = 0.5f;

    yield return new WaitForSeconds(2f);

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
        if (boostApplyer != null)
        {
          boostApplyer.ApplyBoost(2f, 1.1f, 1.5f); // 시간, 크기, 속도
        }
        ;
        rb.AddForce(transform.forward * acceleration * 30f, ForceMode.Acceleration); // 슬로프 탈 때 속도 감속 강제 보정

        float targetBoostSpeed = maxSpeed * 1.25f; // 내 차 최고속도의 125%
        ApplyTransientOverdrive(add: maxSpeed * 0.15f, minFwdIfLower: maxSpeed * 0.5f);
      }

      if (other.CompareTag("Barrel"))
      {
        if (!isBarrelRolling)
          StartCoroutine(BarrelRollCoroutine());
      }

      if (other.CompareTag("BoostPad"))
      {
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

        BeginFinishSequence(1.5f, false);
        FinalCount.Instance.Finish();
      }
    }
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
      lv.z = Mathf.Max(lv.z, targetSpeed); // 전진 속도가 targetSpeed보다 낮아지지 않도록 보정
      rb.velocity = transform.TransformDirection(lv);

      timer -= Time.fixedDeltaTime;
      yield return new WaitForFixedUpdate();
    }
  }

  #endregion

  #region Barrel Roll Coroutine
  IEnumerator BarrelRollCoroutine()
  {
    Vector3 lv = transform.InverseTransformDirection(rb.velocity); // 현재 전진 속도 보존
    if (boostApplyer != null)
      boostApplyer.ApplyBoost(3, 1.1f, 2f);
    rb.AddForce(transform.forward * acceleration * 30f, ForceMode.Acceleration); // 공중 진입을 위한 추가 전진 가속

    // 최소 전진 속도 보장 (공중 진입 실패 방지)
    lv.z = Mathf.Max(lv.z, 30f);
    rb.velocity = transform.TransformDirection(lv);

    ApplyTransientOverdrive(add: maxSpeed * 0.15f, minFwdIfLower: maxSpeed * 0.55f);

    yield return new WaitForSeconds(0.4f);
    yield return new WaitUntil(() => !isGrounded);


    isBarrelRolling = true;
    float timer = barrelRollDuration;
    float rollDir = Mathf.Sign(steerInput); // 조향 입력 방향에 따라 회전 방향 결정

    while (timer > 0)
    {
      // 차량 전방 축 기준 회전 토크 적용
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
      rb.velocity *= 0.5f; // 속도 에너지 1차 감소
      
      // 충돌 법선 기준 반사 + 추가 감쇠
      rb.velocity = Vector3.Reflect(rb.velocity, collision.contacts[0].normal) * 0.3f;
    }
  }
}