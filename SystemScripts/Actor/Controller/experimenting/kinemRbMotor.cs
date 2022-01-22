using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class kinemRbMotor : MonoBehaviour
{

    public Rigidbody rb;


    public CapsuleCollider cc;

    [Space()]
    [Tooltip("calculate x amount of substeps before reaching the destination")]
    public int subStepSample = 8;
    [Tooltip("calculate x amount of times that pushes away from the collider")]
    public int collisionSample = 8;
    [Header("correction")]
    public float terrainCollisionOffset = 0.1f;
    public float minJitter = 0.02f;

    [Header("ground ray")]
    public bool ActiveSnapToGround = true;
    public float groundRayDist = 1;
    [Space()]
    public Vector3 gravityDir = -Vector3.up;
    RaycastHit groundHit;


    [Range(0, 180)]
    [Tooltip("turn off by setting it to 0")]
    public float minAngle = 50f;

 

    private void OnEnable()
    {
        if (PlatformingDelta_C != null) StopCoroutine(PlatformingDelta_C);
        PlatformingDelta_C = StartCoroutine(PlatformingDelta());
    }


    public void Update()
    {

    }
    public void Move(Vector3 velocity)
    {
       // if (velocity.magnitude <= 0.001f) return;
        Vector3 ModifyPos = cc.transform.position;
  

        //------ substeps frame --------//
        for (int i = 0; i < subStepSample; i++)
        {
            //splice velocity into substeps
            Vector3 VelocitySubStep = (velocity / subStepSample);

            //ground check
            GroundCheck(ModifyPos, ref groundHit);
            bool _isGrounded = groundHit.collider != null;

            //give a boost
        
            if (terrainCollisionOffset> 0 && _isGrounded && groundHit.distance <= terrainCollisionOffset)
            {
                //give it a bit of a rise to avoid terrain push
                //if (VelocityDownward.magnitude == 0) 
                ModifyPos += (-gravityDir * terrainCollisionOffset);
            
            }
     

            //col depenetration
            Vector3 depenetrationPush = CollisionIterationPush(ModifyPos + VelocitySubStep/*, _isGrounded*/);
            Vector3 depenetrationVelSubStep = (VelocitySubStep + depenetrationPush);
            Vector3 Pre_ModifyPos = ModifyPos + depenetrationVelSubStep;



            //ground check
            GroundCheck(Pre_ModifyPos, ref groundHit);


            //--------------- minAngle allow ---------------------------------------------//
            if (GetIsMaxSlopeAngle(groundHit) )
            {


                Vector3 OriPosition = cc.transform.position;
                //normal towards 
                Vector3 TowardsDir = (groundHit.point - OriPosition).normalized; //avoid it being zero

                float _towardsAngle = Vector3.Angle(TowardsDir, VelocitySubStep.normalized);
                Debug.DrawRay(OriPosition, TowardsDir, Color.magenta);
                //right
                Vector3 RightDir = Vector3.Cross(TowardsDir, -gravityDir).normalized;
                float RightDot = Vector3.Dot(RightDir, VelocitySubStep.normalized);
                if ((RightDot >= -0.02f && RightDot <= 0.02f) || (RightDot >= 0.98f || RightDot <= -0.98f)) RightDir *= 0;
                else if (RightDot < 0) RightDir *= -1;
                //Debug.Log(RightDot);

                Debug.DrawRay(OriPosition, RightDir, Color.red);


                float _reflectPercSlow = _towardsAngle / 180f;
                _reflectPercSlow *= 0.9f;

                //choose between going pure right or be slowed down by reflect
                //ModifyPos += Vector3.Lerp(RightDir * VelocitySubStep.magnitude, Pre_Push * _reflectPercSlow, _reflectPercSlow);

                ModifyPos = Vector3.Lerp(ModifyPos +(RightDir * VelocitySubStep.magnitude), Pre_ModifyPos, _reflectPercSlow);
            }
            //regular
            else
            {
              
     
                ModifyPos = Pre_ModifyPos;            //modify pos for every step
            }

            /*
            if (undoTerrainOffset)
            {
                ModifyPos -= (-gravityDir * terrainCollisionOffset);
            }
            */
            // ----------- snap ground----------------//
            if (ActiveSnapToGround && groundRayDist != 0)
            {
                ModifyPos += SnapToGroundPush(ModifyPos);  //modify pos for every step
            }

        }



        // ----------- final col depenetration  ----------------//
        ModifyPos += CollisionIterationPush(ModifyPos/*,false*/);           //modify pos for every step
                                        //---------------------------//

        //jitter check
        if (minJitter <= 0 || (ModifyPos - prevPos).magnitude > minJitter)
        {
            prevPos = cc.transform.position;
            cc.transform.position = ModifyPos;
        }
    }
    /// <summary> cache prevPos to compare to jitter threshold </summary>
    Vector3 prevPos;


    /// <param name="currentPos"></param>
    /// <param name="intendedDirection">when u hold a direction, I don't want the terrain collision to dictate where i'm going</param>
    Vector3 CollisionIterationPush(Vector3 currentPos, bool intendedDirection = true)
    {
        //get colliders
        Vector3 Center = currentPos + cc.center + (cc.transform.up * (-cc.height * 0.5f));
        Collider[] colsGather = Physics.OverlapSphere(Center, 10, GameGlobal.instance.groundLayer, QueryTriggerInteraction.Ignore);

        //will modify
        Vector3 ModifyPos = currentPos;

        //more samples means less vibrates of pushing
        for (int x = 0; x < collisionSample; x++)
        {
            // colsGather = colsGather.OrderBy(x => Random.value).ToArray();

            for (int i = 0; i < colsGather.Length; i++)
            {
                Collider ThisCollider = colsGather[i];
                if (ThisCollider == null) continue;

                Vector3 Push_Dir;
                float Push_Dist;
                Physics.ComputePenetration(cc, ModifyPos, cc.transform.rotation, ThisCollider, ThisCollider.transform.position, ThisCollider.transform.rotation, out Push_Dir, out Push_Dist);


                //===========This loosens to push collider on the normals ==========//
                if (intendedDirection)
                {
                    float GravityAnglePerc = Vector3.Angle(Push_Dir, gravityDir) / 145; //is the angle shooting up from the ground.
                    Push_Dir = Vector3.Lerp(Push_Dir, -gravityDir, GravityAnglePerc).normalized; //if it is, then push it with -gravity direction instead
                }
                //=================================================================//


                Vector3 push = (Push_Dir * Push_Dist);
                // if (push.magnitude > 0)
                // {
                ModifyPos += push;
                Debug.DrawLine(currentPos, ModifyPos);
                // }
            }

        }

        //get the difference between the new pos and original pos for push amount
        Vector3 PushAmount = (ModifyPos - currentPos);
        return PushAmount;

    }
    Vector3 SnapToGroundPush(Vector3 CurrentPos)
    {

        //get ground collider
        GroundCheck(CurrentPos, ref groundHit);

        //get ground
        if (groundHit.collider != null)
        {
            /*
            Vector3 ClosestPoint = Physics.ClosestPoint(groundHit.point, cc, CurrentPos, cc.transform.rotation);
            Debug.DrawLine(groundHit.point, ClosestPoint, Color.yellow);

            return (groundHit.point - ClosestPoint);
              */

            float difference = (groundHit.distance - smallValCompensation - (0.01f)); //requires a stupid small number offset?
            if (difference<0) return Vector3.zero;
          //  Debug.Log("difference?" + difference);
   


            return (difference * gravityDir);
          
        }
        else
        {
            return Vector3.zero;
        }
    }
    float smallValCompensation = 0.008f;
    void GroundCheck(Vector3 CurrentPos, ref RaycastHit ThisRayHit)
    {
        Vector3 p1;
        Vector3 p2;
        GetCapsuleStartEndPoints(cc, CurrentPos, out p1, out p2);
        Physics.CapsuleCast(p1, p2, cc.radius - smallValCompensation, gravityDir, out ThisRayHit, groundRayDist, GameGlobal.instance.groundLayer, QueryTriggerInteraction.Ignore);

        Debug.DrawRay(ThisRayHit.point, ThisRayHit.normal, Color.green);
    }



    ///----------- tools --------------------//

    public Vector3 RepairHitSurfaceNormal(RaycastHit hit)
    {
        RaycastHit repairHit;
        Vector3 normalsAvg = Vector3.zero;

        int i = 0;
        while (i < 3)
        {
            Vector3 rightCross = Vector3.Cross(-hit.normal, -gravityDir).normalized;

            var p = hit.point + (hit.normal * 0.1f);
            if (i == 1) p += rightCross * 0.1f;
            else if (i == 2) p -= rightCross * 0.1f;

            Ray ray = new Ray(p, -hit.normal);
            hit.collider.Raycast(ray, out repairHit, 0.11f);
            if (repairHit.collider != null)
            {
                Debug.DrawRay(repairHit.point, repairHit.normal, Color.cyan);
                normalsAvg += repairHit.normal;
                i++;
            }
            else
            {
                break;
            }
        }
        if (i >= 3) normalsAvg = (normalsAvg / (i + 1)).normalized;
        else normalsAvg = Vector3.zero;

        return normalsAvg;
    }
    /*    public Vector3 RepairHitSurfaceNormal(RaycastHit hit)
    {
        var p = hit.point + (hit.normal * 0.1f);
        Ray ray = new Ray(p, -hit.normal);
        RaycastHit repairHit;
        hit.collider.Raycast(ray, out repairHit, 0.15f);

        Debug.DrawRay(repairHit.point, repairHit.normal, Color.cyan);

        return repairHit.normal;
    }*/

    /// <param name="thisCC"></param>
    /// <param name="thisPos"></param>
    /// <param name="heightShrink">needs a small number like -0.05f cause unity may cast outside the capsule height</param>
    /// <param name="p1">start with radius offset</param>
    /// <param name="p2">end with radius offset</param>
    void GetCapsuleStartEndPoints(CapsuleCollider thisCC, Vector3 thisPos, out Vector3 p1, out Vector3 p2)
    {
        p1 = thisPos + thisCC.transform.TransformDirection(  thisCC.center) + (thisCC.transform.up * (((-thisCC.height /*- heightShrink*/) * 0.5f) + cc.radius));
        p2 = p1 + (thisCC.transform.up * ((thisCC.height/* + heightShrink*/) * 0.5f));
    }









RaycastHit currentGroundHit;
    public bool isGrounded { get { return currentGroundHit.collider != null; } }

    float GetGroundAngle (RaycastHit _hit) { return (_hit.collider != null) ? Vector3.Angle(_hit.normal, -gravityDir) : 0; }
    bool IsEdgeStep(RaycastHit _hit)
    {
        if (_hit.collider != null)
        {
            Vector3 RepairNormals = RepairHitSurfaceNormal(_hit);
            float RepairNormalsAngle = Vector3.Angle(RepairNormals, -gravityDir);
            //  Debug.Log("RepairNormalsAngle " + RepairNormalsAngle);
            return RepairNormals != Vector3.zero && RepairNormalsAngle != GetGroundAngle(_hit) && ((RepairNormalsAngle >= 70 && RepairNormalsAngle <= 110) || RepairNormalsAngle == 0);
        }
        return false;
    }
    bool GetIsMaxSlopeAngle(RaycastHit _hit)
    {
        return (_hit.collider != null && !IsEdgeStep(_hit) && minAngle > 0) ? GetGroundAngle(_hit) >= minAngle : false;
    }

    public bool IsMaxSlopeAngle { get { return GetIsMaxSlopeAngle(currentGroundHit); } }



    [Header("Platforming")]
    public bool activePlatformMoving = true;
    public bool rotatePlatformingSurfaceUp; //character would rotate like inception

    Coroutine PlatformingDelta_C;


    Quaternion? lastRot;
    Collider curFloorCol;
    IEnumerator PlatformingDelta()
    {
        while (true)
        {
            GroundCheck(cc.transform.position, ref currentGroundHit);

            if (activePlatformMoving)
            {
                if (currentGroundHit.collider != null && curFloorCol == currentGroundHit.collider && currentGroundHit.rigidbody != null)
                {
                    //pos
                    Vector3 relVel = currentGroundHit.rigidbody.GetPointVelocity(currentGroundHit.point);
                    cc.transform.position += relVel * Time.fixedDeltaTime;

                    //rot
                    Quaternion newRot = currentGroundHit.rigidbody.rotation;
                    if (lastRot.HasValue)
                    {
                        Quaternion deltaEular = newRot * Quaternion.Inverse(lastRot.Value);
                        cc.transform.rotation = deltaEular * cc.transform.rotation;


                        Vector3 _revUp = rotatePlatformingSurfaceUp ? currentGroundHit.normal : -gravityDir;
                        cc.transform.rotation = Quaternion.FromToRotation(cc.transform.up, _revUp) * cc.transform.rotation;

                    }
                    lastRot = newRot;
                }
                else
                {
                    lastRot = null;
                    curFloorCol = currentGroundHit.collider;
                }
            }

            yield return new WaitForFixedUpdate();
        }
    }
}
