using System;
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

  [Header("Reverse")]
  [SerializeField] private float reverseMaxSpeed = 20f;

  [Header("Car Settings")]
  [SerializeField] private float acceleration = 25f;
  [SerializeField] private float maxSpeed = 100f;
  [SerializeField] private float deceleration = 10f;
  [SerializeField] private float steerForce = 15f;
  [SerializeField] private AnimationCurve turningCurve;
  [SerializeField] private float dragCoefficient;

  [Header("Gear")]
  [SerializeField, Range(1, 5)] private int maxGears = 5;
  [SerializeField] private float[] gearsPercents = new float[] { 0.18f, 0.36f, 0.56f, 0.78f, 1 };
  //[SerializeField] private float[] gearAccelMultipliers = new float[] { 1.8f, 1.5f, 1.25f, 1f, 0.8f };
  [SerializeField] private float holdTopSpeed = 5f; // 자동 변속 전 기어 별 최고 속도에서 유지하는 시간(s)
  [SerializeField, Range(0.01f, 0.2f)] private float dropBeforeShiftPercent = 0.05f; // 변속 전 기어 별 최고 속도에서 잠깐 속도 줄이는 비율(%)[실제 기어 변속 하듯이 <- 수동 변속기 클러치 떼는 순간 속도 살짝 줄어드는 느낌]
  [SerializeField] private float shiftLag = 0.3f;
  [SerializeField] private float upShiftHysteresis = 0.02f;
  [SerializeField] private float minTimeBetweenShifts = 0.3f;

  private int currGear = 1; // 현재 기어 단
  private bool isHoldingTop = false; // 기어 최고 속도에서 속도 유지했는지
  private float holdTimer = 0f;
  private bool didDropBeforeShift = false; // 변속 전 속도 떨어뜨렸는지

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
    CalculateCarVelocity(); ApplyDownforce();
    Movement();
    Visuals();
    ApplyGearHoldAndCap();
    GearLogic();
    ApplyReverseSpeed();
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
        if (currCarLocalVel.z > -0.1f && readyToReverse)
        {
          Acceleration();
        }
        else
        {
          Deceleration();
        }
      }
      else
      {
        Deceleration();
      }

      Turn();
      SidewaysDrag();
    }
  }

  void Acceleration()
  {

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
    rb.AddForceAtPosition(deceleration * moveInput * -transform.forward, accelPoint.position, ForceMode.Acceleration);
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

    float dragMagnitude = -currSidewaysSpeed * dragCoefficient;
    Vector3 dragForce = transform.right * dragMagnitude;
    rb.AddForceAtPosition(dragForce, rb.worldCenterOfMass, ForceMode.Acceleration);
  }
  void ApplyDownforce()
  {
    if (!isGrounded) return; // 공중에선 X
    float v = rb.velocity.magnitude;               // m/s
    float down = Mathf.Min(baseDownforce + downforcePerMS * v, maxDownforce);
    rb.AddForce(-transform.up * down, ForceMode.Force);
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
    if (isGrounded && (isDrifting || Mathf.Abs(currCarLocalVel.x) > minSideSkidVel))
    {
      ToggleSkidMarks(true);
      ToggleSkidSmokes(true);
      //ToggleSkidSound(true);
    }
    else
    {
      ToggleSkidMarks(false);
      ToggleSkidSmokes(false);
      //ToggleSkidSound(false);
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

    if (isHoldingTop && moveInput > 0f)
    {
      moveInput = 0f;
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

  #region Trigger
  void OnTriggerEnter(Collider other)
  {
    float effectDuration = 0;
    if (other.CompareTag("SpeedUp"))
    {
      Debug.Log($"감지 : {other.tag}");
      if (acceleration + 20f >= maxSpeed) acceleration = Mathf.Min(acceleration + 20f, maxSpeed);
      else { acceleration += 20f; }
      effectDuration = 3f;
      ApplyEffects(effectDuration);
    }

    if (other.CompareTag("Barrel"))
    {
      Debug.Log($"감지 : {other.tag}");
      if (acceleration + 30f >= maxSpeed) acceleration = Mathf.Min(acceleration + 30f, maxSpeed);
      else { acceleration += 30f; }
      effectDuration = 4f;
      ApplyEffects(effectDuration);
    }
  }

  void ApplyEffects(float timer)
  {
    float originSpeed = acceleration;
    timer -= Time.deltaTime;
    if (timer <= 0)
    {
      acceleration = originSpeed;
    }
  }
  #endregion
}