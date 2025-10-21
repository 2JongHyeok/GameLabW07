// AsteroidRotation.cs
using UnityEngine;

public class AsteroidRotation : MonoBehaviour
{
    [Tooltip("The rotation speed in degrees per second. A positive value spins clockwise, a negative value spins counter-clockwise.")]
    public float rotationSpeed = 30f; // This value can now be set directly in the Inspector

    void Update()
    {
        // Rotate the GameObject this script is attached to around the Z-axis.
        // Time.deltaTime ensures the rotation is smooth and independent of the frame rate.
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}