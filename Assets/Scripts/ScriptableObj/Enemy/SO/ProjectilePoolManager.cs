using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 인터셉터와 같은 발사체 오브젝트를 관리하는 싱글턴 풀 매니저입니다.
/// </summary>
public class ProjectilePoolManager : MonoBehaviour
{
    public static ProjectilePoolManager Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private GameObject interceptorPrefab;
    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxSize = 100;

    public IObjectPool<GameObject> InterceptorPool { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePool();
    }

    private void InitializePool()
    {
        InterceptorPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(interceptorPrefab, transform),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }
}