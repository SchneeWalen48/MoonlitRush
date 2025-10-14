using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cinemachine;

public class IntroWaypointCamera : MonoBehaviour
{
  // ---------- Player/AI Lock ----------
  [Header("Player Lock")]
  public CarController player;                 // 비워두면 isPlayer=true인 RacerInfo를 탐색
  public bool lockPlayerBeforeStart = true;

  [Header("All Lock")]
  public bool lockAllBeforeStart = true;       // 인트로 시작 시 전원 잠금
  public bool unlockAIOnGo = true;       // GO! 시 AI 즉시 출발

  // ---------- Cinemachine / Cameras ----------
  [Header("VCams")]
  public CinemachineVirtualCamera vcamIntro;   // 인트로용 VCam (이 스크립트가 붙은 오브젝트 권장)
  public CinemachineVirtualCamera vcamPlayer;  // 플레이어용 VCam

  [Header("Priority Handoff")]
  public bool priorityHandoff = true;
  public int introPriorityStart = 100;
  public int playerPriorityStart = 10;
  public int introPriorityEnd = 0;
  public int playerPriorityEnd = 200;

  [Header("Safety Options")]
  public bool autoFindPlayerVCam = true;       // vcamPlayer 미지정 시 자동 탐색
  public bool clearIntroFollowLookAt = true;   // 인트로 VCam Follow/LookAt 제거
  public bool ensureBrainOnMainCamera = true;  // Main Camera에 Brain 보장

  // ---------- Waypoints / Motion ----------
  [Header("Waypoints")]
  public Transform waypointRoot;
  public List<Transform> waypoints = new List<Transform>();

  [Header("Motion")]
  public float segmentDuration = 1.8f;         // 웨이포인트 간 1구간 시간
  public float lookLerp = 4.0f;                // 회전 부드러움
  public bool constantEase = true;            // true: 등속, false: 살짝 ease-in/out
  public float startHold = 0f, endHold = 0f;   // 시작/끝 대기 (realtime)

  [Header("End Polish")]
  public bool easeOutAtLast = true;           // 마지막 구간 감속
  [Range(0f, 1f)] public float lastEaseStrength = 0.6f;
  public float endOvershoot = 2f;              // 마지막 방향으로 살짝 연장

  // ---------- UI / Fade / Countdown ----------
  [Header("UI & Fade")]
  public CanvasGroup gameplayUI;               // 인트로 동안 숨기고 끝나면 표시
  public CanvasGroup screenFade;               // 검은 패널(Alpha 0 권장)
  public float fadeDuration = 0.6f;

  [Header("Lead Fade & Handoff")]
  public bool fadeSlightlyBeforeEnd = true;   // 끝나기 전 미리 페이드
  public float fadeLeadSeconds = 0.5f;
  public bool handoffOnFadeOut = true;        // 리드 페이드 중 임계치에서 핸드오프
  [Range(0f, 1f)] public float handoffFadeThreshold = 0.6f;

  [Header("Countdown")]
  public bool startCountOnIntroEnd = true;     // 인트로 끝나면 카운트다운 자동 시작
  public float startCountDelay = 0.0f;         // 인트로 끝과 카운트 사이 지연
  public StartCount startCount;                // 지정 시 이걸 Begin(), 없으면 TryBegin()

  // ---------- Debug ----------
  [Header("Debug")]
  public bool debugLog = false;
  public Color gizmoPathColor = new Color(0.1f, 0.9f, 1f, 0.8f);
  public Color gizmoPointColor = new Color(1f, 0.7f, 0.2f, 0.9f);
  public float gizmoPointSize = 0.25f;

  // ---- runtime state ----
  int i = 0;
  float u = 0f;
  bool running = false;
  bool startedLeadFade = false;
  bool handoffDone = false;

  bool introActive = true;

  [Header("Audio Handoff")]
  public AudioSource cameraAudio;              // 메인 카메라에 있는 BGM/환경음 소스 등
  [Range(0f, 1f)] public float postIntroCamVolume = 0.5f;
  public bool mutePlayerEngineDuringIntro = true;

  void Reset()
  {
    if (!vcamIntro) vcamIntro = GetComponent<CinemachineVirtualCamera>();
    if (!waypointRoot && transform.childCount > 0) waypointRoot = transform;
    RefreshWaypoints();
  }

