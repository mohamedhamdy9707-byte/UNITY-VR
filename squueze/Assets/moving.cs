 using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("إعدادات الحركة")]
    public float moveSpeed = 5f;
    public float fastSpeed = 10f;
    public float rotationSpeed = 2f;

    [Header("إعدادات التحكم")]
    public bool enableMouseLook = true;
    public float lookXLimit = 80f;

    private float rotationX = 0;

    void Update()
    {
        // الحركة الأمامية/الخلفية (W/S)
        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(Vector3.forward * GetCurrentSpeed() * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(Vector3.back * GetCurrentSpeed() * Time.deltaTime);
        }

        // الحركة اليمين/اليسار (D/A)
        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(Vector3.right * GetCurrentSpeed() * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(Vector3.left * GetCurrentSpeed() * Time.deltaTime);
        }

        // الحركة لأعلى/لأسفل (E/Q)
        if (Input.GetKey(KeyCode.E))
        {
            transform.Translate(Vector3.up * GetCurrentSpeed() * Time.deltaTime, Space.World);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Translate(Vector3.down * GetCurrentSpeed() * Time.deltaTime, Space.World);
        }

        // حركة الماوس (النظر حولك)
        if (enableMouseLook)
        {
            rotationX += -Input.GetAxis("Mouse Y") * rotationSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            transform.localRotation = Quaternion.Euler(rotationX, transform.localEulerAngles.y, 0);
            transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * rotationSpeed);
        }
    }

    // دالة للحصول على السرعة الحالية (عادية أو سريعة مع Shift)
    float GetCurrentSpeed()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            return fastSpeed;
        }
        return moveSpeed;
    }
}
