using UnityEngine;

namespace MotionCore.Infrastructure
{
    [DisallowMultipleComponent]
    public sealed class PooledVfx : MonoBehaviour, IPoolLifecycle
    {
        ParticleSystem[] m_ParticleSystems;

        void Awake()
        {
            m_ParticleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        public void OnPoolRent()
        {
            for (int i = 0; i < m_ParticleSystems.Length; i++)
            {
                ParticleSystem particle = m_ParticleSystems[i];
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Clear(true);
                particle.Play(true);
            }
        }

        public void SetSpeed(float speed)
        {
            for (int i = 0; i < m_ParticleSystems.Length; i++)
            {
                ParticleSystem.MainModule main = m_ParticleSystems[i].main;
                main.simulationSpeed = speed;
            }
        }

        public bool IsAlive()
        {
            for (int i = 0; i < m_ParticleSystems.Length; i++)
            {
                ParticleSystem particle = m_ParticleSystems[i];
                if (particle && particle.IsAlive(true))
                    return true;
            }

            return false;
        }

        public void OnPoolReturn()
        {
            for (int i = 0; i < m_ParticleSystems.Length; i++)
            {
                ParticleSystem particle = m_ParticleSystems[i];
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Clear(true);
            }
        }

    }
}
