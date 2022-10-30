using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionListenerTest : MonoBehaviour
{
    void OnCollisionEnter(Collision col)
    {
        if (transform.parent.GetComponent<MonoTrain>() != null)
        {
            transform.parent.GetComponent<MonoTrain>().CollisionDetectionEnter(this);
        }
    }

    void OnCollisionExit(Collision col)
    {
        if (transform.parent.GetComponent<MonoTrain>() != null)
        {
            transform.parent.GetComponent<MonoTrain>().CollisionDetectionExit(this);
        }
    }
}
