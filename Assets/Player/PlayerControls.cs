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

    [SerializeField]
    int minSpeed = 0;
    public float pitchSpeed = 1;
    public float rollSpeed = 1;
    public float maxTiltAngle = 20f;

    Vector2 initTouchPos; // initial touch position in the began phase
    const float MIN_ANGULAR_THRESHOLD = 0.9f; // min angular correlation between the touch motion and swipe direction (up, down, left, right)
    const float MIN_RELATIVE_DISTANCE = 0.1f; // min realive distance in terms of screen size in which touch motion would be considered a swipe 
    float vMinDistance;
    float vMaxDistance = 300f; // After testing 300 seems to be a reasonable assumption for the longest swipe a user would do to go full speed

    Quaternion gyroNeutralPosition;
    bool calibrated = false;


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
        Input.gyro.enabled = true;
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
                }
            }
        }

        // Wait to prevent the game from proclaiming the player the winner before the enemies even spawned
        timeBeforePlayerCanWin -= Time.fixedDeltaTime;
        if (timeBeforePlayerCanWin < 0 && GetEnemyTeam().Count == 0)
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
            Shoot();
        }
    }

    public void Shoot()
    {
        if (target)
        {
            gun.ShootAt(target);
        }
        else
        {
            gun.Shoot();
        }
    }

    Vector2 GetTiltInput()
    {
        Quaternion raw = Quaternion.Inverse(gyroNeutralPosition) * Input.gyro.attitude;

        // Convert to Unity space
        Quaternion relative = new Quaternion(
            raw.x,
            raw.y,
            -raw.z,
            -raw.w
        );

        Vector3 euler = relative.eulerAngles;

        // Convert to [-180, 180]
        if (euler.x > 180f) { 
            euler.x -= 360f; 
        }
        if (euler.z > 180f) { 
            euler.z -= 360f; 
        }

        float pitch = Mathf.Clamp(euler.x / maxTiltAngle, -1f, 1f);
        float roll = Mathf.Clamp(euler.z / maxTiltAngle, -1f, 1f);

        if (Mathf.Abs(pitch) < 0.05f) { 
            pitch = 0f; 
        }
        if (Mathf.Abs(roll) < 0.05f) { 
            roll = 0f; 
        }

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
        //We sensure that we are receiving data from the sensor before calibrate
        if (!calibrated && (Input.gyro.attitude.x != 0 || Input.gyro.attitude.y != 0 || Input.gyro.attitude.z != 0))
        {
            gyroNeutralPosition = Input.gyro.attitude;
            calibrated = true;
        }
    }
}
