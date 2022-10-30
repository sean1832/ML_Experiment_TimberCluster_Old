using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class Joints : MonoBehaviour
{
    // Fields
    public GameObject JointedLayer;
    public float JointRange;

    // ======================================== Public Functions ========================================
    /// <summary>
    /// Connect asset's joints
    /// </summary>
    /// <param name="currentAsset">current moving asset</param>
    /// <param name="assets">all existing assets</param>
    /// <param name="pts">all joints of each existing assets</param>
    /// <param name="isCollided">is assets collided with each other</param>
    /// <returns>(is Connect function triggered, game object connected with)</returns>
    public (bool isConnected, GameObject ConnectWith) Connect(GameObject currentAsset, List<GameObject> assets, List<List<GameObject>> pts, bool isCollided)
    {
        // if current asset is the first asset, isFirstAsset is true, otherwise false
        bool isFirstAsset = currentAsset == assets[0];

        // get jointedLayer's joint points
        List<List<GameObject>> jointedAssetPt = GetJointed(JointedLayer).assetPts;
        // append joint points in pts
        pts.AddRange(jointedAssetPt);

        // setup distance property
        (List<float> distances, float minDistance, GameObject minParent) distProp = GetDistance(currentAsset, assets);
        
        // if current agent is the first asset...
        if (isFirstAsset)
        {
            // if current agent distance is less or equal to joint range, and it is not collided with any geometry...
            if (distProp.minDistance <= JointRange && isCollided == false)
            {
                // connect current agent and move to JointedLayer
                Bind(JointedLayer, currentAsset);
                // connect asset with smallest distance to current agent and move to JointedLayer
                Bind(JointedLayer, distProp.minParent);
                // signal connection is true
                return (true, distProp.minParent);
            }
        }
        // otherwise...
        else
        {
            // if current agent distance is less or equal to joint range, and it is not collided with any geometry, and the target geometry's parent is JointedLayer...
            if (distProp.minDistance <= JointRange && isCollided == false && distProp.minParent.transform.parent.gameObject == JointedLayer)
            {
                // connect current agent and move to JointedLayer
                Bind(JointedLayer, currentAsset);
                // signal connection is true
                return (true, null);
            }
        }
        // signal connection is false
        return (false, null);
    }

    /// <summary>
    /// populate data to lists
    /// </summary>
    /// <param name="host">host gameObject that the script attach to</param>
    /// <returns>(assets data, assets joint data)</returns>
    public (List<GameObject> assets, List<List<GameObject>> assetsJointPt) PopulateList(GameObject host)
    {
        // assign all assets except JointedLayer
        List<GameObject> assets = LGetChildren(host, JointedLayer);
        // assign all assets joint points 
        List<List<GameObject>> assetJointPt = GetGrandChildren(assets);

        // return result
        return (assets, assetJointPt);
    }

    /// <summary>
    /// get children of children from a list of children objects
    /// </summary>
    /// <param name="parent">parent object with children</param>
    /// <param name="filter">optional filter object to exclude</param>
    /// <returns>list of list children objects</returns>
    public List<GameObject> GetChildren(GameObject parent, GameObject filter = null)
    {
        // call local GetChildren function
        return LGetChildren(parent, filter);
    }

    public List<float> GetAgentDistance(GameObject currentObj, List<GameObject> assetList)
    {
        if (currentObj == null) return null;
        Vector3 pos1 = currentObj.transform.localPosition;
        return assetList.Select(asset => asset.transform.localPosition).Select(pos2 => (pos1 - pos2).magnitude).ToList();
    }

    /// <summary>
    /// Get nearest 3 neighbors from current asset
    /// </summary>
    /// <param name="currentAsset">currently selected asset</param>
    /// <param name="assets">all assets</param>
    /// <returns>(list of nearest neighbors, list of neighbors joint points)</returns>
    public (List<GameObject> neighbors, List<List<GameObject>> neighborsPts) GetNeighbors(GameObject currentAsset, List<GameObject> assets)
    {
        int neighborNum = 3;
        Vector3 pos1 = currentAsset.transform.localPosition;
        Dictionary<GameObject, float> neighbors = new Dictionary<GameObject, float>();
        foreach (GameObject asset in assets)
        {
            if (asset == currentAsset) continue;
            Vector3 pos2 = asset.transform.localPosition;
            float distance = (pos1 - pos2).magnitude;
            neighbors.Add(asset, distance);
        }

        var smallestPairs = neighbors.OrderBy(x => x.Value).Take(neighborNum).ToList();
        List<GameObject> neighborsObj = smallestPairs.Select(pairs => pairs.Key).ToList();
        List<List<GameObject>> neighborsPts = neighborsObj.Select(obj => GetChildren(obj)).ToList();
        return (neighborsObj, neighborsPts);
    }
    // ======================================== Private Functions ========================================
    private void Bind(GameObject parent, GameObject child)
    {
        // set child to parent
        child.transform.SetParent(parent.transform);
    }

    public Vector3 GetPosition(GameObject obj)
    {
        // get object position
        return obj.transform.position;
    }

    public List<Vector3> GetPosition(List<GameObject> obj)
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (GameObject item in obj)
        {
            positions.Add(GetPosition(item));
        }
        return positions;
    }
    

    private GameObject GetParent(GameObject child)
    {
        // get child parent
        return child.transform.parent.gameObject;
    }

    /// <summary>
    /// Get a list of children from parent object
    /// </summary>
    /// <param name="parent">parent object</param>
    /// <param name="filter">child to exclude</param>
    /// <returns>list of child object</returns>
    private List<GameObject> LGetChildren(GameObject parent, GameObject filter = null)
    {
        // initialise a new list for children
        List<GameObject> children = new List<GameObject>();
        // for every children...
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            // if child parent is filter object, skip
            if(parent.transform.GetChild(i).gameObject == filter) continue;
            // get child object
            GameObject child = parent.transform.GetChild(i).gameObject;
            // add child object into children list
            children.Add(child);
        }
        // return result
        return children;
    }

    /// <summary>
    /// get children of children from a list of children objects
    /// </summary>
    /// <param name="children">list of children objects</param>
    /// <returns>list of list children objects</returns>
    public List<List<GameObject>> GetGrandChildren(List<GameObject> children)
    {
        // initialise grand children collection, list of list
        List<List<GameObject>> grandChildrenCollection = new List<List<GameObject>>();
        // loop each child in children...
        foreach (GameObject child in children)
        {
            // get grand children, children of child
            List<GameObject> grandChildren = LGetChildren(child);
            // add grand children to list
            grandChildrenCollection.Add(grandChildren);
        }
        // return result
        return grandChildrenCollection;
    }


    private (List<GameObject> assets, List<List<GameObject>> assetPts) GetJointed(GameObject jointedLayer)
    {
        // get a list of jointed assets
        List<GameObject> jointedAssets = LGetChildren(jointedLayer);
        // get a list of list of asset points collection
        List<List<GameObject>> assetPts = GetGrandChildren(jointedAssets);
        // return result
        return (jointedAssets, assetPts);
    }

    private List<GameObject> RemoveCurrentPts(GameObject filterObj, List<List<GameObject>> ptLists)
    {
        // initialise a new list for excluded points
        List<GameObject> excludedList = new List<GameObject>();
        // for each list of points in a points collection...
        foreach (List<GameObject> ptList in ptLists)
        {
            // for each point in a list of points...
            foreach (GameObject pt in ptList)
            {
                // if parent is filter object, skip
                if(GetParent(pt) == filterObj) continue;
                // add point in excluded list
                excludedList.Add(pt);
            }
        }
        // return result
        return excludedList;
    }

    

    private (List<float> distances, float minDistance, GameObject minParent) GetDistance(GameObject currentAsset, List<GameObject> assets)
    {
        // initialise variables
        List<List<GameObject>> ptLists = GetNeighbors(currentAsset, assets).neighborsPts;
        List<float> distances = new List<float>();
        List<GameObject> distanceObjects = new List<GameObject>();
        List<GameObject> currentPts = LGetChildren(currentAsset);
        List<GameObject> otherPts = RemoveCurrentPts(currentAsset, ptLists);

        // for each current object joint point in current object joint points...
        foreach (GameObject currentPt in currentPts)
        {
            // position 1 is defined with current point position
            Vector3 pos1 = GetPosition(currentPt);
            // for each point in other points
            foreach (GameObject otherPt in otherPts)
            {
                // position 2 is defined with other point position
                Vector3 pos2 = GetPosition(otherPt);
                // get distance between position 1 and position 2
                float distance = (pos1 - pos2).magnitude;
                // get the other point parent object
                GameObject parent = GetParent(otherPt);

                // add distance to list of distances
                distances.Add(distance);
                // add parent object to list of parents
                distanceObjects.Add(parent);
            }
        }
        // find minimum distance from the list of distances
        float minDistance = distances.Min();
        // find index of minimum distance within the list
        int idx = distances.IndexOf(minDistance);

        // find parent object for the minimum distance
        GameObject minParent = distanceObjects[idx];
        // return list of distance, minimum distance and minimum distance parent
        return (distances,minDistance, minParent);
    }
}

