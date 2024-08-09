using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UAV : MonoBehaviour
{
    public float thrust = 10f; // Thrust for moving up and down
    public float moveSpeed = 5f; // Speed for moving forward, backward, left, right
    public float smoothing = 0.1f; // Smoothing factor for velocity
    private Rigidbody rb;

    private Vector3 targetVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Disable gravity to make the UAV float
    }

    void Update()
    {
        // Reset target velocity
        targetVelocity = Vector3.zero;

        // Control for upward and downward movement
        if (Input.GetKey(KeyCode.Space))
        {
            targetVelocity += Vector3.up * thrust;
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            targetVelocity += Vector3.down * thrust;
        }

        // Control for forward, backward, left, and right movement
        if (Input.GetKey(KeyCode.W))
        {
            targetVelocity += transform.forward * moveSpeed;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            targetVelocity += -transform.forward * moveSpeed;
        }

        if (Input.GetKey(KeyCode.A))
        {
            targetVelocity += -transform.right * moveSpeed;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            targetVelocity += transform.right * moveSpeed;
        }
    }

    void FixedUpdate()
    {
        // Smoothly apply the target velocity to the Rigidbody
        rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, smoothing);
    }
}
