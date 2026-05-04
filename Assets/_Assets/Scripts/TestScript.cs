using UnityEngine;

[ExecuteInEditMode]
public class TestScript : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 10f;
    [SerializeField] float range = 10f;

    [SerializeField] float currentAngle;
    [SerializeField] float targetAngle = 60f;

    void Update()
    {
        currentAngle += rotationSpeed * Time.deltaTime;

        if (currentAngle >= 100f)
            currentAngle = 0f;

        if (Input.GetKeyDown(KeyCode.E))
        {
            IsSuccess();
        }
    }

    private void IsSuccess()
    {
        float distance = Mathf.Abs(currentAngle - targetAngle);
        distance = Mathf.Min(distance, 100f - distance);

        if (distance <= range)
        {
            print("Success");
        }
        else
        {
            print("Lose");
        }
    }
}