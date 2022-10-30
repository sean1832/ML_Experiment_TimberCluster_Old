using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionListener : MonoBehaviour
{
    void OnCollisionEnter(Collision col)
    {
        if (transform.parent.GetComponent<Train>() != null)
        {
            transform.parent.GetComponent<Train>().CollisionDetectionEnter(this);
        }
    }

    void OnCollisionExit(Collision col)
    {
        if (transform.parent.GetComponent<Train>() != null)
        {
            transform.parent.GetComponent<Train>().CollisionDetectionExit(this);
        }
    }
}
