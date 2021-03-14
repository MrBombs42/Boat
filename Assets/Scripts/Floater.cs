using System.Collections;
using System.Collections.Generic;
using Boat.Assets.Scripts;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Floater : MonoBehaviour
{

    public float AirDrag = 1;
    public float WaterDrag = 10f;
    public Transform[] FloatPoints;
    public bool AffectDirection = false;

    private Rigidbody _rigidbody;
    private Waves _waves;
    private float _waterLine;
    private Vector3[] _waterLinePoints;

    private Vector3 _centerOffset;
    private Vector3 _targetUp;
    private Vector3 _smoothVectorRotation;

    public Vector3 Center {get{return transform.position + _centerOffset;}}

    // Start is called before the first frame update
    void Awake()
    {
        _waves = FindObjectOfType<Waves>();
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.useGravity = false;

        ComputeCenter();

    }

    private void ComputeCenter(){
        _waterLinePoints = new Vector3[FloatPoints.Length];
        for (var i = 0; i < FloatPoints.Length; i++)
        {
            _waterLinePoints[i] = FloatPoints[i].position;
        }

        _centerOffset = PhysicsHelper.GetCenter(_waterLinePoints) - transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        var newWaterLine = 0f;
        var pointUnderWater = false;

        for (var i = 0; i < FloatPoints.Length; i++)
        {
            var floatPoint = FloatPoints[i];
            _waterLinePoints[i] = floatPoint.position;
            _waterLinePoints[i].y = _waves.GetHeight(floatPoint.position);
            newWaterLine += _waterLinePoints[i].y / FloatPoints.Length;

            if(_waterLinePoints[i].y > floatPoint.position.y){
                pointUnderWater = true;
            }
        }

        var waterLineDelta = newWaterLine - _waterLine;
        _waterLine = newWaterLine;

        _targetUp = PhysicsHelper.GetNormal(_waterLinePoints);

        var gravity = Physics.gravity;
        _rigidbody.drag = AirDrag;
        if(_waterLine > Center.y){
            _rigidbody.drag = WaterDrag;

            _rigidbody.position = new Vector3(_rigidbody.position.x, _waterLine - _centerOffset.y, _rigidbody.position.z);
        }
        else
        {
            gravity = AffectDirection ? _targetUp * - Physics.gravity.y : Physics.gravity;
            transform.Translate(Vector3.up * waterLineDelta * 0.9f);
        }

        _rigidbody.AddForce(gravity * Mathf.Clamp(Mathf.Abs(_waterLine - Center.y), 0, 1));

        if(pointUnderWater){
            _targetUp = Vector3.SmoothDamp(transform.up, _targetUp, ref _smoothVectorRotation, 0.2f);
            _rigidbody.rotation = Quaternion.FromToRotation(transform.up, _targetUp) * _rigidbody.rotation;
        }
        
    }
     private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (FloatPoints == null)
            return;

        for (int i = 0; i < FloatPoints.Length; i++)
        {
            if (FloatPoints[i] == null)
                continue;

            if (_waves != null)
            {

                //draw cube
                Gizmos.color = Color.red;
                Gizmos.DrawCube(_waterLinePoints[i], Vector3.one * 0.3f);
            }

            //draw sphere
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(FloatPoints[i].position, 0.1f);

        }

        //draw center
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(new Vector3(Center.x, _waterLine, Center.z), Vector3.one * 1f);
            Gizmos.DrawRay(new Vector3(Center.x, _waterLine, Center.z), _targetUp * 1f);
        }
    }
}
