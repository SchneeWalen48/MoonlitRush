using System.Collections;
using System.Reflection;
using UnityEngine;
using Cinemachine;

// 연결:
// - ItemCollector.ItemSlots ← Canvas/ItemSlots(ItemSlot 컴포넌트)
// - UseItem.slotUI         ← Canvas/ItemSlots/Slot0/Slot0Icon(SlotUI 컴포넌트)
// - VCam.Follow            ← 차 루트
//   VCam.LookAt            ← 차 첫 번째 자식(없으면 루트)
// - Dashboard.car          ← 자동차의 CarController (없으면 GameObject로도 시도)
// - Canvas/ItemSlots.useItem ← 자동차의 UseItem

public class SceneBinder : MonoBehaviour
{
  [Header("Scene refs (Hierarchy에서 드래그)")]
  public CarSpawn carSpawn;                 // 씬의 CarSpawn
  public CinemachineVirtualCamera vcam;     // Hierarchy의 Virtual Camera
  public ItemSlot canvasItemSlots;          // Canvas/ItemSlots (ItemSlot)
  public SlotUI slot0IconUI;                // Canvas/ItemSlots/Slot0/Slot0Icon (SlotUI)
  public Dashboard dashboard;               // Canvas/Dashboard (Dashboard)

  private IEnumerator Start()
  {
    if (!carSpawn) carSpawn = FindObjectOfType<CarSpawn>(true);
    // Wait a moment for the car to spawn
    while (!carSpawn || !carSpawn.lastSpawned) yield return null;

    var car = carSpawn.lastSpawned;

    // --- Cam Binding ---
    if (vcam)
    {
      vcam.Follow = car.transform;
      vcam.LookAt = car.transform.childCount > 0 ? car.transform.GetChild(0) : car.transform;
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

    // (5) Canvas ItemSlot.useItem ← car.UseItem
    AssignByNameThenType(canvasItemSlots, new[] { "useItem", "UseItem" }, useItem);

    Debug.Log("[SceneAutoWire] Wiring complete.");
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
