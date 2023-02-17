using System.Collections;
using UnityEngine;
using Unity.Mathematics;

namespace Nie
{
    /// <summary>
    /// Define the material name used when a collision between 2 CollisionFXMaterial happens
    /// </summary>
    [AddComponentMenu("Nie/Object/CollisionFXMaterial")]
    public class CollisionFXMaterial : MonoBehaviour
    {
        [Tooltip("Registry to used when a collision happens")]
        public CollisionFXRegistry Registry;

        [Tooltip("Material name to use when matching this CollisionFXRegistry with another CollisionFXMaterial from the CollisionFXRegistry when a collision happens")]
        public string MaterialName;

        float m_CoolDownTimer = 0;

        void Update()
        {
            if (m_CoolDownTimer > 0)
                m_CoolDownTimer -= Time.deltaTime;
        }

        void Collide(CollisionFXMaterial other, Vector3 position, float relativeVelocity)
        {
            if (relativeVelocity >= Registry.MinimumVelocity)
                if (Registry.TryGetPair(MaterialName, other.MaterialName, out var pair))
                {
                    var obj = Instantiate(pair.Sound.gameObject, position, Quaternion.identity);
                    if (obj.TryGetComponent<SoundFX>(out var soundFX))
                    {
                        soundFX.OneShotDestroy = true;
                        soundFX.PlayOnAwake = true;
                        var volume = Registry.ComputeVolume(relativeVelocity);
                        soundFX.Volume = volume;
                        //Debug.Log($"Collision FX from '{gameObject.name}' velocity={relativeVelocity}");
                    }
                    m_CoolDownTimer = Registry.Cooldown;
                }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!enabled) return;
            if (m_CoolDownTimer > 0) return;
            foreach (var fx in collision.gameObject.GetComponents<CollisionFXMaterial>())
                Collide(fx, collision.GetContact(0).point, collision.relativeVelocity.magnitude);
        }
    }
}