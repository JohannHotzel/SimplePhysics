using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleShooter : MonoBehaviour
{
    public Camera cam;
    public Rigidbody projectilePrefab;


    [Header("Projectile")]
    public float speed = 20f;
    public float rotationSpeed = 5f;
    public float mass = 1f;
    public Vector3 scale = Vector3.one;
    public bool randomRotation = true;


    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (!cam || !projectilePrefab) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        Vector3 spawnOffset = new Vector3(0f, -1f, 1f);
        Vector3 spawnPos = cam.transform.TransformPoint(spawnOffset);

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            targetPoint = hit.point;
        else
            targetPoint = ray.origin + ray.direction * 1000f;

        Vector3 direction = (targetPoint - spawnPos).normalized;

        Quaternion rot = randomRotation ? Random.rotation : Quaternion.LookRotation(direction);

        Rigidbody rb = Instantiate(projectilePrefab, spawnPos, rot);
        rb.mass = mass;
        rb.transform.localScale = scale;

        rb.linearVelocity = direction * speed;
        rb.angularVelocity = Random.onUnitSphere * rotationSpeed;
    }
}
