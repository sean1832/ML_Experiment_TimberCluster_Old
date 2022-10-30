using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine.EventSystems;
using GameObject = UnityEngine.GameObject;

public class Train : Agent
{
    // search "**" to quickly navigate to reward and punishment
    // ======================================== Mono Behaviour ========================================
    [Header("Training Parameters")]
    [SerializeField] [Range(0.1f,10f)] private float _moveSpeed = 3f;
    [SerializeField] [Range(1f, 100f)] private float _rotSpeed = 50f;
    [SerializeField] [Range(0.01f, 0.5f)] private float _jointRange = 0.3f;
    [SerializeField] private GameObject _jointedLayer;
    [SerializeField] private Renderer _ground;

    [Header("Export Parameters")]
    [SerializeField] private bool _enableExport = false;
    [SerializeField] private string _prefabName;
    [SerializeField] private string _exportPath = "Assets/Prefabs/Output/";

    private GameObject _currentObj;
    private Joints _mJoints;
    private bool _isCollided = false;
    private bool _isConnected = false;
    private List<GameObject> _assets = new List<GameObject>();
    private List<List<GameObject>> _assetJointPt = new List<List<GameObject>>();
    private List<float> _lastDistances = new List<float>();
    private List<Vector3> _lastObjPtsPos = new List<Vector3>();
    private int _maxLastCount = 5;
    private int _idx;

    private Dictionary<GameObject, Vector3> _initialPos = new Dictionary<GameObject, Vector3>();
    private const string INSTANCE_NAME = "InstanceObj";
    // ======================================== Mono Behaviour ========================================
    // Start is called before the first frame update
    void Start()
    {
        // initialise fields and scripts
        Init();
        print($"assets count: {_assets.Count}");
    }
    // ======================================== ML Agent ========================================

    public override void OnEpisodeBegin()
    {

        if (_idx < _assets.Count - 1 && _idx != 0)
        {
            ResetCurrentAsset(_currentObj, _initialPos);
            _isConnected = false;
            _currentObj = _assets[_idx];
        }
        else
        {
            // reset all assets positions, rotations and game object hierarchy
            ResetAllAssets(_jointedLayer, _initialPos);
            // set isConnected back to false
            _isConnected = false;
            // set index number back to 0
            _idx = 0;
            // reset current object index number
            _currentObj = _assets[_idx];
            ResetLastDistances();
        }
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        // add observation for current object local position, vector3 = 3
        sensor.AddObservation(_currentObj.transform.localPosition);
        // add observation for current object local rotation euler angles, vector3 = 3
        sensor.AddObservation(_currentObj.transform.localEulerAngles);
        // current object index = 1
        sensor.AddObservation(_idx);

        // current pt position = 3
        List<GameObject> currentPt = _mJoints.GetChildren(_currentObj);
        List<Vector3> currentPtPos = _mJoints.GetPosition(currentPt);
        foreach (Vector3 pos in currentPtPos)
        {
            sensor.AddObservation(pos);
        }
        // get 3 neighbor pt positions = 3*3*3
        (List<GameObject> neighborObj, List<List<GameObject>> neighborPts) = _mJoints.GetNeighbors(_currentObj, _assets);
        List<GameObject> flattenPts = FlattenLists(neighborPts);
        List<Vector3> flattenPtPos = _mJoints.GetPosition(flattenPts);
        foreach (Vector3 pos in flattenPtPos)
        {
            sensor.AddObservation(pos);
        }
        // 3 neighbor index = 3
        foreach (GameObject obj in neighborObj)
        {
            int idx = _assets.IndexOf(obj);
            sensor.AddObservation(idx);
        }
        // last 5 objects pt pos = 5*3*3
        _lastObjPtsPos = GetLastObjPtPos(_maxLastCount, _lastObjPtsPos);
        
        foreach (Vector3 ptPos in _lastObjPtsPos)
        {
            sensor.AddObservation(ptPos);
        }
        // last 5 objects pt dis = 5*3*3
        _lastDistances = GetLastDistance(_maxLastCount, _lastDistances);
        sensor.AddObservation(_lastDistances);

        // debug
        //print($"pt1_Pos: {_lastObjPtsPos[0]}, pt2_Pos: {_lastObjPtsPos[1]}, pt3_Pos: {_lastObjPtsPos[2]}\n" +
             // $"pt1_Dis: {_lastDistances[0]}, pt2_Dis: {_lastDistances[1]}, pt3_Dis: {_lastDistances[2]}");
    }