  void Awake()
  {
    // Brain 확보
    if (ensureBrainOnMainCamera)
    {
      var cam = Camera.main;
      if (cam && !cam.TryGetComponent<CinemachineBrain>(out _))
        cam.gameObject.AddComponent<CinemachineBrain>();
    }

    if (!vcamIntro) vcamIntro = GetComponent<CinemachineVirtualCamera>();
    if (clearIntroFollowLookAt && vcamIntro)
    {
      vcamIntro.Follow = null;
      vcamIntro.LookAt = null;
    }

    if (autoFindPlayerVCam && !vcamPlayer)
      vcamPlayer = FindFallbackPlayerVCam();

    RefreshWaypoints();
    SnapToFirstWaypoint();

    if (vcamIntro) vcamIntro.m_Transitions.m_InheritPosition = false;
    if (priorityHandoff)
    {
      if (vcamIntro) vcamIntro.Priority = introPriorityStart;
      if (vcamPlayer) vcamPlayer.Priority = playerPriorityStart;
    }

    // ----- 인트로 시작 전 락 -----
    if (lockPlayerBeforeStart) LockPlayer(true);
    if (lockAllBeforeStart) LockAll(true);

    // UI/Fade 초기화
    SetUIVisible(false);
    SetFadeAlpha(0f);

    if (!cameraAudio)
    {
      var cam = Camera.main;
      if (cam) cameraAudio = cam.GetComponent<AudioSource>();
      if (!cameraAudio && vcamIntro) cameraAudio = vcamIntro.GetComponent<AudioSource>();
      if (!cameraAudio && vcamPlayer) cameraAudio = vcamPlayer.GetComponent<AudioSource>();
    }

    // ⭐ 인트로 시작 전에 엔진음 반드시 뮤트
    if (mutePlayerEngineDuringIntro)
    {
      var target = player;
      if (target == null)
      {
        var p = FindObjectsOfType<RacerInfo>(true).FirstOrDefault(r => r.isPlayer);
        if (p) target = p.GetComponent<CarController>();
      }
      target?.SetEngineMute(true, stopIfMuting: true);
    }
  }

  void Start()
  {
    RefreshWaypoints();
    if (waypoints.Count < 2 || !vcamIntro)
    {
      enabled = false;
      if (debugLog) Debug.LogWarning("[Intro] Not enough waypoints or intro VCam missing.");
      // 인트로 불가 → 바로 카운트다운만 시작
      if (startCountOnIntroEnd)
      {
        if (startCount != null) startCount.Begin();
        else StartCount.TryBegin();
      }
      return;
    }

    var tf = vcamIntro.transform;
    tf.SetPositionAndRotation(waypoints[0].position, waypoints[0].rotation);
    startedLeadFade = false;
    handoffDone = false;
    i = 0; u = 0f;

    if (startHold > 0f) StartCoroutine(BeginAfter(startHold));
    else BeginIntro();
      StartCoroutine(EnsureLocksUntilIntroEnds());
  }
  IEnumerator EnsureLocksUntilIntroEnds()
  {
    // 인트로 동안 주기적으로 새로 생긴 레이서/플레이어까지 잠금 보장
    while (introActive)
    {
      if (lockAllBeforeStart) LockAll(true);
      else if (lockPlayerBeforeStart) LockPlayer(true);
      yield return null; // 매 프레임
                         // 혹시 너무 빡세면 yield return new WaitForSecondsRealtime(0.05f);
    }
  }
  // ---- Public: 카운트다운 "GO!" 프레임에 호출 (StartCount에서 호출) ----
  public void OnCountdownFinishedGo()
  {
    // 전원 해제 + 타이머 시작
    introActive = false;
    LockAll(false);
    TimeManager.Instance?.StartTimer();
    // 여기선 페이드는 건드리지 않음(게임 시작)
  }

  // ---- Intro 재생 제어 ----
  public void BeginIntro()
  {
    if (running) return;
    if (!vcamIntro) { if (debugLog) Debug.LogWarning("[Intro] introCamera is null"); return; }
    if (priorityHandoff)
    {
      if (vcamIntro) vcamIntro.Priority = introPriorityStart;
      if (vcamPlayer) vcamPlayer.Priority = playerPriorityStart;
    }
    StartCoroutine(CoPlayIntro());
  }

  IEnumerator BeginAfter(float delay)
  {
    yield return new WaitForSecondsRealtime(delay);
    BeginIntro();
  }

