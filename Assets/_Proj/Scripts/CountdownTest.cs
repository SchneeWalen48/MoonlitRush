using UnityEngine;
using System.Collections;

[DefaultExecutionOrder(100)] // CarSpawn 이후 실행 보장
public class CountdownTest : MonoBehaviour
{
  public CarSpawn carSpawn;        // 인스펙터에 CarSpawn 할당
  [SerializeField] bool debugAutoStart = true; // 개발용: 자동 시작
  [SerializeField] KeyCode debugKey = KeyCode.Space; // 개발용: 수동 시작 키

  bool started;

  void Start()
  {
    // 개발용 자동 시작(인트로 코드 들어오면 false로 꺼두면 됨)
    if (debugAutoStart) StartCoroutine(AutoStartAfterRealtime(0.5f));
  }

  IEnumerator AutoStartAfterRealtime(float sec)
  {
    yield return new WaitForSecondsRealtime(sec);
    if (!started) StartRace(debugSetTimescale: true);
  }

  void Update()
  {
    // 정식 플로우: timeScale이 1이 되는 프레임에 시작
    if (!started && Time.timeScale >= 1f && carSpawn && carSpawn.lastSpawned)
      StartRace(debugSetTimescale: false);

    // 개발용 수동 시작
    if (!started && debugAutoStart && Input.GetKeyDown(debugKey))
      StartRace(debugSetTimescale: true);
  }

  public void StartRace(bool debugSetTimescale)
  {
    var go = carSpawn ? carSpawn.lastSpawned : null;
    if (!go) return;

    // 컨트롤러 켜기
    var pc = go.GetComponentInChildren<CarController>(true);
    if (pc) pc.enabled = true;
    var cc = go.GetComponentInChildren<CarController>(true);
    if (cc) cc.enabled = true;

    // 혹시 비활성 스크립트가 더 있다면 전부 켜기(원치 않으면 빼도 됨)
    foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
      if (mb && !mb.enabled) mb.enabled = true;

    // 물리 깨우기 (자식 RB까지 모두)
    foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
    {
      rb.isKinematic = false;
      rb.useGravity = true;
      rb.constraints = RigidbodyConstraints.None;
      rb.WakeUp();
    }

    // 개발용으로만 타임스케일 복구(인트로 합치면 debugAutoStart 끄고 이 경로 안 탐)
    if (debugSetTimescale && Time.timeScale == 0f)
      Time.timeScale = 1f;

    started = true;
    Debug.Log("[CountdownTest] Race started");
    Destroy(this); // 1회성
  }
}