    private List<float> GetLastDistance(int maxCount, List<float> lastDistanceList)
    {
        List<GameObject> flattenObjPts = GetLastPts(maxCount);
        List<GameObject> currentObjPts = _mJoints.GetChildren(_currentObj);
        List<List<float>> distanceLists = new List<List<float>>();
        foreach (GameObject currentPt in currentObjPts)
        {
            distanceLists.Add(_mJoints.GetAgentDistance(currentPt, flattenObjPts));
        }
        List<float> distances = FlattenLists(distanceLists);
        for (int i = 0; i < distances.Count; i++)
        {
            lastDistanceList[i] = distances[i];
        }
        return lastDistanceList;
    }

    private List<Vector3> GetLastObjPtPos(int maxCount, List<Vector3> lastObjPtPos)
    {
        List<GameObject> flattenObjPts = GetLastPts(maxCount);
        List<Vector3> ptPos = _mJoints.GetPosition(flattenObjPts);
        for (int i = 0; i < ptPos.Count; i++)
        {
            lastObjPtPos[i] = ptPos[i];
        }
        return lastObjPtPos;
    }

    private List<GameObject> GetLastPts(int maxCount)
    {
        List<GameObject> lastObjects = GetLastObjects(maxCount);
        List<List<GameObject>> lastObjPts = _mJoints.GetGrandChildren(lastObjects);
        List<GameObject> flattenObjPts = FlattenLists(lastObjPts);
        return flattenObjPts;
    }

    private List<GameObject> GetLastObjects(int maxCount)
    {
        List<GameObject> lastObjects = new List<GameObject>();
        for (int i = 0; i < maxCount; i++)
        {
            int increment = i + 1;
            if (_idx - increment <= 0) break;
            GameObject obj = _assets[_idx - increment];
            lastObjects.Add(obj);
        }

        return lastObjects;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // set current object index number
        _currentObj = _assets[_idx];
        // set translation X action
        float moveX = actions.ContinuousActions[0];
        // set translation Y action
        float moveY = actions.ContinuousActions[1];
        // set translation Z action
        float moveZ = actions.ContinuousActions[2];

        // set Rotation X action
        float rotX = actions.ContinuousActions[3] * Time.deltaTime * _rotSpeed;
        // set Rotation Y action
        float rotY = actions.ContinuousActions[4] * Time.deltaTime * _rotSpeed;
        // set Rotation Z action
        float rotZ = actions.ContinuousActions[5] * Time.deltaTime * _rotSpeed;

        // translate current object
        _currentObj.transform.localPosition += _moveSpeed * Time.deltaTime * new Vector3(moveX, moveY, moveZ);
        // rotate current object
        _currentObj.transform.localEulerAngles += new Vector3(rotX, rotY, rotZ);
        // attempt to connect current object with other assets, return true if connected, and return object connected with
        (bool isConnected, GameObject connectedWith) connection = _mJoints.Connect(_currentObj, _assets, _assetJointPt, _isCollided);

        _isConnected = connection.isConnected;
        int connectedWithIdx = 0;
        if (connection.connectedWith != null)
        {
            connectedWithIdx = _assets.IndexOf(connection.connectedWith);
        }
        // if is Connected is false, return until it is true.
        if (!_isConnected) return;
        // ** add reward upon connect
        AddReward(_idx+1);
        // if assets are not all connected...
        if (_idx < _assets.Count - 1)
        {
            // if index is 0...
            if (_idx == connectedWithIdx)
            {
                // skip the next asset
                _idx += 2;
            }
            // otherwise...
            else
            {
                _currentObj.GetComponent<Renderer>().material.color = Color.yellow;
                // next asset
                _idx++;
            }
        }
        // otherwise if all assets connected...
        else
        {
            // ** add large reward for connecting all assets
            AddReward(+20f);
            // set ground material to green.
            _ground.material.color = Color.green;


            List<float> lowList = GetHeightList(_assets,-1.0f,1.3f);
            List<float> highList = GetHeightList(_assets, 1.3f, 2.0f);
            float lowPercent = (float)lowList.Count/(float)_assets.Count;
            float highPercent = (float)highList.Count/(float)_assets.Count;

            print($"low: ({lowPercent * 100}%),  high: ({highPercent * 100}%)");

            if (lowPercent is > 0.10f and < 0.15f)
            {
                AddReward(+10);
            }
            else
            {
                AddReward(-10);
            }

            if (highPercent is > 0.30f and < 0.85f)
            {
                AddReward(+10);
            }
            else
            {
                AddReward(-10);
            }

            if (_enableExport) ExportObj(_jointedLayer, _prefabName, _exportPath);


            // end episode
            EndEpisode();
        }
        // loop through each assets
        // each asset is move until connect, then go to next asset.
        // end episode once every assets are connected
    }

