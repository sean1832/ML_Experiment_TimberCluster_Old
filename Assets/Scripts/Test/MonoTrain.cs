using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MonoTrain : MonoBehaviour
{
    [SerializeField][Range(0.01f, 0.5f)] private float _jointRange = 0.3f;
    [SerializeField] private GameObject _jointedLayer;
    [SerializeField] private Renderer _ground;
    [SerializeField] private int _idx;

    private GameObject _currentObj;
    private Joints _mJoints;
    private bool _isCollided = false;
    private List<GameObject> _assets = new List<GameObject>();
    private List<List<GameObject>> _assetJointPt = new List<List<GameObject>>();
    

    private List<Vector3> _initPositions;
    private const string INSTANCE_NAME = "InstanceObj";

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        _currentObj = _assets[_idx];
        _mJoints.Connect(_currentObj, _assets, _assetJointPt, _isCollided);
    }

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
        _initPositions = GetInitPositions(_assets);
        // set index number back to 0
        _idx = 0;
    }
    private List<Vector3> GetInitPositions(List<GameObject> assetList)
    {
        // return all assets local position as list of vector3
        return assetList.Select(asset => asset.transform.localPosition).ToList();
    }
    /// <summary>
    /// CollisionDetectionEnter is call when collision is detected
    /// </summary>
    /// <param name="listener">listener class script</param>
    public void CollisionDetectionEnter(CollisionListenerTest listener)
    {
        _isCollided = true;
    }

    /// <summary>
    /// CollisionDetectionExit is call when object exit the collision
    /// </summary>
    /// <param name="listener">listener class script</param>
    public void CollisionDetectionExit(CollisionListenerTest listener)
    {
        _isCollided = false;
    }
}
