
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class AICarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody carRB;
    [SerializeField] private Transform[] rayPoints;
    [SerializeField] private LayerMask ground;   //drivable
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
    [HideInInspector] public float moveInput = 0;
    [HideInInspector] public float steerInput = 0;

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


    [Header("Drift")] //player 스크립트에서 가져옴
    [SerializeField] private float driftDragMultiplier = 2f;
    [SerializeField] private float driftTransitionSpeed = 5f;
    private float currDragCoefficient;

    [Header("Recovery Settings")]
    [SerializeField] private float stuckTimeThreshold = 5f; // 멈췄다고 판단하는 시간 (초)
    [SerializeField] private float recoveryTime = 3f;      // 복구 후 다시 움직이는 딜레이
    [SerializeField] private float rotationResetSpeed = 1f; // 회전 복구 속도

    [Header("SpeedBoostPad")]
    //스피드 발판 및 슬로프
    bool isBoosted = false;
    Coroutine boostCoroutine; //스피드 발판
    bool isSpeedUp = false;
    public float speedUpDuration = 2f;
    private float downforce = 60f;
    private float forwardForce = 20f;
    Coroutine speedUpCoroutine;

    [Header("Etc")]
    public bool moveStart = false; //게임 시작 시 움직임 변수
    private FinalCount final; //완주 시 게임 종료 알리는 카운트 스크립트

    public bool isInvincible = false;

    private Vector3 lastPosition;
    private float stuckTimer;
    private bool isRecovering = false;
    public bool isFinished = false;
    private void Start()
    {
        carRB = GetComponent<Rigidbody>();
        final = FindAnyObjectByType<FinalCount>();
    }

    private void FixedUpdate()
    {
        // if (moveStart == false) return;

        if (WaypointTest == null || WaypointTest.Count == 0) return;

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
        Vector3 target = WaypointTest.GetWaypoint(currentWaypointIndex).position;
        Vector3 localTarget = transform.InverseTransformPoint(target); //InverseTransformPoint(): 월드 좌표를 현재 차량의 로컬 좌표계로 변환

        //전방 레이캐스트
        RaycastHit hit;
        float rayDistance = 3f; // 레이캐스트 거리
        if (Physics.Raycast(transform.position, transform.forward, out hit, rayDistance, ground))
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
        Vector3 toNext = (nextWP.position - currentWP.position).normalized; //다음 웨이포인트 계산
        Vector3 toCar = (target - transform.position).normalized;
        float angleToNext = Vector3.Angle(transform.forward, toCar);

        Debug.Log($"Current Angle: {angleToNext}, Current Speed: {currentSpeed}"); //콘솔에 이상한 값이 찍힘 그렇담 드리프트 로직이 문제란 소리...., 픽시드 업데이트에서 디버그 찍는 중이라 계속 나오는 게 당연

        currentSpeed = carRB.velocity.magnitude;


        if (angleToNext > 20f && currentSpeed > 20f || Mathf.Abs(carLocalVelocity.x) > 5f) //급커브 드리프트
        {
            isDrifting = true;

            Debug.Log($"Drifting started! Angle: {angleToNext}, Speed: {currentSpeed}"); //이게 연속으로 나오면 문제있음
        }
        else if (angleToNext < 13f && isDrifting && Mathf.Abs(carLocalVelocity.x) < 2f)
        { //직선 구간
            isDrifting = false;

        }


        if (isDrifting)
        {
            // 드리프트 중에는 조향 강도를 높임            
            steerInput = Mathf.Clamp(localTarget.x / localTarget.magnitude, -1f, 1f) * 3.5f;
            moveInput = 0.8f;
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

        //웨이포인트 정방향으로
        float dotWP = Vector3.Dot(transform.forward, toCar);
        if (dotWP < 0f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % WaypointTest.Count;
        }

        //다음 Waypoint 확인        
        float distance = Vector3.Distance(transform.position, target);
        if (distance < 10f || dotWP < 0f)
        {                                                  //마지막에 도달하면 다시 0부터 시작(루프)
            currentWaypointIndex = (currentWaypointIndex + 1) % WaypointTest.Count;
        }
    }

    void Suspension()
    {
        for (int i = 0; i < rayPoints.Length; i++)
        {
            RaycastHit hit;
            float maxDistance = restLength;

            if (Physics.Raycast(rayPoints[i].position, -rayPoints[i].up, out hit, maxDistance + wheelRadius, ground))
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
        if (!isGrounded)
        {
            Air();
        }
        else if (isGrounded)
        {
            Debug.Log("공중 제어 X, 일반 주행 시작");
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
        }

        void Decelerate()
        {
            if (isDrifting && carLocalVelocity.z > targetSpeed) //드리프트 중
            {
                carRB.AddForce((brakingDeceleration * 0.4f) * -carRB.transform.forward, ForceMode.Acceleration);
            }
            // AI는 키 입력 대신 속도에 따라 감속
            else if (!isDrifting && carLocalVelocity.z > targetSpeed) //일반주행
            {
                carRB.AddForce(brakingDeceleration * -carRB.transform.forward, ForceMode.Acceleration);
            }
            else if (Mathf.Abs(carLocalVelocity.z) > 0 && moveInput == 0) //moveinput이 0일 때만 자연 감속
            {
                // 가속하지 않을 때 자연스러운 감속
                carRB.AddForce(deceleration * -carRB.transform.forward, ForceMode.Acceleration);
            }
        }

        void Turn()
        {
            carRB.AddRelativeTorque(steerStrength * steerInput * turningCurve.Evaluate(Mathf.Abs(carVelocityRatio)) * Mathf.Sign(carVelocityRatio) * carRB.transform.up, ForceMode.Acceleration);
        }

        void SidewaysDrag()
        {
            float currentSidewaysSpeed = carLocalVelocity.x;

            float targetDrag = isDrifting ? dragCoefficient * driftDragMultiplier : dragCoefficient;
            currDragCoefficient = Mathf.Lerp(currDragCoefficient, targetDrag, Time.deltaTime * driftTransitionSpeed);
            float dragMagnitude = -currentSidewaysSpeed * currDragCoefficient;


            Vector3 dragForce = carRB.transform.right * dragMagnitude;
            carRB.AddForceAtPosition(dragForce, carRB.worldCenterOfMass, ForceMode.Acceleration);
        }

        void Air()
        {
            Debug.Log("공중 제어");
            steerInput = 0;
            moveInput = 0;
            carRB.angularVelocity *= 0.9f;
            carRB.AddForce(Vector3.down * 100f);
            //수직
            //Vector3 velo = carRB.velocity;
            //velo.y *= 0.8f;
            //carRB.velocity = velo;

            //수평 유도
            Quaternion targetPos = Quaternion.Euler(0, carRB.transform.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetPos, Time.fixedDeltaTime * 1.2f);
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
            if (boostApplyer != null)
            {
                boostApplyer.ApplyBoost(2f, 1f, 1.5f);
            }

            boostCoroutine = StartCoroutine(BoostRoutine(40, 1.5f));
        }
        else if (other.CompareTag("SpeedUp"))
        {

            isSpeedUp = true;
            if (speedUpCoroutine != null)
            {
                StopCoroutine(speedUpCoroutine);
            }
            if (boostApplyer != null)
            {
                boostApplyer.ApplyBoost(2f, 1f, 1.5f);
            }

            carRB.AddForce((transform.forward * forwardForce), ForceMode.Acceleration);
            carRB.AddForce((Vector3.down * downforce) * 5, ForceMode.Acceleration);
            speedUpCoroutine = StartCoroutine(SpeedUpRoutine(40, 1.5f));
        }
        else if (other.CompareTag("Goal"))
        {
            if (isFinished) return;

            StartCoroutine(SmoothStop(2f));
            Debug.Log("완주!");
        }
    }

    IEnumerator BoostRoutine(float force, float duration)
    {
        Debug.Log("AI 부스트 패드 코루틴 시작");

        // 초기 속도 강제 설정
        Vector3 localVelocity = transform.InverseTransformDirection(carRB.velocity);
        localVelocity.z = Mathf.Max(localVelocity.z, force);
        carRB.velocity = transform.TransformDirection(localVelocity);

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            // 현재 웨이포인트
            Vector3 target = WaypointTest.GetWaypoint(currentWaypointIndex).position;
            Vector3 localTarget = transform.InverseTransformPoint(target);
            float angleToNext = Vector3.Angle(transform.forward, (target - transform.position).normalized);

            // 공중 제어
            if (!isGrounded)
            {
                Vector3 lv = transform.InverseTransformDirection(carRB.velocity);

                // 전진 속도 제한
                lv.z = Mathf.Min(lv.z, 20f);

                // 좌우 속도 제한
                lv.x = Mathf.Clamp(lv.x, -5f, 5f);

                // 수직 속도 제한 (너무 급하게 떨어지지 않도록)
                lv.y = Mathf.Max(lv.y, -150f);

                carRB.velocity = transform.TransformDirection(lv);

                // 공중에서도 드리프트 유지
                isDrifting = true;

                // 착지 직전 약간 스티어 보정
                if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 3f))
                {
                    float steerBoost = 1.5f;
                    carRB.AddRelativeTorque(steerStrength * steerInput * steerBoost * carRB.transform.up, ForceMode.Acceleration);
                }
            }
            else
            {
                //착지 직후 가속 보조 (최소 속도 유지)
                Vector3 lv = transform.InverseTransformDirection(carRB.velocity);
                float minSpeed = targetSpeed * 0.6f; // 필요에 따라 0.5~0.7 정도 조절 가능
                if (lv.z < minSpeed)
                {
                    lv.z = minSpeed;
                    carRB.velocity = transform.TransformDirection(lv);
                }

                // 지면 위 커브 제어
                if (angleToNext > 25f)
                {
                    lv = transform.InverseTransformDirection(carRB.velocity);
                    lv.x *= 0.5f; // 좌우 미끄러짐 줄이기
                    carRB.velocity = transform.TransformDirection(lv);

                    float steerBoost = 2f;
                    carRB.AddRelativeTorque(steerStrength * steerInput * steerBoost * carRB.transform.up, ForceMode.Acceleration);

                    // 약간 감속 적용
                    carRB.AddForce(-carRB.transform.forward * brakingDeceleration * 0.2f, ForceMode.Acceleration);

                    isDrifting = true;
                }
                else
                {
                    // 커브가 아니면 드리프트 해제
                    isDrifting = false;
                }
            }

            yield return null;
        }

        // 부스터 종료 직전 속도를 targetSpeed에 맞춤
        Vector3 finalLv = transform.InverseTransformDirection(carRB.velocity);
        finalLv.z = Mathf.Min(finalLv.z, targetSpeed);
        carRB.velocity = transform.TransformDirection(finalLv);

        Debug.Log("AI 부스트 패드 코루틴 끝");

        isBoosted = false;
    }

    IEnumerator SpeedUpRoutine(float force, float duration)
    {
        Debug.Log("AI 슬로프 시작");
        Vector3 localVelocity = carRB.transform.InverseTransformDirection(carRB.velocity);
        localVelocity.z = Mathf.Max(localVelocity.z, force);
        carRB.velocity = transform.TransformDirection(localVelocity);

        yield return new WaitForSeconds(duration);

        Debug.Log("AI 슬로프 종료");

        isSpeedUp = false;
    }

    public IEnumerator HitByMissileCoroutine() //미사일 공격 받을 시 코루틴
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
        if (!isInvincible)
        {
            StabilizeAfterHit();
        }

        //플레이어와 충돌 순간 반응
        //if (other.collider.CompareTag("Player"))
        //{
        //    StabilizeAfterHit();
        //}
    }

    void StabilizeAfterHit()
    {
        carRB.angularVelocity *= 0.3f; //회전속도: 차량 흔들림 조절

        Vector3 localVel = transform.InverseTransformDirection(carRB.velocity); //속도를 차량 로컬 좌표계로 바꿈
        localVel.x *= 0.5f; //차량 튕기는 방향 조절
        localVel.z = Mathf.Max(localVel.z, 5f); // 최소 전진 속도 추가
        carRB.velocity = transform.TransformDirection(localVel); //로컬 좌표계를 월드 좌표계 속도로 바꿈

        // StartCoroutine(TemporaryStabilize());
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
        if (isFinished) return;
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

        int nearest = -1;
        float minDist = Mathf.Infinity;
        for (int i = 0; i < WaypointTest.Count; i++) //가까운 웨이포인트 탐색
        {
            float dot = Vector3.Dot(transform.forward, (WaypointTest.GetWaypoint(i).position - transform.position).normalized);
            float dist = Vector3.Distance(transform.position, WaypointTest.GetWaypoint(i).position);

            if (dist < minDist && dot > 0f)
            {
                minDist = dist;
                nearest = i;
            }
        }

        if (nearest != -1)
        {
            //이전 웨이포인트 인덱스
            int beforeWpIndex = nearest - 1;

            if (beforeWpIndex < 0)
            {
                beforeWpIndex = WaypointTest.Count - 1;
            }

            Transform respawnWP = WaypointTest.GetWaypoint(beforeWpIndex);

            transform.position = respawnWP.position + Vector3.up * 1f; //리스폰 지점
            transform.rotation = Quaternion.LookRotation(respawnWP.forward);
            carRB.velocity = Vector3.zero;
            carRB.angularVelocity = Vector3.zero;

            currentWaypointIndex = beforeWpIndex; //현재 웨이포인트 갱신
        }

        yield return new WaitForSeconds(recoveryTime);
        isRecovering = false;


    }

    public IEnumerator SmoothStop(float duration = 1.5f)
    {
        isFinished = true;

        // 엑셀은 즉시 막고, 핸들은 계속 살아 있게 둠
        moveInput = 0;

        float timer = 0f;
        Vector3 initVel = carRB.velocity;
        Vector3 initAngularVel = carRB.angularVelocity;

        while (timer < duration)
        {
            float t = timer / duration;

            carRB.velocity = Vector3.Lerp(initVel, Vector3.zero, t);
            carRB.angularVelocity = Vector3.Lerp(initAngularVel, Vector3.zero, t);

            timer += Time.deltaTime;
            yield return null;
        }

        carRB.velocity = Vector3.zero;
        carRB.angularVelocity = Vector3.zero;
    }
}
