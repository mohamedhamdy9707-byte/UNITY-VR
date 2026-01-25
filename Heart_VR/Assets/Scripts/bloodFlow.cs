using UnityEngine;

public class BloodToExit : MonoBehaviour
{
    public Transform[] exits; // ???????
    public float speed = 1f;
    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void LateUpdate()
    {
        int count = ps.particleCount;
        if (particles == null || particles.Length < count)
            particles = new ParticleSystem.Particle[count];
        ps.GetParticles(particles);

        for (int i = 0; i < count; i++)
        {
            Transform target = exits[0]; // ?? ???? ???? ????? ????? ??? ????
            float minDist = Vector3.Distance(particles[i].position, exits[0].position);

            // ????? ???? ????
            foreach (var e in exits)
            {
                float dist = Vector3.Distance(particles[i].position, e.position);
                if (dist < minDist)
                {
                    target = e;
                    minDist = dist;
                }
            }

            Vector3 dir = (target.position - particles[i].position).normalized;
            particles[i].position += dir * speed * Time.deltaTime;

            // optional: kill particle if close enough
            if (minDist < 0.05f)
                particles[i].remainingLifetime = 0;
        }

        ps.SetParticles(particles, count);
    }
}