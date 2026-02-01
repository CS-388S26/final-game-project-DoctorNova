using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIEnemyPerception : MonoBehaviour
{
    FighterAI fighter;
    SpaceshipGun gun;

    // Start is called before the first frame update
    void Awake()
    {
        fighter = GetComponentInParent<FighterAI>();
        gun = GetComponentInParent<SpaceshipGun>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (fighter.IsEnemy(other.gameObject))
        {
            gun.ShootAt(other.gameObject.transform.position);
        }
    }
}