    private void ExportObj(GameObject rootObj, string customName = "", string path = "Assets/Prefabs/Output/")
    {
        string defaultPath = "Assets/Prefabs/Output/";
        string defaultName = "";
        if (customName == string.Empty) customName = defaultName;
        if (path == string.Empty) path = defaultPath;

        GameObject copiedObj = Instantiate(_jointedLayer);

        if (customName == "") customName = rootObj.name;
        string assetPath = path + customName + ".prefab";
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
        PrefabUtility.SaveAsPrefabAssetAndConnect(copiedObj, assetPath, InteractionMode.UserAction);
        Destroy(copiedObj);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // set action segment to a list of continuous actions
        ActionSegment<float> contActions = actionsOut.ContinuousActions;
        // set translation X action to horizontal axis input
        contActions[0] = Input.GetAxisRaw("Horizontal") * _moveSpeed * Time.deltaTime;
        // set rotation Z action to vertical axis input
        contActions[5] = Input.GetAxisRaw("Vertical") * _moveSpeed * Time.deltaTime;
    }


    // ======================================== Init Functions ========================================
    /// <summary>
    /// Initialise the all data and functions, call upon start
    /// </summary>
    void Init()
    {
        // initialise class scripts
        InitClass();
        // initialise fields data
        InitFields();
    }

    /// <summary>
    /// Initialise class scripts
    /// </summary>
    void InitClass()
    {
        // create a new game object instance
        GameObject geo = new GameObject();
        // attach "Joints" class to the new game object
        geo.AddComponent<Joints>();
        // set game object name to "InstanceObj"
        geo.GetComponent<Joints>().name = INSTANCE_NAME;
        // define mJoints
        _mJoints = geo.GetComponent<Joints>();
    }

    /// <summary>
    /// Initialise fields data
    /// </summary>
    void InitFields()
    {
        // assign jointed layer game object to public variable "JointedLayer" in Joints class
        _mJoints.JointedLayer = _jointedLayer;
        // assign jointed range float to public variable "JointRange" in Joints class
        _mJoints.JointRange = _jointRange;

        // assign tuple variables for assets and assets joint points
        (List<GameObject> assets, List<List<GameObject>> assetJointPt) populatedList = _mJoints.PopulateList(gameObject);
        // populate local variable assets
        _assets = populatedList.assets;
        // populate local variable asset joint points
        _assetJointPt = populatedList.assetJointPt;
        // get current initial position and assign to local variable 
        _initialPos = GetInitPositions(_assets);
        // set index number back to 0
        _idx = 0;

        // initialise last distance, create dummy value
        InitLastDistances();
        InitLastObjPtsPos();
        
    }

    private void InitLastObjPtsPos()
    {
        int ptCount = _mJoints.GetChildren(_assets[0]).Count;
        for (int i = 0; i < _maxLastCount*ptCount; i++)
        {
            _lastObjPtsPos.Add(new Vector3(0,0,0));
        }
    }

    private void InitLastDistances()
    {
        int ptCount = _mJoints.GetChildren(_assets[0]).Count;
        for (int i = 0; i < _maxLastCount * ptCount * ptCount; i++)
        {
            _lastDistances.Add(-1f);
        }
    }

    private void ResetLastDistances()
    {
        int ptCount = _mJoints.GetChildren(_assets[0]).Count;
        for (int i = 0; i < _lastDistances.Count; i++)
        {
            _lastDistances[i] = -1.0f;
        }
    }

