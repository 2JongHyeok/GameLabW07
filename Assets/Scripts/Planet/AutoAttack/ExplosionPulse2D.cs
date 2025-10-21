using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ExplosionPulse2D : MonoBehaviour
{
    [Header("필수: 원형 스프라이트")]
    public Sprite circleSprite;                 // 비워두면 런타임 생성 실패 → 인스펙터에서 지정

    [Header("기본 파라미터")]
    public float startRadius = 0.2f;            // 시작 반지름(월드 단위)
    public float endRadius = 1.2f;            // 종료 반지름(월드 단위)
    public float duration = 0.25f;           // 전체 시간(초)
    public Color color = new Color(1f, 0.75f, 0.3f, 1f); // 시작 색(알파 포함)
    public int sortingOrder = 2000;            // 다른 스프라이트 위에 보이도록

    // 풀링 사용 시, SetActive(false)로 반환만 할 지 결정
    public bool destroyOnEnd = true;

    private SpriteRenderer sr;
    private MaterialPropertyBlock mpb;
    private Coroutine playCo;

    void Awake()
    {
        sr = gameObject.GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        if (mpb == null) mpb = new MaterialPropertyBlock();

        if (circleSprite != null)
        {
            sr.sprite = circleSprite;
        }
        sr.sortingOrder = sortingOrder;
        sr.drawMode = SpriteDrawMode.Simple; // 단순 1장
        sr.sharedMaterial = sr.sharedMaterial ?? new Material(Shader.Find("Sprites/Default"));
    }

    public void Play(Vector3 position)
    {
        transform.position = position;

        if (playCo != null) StopCoroutine(playCo);
        playCo = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        // 스프라이트 크기(지름) 보정: 스프라이트의 반지름 = bounds.extents.x (정사각 기준)
        float spriteRadius = Mathf.Max(0.0001f, sr.sprite.bounds.extents.x);
        // 원하는 월드 반지름 / 스프라이트 반지름 = 필요한 스케일
        float startScale = startRadius / spriteRadius;
        float endScale = endRadius / spriteRadius;

        float t = 0f;
        Color c0 = color;

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            // 살짝 빠르게 퍼지고 느리게 끝나는 이징
            float ease = 1f - Mathf.Pow(1f - u, 2f);

            float scale = Mathf.Lerp(startScale, endScale, ease);
            transform.localScale = new Vector3(scale, scale, 1f);

            var c = c0; c.a = Mathf.Lerp(c0.a, 0f, u);
            sr.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", c);
            sr.SetPropertyBlock(mpb);

            yield return null;
        }

        // 종료 정리
        if (destroyOnEnd) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}
