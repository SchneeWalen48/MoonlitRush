using System.Collections;
using System.Reflection;
using UnityEngine;
using Cinemachine;
using TMPro;
// 중앙 연결기

// 연결:
// - ItemCollector.ItemSlots ← Canvas/ItemSlots(ItemSlot 컴포넌트)
// - UseItem.slotUI         ← Canvas/ItemSlots/Slot0/Slot0Icon(SlotUI 컴포넌트)
// - VCam.Follow            ← 차 루트
//   VCam.LookAt            ← 차 첫 번째 자식(없으면 루트)
// - Dashboard.car          ← 자동차의 CarController (없으면 GameObject로도 시도)
// - Canvas/ItemSlots.useItem ← 자동차의 UseItem
// - FinalCount.PlayerCar   ← 차 루트
// - StartCount.PlayerCar   ← 차 루트
// - LapCounter.timeText    ← Canvas/LapTimeText
//   LapCounter.CheckpointManager ← Checkpoints 프리팹
// - Canvas/MinimapUI/RawImageMinimap/MinimapIcon.Target ← 차 루트
public class SceneBinder : MonoBehaviour
{
  [Header("Scene refs (Hierarchy에서 드래그)")]
  public CarSpawn carSpawn;                 // Hierarchy의 CarSpawn
  public CinemachineVirtualCamera vcam;     // Hierarchy의 Virtual Camera
  public ItemSlot canvasItemSlots;          // Canvas/ItemSlots (ItemSlot)
  public SlotUI slot0IconUI;                // Canvas/ItemSlots/Slot0/Slot0Icon (SlotUI)
  public Dashboard dashboard;               // Canvas/Dashboard (Dashboard)

  public StartCount startCnt;               
  public FinalCount finalCnt;              
  public TextMeshProUGUI lapTimeText;       
  public CheckpointManager checkpoints;     
  public MinimapIcon pMinimapIcon;
  public TextMeshProUGUI lapCntText;

  // 차량 스폰이 비동기적으로 완료되기 때문에 LastSpawne가 세팅될 때까지 안전하게 대기
  IEnumerator Start()
  {
    if (!carSpawn) carSpawn = FindObjectOfType<CarSpawn>(true);
    // Wait a moment for the car to spawn
    while (!carSpawn || !carSpawn.lastSpawned) yield return null;

    var car = carSpawn.lastSpawned;

    // --- Cam Binding ---
    if (vcam)
    {
      vcam.Follow = car.transform; // 추적 대상은 차량 루트
      vcam.LookAt = car.transform.childCount > 0 ? car.transform.GetChild(0) : car.transform;  // LookAt은 1번 자식(차체 기준점) 우선, 없으면 루트
      vcam.PreviousStateIsValid = false; // 첫 프레임 튐 방지
      SnapBehind(vcam, car.transform, dist: 4.5f, height: 1.8f, side: 0.0f, fov: 55f);
    }

    // --- Getting automotive components (루트 우선, 없으면 자식까지) ---
    var collector = car.GetComponent<ItemCollector>() ?? car.GetComponentInChildren<ItemCollector>(true);
    var useItem = car.GetComponent<UseItem>() ?? car.GetComponentInChildren<UseItem>(true);
    var carCtrl = car.GetComponent<CarController>() ?? car.GetComponentInChildren<CarController>(true);

    // ItemCollector.ItemSlots ← Canvas ItemSlots
    AssignByNameThenType(collector, new[] { "ItemSlots", "itemSlots" }, canvasItemSlots);

    // UseItem.slotUI ← Slot0Icon(SlotUI)
    AssignByNameThenType(useItem, new[] { "slotUI", "SlotUI" }, slot0IconUI);

    // Dashboard.car ← CarController (또는 GameObject)
    if (dashboard)
    {
      if (!AssignByNameThenType(dashboard, new[] { "car", "Car" }, carCtrl))
        AssignByTypeOnly(dashboard, typeof(GameObject), car);
    }


    // Canvas ItemSlot.useItem ← car.UseItem
    AssignByNameThenType(canvasItemSlots, new[] { "useItem", "UseItem" }, useItem);

    var lapCounter = car.GetComponent<LapCounter>() ?? car.GetComponentInChildren<LapCounter>();
    if (!lapCntText)
      lapCntText = GameObject.Find("Lap Text")?.GetComponent<TextMeshProUGUI>();
    if (lapCounter)
    {
      if (lapCntText) lapCounter.lapText = lapCntText;
      if (lapTimeText) lapCounter.timeText = lapTimeText;
      if (checkpoints) lapCounter.checkpointManager = checkpoints;
    }


    // 미니맵 아이콘 추적 대상 설정
    if (pMinimapIcon)
      pMinimapIcon.target = car.transform;
  }

