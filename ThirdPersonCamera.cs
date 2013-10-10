using UnityEngine;
using System.Collections;

public class ThirdPersonCamera : MonoBehaviour {

   [SerializeField]
   private float distanceAway;
   [SerializeField]
   private float distanceUp;
   [SerializeField]
   private float smooth;
   [SerializeField]
   private Transform follow;
   [SerializeField]
   private float widescreen = 0.2f;
   [SerializeField]
   private float targetingTime = 0.5f;
   [SerializeField]
   private TurtleState turtleState;

   private Vector3 targetPosition;
   private Vector3 lookDir;
   private Vector3 velocityCamSmooth = Vector3.zero;
   private float camSmoothDampTime = 0.1f;
   private CamStates camState = CamStates.Behind;

   public enum CamStates {
     Behind,
     Target
   }

   // Use this for initialization
   void Start () {
     follow = GameObject.FindWithTag("Player").transform;
     lookDir = follow.forward;
   }
   
   // Update is called once per frame
   void Update () {
   
   }

   void LateUpdate() {
     distanceAway = 15;
     distanceUp = 2;
     //if (turtleState.isUnderwater()){
     //  distanceAway = 15;
     //  distanceUp = 2;
     //} else {
     //  distanceAway = 10;
     //  distanceUp = 4;
     //}

     Vector3 characterOffset = follow.position + new Vector3(0f, distanceUp, 0f);

     // Determine camera state
     if (Input.GetAxis("Fire3") > 0.01f){
       //barEffect.coverage = Mathf.SmoothStep(barEffect.coverage, widescreen, targetingTime);
       camState = CamStates.Target;
     } else {
       //barEffect.coverage = Mathf.SmoothStep(barEffect.coverage, 0f, targetingTime);
       camState = CamStates.Behind;
     }

     switch(camState){
       case CamStates.Behind:
         // calculate direction from camera to player, kill y, and normalize to give a valid direction with unit magnitude
         lookDir = characterOffset - this.transform.position;
         lookDir.y = 0;
         lookDir.Normalize();
         //Debug.DrawRay(this.transform.position, lookDir, Color.green);
         //Debug.DrawLine(follow.position, targetPosition, Color.magenta);
         break;
       case CamStates.Target:
         lookDir = follow.forward;
         break;
     }

     targetPosition = characterOffset + follow.up * distanceUp - lookDir * distanceAway;
     //CompensateForWalls(characterOffset, ref targetPosition);
     smoothPosition(this.transform.position, targetPosition);
     transform.LookAt(follow);
   }

   private void CompensateForWalls(Vector3 fromObject, ref Vector3 toTarget){
     Debug.DrawLine(fromObject, toTarget, Color.cyan);
     RaycastHit wallHit = new RaycastHit();
     if (Physics.Linecast(fromObject, toTarget, out wallHit))
     {
       Debug.DrawRay(wallHit.point, Vector3.left, Color.red);
       toTarget = new Vector3(wallHit.point.x, toTarget.y, wallHit.point.z);
     }
   }

   private void smoothPosition(Vector3 fromPos, Vector3 toPos){
     this.transform.position = Vector3.SmoothDamp(fromPos, toPos, ref velocityCamSmooth, camSmoothDampTime);
   }
}
