using System.Collections;
using UnityEngine;

public class AIItemCollector : MonoBehaviour
{
    public AIItemControl itemControl; // 아이템 관리 스크립트 연결
    public ItemData defaultBooster;
    public ItemData defaultShield;
    public ItemData defaultMissile;
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("ItemBooster") && !other.CompareTag("ItemShield") && !other.CompareTag("ItemMissile"))
            return;

        ItemBox box = other.GetComponent<ItemBox>();
        if (box == null) return;

        ItemData dataToUse = box.itemData;
        if (dataToUse == null)
        {
            // 기본값 처리
            if (other.CompareTag("ItemBooster")) dataToUse = defaultBooster;
            else if (other.CompareTag("ItemShield")) dataToUse = defaultShield;
            else if (other.CompareTag("ItemMissile")) dataToUse = defaultMissile;
        }

        // AIItemControl에 등록
        if (itemControl != null && dataToUse != null)
        {
            itemControl.PickupItem(dataToUse);
        }

        // 박스 비활성화 & 리스폰
        StartCoroutine(BoxRespawnCoroutine(other.gameObject, box));
    }

    IEnumerator BoxRespawnCoroutine(GameObject box, ItemBox pick)
    {
        Vector3 pos = box.transform.position;
        Quaternion rot = box.transform.rotation;

        box.SetActive(false);
        yield return new WaitForSeconds(pick.respawnDelay);

        pick.ResetVisual();
        box.transform.position = pos;
        box.transform.rotation = rot;
        box.SetActive(true);
    }
}
