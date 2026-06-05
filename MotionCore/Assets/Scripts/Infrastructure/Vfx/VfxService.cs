using System;
using System.Collections.Generic;
using UnityEngine;

namespace MotionCore.Infrastructure
{
    public sealed class VfxService : IVfxService, IDisposable
    {
        readonly IAssetProvider m_Assets;
        readonly ITimerService m_Timer;
        readonly Transform m_PoolRoot;
        readonly Dictionary<string, PrefabPool<PooledVfx>> m_Pools = new();
        readonly Dictionary<PooledVfx, TimerHandle> m_ReleaseTimers = new();

        public VfxService(IAssetProvider assets, ITimerService timer, Transform poolRoot)
        {
            m_Assets = assets;
            m_Timer = timer;
            m_PoolRoot = poolRoot;
        }

        public PooledVfx Play(in VfxSpawnRequest request)
        {
            Transform parent = request.Parent ? request.Parent : m_PoolRoot;
            if (request.ReuseMode == VfxReuseMode.OneShot)
                return PlayOneShot(request, parent);

            return PlayPooled(request, parent);
        }

        public void Dispose()
        {
            foreach (KeyValuePair<string, PrefabPool<PooledVfx>> pair in m_Pools)
                pair.Value.Dispose();

            m_Pools.Clear();
            m_ReleaseTimers.Clear();
        }

        PooledVfx PlayPooled(in VfxSpawnRequest request, Transform parent)
        {
            PrefabPool<PooledVfx> pool = GetOrCreatePool(request);
            PooledVfx instance = pool.Get(parent);

            ApplyRequest(instance, request);

            TimerHandle timer = GetTimerHandle(instance);
            m_Timer.Delay(instance, request.ReleaseDelay, timer, () => pool.Release(instance));
            return instance;
        }

        PooledVfx PlayOneShot(in VfxSpawnRequest request, Transform parent)
        {
            PooledVfx prefab = m_Assets.LoadComponent<PooledVfx>(request.AssetKey);
            PooledVfx instance = UnityEngine.Object.Instantiate(prefab, parent, false);

            instance.gameObject.SetActive(false);
            ApplyRequest(instance, request);
            instance.gameObject.SetActive(true);
            instance.OnPoolRent();

            TimerHandle timer = new TimerHandle();
            m_Timer.Delay(instance, request.ReleaseDelay, timer, () => DestroyOneShot(instance));
            return instance;
        }

        static void ApplyRequest(PooledVfx instance, in VfxSpawnRequest request)
        {
            instance.transform.SetPositionAndRotation(request.Position, request.Rotation);
            instance.transform.localScale = Vector3.one * request.Scale;
            instance.SetSpeed(request.Speed);
        }

        static void DestroyOneShot(PooledVfx instance)
        {
            UnityEngine.Object.Destroy(instance.gameObject);
        }

        PrefabPool<PooledVfx> GetOrCreatePool(in VfxSpawnRequest request)
        {
            if (m_Pools.TryGetValue(request.AssetKey, out PrefabPool<PooledVfx> pool))
                return pool;

            pool = new PrefabPool<PooledVfx>(
                m_Assets,
                request.AssetKey,
                m_PoolRoot,
                request.InitialCapacity,
                request.MinCachedCount);
            pool.Prewarm();
            m_Pools.Add(request.AssetKey, pool);
            return pool;
        }

        TimerHandle GetTimerHandle(PooledVfx instance)
        {
            if (m_ReleaseTimers.TryGetValue(instance, out TimerHandle handle))
                return handle;

            handle = new TimerHandle();
            m_ReleaseTimers.Add(instance, handle);
            return handle;
        }
    }
}
