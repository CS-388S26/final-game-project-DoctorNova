using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIEnemyPerception : MonoBehaviour
{
    FighterAI fighter;
    SpaceshipGun gun;
    AudioSource audioSource;

    // Start is called before the first frame update
    void Awake()
    {
        fighter = GetComponentInParent<FighterAI>();
        gun = GetComponentInParent<SpaceshipGun>();
        audioSource = GetComponentInParent<AudioSource>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (fighter.IsEnemy(other.gameObject))
        {
            if (gun.ShootAt(other.gameObject.transform.position) && audioSource.enabled)
            {
                audioSource.Play();
            }
        }
    }
}