  IEnumerator CoPlayIntro()
  {
    running = true;

    if (waypoints == null || waypoints.Count == 0)
    {
      if (debugLog) Debug.Log("[Intro] No waypoints. Skipping.");
      yield return OnIntroEndFlow();
      yield break;
    }

    // 시작 위치 스냅
    var tf = vcamIntro.transform;
    tf.SetPositionAndRotation(waypoints[0].position, waypoints[0].rotation);

    while (true)
    {
      // 인트로 종료 체크
      if (i >= waypoints.Count - 1) break;

      float du = Time.unscaledDeltaTime / Mathf.Max(0.0001f, segmentDuration);
      float step = constantEase ? du : du * (0.8f + 0.2f * Mathf.SmoothStep(0, 1, u));

      bool lastSeg = (i >= waypoints.Count - 2);
      if (lastSeg && easeOutAtLast)
        step *= Mathf.Lerp(1f, 0.25f, u); // 마지막에 감속

      u += step;

      while (u >= 1f && i < waypoints.Count - 1)
      {
        u -= 1f;
        i++;
        if (i >= waypoints.Count - 1) u = 0f;
      }
      if (i >= waypoints.Count - 1) break;

      int i0 = Mathf.Max(i - 1, 0);
      int i1 = i;
      int i2 = i + 1;
      int i3 = Mathf.Min(i + 2, waypoints.Count - 1);

      Vector3 p0 = waypoints[i0].position;
      Vector3 p1 = waypoints[i1].position;
      Vector3 p2 = waypoints[i2].position;
      Vector3 p3 = waypoints[i3].position;

      if (lastSeg)
        p3 = p2 + (p2 - p1).normalized * Mathf.Max(0f, endOvershoot);

      float tu = u;
      if (lastSeg && easeOutAtLast)
      {
        float k = 1f + Mathf.Clamp01(lastEaseStrength);
        tu = 1f - Mathf.Pow(1f - u, k);
      }

      Vector3 pos = Catmull(p0, p1, p2, p3, tu);
      Vector3 next = Catmull(p0, p1, p2, p3, Mathf.Min(tu + 0.01f, 1f));
      Vector3 fwd = (next - pos); if (fwd.sqrMagnitude < 1e-6f) fwd = (p2 - p1);

      tf.position = pos;
      Quaternion targetRot = Quaternion.LookRotation(fwd.normalized, Vector3.up);
      tf.rotation = Quaternion.Slerp(tf.rotation, targetRot, Time.unscaledDeltaTime * lookLerp);

      // 끝나기 직전 리드 페이드
      if (lastSeg && fadeSlightlyBeforeEnd && !startedLeadFade && screenFade)
      {
        float leadU = 1f - (fadeLeadSeconds / Mathf.Max(0.0001f, segmentDuration));
        if (u >= leadU)
        {
          startedLeadFade = true;
          StartCoroutine(FadeTo(screenFade, 1f, fadeLeadSeconds, monitorForHandoff: handoffOnFadeOut));
        }
      }

      yield return null;
    }

    running = false;

    if (endHold > 0f) yield return new WaitForSecondsRealtime(endHold);
    yield return OnIntroEndFlow();
  }

  // 인트로 종료: (필요시 추가 페이드) → UI 켜기 → VCam 우선순위 스왑 → StartCount → 페이드인
  IEnumerator OnIntroEndFlow()
  {
    if (cameraAudio) cameraAudio.volume = postIntroCamVolume;
    if (player == null)
    {
      var p = FindObjectsOfType<RacerInfo>(true).FirstOrDefault(r => r.isPlayer);
      if (p) player = p.GetComponent<CarController>();
    }
    player?.SetEngineMute(false, stopIfMuting: false, restartIfUnmuted: true);

    // 리드 페이드를 안 썼다면 여기서 어둡게 + 임계치에서 핸드오프
    if (screenFade && !startedLeadFade)
      yield return StartCoroutine(FadeTo(screenFade, 1f, fadeDuration, monitorForHandoff: handoffOnFadeOut));

    // UI 표시
    SetUIVisible(true);
    foreach (var lc in FindObjectsOfType<LapCounter>(true))
      lc.RefreshLapUI();

    // 핸드오프(재확인)
    DoHandoffToPlayer();

    // 밝게
    if (screenFade)
      yield return StartCoroutine(FadeTo(screenFade, 0f, fadeDuration));

    // 카운트다운 시작
    if (startCountOnIntroEnd)
    {
      if (startCountDelay > 0f) yield return new WaitForSecondsRealtime(startCountDelay);
      if (startCount != null) startCount.Begin();
      else StartCount.TryBegin();
    }

    enabled = false;
  }


  public void LockPlayer(bool on)
  {
    CarController target = player;
    if (target == null)
    {
      var p = FindObjectsOfType<RacerInfo>(true).FirstOrDefault(r => r.isPlayer);
      if (p) target = p.GetComponent<CarController>();
    }
    if (target == null) return;

    var rb = target.GetComponent<Rigidbody>();
    var ai = target.GetComponent<AICarController>();

    if (on)
    {
      if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; }
      target.enabled = false;
      if (ai) { ai.enabled = false; ai.moveStart = false; }
    }
    else
    {
      if (rb) { rb.isKinematic = false; rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
      target.enabled = true;
      if (ai) { ai.enabled = false; ai.moveStart = false; }
    }
  }

