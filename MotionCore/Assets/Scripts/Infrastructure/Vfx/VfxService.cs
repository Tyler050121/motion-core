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

        public PooledVfx Play(VfxPreset preset, in VfxSpawnRequest request)
        {
            if (preset == null || !preset.IsValid)
                return null;

            Transform parent = request.Parent ? request.Parent : m_PoolRoot;
            if (preset.ReuseMode == VfxReuseMode.OneShot)
                return PlayOneShot(preset, request, parent);

            return PlayPooled(preset, request, parent);
        }

        public void Dispose()
        {
            foreach (KeyValuePair<string, PrefabPool<PooledVfx>> pair in m_Pools)
                pair.Value.Dispose();

            m_Pools.Clear();
            m_ReleaseTimers.Clear();
        }

        PooledVfx PlayPooled(VfxPreset preset, in VfxSpawnRequest request, Transform parent)
        {
            PrefabPool<PooledVfx> pool = GetOrCreatePool(preset);
            PooledVfx instance = pool.Get(parent);

            ApplyRequest(instance, request);
            ScheduleRelease(instance, preset.ReleaseDelay, () => pool.Release(instance));
            return instance;
        }

        PooledVfx PlayOneShot(VfxPreset preset, in VfxSpawnRequest request, Transform parent)
        {
            PooledVfx prefab = m_Assets.LoadComponent<PooledVfx>(preset.AssetKey);
            PooledVfx instance = UnityEngine.Object.Instantiate(prefab, parent, false);

            instance.gameObject.SetActive(false);
            ApplyRequest(instance, request);
            instance.gameObject.SetActive(true);
            instance.OnPoolRent();

            ScheduleRelease(instance, preset.ReleaseDelay, () => DestroyOneShot(instance));
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

        void ScheduleRelease(PooledVfx instance, float releaseDelay, Action release)
        {
            TimerHandle timer = GetTimerHandle(instance);
            m_Timer.Every(instance, 0.05f, timer, () =>
            {
                if (instance && instance.IsAlive())
                    return;

                m_Timer.Remove(timer);
                if (releaseDelay <= 0f)
                {
                    release();
                    return;
                }

                m_Timer.Delay(instance, releaseDelay, timer, release);
            });
        }

        PrefabPool<PooledVfx> GetOrCreatePool(VfxPreset preset)
        {
            string assetKey = preset.AssetKey;
            if (m_Pools.TryGetValue(assetKey, out PrefabPool<PooledVfx> pool))
                return pool;

            pool = new PrefabPool<PooledVfx>(
                m_Assets,
                assetKey,
                m_PoolRoot,
                preset.InitialCapacity,
                preset.MinCachedCount);
            pool.Prewarm();
            m_Pools.Add(assetKey, pool);
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
