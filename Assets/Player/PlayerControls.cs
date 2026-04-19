using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControls : FighterAI
{
    public HUD hud;
    float timeBeforePlayerCanWin = 10f;

    public FighterAI target;
    public SpaceshipGun gun;

    public AudioClip audioClipHeavyGun;
    public AudioClip audioClipGun;
    public List<AudioSource> audioSources = new List<AudioSource>();
    int currentAudioSource = 0;

    public int minSpeed = 0;
    public float pitchSpeed = 1;
    public float rollSpeed = 1;
    public float maxTiltAngle = 20f;

    Vector2 initTouchPos; // initial touch position in the began phase
    const float MIN_ANGULAR_THRESHOLD = 0.9f; // min angular correlation between the touch motion and swipe direction (up, down, left, right)
    const float MIN_RELATIVE_DISTANCE = 0.1f; // min realive distance in terms of screen size in which touch motion would be considered a swipe 
    float vMinDistance;
    float vMaxDistance = 300f; // After testing 300 seems to be a reasonable assumption for the longest swipe a user would do to go full speed
    float touchBeginTime;

    Vector3 accelNeutral;   // normalized gravity direction at neutral
    bool calibrated = false;
    Vector3 previousAcceleration;

    // Optional: default neutral tilt (45° around X axis)
    [SerializeField] float defaultNeutralAngle = 45f;


    protected new void Awake()
    {
        base.Awake();
        /* We always need to normalize the min required distance in terms of the screen size. In this example, 
         * we are considering around a 10 percent of the screen size as an amount enough to consider the motion as a valid swipe  
         */
        vMinDistance = MIN_RELATIVE_DISTANCE * Screen.height;  //stores the min relative distance in pixels for the vertical axis 
    }

    // Start is called before the first frame update
    void Start()
    {
        Vector3 baseDown = Vector3.down;
        Quaternion tilt = Quaternion.Euler(defaultNeutralAngle, 0, 0);
        accelNeutral = (tilt * baseDown).normalized;
        calibrated = true;

        gun = GetComponentInChildren<SpaceshipGun>();

        speed = minSpeed + (maxSpeed - minSpeed) / 2;
    }

    private void Update()
    {
        // If the game is not ready don't read user input
        if (!LoadingScreen.IsGameReady())
            return;

        foreach (Touch touch in Input.touches)
        {
            if (touch.phase == TouchPhase.Began)
            {
                initTouchPos = touch.position; // stores the initial touch position in the began phase
                touchBeginTime = Time.time;
            }

            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Ended)
            {
                Vector2 direction = (touch.position - initTouchPos).normalized;   // calculates the direction of motion between actual position and initialPos           
                float distance = Vector2.Distance(initTouchPos, touch.position);    // calculate the distance in screen coordinates 

                if (distance > vMinDistance)  //evaluates if the motion/distance is enought to be considered a swipe (min 10% of the screen)
                {
                    // evaluates if direction of motion is highly correlated with the up axis
                    if (Vector2.Dot(direction, Vector2.up) > MIN_ANGULAR_THRESHOLD)
                    {
                        float increase = Mathf.Clamp(distance / vMaxDistance, 0, 1);
                        speed = increase * maxSpeed;
                    }

                    else if (Vector2.Dot(direction, Vector2.down) > MIN_ANGULAR_THRESHOLD) // evaluates if direction of motion is highly correlated with the down axis
                    {
                        float decrease = 1 - Mathf.Clamp(distance / vMaxDistance, 0, 1);
                        speed = decrease * maxSpeed + minSpeed;
                    }
                } else if (touch.phase == TouchPhase.Ended)
                {
                    float timeSincePress = Time.time - touchBeginTime;
                    bool useHeavyProjectile = Input.touchPressureSupported && touch.pressure > 1.1f || timeSincePress > 0.5f;
                    Shoot(useHeavyProjectile);
                }
            }
        }

        // Wait to prevent the game from proclaiming the player the winner before the enemies even spawned
        timeBeforePlayerCanWin -= Time.fixedDeltaTime;
        if (hud && timeBeforePlayerCanWin < 0 && GetEnemyTeam().Count == 0)
        {
            hud.Won();
        }

        RotateWithPCControls();
        RotateWithMobileControls();
    }

    // FixedUpdate for physics and transform updates
    void FixedUpdate()
    {
        // If the game is not ready don't read user input
        if (!LoadingScreen.IsGameReady())
            return;



        rb.velocity = transform.forward.normalized * speed * Time.fixedDeltaTime;
    }

    public new void OnShieldDestroyed()
    {
        hud.Defeat();
    }

    private float EaseInput(float x)
    {
        return x * x * (x < 0 ? -1.0f: 1.0f);
    }

    void RotateWithPCControls()
    {
        Vector3 rotation = new Vector3(0, 0, 0);

        if (Input.GetKey(KeyCode.Space))
        {
            speed = Mathf.Lerp(
                speed,
                maxSpeed,
                acceleration * Time.fixedDeltaTime
            );
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed = Mathf.Lerp(
                speed,
                minSpeed,
                acceleration * Time.fixedDeltaTime
            );
        }

        const int rotationSpeed = 10;
        if (Input.GetKey(KeyCode.W))
        {
            rotation.x -= rotationSpeed;
        }
        if (Input.GetKey(KeyCode.S))
        {
            rotation.x += rotationSpeed;
        }
        if (Input.GetKey(KeyCode.A))
        {
            rotation.z += rotationSpeed;
        }
        if (Input.GetKey(KeyCode.D))
        {
            rotation.z -= rotationSpeed;
        }
        if (rotation.sqrMagnitude > 0)
        {
            Quaternion keyboardRotation = Quaternion.Euler(rotation);
            this.transform.rotation = Quaternion.Slerp(this.transform.rotation, this.transform.rotation * keyboardRotation, Time.deltaTime * 4);
        }

        if (Input.GetKey(KeyCode.Q))
        {
            Shoot(false);
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            Shoot(true);
        }
    }

    public void Shoot(bool useHeavyProjectile)
    {
        bool shot = false;

        if (target)
        {
            shot = gun.ShootAt(target, useHeavyProjectile);
        }
        else
        {
            shot = gun.Shoot(useHeavyProjectile).Count > 0;
        }

        if (shot)
        {
            audioSources[currentAudioSource].Stop();
            audioSources[currentAudioSource].clip = useHeavyProjectile ? audioClipHeavyGun : audioClipGun;
            audioSources[currentAudioSource].Play();
            currentAudioSource = ++currentAudioSource % audioSources.Count;
        }
    }

    Vector2 GetTiltInput()
    {
        Vector3 g = Input.acceleration.normalized;

        g = Vector3.Lerp(previousAcceleration, g, 0.1f);

        // Relative rotation from neutral -> current
        Quaternion delta = Quaternion.FromToRotation(accelNeutral, g);

        Vector3 euler = delta.eulerAngles;

        // Convert to [-180, 180]
        if (euler.x > 180f) euler.x -= 360f;
        if (euler.z > 180f) euler.z -= 360f;

        float pitch = Mathf.Clamp(euler.x / maxTiltAngle, -1f, 1f);
        float roll = Mathf.Clamp(-euler.z / maxTiltAngle, -1f, 1f);

        pitch = EaseInput(pitch);
        roll = EaseInput(roll);

        previousAcceleration = g;

        return new Vector2(pitch, roll);
    }


    void RotateWithMobileControls()
    {
        Calibrate();

        if (calibrated)
        {
            Vector2 tilt = GetTiltInput();

            float pitchRate = tilt.x * pitchSpeed;
            float rollRate = tilt.y * rollSpeed;

            Quaternion delta =
                Quaternion.AngleAxis(pitchRate * Time.deltaTime, transform.right) *
                Quaternion.AngleAxis(rollRate * Time.deltaTime, transform.forward);

            transform.rotation = delta * transform.rotation;
        }
    }

    public void Recalibrate()
    {
        calibrated = false;
        Calibrate();
    }

    void Calibrate()
    {
        Vector3 g = Input.acceleration;

        if (!calibrated && g.sqrMagnitude > 0.01f)
        {
            accelNeutral = g.normalized;
            previousAcceleration = accelNeutral;
            calibrated = true;
        }
    }
}