  public void LockAll(bool on)
  {
    var racers = FindObjectsOfType<RacerInfo>(true);
    foreach (var r in racers)
    {
      if (!r) continue;
      var rb = r.GetComponent<Rigidbody>();
      var car = r.GetComponent<CarController>();
      var ai = r.GetComponent<AICarController>();

      if (on)
      {
        if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; }
        if (car) car.enabled = false;
        if (ai) { ai.enabled = false; ai.moveStart = false; }
      }
      else
      {
        if (rb) { rb.isKinematic = false; rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        if (r.isPlayer)
        {
          if (car) car.enabled = true;
          if (ai) { ai.enabled = false; ai.moveStart = false; }
        }
        else
        {
          if (car) car.enabled = false;
          if (ai) { ai.enabled = true; ai.moveStart = unlockAIOnGo; } // GO! 시 AI 즉시 출발
        }
      }
    }
  }


  CinemachineVirtualCamera FindFallbackPlayerVCam()
  {
    CinemachineVirtualCamera best = null;
    var cams = FindObjectsOfType<CinemachineVirtualCamera>(true);
    foreach (var cam in cams)
    {
      if (cam == vcamIntro) continue;
      if (best == null || cam.Priority > best.Priority) best = cam;
    }
    return best;
  }

  void RefreshWaypoints()
  {
    waypoints.Clear();
    if (!waypointRoot) return;
    foreach (Transform c in waypointRoot)
      if (c.gameObject.activeInHierarchy) waypoints.Add(c);
  }

  void SnapToFirstWaypoint()
  {
    if (!vcamIntro || waypoints.Count == 0) return;
    var p = waypoints[0].position;
    var r = waypoints[0].rotation;

    vcamIntro.transform.SetPositionAndRotation(p, r);
    vcamIntro.PreviousStateIsValid = false;
    vcamIntro.ForceCameraPosition(p, r);
  }

  static Vector3 Catmull(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
  {
    float t2 = t * t, t3 = t2 * t;
    return 0.5f * ((2f * p1) +
                   (-p0 + p2) * t +
                   (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                   (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
  }

  void DoHandoffToPlayer()
  {
    if (handoffDone) return;

    if (priorityHandoff)
    {
      if (vcamPlayer) vcamPlayer.Priority = playerPriorityEnd;
      if (vcamIntro) vcamIntro.Priority = introPriorityEnd;
    }
    handoffDone = true;

    // 브레인 강제 갱신(컷 한 번)
    StartCoroutine(ForceCutToPlayer());
  }

  IEnumerator ForceCutToPlayer()
  {
    if (vcamIntro) vcamIntro.gameObject.SetActive(false);
    yield return new WaitForSecondsRealtime(0.05f);
    if (vcamIntro) vcamIntro.gameObject.SetActive(true);
  }

  void SetUIVisible(bool visible)
  {
    if (!gameplayUI) return;
    gameplayUI.alpha = visible ? 1f : 0f;
    gameplayUI.blocksRaycasts = visible;
    gameplayUI.interactable = visible;
  }

  void SetFadeAlpha(float a)
  {
    if (!screenFade) return;
    screenFade.alpha = a;
    screenFade.blocksRaycasts = a > 0.001f;
  }

  IEnumerator FadeTo(CanvasGroup cg, float target, float dur, bool monitorForHandoff = false)
  {
    if (!cg || dur <= 0f) { if (cg) cg.alpha = target; yield break; }

    float start = cg.alpha;
    float t = 0f;
    bool shouldMonitor = monitorForHandoff && !handoffDone && target > start;

    while (t < 1f)
    {
      t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur);
      float a = Mathf.Lerp(start, target, t);
      cg.alpha = a;

      if (shouldMonitor && a >= handoffFadeThreshold)
      {
        shouldMonitor = false;
        DoHandoffToPlayer();
      }
      yield return null;
    }

    cg.alpha = target;
    cg.blocksRaycasts = target > 0.001f;
  }

#if UNITY_EDITOR
  [ContextMenu("Refresh Waypoints")]
  void EditorRefresh() => RefreshWaypoints();

  void OnDrawGizmos()
  {
    if (!waypointRoot) return;
    var list = new List<Transform>();
    foreach (Transform c in waypointRoot) if (c.gameObject.activeInHierarchy) list.Add(c);
    if (list.Count == 0) return;

    Gizmos.color = gizmoPointColor;
    foreach (var t in list) Gizmos.DrawSphere(t.position, gizmoPointSize);

    Gizmos.color = gizmoPathColor;
    for (int k = 0; k < list.Count - 1; k++)
      Gizmos.DrawLine(list[k].position, list[k + 1].position);
#if UNITY_EDITOR
    for (int k = 0; k < list.Count; k++)
      UnityEditor.Handles.Label(list[k].position + Vector3.up * 0.25f, k.ToString());
#endif
  }
#endif
}