using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerListener : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        GameObject AssetLayer = GetAssetLayer();
        if (AssetLayer.GetComponent<Train>() != null)
        {
            AssetLayer.GetComponent<Train>().TriggerDetectionEnter(this);
        }
    }

    GameObject GetAssetLayer()
    {
        return GameObject.Find("Assets");
    }
}
