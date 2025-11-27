using UnityEngine;

public class TeacherMovement : MonoBehaviour
{
    public float speed = 2f;          // How fast the teacher moves
    public float moveDistance = 2f;   // How far left/right from the start
    private Vector3 startPos;
    private bool movingRight = true;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Move left and right
        if (movingRight)
        {
            transform.position += Vector3.right * speed * Time.deltaTime;
            if (transform.position.x >= startPos.x + moveDistance)
                movingRight = false;
        }
        else
        {
            transform.position += Vector3.left * speed * Time.deltaTime;
            if (transform.position.x <= startPos.x - moveDistance)
                movingRight = true;
        }
    }
}
