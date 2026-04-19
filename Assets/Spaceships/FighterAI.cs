using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FighterAI : MonoBehaviour
{
    public static List<FighterAI> allFighters = new List<FighterAI>();
    public static List<FighterAI>[] teams = new List<FighterAI>[2]{
        new List<FighterAI>(),
        new List<FighterAI>()
    };

    public float maxSpeed = 5.5f;
    public float acceleration = 2;

    public GameObject explosion; 

    // speed is for debugging
    public float speed = 0;

    public Rigidbody rb;
    public Vector3 desiredDirection = Vector3.zero; 
    public SpaceshipShield shield;

    public bool useBoidMovement = true;

    protected void Awake()
    {
        rb = GetComponent<Rigidbody>();
        shield = GetComponentInChildren<SpaceshipShield>();
    }

    public List<FighterAI> GetTeam()
    {
        return teams[gameObject.layer % 2];
    }

    public List<FighterAI> GetEnemyTeam()
    {
        return teams[(gameObject.layer + 1) % 2];
    }

    public bool IsEnemy(GameObject other)
    {
        return gameObject.layer % 2 != other.layer % 2;
    }

    private void OnEnable()
    {
        allFighters.Add(this);
        GetTeam().Add(this);
    }

    private void OnDisable()
    {
        allFighters.Remove(this);
        GetTeam().Remove(this);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!LoadingScreen.IsGameReady())
            return;

        Vector3 newDirection = Vector3.Lerp(rb.velocity.normalized, desiredDirection, acceleration * Time.fixedDeltaTime);
        rb.velocity = newDirection * maxSpeed;

        if (newDirection.sqrMagnitude > 0.001f)
        {
            transform.LookAt(transform.position + newDirection);
        }

        speed = rb.velocity.magnitude;
    }

    public void OnShieldDestroyed()
    {
        Destroy(gameObject);

        if (explosion)
        {
            GameObject explosionInstant = Instantiate(explosion, transform.position, Quaternion.identity);
            AudioSource audioSource = explosionInstant.GetComponent<AudioSource>();
            Destroy(explosionInstant, audioSource.clip.length);
        }
    }
}
