using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Об'єкт, який уміє скинути свій стан при поверненні з пулу.
///
/// Пул віддає той самий екземпляр повторно, тож усе, що скрипт міняв за час
/// життя — таймери, прозорість, увімкнені колайдери — треба повернути до
/// початкового. Поля з ініціалізацією (private float t = 1f) виконуються
/// лише раз при створенні, тому на них покладатися не можна.
/// </summary>
public interface IPoolable
{
    void OnSpawnFromPool();
}

/// <summary>
/// Пули для часто створюваних об'єктів: цифри шкоди, кров, ефекти.
///
/// Вішається на об'єкт у сцені, префаби й розміри задаються в інспекторі.
/// У рантаймі створюються лише екземпляри префабів через Instantiate —
/// нових типів об'єктів не з'являється.
///
/// Ключове: якщо реєстру в сцені немає або для префаба не налаштовано пул,
/// Spawn/Despawn поводяться як звичайні Instantiate/Destroy. Тому код
/// безпечно переводити на пул поступово — нічого не ламається, доки
/// префаби не прив'язані.
/// </summary>
public class PoolRegistry : MonoBehaviour
{
    public static PoolRegistry Instance { get; private set; }

    [System.Serializable]
    public class Entry
    {
        public GameObject prefab;

        [Tooltip("Скільки створити наперед, щоб не було ривка на першому пострілі")]
        public int prewarm = 16;

        [Tooltip("Більше за це пул не тримає — зайве знищується")]
        public int maxRetained = 128;
    }

    [Header("Пули")]
    public Entry[] pools;

    private class Pool
    {
        public Entry entry;
        public readonly Stack<GameObject> free = new Stack<GameObject>();
    }

    private readonly Dictionary<GameObject, Pool> poolByPrefab = new Dictionary<GameObject, Pool>();
    private readonly Dictionary<GameObject, Pool> poolByInstance = new Dictionary<GameObject, Pool>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (pools == null) return;

        foreach (var entry in pools)
        {
            if (entry == null || entry.prefab == null) continue;
            if (poolByPrefab.ContainsKey(entry.prefab)) continue;

            var pool = new Pool { entry = entry };
            poolByPrefab[entry.prefab] = pool;

            for (int i = 0; i < entry.prewarm; i++)
            {
                var instance = Instantiate(entry.prefab, transform);
                instance.SetActive(false);
                poolByInstance[instance] = pool;
                pool.free.Push(instance);
            }
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------------

    /// <summary>
    /// Дає екземпляр префаба. Без налаштованого пулу — звичайний Instantiate.
    /// </summary>
    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        var registry = Instance;
        if (registry == null || !registry.poolByPrefab.TryGetValue(prefab, out var pool))
        {
            // Без пулу поводимося так само, тільки без перевикористання.
            // OnSpawnFromPool кличемо й тут, щоб логіка старту об'єкта була
            // однаковою в обох випадках.
            var fresh = Instantiate(prefab, position, rotation);
            NotifySpawned(fresh);
            return fresh;
        }

        GameObject instance = null;

        // У стеку можуть лежати знищені ззовні об'єкти — пропускаємо їх.
        while (pool.free.Count > 0 && instance == null)
            instance = pool.free.Pop();

        if (instance == null)
        {
            instance = Instantiate(prefab, registry.transform);
            registry.poolByInstance[instance] = pool;
        }

        instance.transform.SetParent(registry.transform, false);
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);

        NotifySpawned(instance);

        return instance;
    }

    private static void NotifySpawned(GameObject instance)
    {
        if (instance == null) return;
        foreach (var poolable in instance.GetComponentsInChildren<IPoolable>(true))
            poolable.OnSpawnFromPool();
    }

    /// <summary>
    /// Повертає екземпляр у пул. Без пулу — звичайний Destroy.
    /// </summary>
    public static void Despawn(GameObject instance)
    {
        if (instance == null) return;

        // Захист від подвійного повернення: об'єкт міг despawn-нутись і за
        // таймером, і за влучанням. Без цього той самий екземпляр потрапляв
        // би у стек двічі й потім видавався двом власникам одночасно.
        if (!instance.activeSelf) return;

        var registry = Instance;
        if (registry == null || !registry.poolByInstance.TryGetValue(instance, out var pool))
        {
            Destroy(instance);
            return;
        }

        if (pool.free.Count >= pool.entry.maxRetained)
        {
            registry.poolByInstance.Remove(instance);
            Destroy(instance);
            return;
        }

        instance.SetActive(false);
        instance.transform.SetParent(registry.transform, false);
        pool.free.Push(instance);
    }

    /// <summary>
    /// Повертає екземпляр через заданий час. Без пулу — Destroy із затримкою,
    /// тож об'єкти без власного скрипта (наприклад ефект крові) більше не
    /// лишаються в сцені назавжди.
    /// </summary>
    public static void DespawnAfter(GameObject instance, float delay)
    {
        if (instance == null) return;

        var registry = Instance;
        if (registry == null || !registry.poolByInstance.ContainsKey(instance))
        {
            Destroy(instance, delay);
            return;
        }

        registry.StartCoroutine(registry.DespawnRoutine(instance, delay));
    }

    private IEnumerator DespawnRoutine(GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        Despawn(instance);
    }
}