  static void SnapBehind(CinemachineVirtualCamera cam, Transform target,
                       float dist, float height, float side, float fov)
  {
    // Default offset settings by body type
    var tFollow = cam.GetCinemachineComponent<CinemachineTransposer>();
    if (tFollow)
    {
      // Follow Offset is local (relative to target): (x=left/right, y=height, z=back)
      tFollow.m_FollowOffset = new Vector3(side, height, -dist);
      tFollow.m_XDamping = tFollow.m_YDamping = tFollow.m_ZDamping = 0f; // 첫 프레임 스냅
    }

    // 3rdPersonFollow 사용 시
    var third = cam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
    if (third)
    {
      third.CameraDistance = dist;
      third.CameraSide = side;     // -1 왼쪽 ~ +1 오른쪽 (0이면 정가운데)
      third.ShoulderOffset.y = height;
      third.Damping = Vector3.zero; // First frame snap
    }

    // Physical location is also enforced immediately
    var camPos = target.position - target.forward * dist + Vector3.up * height + target.right * side;
    var lookPos = target.position + target.forward * 5f;
    cam.ForceCameraPosition(camPos, Quaternion.LookRotation(lookPos - camPos, Vector3.up));

    // 보기 좋게 FOV 조정
    var lens = cam.m_Lens;
    lens.FieldOfView = fov;
    cam.m_Lens = lens;

    // Restore original damping from next frame (Transposer/3rdPersonFollow 쓰는 경우)
    RestoreDampingNextFrame(cam);
  }

  // 첫 프레임 이후 정상적인 카메라 댐핑 복원
  static async void RestoreDampingNextFrame(CinemachineVirtualCamera cam)
  {
    await System.Threading.Tasks.Task.Yield();
    var t = cam.GetCinemachineComponent<CinemachineTransposer>();
    if (t) { t.m_XDamping = 0.5f; t.m_YDamping = 0.8f; t.m_ZDamping = 0.6f; }

    var third = cam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
    if (third) third.Damping = new Vector3(0.5f, 0.8f, 0.6f);
  }

  // ---- First try with field name (multiple candidates), if that fails, match with type ----
  static bool AssignByNameThenType(object target, string[] names, object value)
  {
    if (target == null || value == null) return false;
    var t = target.GetType();
    var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    // 이름으로 먼저
    foreach (var n in names)
    {
      var f = t.GetField(n, flags);
      if (f == null) continue;
      if (IsAssignable(f.FieldType, value, out var boxed))
      { f.SetValue(target, boxed); return true; }
    }
    // 타입으로 대체
    return AssignByTypeOnly(target, value.GetType(), value);
  }

  // 타입만으로 필드 자동 매칭
  static bool AssignByTypeOnly(object target, System.Type desired, object value)
  {
    if (target == null || value == null) return false;
    var t = target.GetType();
    var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    foreach (var f in t.GetFields(flags))
    {
      if (f.FieldType == desired || f.FieldType.IsAssignableFrom(desired))
      {
        if (IsAssignable(f.FieldType, value, out var boxed))
        { f.SetValue(target, boxed); return true; }
      }
    }
    return false;
  }

  // GameObject/Component/Transform 간 형 변환 보정 포함
  static bool IsAssignable(System.Type fieldType, object value, out object boxed)
  {
    boxed = null;
    if (value == null) return false;

    var vType = value.GetType();
    if (fieldType.IsAssignableFrom(vType))
    { boxed = value; return true; }

    if (value is GameObject go)
    {
      if (fieldType == typeof(Transform)) { boxed = go.transform; return true; }
      if (typeof(Component).IsAssignableFrom(fieldType))
      {
        var comp = go.GetComponent(fieldType);
        if (comp) { boxed = comp; return true; }
      }
    }
    else if (value is Component c)
    {
      if (fieldType == typeof(GameObject)) { boxed = c.gameObject; return true; }
      if (fieldType == typeof(Transform)) { boxed = c.transform; return true; }
    }
    return false;
  }
}
