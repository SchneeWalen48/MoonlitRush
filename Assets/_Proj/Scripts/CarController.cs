using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody carRB;
    [SerializeField] private Transform[] rayPoints;
    [SerializeField] private LayerMask drivable;
    [SerializeField] private Transform accelerationPoint;
    [SerializeField] private GameObject[] tires = new GameObject[4];
    [SerializeField] private GameObject[] frontTireParent = new GameObject[2];

    [Header("Suspension Settings")]
    [SerializeField] private float springStiffness;
    [SerializeField] private float damperStiffness;
    [SerializeField] private float restLength;
    [SerializeField] private float springTravel;
    [SerializeField] private float wheelRadius;

    [Header("AI Move")]
    public WaypointTest WaypointTest;
    private int currentWaypointIndex = 0;
    private float targetSpeed; //웨이포인트용
    private float moveInput = 0;
    private float steerInput = 0;

    [Header("Car settings")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float steerStrength = 15f;
    [SerializeField] private AnimationCurve turningCurve;
    [SerializeField] private float dragCoefficient = 1f;
    [SerializeField] private float brakingDeceleration = 100f;
    [SerializeField] private float brakingDragCoefficient = 0.5f;
    [SerializeField] private float driftingDragCoefficient = 0.1f; // 드리프트 시 사이드 드래그 계수
    private bool isDrifting = false;

    private Vector3 carLocalVelocity;
    private float carVelocityRatio = 0;
    private float currentSpeed = 0;

    private int[] wheelsIsGrounded = new int[4];
    private bool isGrounded = false;

    [Header("Visuals")]
    [SerializeField] private float tireRotSpeed = 3000f;
    [SerializeField] private float maxSteeringAngle = 30f;

    [Header("Ray settings")] //감지 설정
    private float downDis = 2f;

    //스피드 발판    
    Coroutine boostCoroutine;
    ISpeedUp lastSpeedUp;

    private void Start()
    {
        carRB = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (WaypointTest == null || WaypointTest.Count == 0) return;

        //raycast 중심점                      차체의 앞쪽 범퍼 근처에서 레이 시작 권장
        Vector3 origin = transform.position + Vector3.up * 0.7f + transform.forward * 1.15f;
        //다운 레이
        Debug.DrawRay(origin, Vector3.down * downDis, Color.blue);
        bool isDown = Physics.Raycast(origin, Vector3.down, out RaycastHit downHit, downDis);

        UpdateAIControls();
        Suspension();
        GroundCheck();
        CalculateCarVelocity();
        Movement();
        Visuals();
    }

    void UpdateAIControls()
    {
        Debug.Log("UpdateAIControls() is running.");        

        Vector3 target = WaypointTest.GetWaypoint(currentWaypointIndex).position;
        Vector3 localTarget = transform.InverseTransformPoint(target); //InverseTransformPoint(): 월드 좌표를 현재 차량의 로컬 좌표계로 변환

        // 전방 레이캐스트
        //RaycastHit hit;
        //float rayDistance = 20f; // 레이캐스트 거리
        //if (Physics.Raycast(transform.position, transform.forward, out hit, rayDistance, drivable))
        //{
        //    if (hit.collider.CompareTag("Obstacle")) // 장애물 태그 확인
        //    {
        //        // 장애물이 있으면 좌/우로 steerInput을 조절하여 회피
        //        if (Random.value > 0.5f)
        //        {
        //            steerInput = 1f; // 우회전
        //        }
        //        else
        //        {
        //            steerInput = -1f; // 좌회전
        //        }
        //        moveInput = -0.5f; // 감속 또는 후진하여 충돌 회피
        //    }

        //    Debug.DrawLine(transform.position, transform.forward * rayDistance, Color.magenta);

        //}

        //Waypoint의 x방향 위치에 따라 핸들 방향 계산
        steerInput = Mathf.Clamp(localTarget.x / localTarget.magnitude, -1f, 1f);

        //다음 웨이포인트 간의 각도 계산
        Transform currentWP = WaypointTest.GetWaypoint(currentWaypointIndex);
        Transform nextWP = WaypointTest.GetWaypoint((currentWaypointIndex + 1) % WaypointTest.Count);
        Vector3 toNext = (nextWP.position - currentWP.position).normalized;
        Vector3 toCar = (target - transform.position).normalized;
        float angleToNext = Vector3.Angle(transform.forward, toCar);

        Debug.Log($"Current Angle: {angleToNext}, Current Speed: {currentSpeed}"); //콘솔에 찍힘 그렇담 드리프트 로직이 문제란 소리....

        currentSpeed = carRB.velocity.magnitude * 3.6f; //rigidbody 속도(m/s)를 km/h로 변환
        if (angleToNext > 15f && currentSpeed > 70f && !isDrifting) //급커브 드리프트
        {
            isDrifting = true;            
            Debug.Log($"Drifting started! Angle: {angleToNext}, Speed: {currentSpeed}");                    
        }
        else if (angleToNext < 10f && isDrifting)
        { //직선 구간
            isDrifting = false;
        }

            if (isDrifting)
            {
                // 드리프트 중에는 조향 강도를 높임
                steerInput = Mathf.Clamp(localTarget.x / localTarget.magnitude, -1f, 1f) * 7f;
                moveInput = 0.5f;                
            }
            else // 드리프트 중이 아닐 때, 일반 주행 로직을 실행
            {
                //일반 주행
                steerInput = Mathf.Clamp(localTarget.x / localTarget.magnitude, -1f, 1f);
                targetSpeed = maxSpeed;
               
                if (currentSpeed < targetSpeed)
                {
                    moveInput = 1f;
                }
                else
                {//목표 속도에 도달하면 가속을 멈추거나 필요 시 감소
                    moveInput = 0f;
                }
            }
        
        
        //다음 Waypoint 확인        
        float distance = Vector3.Distance(transform.position, target);
        if (distance < 10f)
        {                                                  //마지막에 도달하면 다시 0부터 시작(루프)
            currentWaypointIndex = (currentWaypointIndex + 1) % WaypointTest.Count;
        }

        Debug.Log("UpdateAIControls() finished.");
    }

    void Suspension()
    {
        for (int i = 0; i < rayPoints.Length; i++)
        {
            RaycastHit hit;
            float maxDistance = restLength;

            if (Physics.Raycast(rayPoints[i].position, -rayPoints[i].up, out hit, maxDistance + wheelRadius, drivable))
            {
                wheelsIsGrounded[i] = 1;

                float currentSpringLength = hit.distance - wheelRadius;
                float springCompression = (restLength - currentSpringLength) / springTravel;

                float springVelocity = Vector3.Dot(carRB.GetPointVelocity(hit.point), rayPoints[i].up);
                float dampForce = damperStiffness * springVelocity;

                float springForce = springCompression * springStiffness;

                float netForce = springForce - dampForce;

                carRB.AddForceAtPosition(netForce * rayPoints[i].up, rayPoints[i].position);

                //Visuals
                SetTirePosition(tires[i], hit.point + rayPoints[i].up * wheelRadius);

                Debug.DrawLine(rayPoints[i].position, hit.point, Color.red);
            }
            else
            {
                wheelsIsGrounded[i] = 0;

                //Visuals
                SetTirePosition(tires[i], rayPoints[i].position - rayPoints[i].up * maxDistance);

                Debug.DrawLine(rayPoints[i].position, rayPoints[i].position + (maxDistance + wheelRadius) * -rayPoints[i].up, Color.green);
            }
        }
    }

    void Visuals()
    {
        TireVisuals();
    }

    void TireVisuals()
    {
        float steeringAngle = maxSteeringAngle * steerInput;

        for (int i = 0; i < tires.Length; i++)
        {
            if (i < 2)
            {
                tires[i].transform.Rotate(Vector3.right, tireRotSpeed * carVelocityRatio * Time.deltaTime, Space.Self);

                frontTireParent[i].transform.localEulerAngles = new Vector3(frontTireParent[i].transform.localEulerAngles.x, steeringAngle, frontTireParent[i].transform.localEulerAngles.z);
            }
            else
            {
                tires[i].transform.Rotate(Vector3.right, tireRotSpeed * moveInput * Time.deltaTime, Space.Self);
            }
        }
    }

    void SetTirePosition(GameObject tire, Vector3 targetPosition)
    {
        tire.transform.position = targetPosition;
    }

    void GroundCheck()
    {
        int tempGroundedWheels = 0;

        for (int i = 0; i < wheelsIsGrounded.Length; i++)
        {
            tempGroundedWheels += wheelsIsGrounded[i];
        }

        if (tempGroundedWheels > 1)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    void CalculateCarVelocity()
    {
        carLocalVelocity = transform.InverseTransformDirection(carRB.velocity);
        carVelocityRatio = carLocalVelocity.z / maxSpeed;
    }

    private void Movement()
    {
        if (isGrounded)
        {
            Accelerate();
            Decelerate();
            Turn();
            SidewaysDrag();
        }

        void Accelerate()
        {
            if (carLocalVelocity.z < targetSpeed) //목표 속도에 따라 가속
            {
                carRB.AddForceAtPosition(acceleration * moveInput * carRB.transform.forward, accelerationPoint.position, ForceMode.Acceleration);
            }

            Debug.Log(carLocalVelocity.z);
        }

        void Decelerate()
        {
            if (isDrifting && carLocalVelocity.z > targetSpeed) {
                carRB.AddForce(brakingDeceleration * -carRB.transform.forward, ForceMode.Acceleration);
            }
            // AI는 키 입력 대신 속도에 따라 감속
            else if (!isDrifting && carLocalVelocity.z > targetSpeed)
            {
                carRB.AddForce(brakingDeceleration * -carRB.transform.forward, ForceMode.Acceleration);
            }
            else if (Mathf.Abs(carLocalVelocity.z) > 0 && moveInput == 0) //moveinput이 0일 때만 자연 감속
            {
                // 가속하지 않을 때 자연스러운 감속
                carRB.AddForce(deceleration * carVelocityRatio * -carRB.transform.forward, ForceMode.Acceleration);
            }
        }

        void Turn()
        {
            carRB.AddRelativeTorque(steerStrength * steerInput * turningCurve.Evaluate(Mathf.Abs(carVelocityRatio)) * Mathf.Sign(carVelocityRatio) * carRB.transform.up, ForceMode.Acceleration);
        }

        void SidewaysDrag()
        {
            float currentSidewaysSpeed = carLocalVelocity.x;
            float dragMagnitude;
            if (isDrifting)
            {
                dragMagnitude = -currentSidewaysSpeed * driftingDragCoefficient;
            }
            else
            {
                dragMagnitude = -currentSidewaysSpeed * (currentSpeed > targetSpeed ? brakingDragCoefficient : dragCoefficient); //코너링 시 옆으로 미끄러지는 현상을 더 강하게 제어. 감속과 동시에 코너링 안정성을 높이는 역할
            }

            Vector3 dragForce = carRB.transform.right * dragMagnitude;
            carRB.AddForceAtPosition(dragForce, carRB.worldCenterOfMass, ForceMode.Acceleration);
        }
    }

    public void ApplySpeedPadBoost(float force, float duration)
    {
        if (boostCoroutine != null)
        {
            StopCoroutine(boostCoroutine);
        }
        boostCoroutine = StartCoroutine(BoostRoutine(force, duration));
    }

    IEnumerator BoostRoutine(float force, float duration)
    {
        Debug.Log("AI 스피드 패드 코루틴 시작");
        maxSpeed += force;
        yield return new WaitForSeconds(duration);

        Debug.Log("AI 스피드 패드 코루틴 끝");
        maxSpeed -= force;
    }
}
