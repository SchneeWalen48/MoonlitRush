using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class IntroduceCamera : MonoBehaviour
{
    [Header("Camera References")]
    public Camera cam;                // Main Camera
    public Transform startPos;        // 시작 위치
    public Transform endPos;          // 내려올 위치
    public float introDuration = 3f;  // 내려오는 시간

    [Header("Player Intro Settings")]
    public float focusDuration = 1.5f;    // 한 명 비추는 시간
    public Vector3 lookOffset = new Vector3(0, 2f, 0); // 플레이어 머리 위 포커스
    public float dollyIn = 2.0f;         // 살짝 앞으로 다가가기
    public float dollySpeed = 2.0f;      // 전진 속도

    private List<Transform> racers;
    private bool introFinished = false;

    void Start()
    {
        cam.transform.position = startPos.position;
        cam.transform.rotation = startPos.rotation;

        // 플레이어 목록 태그로 수집
        racers = GameObject.FindGameObjectsWithTag("Player")
            .Select(go => go.transform)
            .ToList();

        // 인트로 시작
        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        // 카메라 내려오기
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / introDuration;
            float s = Mathf.SmoothStep(0, 1, t);

            cam.transform.position = Vector3.Lerp(startPos.position, endPos.position, s);
            cam.transform.rotation = Quaternion.Slerp(startPos.rotation, endPos.rotation, s);

            yield return null;
        }

        // 플레이어 하나씩 비추기
        yield return StartCoroutine(FocusPlayers());

        // 최종 앵글로 복귀
        yield return StartCoroutine(BlendTo(endPos.position, endPos.rotation, 1f));

        // 카운트다운 시작
        //introFinished = true;
        //FindObjectOfType<FinalCount>()?.StartCountdown();
    }

    IEnumerator FocusPlayers()
    {
        foreach (var r in racers)
        {
            Vector3 targetLook = r.position + lookOffset;
            Quaternion targetRot = Quaternion.LookRotation(
                (targetLook - cam.transform.position).normalized, Vector3.up);

            Vector3 originalPos = cam.transform.position;

            float elapsed = 0f;
            while (elapsed < focusDuration)
            {
                elapsed += Time.deltaTime;

                // 카메라 회전 → 플레이어 쪽 바라보기
                cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, targetRot, Time.deltaTime * 3f);

                // 카메라 살짝 전진
                cam.transform.position = Vector3.MoveTowards(
                    cam.transform.position,
                    originalPos + (cam.transform.forward * dollyIn),
                    Time.deltaTime * dollySpeed
                );

                yield return null;
            }

            // 원래 위치로 복귀
            cam.transform.position = Vector3.Lerp(cam.transform.position, originalPos, 0.6f);
        }
    }

    IEnumerator BlendTo(Vector3 pos, Quaternion rot, float dur)
    {
        Vector3 sp = cam.transform.position;
        Quaternion sr = cam.transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float s = Mathf.SmoothStep(0, 1, t);

            cam.transform.position = Vector3.Lerp(sp, pos, s);
            cam.transform.rotation = Quaternion.Slerp(sr, rot, s);

            yield return null;
        }
    }
}