    // ======================================== Private Functions ========================================
    /// <summary>
    /// flatten a list of list structure into a single list
    /// </summary>
    /// <returns>list of flatten item</returns>
    private List<T> FlattenLists<T>(List<List<T>> lists)
    {
        List<T> flatten = new List<T>();
        foreach (List<T> list in lists)
        {
            foreach (T item in list)
            {
                flatten.Add(item);
            }
        }
        return flatten;
    }


    /// <summary>
    /// find object initial position
    /// </summary>
    /// <param name="assetList">list of assets</param>
    /// <returns>list of vector3 positions</returns>
    private Dictionary<GameObject, Vector3> GetInitPositions(List<GameObject> assetList)
    {
        Dictionary<GameObject,Vector3> positions = new Dictionary<GameObject, Vector3>();
        foreach (GameObject asset in assetList)
        {
            Vector3 pos = asset.transform.localPosition;
            positions.Add(asset,pos);
        }
        return positions;
    }

    /// <summary>
    /// reset assets positions and hierarchy 
    /// </summary>
    /// <param name="jointedLayer">layer that is jointed</param>
    /// <param name="initialPositions">list of initial positions</param>
    private void ResetAllAssets(GameObject jointedLayer, Dictionary<GameObject,Vector3> initialPositions)
    {
        // reset hierarchy
        // get a list of jointed layer's children
        List<GameObject> assets = _mJoints.GetChildren(jointedLayer);
        // sort the list by their name
        assets.Sort((GameObject x, GameObject y) => string.Compare(x.name, y.name, StringComparison.Ordinal));

        // put ever assets in to jointed layer
        foreach (GameObject asset in assets)
        {
            asset.transform.SetParent(jointedLayer.transform);
        }
        // for each asset in the list...
        foreach (GameObject asset in assets)
        {
            // set each asset parent to the game object this script attached to
            asset.transform.SetParent(gameObject.transform);
            // reset each assets rotation to (0,0,0)
            asset.transform.localEulerAngles = new Vector3(0, 0, 0);
            asset.GetComponent<Renderer>().material.color = Color.white;
        }

        // reset positions
        foreach (GameObject asset in assets)
        {
            asset.transform.localPosition = initialPositions[asset];
        }

        // if current object is not null...
        if (_currentObj == null) return;
        // reset current object position
        _currentObj.transform.localPosition = initialPositions[_currentObj];
        // reset current object rotation
        _currentObj.transform.localEulerAngles = new Vector3(0,0,0);

        
    }

    private void ResetCurrentAsset(GameObject currentAsset, Dictionary<GameObject, Vector3> initialPositions)
    {
        currentAsset.transform.SetParent(gameObject.transform);
        currentAsset.transform.localPosition = initialPositions[currentAsset];
        currentAsset.transform.localEulerAngles = new Vector3(0, 0, 0);
    }

    private List<float> GetHeightList(List<GameObject> assetsList, float? min = null, float? max = null)
    {
        List<float> heightList = assetsList.Select(asset => asset.transform.localPosition.y).ToList();

        List<float> filteredHeights = new List<float>();
        foreach (float height in heightList)
        {
            if (min == null || max == null)
            {
                filteredHeights = heightList;
            }
            else if (height >= min && height < max)
            {
                filteredHeights.Add(height);
            }
        }

        return filteredHeights;
    }

    // ======================================== Listener ========================================
    /// <summary>
    /// TriggeredDetectionEnter is call when triggered is detected
    /// </summary>
    /// <param name="listener"></param>
    public void TriggerDetectionEnter(TriggerListener listener)
    {
        // ** punishment if triggered
        AddReward(-20f);
        // ground color change to red
        _ground.material.color = Color.red;
        // end episode
        EndEpisode();
    }


    /// <summary>
    /// CollisionDetectionEnter is call when collision is detected
    /// </summary>
    /// <param name="listener">listener class script</param>
    public void CollisionDetectionEnter(CollisionListener listener)
    {
        _isCollided = true;
    }

    /// <summary>
    /// CollisionDetectionExit is call when object exit the collision
    /// </summary>
    /// <param name="listener">listener class script</param>
    public void CollisionDetectionExit(CollisionListener listener)
    {
        _isCollided = false;
    }
}
