using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int speed = 1000;
    public int damage = 1;
    public GameObject shooter;

    public float maxRange = 1000;
    public FighterAI target = null;

    Rigidbody rb;
    Vector3 startPosition;
    float maxRangeSq;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        maxRangeSq = maxRange * maxRange;
    }

    // Update is called once per frame
    void Update()
    {
        if (target)
        {
            transform.LookAt(target.transform.position);
        }

        rb.velocity = this.transform.forward.normalized * Time.deltaTime * speed;

        if ((transform.position - startPosition).sqrMagnitude > maxRangeSq)
        {
            Destroy(gameObject);
        }
    }

    private bool IsSameTeam(GameObject other)

    {
        // Red teams collision layers are even and blue teams layers are odd.
        return gameObject.layer % 2 == other.layer % 2;
    }

    private void OnTriggerEnter(Collider collider)
    {
        // Ignore collision with the spaceship that shot the projectile
        if (collider.gameObject == shooter || (collider.transform.parent && collider.transform.parent.gameObject == shooter))
        {
            return;
        }

        SpaceshipShield shield = collider.gameObject.GetComponent<SpaceshipShield>();

        if (!shield && collider.transform.parent)
        {
            shield = collider.transform.parent.gameObject.GetComponent<SpaceshipShield>();
        }

        // Only do damage to the shields of enemy ships
        if (shield && !IsSameTeam(collider.gameObject))
        {
            shield.Damage(damage);
        } 
        
        if (!IsSameTeam(collider.gameObject))
        {
            Destroy(gameObject);
        }
    }
}
