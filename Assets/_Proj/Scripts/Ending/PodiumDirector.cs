using UnityEngine;
using TMPro;

public class PodiumDirector : MonoBehaviour
{
    [System.Serializable]
    public class PodiumSlot
    {
        public SpriteRenderer basePodium;
        public Transform root;
        public TMP_Text nameText;
        
    }

    public PodiumSlot[] slots;
    public GameObject confettiEffect;

    void Start()
    {
        ShowPodium();
    }

    public void ShowPodium()
    {
        var ranking = TimeManager.Instance.GetRanking();
        for (int i = 0; i < slots.Length; i++)
        {
            if (i >= ranking.Count) continue;

            var data = ranking[i];
            slots[i].nameText.text = data.playerName;
            

            StartCoroutine(RiseUp(slots[i].root));
        }
        if (confettiEffect != null)
            confettiEffect.SetActive(true);
    }
    private System.Collections.IEnumerator RiseUp(Transform t)
    {
        Vector3 starPos = t.localPosition - new Vector3(0, 200f, 0);
        Vector3 endPos = t.localPosition;
        float duration = 0.8f;
        float time = 0f;

        t.localPosition = starPos;

        while (time < duration)
        {
            t.localPosition = Vector3.Lerp(starPos, endPos, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        t.localPosition = endPos;
    }
}
