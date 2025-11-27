using UnityEngine;

public class TeacherEnter : MonoBehaviour
{
    public float speed = 3f;
    public Transform targetPosition; // Empty GameObject in scene marking where teacher stops
    private bool moving = true;

    void Update()
    {
        if (moving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition.position, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition.position) < 0.05f)
            {
                moving = false;
            }
        }
    }
}
