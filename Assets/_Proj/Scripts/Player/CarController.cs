using System;
using System.Collections;
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

  float originAccel;
  float originMaxSpeed;

  [SerializeField] private float speedUpAccelMultiplier = 1.5f;
  [SerializeField] private float speedUpMaxSpeedMultiplier = 1.2f;
  [SerializeField] private float speedUpDuration = 3f;


  [Header("Car Settings")]
  [SerializeField] private float acceleration = 25f;
  [SerializeField] private float maxSpeed = 100f;
  [SerializeField] private float deceleration = 10f;
  [SerializeField] private float steerForce = 15f;
  [SerializeField] private AnimationCurve turningCurve;
  [SerializeField] private float dragCoefficient;

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

    originAccel = acceleration;
    originMaxSpeed = maxSpeed;
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
    CalculateCarVelocity();
    Movement();
    Visuals();
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
      else if(moveInput < -0.01f)
      {
        if(currCarLocalVel.z > -0.1f && readyToReverse)
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
    rb.AddForceAtPosition(acceleration * moveInput * transform.forward, accelPoint.position, ForceMode.Acceleration);
  }

  void Deceleration()
  {
    rb.AddForceAtPosition(deceleration * moveInput * -transform.forward, accelPoint.position, ForceMode.Acceleration);
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
    for(int i = 0; i < tires.Length; i++)
    {
      if(i < 2)
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
    if(isGrounded && (isDrifting || Mathf.Abs(currCarLocalVel.x) > minSideSkidVel))
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
    foreach(var skidMark in skidMarks)
    {
      skidMark.emitting = toggle;
    }
  }

  void ToggleSkidSmokes(bool toggle)
  {
    foreach(var smoke in skidFxs)
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
    moveInput = Input.GetAxis("Vertical");
    steerInput = Input.GetAxis("Horizontal");

    isDrifting = Mathf.Abs(currCarLocalVel.z) > 1f && Mathf.Abs(steerInput) > 0.1f && Input.GetKey(KeyCode.LeftShift);

    if(moveInput < -0.01f && Mathf.Abs(currCarLocalVel.z) < 0.1f && !readyToReverse)
    {
      readyToReverse = true;
      moveInput = 0;
    }
    else
    {
      if(moveInput > 0.01f)
      {
        readyToReverse = false;
      }
      moveInput = this.moveInput;
    }
  }
  #region Suspension
  void Suspension()
  {

    for (int i = 0; i < rayPoints.Length; i++)
    {
    RaycastHit hit;
      float maxDistance = restLen + springTravel;

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

        // [수정된 부분] 휠의 위치를 충돌 지점 (hit.point) 에서 휠의 반지름만큼 위로 올립니다.
        SetTirePosition(tires[i], hit.point + rayPoints[i].up * wheelRadius);
        Debug.DrawLine(rayPoints[i].position, hit.point, Color.red);
      
    }
      else
      {
        wheelIsGrounded[i] = 0;

        SetTirePosition(tires[i], rayPoints[i].position - rayPoints[i].up * (restLen + springTravel));
        Debug.DrawLine(rayPoints[i].position, rayPoints[i].position + maxDistance * -rayPoints[i].up, Color.green);
      }
    }
  }
  #endregion

  void OnTriggerEnter(Collider other)
  {
    if (other.CompareTag("SpeedUp"))
    {
      SpeedUp();
    }
  }

  private void SpeedUp()
  {
    acceleration = originAccel * speedUpAccelMultiplier;
    maxSpeed = originMaxSpeed * speedUpMaxSpeedMultiplier;
    currSpeed += acceleration;

    StartCoroutine(ResetSpeed());
  }

  IEnumerator ResetSpeed()
  {
    yield return new WaitForSeconds(speedUpDuration);
    currSpeed -= acceleration;
    acceleration = originAccel;
    maxSpeed = originMaxSpeed;
  }
}
