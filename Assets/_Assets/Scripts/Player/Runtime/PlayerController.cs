using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float acceleration = 10f;
    public float deceleration = 10f;

    public Rigidbody2D rb;

    private Vector2 currentVelocity = Vector2.zero;
    private Vector2 inputDirection = Vector2.zero;

    private bool HasLocalControl
    {
        get
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return true;

            return IsOwner;
        }
    }

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    public void Move(float inputX, float inputY)
    {
        inputDirection = new Vector2(inputX, inputY).normalized;

        Vector2 targetVelocity = inputDirection * speed;

        if (inputDirection != Vector2.zero)
        {
            currentVelocity = Vector2.MoveTowards(
                currentVelocity,
                targetVelocity,
                acceleration * Time.deltaTime
            );
        }
        else
        {
            currentVelocity = Vector2.MoveTowards(
                currentVelocity,
                Vector2.zero,
                deceleration * Time.deltaTime
            );
        }
    }

    private void FixedUpdate()
    {
        if (!HasLocalControl)
            return;

        rb.velocity = currentVelocity;
    }
}