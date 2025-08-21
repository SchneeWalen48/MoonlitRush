using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIItemControl : MonoBehaviour
{
    private List<ItemData> itemInventory = new List<ItemData>(); // 보유 아이템 리스트
    private float decisionDelay = 1f; // 사용 판단 간격
    private float lastDecisionTime; //마지막 사용 시간
    private Transform aiTransform;
    

    private void Awake()
    {
        aiTransform = transform;
    }

    private void Update()
    {
        if (itemInventory.Count == 0) return;
        if (Time.time - lastDecisionTime < decisionDelay) return;

        // 리스트 순회하며 조건에 맞으면 사용
        for (int i = 0; i < itemInventory.Count; i++)
        {
            ItemData item = itemInventory[i];
            switch (item.type)
            {
                case ItemType.Booster:
                    if (IsStraightRoad()) { Use(i); break; }
                    //직선 구간에서 사용
                    break;
                case ItemType.Missile:
                    if (HasTargetAhead()) { Use(i); break; }
                    //앞에 상대가 있으면 사용
                    break;
                case ItemType.Shield:
                    if (IsThreatDetected()) { Use(i); break; }
                    //미사일 공격 받을 시 사용(확률 5:5)
                    break;
            }
        }
    }

    // 아이템 획득 (AIItemCollector에서 호출)
    public void PickupItem(ItemData item)
    {
        if (itemInventory.Count >= 2) return; // 최대 2개까지만 보유
        itemInventory.Add(item);
        Debug.Log("AI 아이템 획득: " + item.itemName);
    }

    // 실제 사용 처리
    private void Use(int index)
    {
        ItemData item = itemInventory[index];
        switch (item.type)
        {
            case ItemType.Booster:
                GetComponent<BoosterItem>()?.Activate(item);
                break;
            case ItemType.Missile:
                GetComponent<MissileItem>()?.Activate(item);
                break;
            case ItemType.Shield:
                GetComponent<ShieldItem>()?.Activate(item);
                break;
        }

        itemInventory.RemoveAt(index); // 사용 후 제거
        lastDecisionTime = Time.time;
    }

    private bool IsThreatDetected()
    {
        // 뒤쪽에서 미사일이 있는지 확인
        Collider[] hits = Physics.OverlapSphere(transform.position, 15f); // 반경 15 유닛 탐지
        foreach (var hit in hits)
        {
            if (hit.CompareTag("ItemMissile")) // 미사일 태그
            {
                Vector3 dirToMissile = hit.transform.position - transform.position;
                // 뒤쪽에서 오는 경우만 판단
                if (Vector3.Dot(transform.forward, dirToMissile.normalized) < -0.5f)
                {
                    return Random.value < 0.5f; // 50% 확률로 사용
                }
            }
        }

        return false;
    }

    //직선 구간 판단
    private bool IsStraightRoad()
    {
        Vector3 forward = aiTransform.forward;
        Vector3 projected = Vector3.ProjectOnPlane(forward, Vector3.up);
        return Vector3.Angle(projected, Vector3.forward) < 15f;
    }

    //전방에 상대가 있는지 탐색
    private bool HasTargetAhead()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 30f))
        {
            if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("AIPlayer"))
                return true;
        }
        return false;
    }
}
