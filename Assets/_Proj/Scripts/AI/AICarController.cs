using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody carRB;
    [SerializeField] private Transform[] rayPoints;
    [SerializeField] private LayerMask drivable;
    [SerializeField] private Transform accelerationPoint;
    [SerializeField] private GameObject[] tires = new GameObject[4];
    [SerializeField] private GameObject[] frontTireParent = new GameObject[2];
    [SerializeField] public BoostApplyer boostApplyer;
    [SerializeField]
    private ParticleSystem[] skidFxs = new ParticleSystem[2];

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
    public float moveInput = 0;
    public float steerInput = 0;

    [Header("Car settings")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxSpeed = 50f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float steerStrength = 15f;
    [SerializeField] private AnimationCurve turningCurve;
    [SerializeField] private float dragCoefficient = 1f;
    [SerializeField] private float brakingDeceleration = 100f;
    //[SerializeField] private float brakingDragCoefficient = 0.5f;
    //[SerializeField] private float driftingDragCoefficient = 0.1f; // 드리프트 시 사이드 드래그 계수
    private bool isDrifting = false;

    private Vector3 carLocalVelocity;
    private float carVelocityRatio = 0;
    private float currentSpeed = 0;

    private int[] wheelsIsGrounded = new int[4];
    private bool isGrounded = false;

    [Header("Visuals")]
    [SerializeField] private float tireRotSpeed = 3000f;
    [SerializeField] private float maxSteeringAngle = 30f;
    [SerializeField] private float minSideSkidVel = 10f;

    [Header("Ray settings")] //감지 설정
    private float downDis = 2f;

    [Header("Barrel Roll Settings")]
    public float barrelRollTorque = 1000f; // 배럴롤 회전력 250f, 
    public float barrelRollDuration = 3f; //배럴롤 지속 시간 1.5f
    bool isBarrelRolling = false;

    //스피드 발판 및 슬로프
    bool isBoosted = false;
    Coroutine boostCoroutine; //스피드 발판
    bool isSpeedUp = false;
    public float speedUpDuration = 2f;
    public float downforce = 25f;
    Coroutine speedUpCoroutine;

    [Header("Drift")] //player 스크립트에서 가져옴
    [SerializeField] private float driftDragMultiplier = 2f;
    [SerializeField] private float driftTransitionSpeed = 5f;
    private float currDragCoefficient;

    public bool moveStart = false; //게임 시작 시 움직임 변수
    private FinalCount final; //완주 시 게임 종료 알리는 카운트 스크립트

    public bool isInvincible = false;

    [Header("Recovery Settings")]
    [SerializeField] private float stuckTimeThreshold = 1.5f; // 멈췄다고 판단하는 시간 (초)
    [SerializeField] private float recoveryTime = 1f;      // 복구 후 다시 움직이는 딜레이
    [SerializeField] private float rotationResetSpeed = 1f; // 회전 복구 속도

    private Vector3 lastPosition;
    private float stuckTimer;
    private bool isRecovering = false;
  public  bool isFinished = false;
    private void Start()
    {
        carRB = GetComponent<Rigidbody>();
        final = FindAnyObjectByType<FinalCount>();
    }

    private void FixedUpdate()
    {
        if (moveStart == false) return;

        if (WaypointTest == null || WaypointTest.Count == 0) return;

        //raycast 중심점                      차체의 앞쪽 범퍼 근처에서 레이 시작 권장
        Vector3 origin = transform.position + Vector3.up * 0.7f + transform.forward * 1.2f;
        //다운 레이
        Debug.DrawRay(origin, Vector3.down * downDis, Color.blue);
        bool isDown = Physics.Raycast(origin, Vector3.down, out RaycastHit downHit, downDis);

        //if (isDown)
        //{   //슬로프 감지

        //    if (downHit.collider.CompareTag("SpeedUp"))
        //    {
        //        Debug.Log($"슬로프 감지. downforce: {downforce}");
        //        carRB.AddForce(-transform.up * downforce, ForceMode.Acceleration);

        //        StartCoroutine(SpeedUpRoutine());


        //    }
            //    //배럴롤 점프대 감지
            //    else if (downHit.collider.CompareTag("Barrel"))
            //    {
            //        if (isBarrelRolling == false)
            //        {
            //            StartCoroutine(BarrelRollRoutine());
            //            Debug.Log("배럴롤 점프대");
            //        }
            //    }

        //}
        //else { lastSpeedUp = null; }



        UpdateAIControls();
        Suspension();
        GroundCheck();
        CalculateCarVelocity();
        Movement();
        Visuals();
        VFX();
        CheckForStuck();
        CheckForFlip();
    }

    void UpdateAIControls()
    {
        Debug.Log("UpdateAIControls() is running.");

        Vector3 target = WaypointTest.GetWaypoint(currentWaypointIndex).position;
        Vector3 localTarget = transform.InverseTransformPoint(target); //InverseTransformPoint(): 월드 좌표를 현재 차량의 로컬 좌표계로 변환

        //전방 레이캐스트
        RaycastHit hit;
        float rayDistance = 20f; // 레이캐스트 거리
        if (Physics.Raycast(transform.position, transform.forward, out hit, rayDistance, drivable))
        {
            if (hit.collider.CompareTag("Player")) // 플레이어 태그 확인
            {
                Debug.Log("플레이어 감지! 회피 로직 실행");

                // 플레이어의 위치를 AI 차량의 로컬 좌표로 변환
                Vector3 localPlayerPos = transform.InverseTransformPoint(hit.collider.transform.position);

                // 플레이어가 AI의 왼쪽에 있는지, 오른쪽에 있는지 판단
                if (localPlayerPos.x < 0)
                {
                    // 플레이어가 왼쪽에 있으면 오른쪽으로 회피
                    steerInput = 1.0f;
                }
                else
                {
                    // 플레이어가 오른쪽에 있으면 왼쪽으로 회피
                    steerInput = -1.0f;
                }

                // 감속
                moveInput = 0.5f;

                // 회피 로직이 실행될 때는 웨이포인트 로직을 무시하도록 return; 추가
                return;
            }

            Debug.DrawLine(transform.position, transform.forward * rayDistance, Color.magenta);

        }

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

        if (angleToNext > 10f && currentSpeed > 50f && !isDrifting) //급커브 드리프트
        {
            isDrifting = true;
            Debug.Log($"Drifting started! Angle: {angleToNext}, Speed: {currentSpeed}");
        }
        else if (angleToNext < 5f && isDrifting)
        { //직선 구간
            isDrifting = false;
        }

        //코루틴으로 barrelRoll 상태가 true면 AddRelativeTorque 함수로 차량 회전
        if (isBarrelRolling)
        {
            // carRB.AddRelativeTorque(Vector3.right * barrelRollTorque, ForceMode.Acceleration); //z축 배럴롤 forward, x축 배럴롤 right
            carRB.AddRelativeTorque(Vector3.forward * barrelRollTorque, ForceMode.Acceleration);
            steerInput = 0;
            moveInput = 0;
        }
        else if (isDrifting)
        {
            // 드리프트 중에는 조향 강도를 높임            
            steerInput = Mathf.Clamp(localTarget.x / localTarget.magnitude, -1f, 1f) * 5.5f;
            moveInput = 0.5f;
            // maxSpeed = 70f;


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
                SetTirePosition(tires[i], hit.point + rayPoints[i].up * wheelRadius / 2);

                Debug.DrawLine(rayPoints[i].position, hit.point, Color.red);
            }
            else
            {
                wheelsIsGrounded[i] = 0;

                //Visuals
                SetTirePosition(tires[i], rayPoints[i].position - rayPoints[i].up * (restLength + springTravel) * 0.9f /*maxDistance*/);

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

    void VFX()
    {
        bool allowFwdFx = (carLocalVelocity.z > 0.1f || moveInput > 0.01f) && carLocalVelocity.z >= 0f;

        bool doSkid = (isGrounded && allowFwdFx) && (isDrifting || Mathf.Abs(carLocalVelocity.x) > minSideSkidVel);

        ToggleSkidSmokes(doSkid);

        if (boostApplyer != null && boostApplyer.fx != null)
        {
            boostApplyer.fx.SetEmission(allowFwdFx);
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
        if (isBarrelRolling) return;
        else if (isGrounded)
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
            if (isDrifting && carLocalVelocity.z > targetSpeed)
            {
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
            //float dragMagnitude;
            //if (isDrifting)
            //{
            //    dragMagnitude = -currentSidewaysSpeed * driftingDragCoefficient;
            //}
            //else
            //{
            //    dragMagnitude = -currentSidewaysSpeed * (currentSpeed > targetSpeed ? brakingDragCoefficient : dragCoefficient); //코너링 시 옆으로 미끄러지는 현상을 더 강하게 제어. 감속과 동시에 코너링 안정성을 높이는 역할
            //}

            float targetDrag = isDrifting ? dragCoefficient / driftDragMultiplier : dragCoefficient;
            currDragCoefficient = Mathf.Lerp(currDragCoefficient, targetDrag, Time.deltaTime * driftTransitionSpeed);
            float dragMagnitude = -currentSidewaysSpeed * dragCoefficient;


            Vector3 dragForce = carRB.transform.right * dragMagnitude;
            carRB.AddForceAtPosition(dragForce, carRB.worldCenterOfMass, ForceMode.Acceleration);
        }
    }
    
    public void OnTriggerEnter(Collider other)
    {
        if (isBoosted) return;
        if (isSpeedUp) return;

        if (other.CompareTag("BoostPad"))
        {
            isBoosted = true;

            if (boostCoroutine != null)
            {
                StopCoroutine(boostCoroutine);
            }

            boostCoroutine = StartCoroutine(BoostRoutine(20, 3f));
        }
        else if (other.CompareTag("SpeedUp"))
        {        

            isSpeedUp = true;
            if (speedUpCoroutine != null)
            {               
                StopCoroutine (speedUpCoroutine);
            }
            carRB.AddForce(transform.forward * downforce, ForceMode.Acceleration);
            speedUpCoroutine = StartCoroutine(SpeedUpRoutine(150, 3f));
        }
        else if (other.CompareTag("Goal"))
        {
            isFinished = true;   
            StopAllCoroutines();
            Debug.Log("완주!");
            moveInput = 0;
            steerInput = 0;
                       
            carRB.drag = 20;
            carRB.angularDrag = 20;
            carRB.isKinematic = true;
            final.Finish();
        }

    }

    IEnumerator BoostRoutine(float force, float duration)
    {
        Debug.Log("AI 스피드 패드 코루틴 시작");
        maxSpeed += force;
        yield return new WaitForSeconds(duration);

        Debug.Log("AI 스피드 패드 코루틴 끝");
        maxSpeed -= force;
        isBoosted = false;
    }

    //배럴롤 코루틴
    //IEnumerator BarrelRollRoutine()
    //{
    //    isBarrelRolling = true;
    //    Debug.Log("AI 배럴롤 시작");
    //    //carRB.useGravity = false;        
    //    // carRB.AddRelativeTorque(Vector3.forward * barrelRollTorque, ForceMode.Acceleration);

    //    yield return new WaitForSeconds(barrelRollDuration); //회전 시간 

    //    isBarrelRolling = false;
    //    Debug.Log("AI 배럴롤 종료");
    //    //carRB.useGravity = true;

    //    moveInput = 1f;

    //}

    IEnumerator SpeedUpRoutine(float force, float duration)
    {        
        Debug.Log("AI 슬로프 시작");
        maxSpeed += force;

        yield return new WaitForSeconds(duration);

        Debug.Log("AI 슬로프 종료");
        maxSpeed -= force;
        isSpeedUp = false;

    }

    public IEnumerator HitByMissileCoroutine()
    {
        if (isInvincible) yield break;

        carRB.velocity *= 0.3f;
        carRB.AddForce(Vector3.up * 8f, ForceMode.VelocityChange);
        float originDrag = carRB.drag;
        carRB.drag = 0.5f;

        yield return new WaitForSeconds(2f);

        carRB.drag = originDrag;
    }

    private void OnCollisionEnter(Collision other)
    {
        //플레이어와 충돌 순간 반응
        if (other.collider.CompareTag("Player"))
        {
            StabilizeAfterHit();
        }
    }

    void StabilizeAfterHit()
    {
        carRB.angularVelocity *= 0.3f; //회전속도: 차량 흔들림 조절

        Vector3 localVel = transform.InverseTransformDirection(carRB.velocity); //속도를 차량 로컬 좌표계로 바꿈
        localVel.x *= 0.5f; //차량 튕기는 방향 조절
        carRB.velocity = transform.TransformDirection(localVel); //로컬 좌표계를 월드 좌표계 속도로 바꿈

        StartCoroutine(TemporaryStabilize());
    }

    //일시적 안정화 코루틴
    IEnumerator TemporaryStabilize()
    {
        float originalDrag = carRB.drag;
        float originalAngularDrag = carRB.angularDrag;

        carRB.drag = 4f;
        carRB.angularDrag = 4f;

        yield return new WaitForSeconds(1f);

        carRB.drag = originalDrag;
        carRB.angularDrag = originalAngularDrag;
    }
        
    void CheckForStuck()
    {
        if(isFinished) return;
        // 차량이 움직이는지 확인
        if (Vector3.Distance(transform.position, lastPosition) < 0.1f)
        {
            stuckTimer += Time.deltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }
        lastPosition = transform.position;

        // 일정 시간 이상 멈춰 있었다면 복구 루틴을 시작
        if (stuckTimer >= stuckTimeThreshold && !isRecovering)
        {
            StartCoroutine(RespawnToNearestWaypoint());
        }
    }

    // 차량이 뒤집혔는지 확인
    void CheckForFlip()
    {
        // 차의 '위' 방향(transform.up)이 월드 '위' 방향(Vector3.up)과 반대이면 뒤집힌 것으로 간주
        if (Vector3.Dot(transform.up, Vector3.up) < 0.5f && !isRecovering)
        {
            StartCoroutine(RespawnToNearestWaypoint());
        }
    }
   
    // 복구 루틴 코루틴: 웨이포인트 기준 리스폰
    IEnumerator RespawnToNearestWaypoint()
    {


        isRecovering = true;              

        Transform nearest = null;
        float minDist = Mathf.Infinity;
        for (int i = 0; i < WaypointTest.Count; i++) //가까운 웨이포인트 탐색
        {
            float dist = Vector3.Distance(transform.position, WaypointTest.GetWaypoint(i).position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = WaypointTest.GetWaypoint(i);
                currentWaypointIndex = i;
            }
        }

        if (nearest != null)
        {
            transform.position = nearest.position + Vector3.up * 1f; //리스폰 지점
            transform.rotation = Quaternion.LookRotation(nearest.forward);
            carRB.velocity = Vector3.zero;
            carRB.angularVelocity = Vector3.zero;
        }

        yield return new WaitForSeconds(recoveryTime);
        isRecovering = false;
    }
}
