using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class kinemRbMotor : MonoBehaviour
{
    public CapsuleCollider cc;
    public Rigidbody rb;
    public LayerMask obsticleLayer;
    public InputReciever inputReciever;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(rb.position+(transform.up * cc.radius) +(gravityDir * addedDistance), cc.radius);
        if (nearestGroundHit.collider!= null)  Gizmos.DrawSphere(nearestGroundHit.point, 0.04f);
    }
    private void OnEnable()
    {
        if (PlatformingDelta_C != null) StopCoroutine(PlatformingDelta_C);
        PlatformingDelta_C = StartCoroutine(PlatformingDelta());
    }



    Vector3 MoveRequested;
    /// <summary>
    /// call move vector here
    /// </summary>
    public void Move(Vector3 _move)
    {
        MoveRequested += _move;
    }

    private void FixedUpdate()
    {
        InputControls(); //remove this for ur own custom inputs

        // =========== move substeps =============//
        int totalSubSteps = 10;
        Vector3 _MoveRequest = MoveRequested/ totalSubSteps;

        //substep
        for (int i = 0; i < totalSubSteps; i++)
        {
            CollisionLogic(rb.position);

            _MoveRequest = SlopeLimitation(_MoveRequest);
            Vector3 _move = _MoveRequest + GroundSnap();

            float per =(float) i+1 / (float)totalSubSteps;
            Vector3 stepMove = Vector3.Lerp(rb.position, _move, per);
            Vector3 _depenetration = ComputePenetrate(stepMove);
            rb.position = stepMove+_depenetration;
        }

        //============== move with rb movePosition ================ 
        /*
        CollisionLogic(rb.position);

        Vector3 _move = MoveRequested + GroundSnap();
        _move += ComputePenetrate(_move);
        rb.MovePosition(_move);
        
        */
        //==========================
        MoveRequested = Vector3.zero;
    }


    public float addedDistance = 1;
    RaycastHit[] grounderHits;
    RaycastHit nearestGroundHit;
    Vector3 hitNormalsFixed;

    public bool isGrounded;

    public float skinOffset = 0.08f;



    void CollisionLogic(Vector3 _pos)
    {
        //gather collision data

           Physics.CapsuleCast(_pos + (transform.up *cc.radius),
              _pos + (transform.up *(cc.height -cc.radius) ), cc.radius-0.005f ,gravityDir, out nearestGroundHit, addedDistance+ 0.005f, obsticleLayer, QueryTriggerInteraction.Ignore);


        //===========
        // if (nearestGroundHit.collider != null) rb.position += -gravityDir * 0.05f; //forcing it to have some space between the floor, cause I dunno why. TIRED OF UR bullSHIT UNITY 
        // rb.SweepTest(gravityDir,out nearestGroundHit, addedDistance, QueryTriggerInteraction.Ignore);

        //=========
        isGrounded = nearestGroundHit.collider != null;
        //=== normal fix ==//
        if (nearestGroundHit.collider != null) hitNormalsFixed = RepairHitSurfaceNormal(nearestGroundHit);
        else hitNormalsFixed = -gravityDir;
    }

    Vector3 GroundSnap()
    {
   
        Vector3 _requestMovePosition = rb.position ;
        // ========= ground snap ==================//
        /*
     //only snaps to the center pivot of the transform. doesn't work well on other corners of the capsule

     if (nearestGroundHit.collider != null)
     {
         Vector3 hitPoint = nearestGroundHit.point;

         Vector3 groundSnapDir = new Vector3(Mathf.Lerp(_requestMovePosition.x, hitPoint.x, Mathf.Abs(gravityDir.x)), Mathf.Lerp(_requestMovePosition.y, hitPoint.y, Mathf.Abs(gravityDir.y)), Mathf.Lerp(_requestMovePosition.z, hitPoint.z, Mathf.Abs(gravityDir.z)));
         _requestMovePosition = (groundSnapDir + (skinOffset * -gravityDir));

     }
     */

        //=============== smooth transition ===========//

        if (nearestGroundHit.collider != null)
        {
            Vector3 closestHitBoundPos = Physics.ClosestPoint(nearestGroundHit.point, cc, rb.position, transform.rotation);// thisCollider.ClosestPoint(nearestHit.point);

            Vector3 _distDifference = (closestHitBoundPos - nearestGroundHit.point) ;
            _distDifference -= (skinOffset * _distDifference.normalized);

            bool PointIsInsideCollider = false; // cc.bounds.Contains(nearestGroundHit.point);
            bool IsNotRejectedFromBadPoint = _distDifference.magnitude <= addedDistance;

            bool IsSnapping = IsNotRejectedFromBadPoint && !PointIsInsideCollider; //make sure unity isn't bugged when it can't read ClosestPoint correctly
            if (IsSnapping)
            {
                //get any upward difference only
                Vector3 posGravity = new Vector3(Mathf.Abs(gravityDir.x), Mathf.Abs(gravityDir.y), Mathf.Abs(gravityDir.z));
                _distDifference = Vector3.Scale(_distDifference, posGravity);

                _requestMovePosition =(rb.position + (skinOffset * -gravityDir) - (_distDifference));  // i dunno why, but SkinOffset fixed cc from going other direction when climbing or dropping from hills
            }

        }

        return _requestMovePosition;
    }
    Vector3 ComputePenetrate(Vector3 predictionPos)
    {
        //================= physics compute collision push =============================
        Vector3 _requestMovePosition = Vector3.zero;

        Collider[] neighbours =Physics.OverlapCapsule(rb.position, rb.position+ (transform.up * cc.height),cc. radius *4, obsticleLayer, QueryTriggerInteraction.Ignore);

        int samples = 8;
        while (samples > 0)
        {
            for (int i = 0; i < neighbours.Length; ++i)
            {
                Collider collider = neighbours[i];

                if (collider == cc) continue;

                Vector3 otherPosition = collider.transform.position;
                Quaternion otherRotation = collider.transform.rotation;


                Vector3 direction;
                float distance;

                bool overlapped = Physics.ComputePenetration(
                    cc, predictionPos + _requestMovePosition
                    , transform.rotation,
                    collider, otherPosition, otherRotation,
                    out direction, out distance
                );

                // draw a line showing the depenetration direction if overlapped
                if (overlapped)
                {
                    _requestMovePosition += (direction * distance);
                    //print(_requestMovePosition);
                }
            }
            samples -= 1;
        }
   
       // _requestMovePosition = new Vector3(Mathf.Lerp(_requestMovePosition.x,0, Mathf.Abs(gravityDir.x)), Mathf.Lerp(_requestMovePosition.y, 0, Mathf.Abs(gravityDir.y)), Mathf.Lerp(_requestMovePosition.z, 0, Mathf.Abs(gravityDir.z))  );
        return _requestMovePosition;
    }


    [Header("slope limitation")]
    [Range(0,89)]
    [Tooltip("turn off slope limitation when set to 0")]
    public float minAngle = 50;
    public float currentAngle;

    Vector3 SlopeLimitation(Vector3 _move)
    {
        //needs multiple raycast so it doesn't detect small edges

        currentAngle = Vector3.Angle(-gravityDir, hitNormalsFixed);

        bool CanSlopeDetect = minAngle > 0;
        if (nearestGroundHit.collider != null && currentAngle > minAngle && currentAngle<90 && CanSlopeDetect)
        {
            
            Vector3 XYNormals = new Vector3(
                Mathf.Lerp(hitNormalsFixed.x, 0,Mathf.Abs( gravityDir.x)) ,
                Mathf.Lerp(hitNormalsFixed.y, 0, Mathf.Abs(gravityDir.y)),
                Mathf.Lerp(hitNormalsFixed.z, 0, Mathf.Abs(gravityDir.z))
                ).normalized;
            Debug.DrawRay(rb.position, XYNormals.normalized);
            
            Vector3 crossForwd = Vector3.Cross(hitNormalsFixed, -gravityDir.normalized).normalized;
            Debug.DrawRay(rb.position, crossForwd);
            Vector3 crossDown = Vector3.Cross(hitNormalsFixed, crossForwd).normalized;
            Debug.DrawRay(rb.position, crossDown.normalized);

            float rawPerc = (Vector3.Angle(-XYNormals, _move) ) / (180 );
            float perc = (Vector3.Angle(-XYNormals, _move)) / (180);
            if (perc <= 0.5f) perc = 0;
            // Vector3 subtract =Vector3.Slerp (( Vector3.ProjectOnPlane( _move.normalized, XYNormals)).normalized , _move.normalized, rawPerc);
            Vector3 subtract = Vector3.ProjectOnPlane(_move.normalized,Vector3.Lerp( XYNormals , -gravityDir, perc));

            // Vector3 subtract = Vector3.Slerp((crossDown.normalized), _move.normalized, Vector3.Angle(-XYNormals, _move) / 180);
            Vector3 _reflect = (subtract * _move.magnitude) ;
             //Vector3 _reflect = crossDown.normalized *_move.magnitude;


            return _reflect;
        }
        else
        {
            return _move;
        }
    }


    [Space()]
    public Vector3 gravityDir = -Vector3.up;
    public float gravityPow = 4;
    Vector3 dirMove;
    Vector3 dirMoveSmooth;
    public float speed = 10;
    void  InputControls()
    {
        Camera cam = Camera.main;

        Vector3 crossForwd = Vector3.Cross(cam.transform.right, -gravityDir);
        Vector3 crossRight = Vector3.Cross(-gravityDir, crossForwd);

        dirMove = ((crossForwd * inputReciever.GetMoveAxis.y) + (crossRight * inputReciever.GetMoveAxis.x));

        dirMoveSmooth = Vector3.Lerp(dirMoveSmooth, dirMove, Time.deltaTime * 6);
        dirMoveSmooth = Vector3.ProjectOnPlane(dirMoveSmooth, -gravityDir);

        //move
        //bool UseRootMover_Move = animRootMover != null && animRootMover.rootMotion.useRootMotion;
        //if (UseRootMover_Move)
        //{
         //   Move(animRootMover.deltaPos);
        //}
        //else
        //{
            Vector3 _move = (dirMoveSmooth * speed * Time.fixedDeltaTime);
            Move(_move);
        //}
        //rotate
       // bool UseRootMover_Rotate = animRootMover != null && animRootMover.rootMotion.useRootMotion;
       // if (UseRootMover_Rotate)
       // {
       //     rb.rotation *= animRootMover.deltaRotation;
       // }
       // bool CanRotate = animRootMover == null || (animRootMover != null && !animRootMover.rootMotion.useRootMotion) || (animRootMover != null && animRootMover.rootMotion.enableInputRotate);
       // if (CanRotate)
       // {
            if (dirMoveSmooth != Vector3.zero)
            {
                rb.rotation = Quaternion.Lerp(rb.rotation, Quaternion.LookRotation(dirMoveSmooth, -gravityDir), Time.fixedDeltaTime * 20);
            }
       // }
    }







    /// <summary>
    /// if spherecast or capsule cast hits an edge of the box, it interpolates the normals. use this to get the correct normal
    /// </summary>
    /// <returns></returns>
    public Vector3 RepairHitSurfaceNormal(RaycastHit hit)
    {
        /*
        if (hit.collider is MeshCollider && (hit.collider as MeshCollider).sharedMesh.isReadable)
        {
            var collider = hit.collider as MeshCollider;
            var mesh = collider.sharedMesh;
            var tris = mesh.triangles;
            var verts = mesh.vertices;

            var v0 = verts[tris[hit.triangleIndex * 3]];
            var v1 = verts[tris[hit.triangleIndex * 3 + 1]];
            var v2 = verts[tris[hit.triangleIndex * 3 + 2]];

            var n = Vector3.Cross(v1 - v0, v2 - v1).normalized;

            return hit.transform.TransformDirection(n);
        }
        else
        {*/

        var p = hit.point + (hit.normal * 0.01f);
        Ray ray = new Ray(p, -hit.normal);
        hit.collider.Raycast(ray, out hit, 0.015f);

        Debug.DrawRay(hit.point, hit.normal, Color.yellow);

        return hit.normal;
        // }
    }





    Coroutine PlatformingDelta_C;
    Transform platformingTr;
    Vector3 deltaMove;
    Quaternion deltaQuaturnian;
    Vector3 deltaEulre;
    [Header("Platforming")]
    public bool rotatePlatformingSurfaceUp; //character would rotate like inception
    IEnumerator PlatformingDelta()
    {
        while (true)
        {
            if (platformingTr == null) platformingTr = new GameObject("_platformDelta_" + this.transform.name).transform;

            if (nearestGroundHit.collider != null)
            {
                //setup for frame delta
                if (!platformingTr.IsChildOf(nearestGroundHit.collider.transform)) platformingTr.parent = nearestGroundHit.collider.transform;
                platformingTr.transform.position = rb.position;
                deltaMove = platformingTr.transform.position;

                platformingTr.transform.rotation = rb.rotation;
                deltaQuaturnian = platformingTr.transform.rotation;
                deltaEulre = platformingTr.transform.eulerAngles;

                yield return null;

                //get delta calc
                bool PosChanged = deltaMove != platformingTr.transform.position;
                deltaMove = platformingTr.transform.position - deltaMove;
                if (PosChanged) rb.position += deltaMove;

                /*if (rotatePlatformingSurfaceUp || worldUpType == WorldUpType.RelativeUp)
                {

                    bool RotChanged = deltaEulre != platformingTr.transform.eulerAngles;
                    deltaEulre = platformingTr.transform.eulerAngles - deltaEulre;

                    if (RotChanged) rb.MoveRotation(Quaternion.Euler(deltaEulre) * Quaternion.FromToRotation(cc.transform.up, GetReletiveUp()) * rb.rotation);

                }
                else
                {*/
                bool RotChanged = deltaEulre != platformingTr.transform.eulerAngles;
                deltaEulre = platformingTr.transform.eulerAngles - deltaEulre;
                deltaEulre.x = 0;
                deltaEulre.z = 0;

                //  if (RotChanged) transform.rotation =(Quaternion.Euler(deltaEulre) * Quaternion.FromToRotation(cc.transform.up, -gravityDir) * transform.rotation);
                if (RotChanged)
                {
                    rb.rotation = (Quaternion.Euler(deltaEulre) * transform.rotation);
                    if (rotatePlatformingSurfaceUp) rb.rotation = Quaternion.FromToRotation(cc.transform.up, -gravityDir) * transform.rotation;
                }
                // }
            }
            else
            {
                //restart
                platformingTr.parent = cc.transform;
                platformingTr.localPosition = Vector3.zero;
                platformingTr.localRotation = Quaternion.identity;
                yield return null;
            }

            //do not put yeild return null as that would lag on build
        }
    }
}
