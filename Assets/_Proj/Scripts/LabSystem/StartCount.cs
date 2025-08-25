using System.Collections;
using TMPro;
using UnityEngine;

public class StartCount : MonoBehaviour
{
  [Header("UI")]
  [SerializeField] private TextMeshProUGUI startCountText;

  [Header("Options")]
  public bool autoStart = false;   // 씬 시작시 자동 카운트다운
  [Min(1)] public int seconds = 3; // 3,2,1
  [Min(0)] public float goHold = 0.7f; // "GO!" 표시 유지 시간

  [Header("Audio")]
  public AudioSource mainSource; //�ܺμҸ� ȿ����  
  public AudioClip countClip;
  public AudioClip startClip;

  void Start()
  {
    TimeManager.Instance?.StopTimer();
    FreezeAllCars(true);                 // 안전하게 전원 정지
    if (autoStart) Begin();
  }

  public void Begin()
  {
    if (!gameObject.activeInHierarchy) return;
    StopAllCoroutines();
    StartCoroutine(CountRoutine());
  }

  public static void TryBegin()
  {
    var sc = FindObjectOfType<StartCount>(true);
    if (sc != null) sc.Begin();
  }
  IEnumerator CountRoutine()
  {
    mainSource.PlayOneShot(countClip);
    mainSource.PlayOneShot(startClip);
    // 카운트 동안 시간 정지 + unscaled 대기
    Time.timeScale = 0f;

    for (int i = seconds; i > 0; i--)
    {
      if (startCountText) startCountText.text = i.ToString();
      yield return new WaitForSecondsRealtime(1f);
    }

    if (startCountText) startCountText.text = "GO!";
    yield return new WaitForSecondsRealtime(goHold);

    // 인트로가 있으면 인트로에게 해제/타이머 시작을 맡긴다
    var intro = FindObjectOfType<IntroWaypointCamera>(true);
    if (intro != null)
    {
      Time.timeScale = 1f;            // 시간 재개
      intro.OnCountdownFinishedGo();  // 내부에서 LockAll(false)+StartTimer()
    }
    else
    {
      // 인트로가 없으면 여기서 직접 해제+타이머 시작
      Time.timeScale = 1f;
      FreezeAllCars(false);
      TimeManager.Instance?.ResetTimer();
      TimeManager.Instance?.StartTimer();
    }

    if (startCountText) startCountText.text = string.Empty;
  }
  void FreezeAllCars(bool freeze)
  {
    // Player
    foreach (var pc in FindObjectsOfType<CarController>(true))
    {
      var rb = pc.GetComponent<Rigidbody>();
      if (freeze)
      {
        if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; }
        pc.enabled = false;
      }
      else
      {
        if (rb) rb.isKinematic = false;
        pc.enabled = true;
      }
    }
    // AI
    foreach (var ai in FindObjectsOfType<AICarController>(true))
    {
      var rb = ai.GetComponent<Rigidbody>();
      if (freeze)
      {
        if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; }
        ai.moveStart = false;
        ai.enabled = false;
      }
      else
      {
        if (rb) rb.isKinematic = false;
        ai.enabled = true;
        ai.moveStart = true;
      }
    }
  }
}
