using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControls : FighterAI
{
    Quaternion gyroOffset;
    bool calibrated = false;

    [SerializeField]
    int minSpeed = 0;
    [SerializeField]   
    float speed = 0;

    SpaceshipGun gun;

    // Start is called before the first frame update
    void Start()
    {
        Input.gyro.enabled = true;
        gun = GetComponentInChildren<SpaceshipGun>();
        rb = GetComponent<Rigidbody>();

        speed = minSpeed;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        RotateWithPCControls();
        RotateWithMobileControls();

        rb.velocity = transform.forward.normalized * speed * Time.deltaTime;
    }

    void RotateWithPCControls()
    {
        Vector3 rotation = new Vector3(0, 0, 0);

        if (Input.GetKey(KeyCode.Space))
        {
            speed = Mathf.Lerp(
                rb.velocity.magnitude,
                maxSpeed,
                acceleration * Time.fixedDeltaTime
            );
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed = Mathf.Lerp(
                rb.velocity.magnitude,
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
            gun.Shoot();
        }
    }

    void RotateWithMobileControls()
    {
        Calibrate();

        if (calibrated)
        {
            //Calibration
            Quaternion calibratedGyro = Quaternion.Inverse(gyroOffset) * Input.gyro.attitude;

            //Registration
            Quaternion unityCalibratedGyro = new Quaternion(calibratedGyro.x, calibratedGyro.y, -calibratedGyro.z, -calibratedGyro.w).normalized;

            //Interaction
            this.transform.rotation = Quaternion.Slerp(this.transform.rotation, this.transform.rotation * unityCalibratedGyro, Time.deltaTime * 4);
        }
    }


    void Calibrate()
    {
        //We sensure that we are receiving data from the sensor before calibrate
        if (!calibrated && (Input.gyro.attitude.x != 0 || Input.gyro.attitude.y != 0 || Input.gyro.attitude.z != 0))
        {
            gyroOffset = Input.gyro.attitude;
            calibrated = true;
        }
    }
}
