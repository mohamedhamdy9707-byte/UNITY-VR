using UnityEngine;

public class ParticleCollisionFix : MonoBehaviour
{
    void OnParticleCollision(GameObject other)
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.main.maxParticles];
        int count = ps.GetParticles(particles);

        for (int i = 0; i < count; i++)
        {
            // ??? ??????? ????? ????? ?? ?? ?????
            Vector3 vel = particles[i].velocity;
            vel = new Vector3(vel.x * 0.2f, vel.y, vel.z);
            particles[i].velocity = vel;
        }

        ps.SetParticles(particles, count);
    }
}
