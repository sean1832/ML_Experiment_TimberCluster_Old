using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


public class Connection : MonoBehaviour
{
    public string CurrentObjName;
    public float JointRange = 0.3f;
    

    private GameObject _bindLayer;

    private bool _isCollided = false;

    private Dictionary<string, GameObject> _objects;
    private Dictionary<GameObject, List<GameObject>> _objectsPts;

    // Start is called before the first frame update
    void Start()
    {
        _bindLayer = GameObject.Find("Bind");
        _objects = new Dictionary<string, GameObject>();
        _objectsPts = new Dictionary<GameObject, List<GameObject>>();

        AssignToDict();
    }

    // Update is called once per frame
    void Update()
    {
        Connect(_isCollided, CurrentObjName, _objects, _objectsPts, _bindLayer);
        // Connect(_isCollided);
    }

    void AssignToDict()
    {
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            if (gameObject.transform.GetChild(i).gameObject == _bindLayer) continue;
            GameObject item = gameObject.transform.GetChild(i).gameObject;
            _objects.Add(item.name, item);
        }

        List<GameObject> parents = DictToList(_objects).Vals;

        foreach (GameObject parent in parents)
        {
            List<GameObject> children = new List<GameObject>();
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                GameObject child = parent.transform.GetChild(i).gameObject;
                children.Add(child);
            }
            _objectsPts.Add(parent, children);
        }
    }

    (List<T1> keys, List<T2> Vals) DictToList<T1,T2>(Dictionary<T1,T2> dictionary)
    {
        List<T2> vals = new List<T2>();
        List<T1> keys = dictionary.Keys.ToList();

        foreach (T1 key in keys)
        {
            T2 item = dictionary[key];
            vals.Add(item);
        }
        return (keys, vals);
    }


    GameObject GetChildPoint(string parentName, string childName)
    {
        GameObject parent = GameObject.Find(parentName);
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            if (parent.transform.GetChild(i).name == childName)
            {
                return parent.transform.GetChild(i).gameObject;
            }
        }
        return null;
    }


    Vector3 GetPosition(GameObject obj)
    {
        Vector3 pos = obj.transform.position;
        return pos;
    }

    float GetDistance(GameObject obj1, GameObject obj2)
    {
        Vector3 pos1 = GetPosition(obj1);
        Vector3 pos2 = GetPosition(obj2);

        float dis = (pos1 - pos2).magnitude;
        return dis;
    }

    (float minDistance, GameObject minParent) GetDistance(GameObject obj, List<List<GameObject>> ptList)
    {
        List<float> distances = new List<float>();
        List<GameObject> distanceObject = new List<GameObject>();
        List<GameObject> currentPts = new List<GameObject>();
        List<GameObject> otherPts = new List<GameObject>();


        for (int i = 0; i < obj.transform.childCount; i++)
        {
            GameObject pt = obj.transform.GetChild(i).gameObject;
            currentPts.Add(pt);
        }

        // remove current objects points from list
        foreach (List<GameObject> ptLs in ptList)
        {
            foreach (GameObject pt in ptLs)
            {
                if (pt.transform.parent.name == obj.name) continue;
                otherPts.Add(pt);
            }
        }

        foreach (GameObject pt in currentPts)
        {
            Vector3 pos1 = GetPosition(pt);

            foreach (GameObject otherPt in otherPts)
            {
                Vector3 pos2 = GetPosition(otherPt);

                float distance = (pos1 - pos2).magnitude;

                GameObject parent = otherPt.transform.parent.gameObject;

                distanceObject.Add(parent);
                distances.Add(distance);
            }
        }
        float minDistance = distances.Min();
        int idx = distances.IndexOf(minDistance);

        GameObject minParent = distanceObject[idx];

        return (minDistance, minParent);
    }


    void Bind(GameObject parent, GameObject child)
    {
        child.transform.SetParent(parent.transform);
    }


    void Connect(bool isCollided, string currentObjName, Dictionary<string, GameObject> objects, Dictionary<GameObject, List<GameObject>> objectPts, GameObject bindLayer)
    {
        List<GameObject> objOriginalList = DictToList(objects).Vals;
        List<GameObject> otherObjects = new List<GameObject>();
        List<List<GameObject>> lObjectsPts = DictToList(objectPts).Vals;

        foreach (GameObject i in objOriginalList)
        {
            if (i.name == currentObjName) continue;
            otherObjects.Add(i);
        }
        print(otherObjects.Count);
        foreach (GameObject obj in otherObjects)
        {
            (float minDistance, GameObject minParent) distanceAttribute = GetDistance(obj, lObjectsPts);

            // debug
            print(distanceAttribute.minDistance);
            // debug

            if (distanceAttribute.minDistance <= JointRange && isCollided == false)
            {
                Bind(bindLayer, distanceAttribute.minParent);
                Bind(bindLayer, obj);
            }
        }
    }


    // real time detection of collision
    public void CollisionDetectionEnter(CollisionListenerTest listenerTest)
    {
        _isCollided = true;
    }

    public void CollisionDetectionExit(CollisionListenerTest listenerTest)
    {
        _isCollided = false;
    }
}
