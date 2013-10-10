using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FishMovement : MonoBehaviour {
  [SerializeField]
  private List<GameObject> waypoints;
  [SerializeField]
  private float forwardSpeed = 25f;
  [SerializeField]
  private float burstSpeed = 40f;
  [SerializeField]
  private float fastRotationSpeed = .75f;
  [SerializeField]
  private float obstacleAvoidanceRotationSpeed = 1f;
  [SerializeField]
  private float quickChangeOfDirectionDistance = 2f;
  [SerializeField]
  public bool isLeadFish = false;
  [SerializeField]
  private SchoolOfFishMovement schoolOfFish;

  private Transform nextWaypoint;
  private Transform lastWaypoint;
  private int nextWaypointIndex;
  private GameObject leadFish;
  private Vector3 leadFishOffset;
  private float leadFishDistance;
  private Vector3 adjustedTarget;
  private float burstTimer = 1.5f;
  private float timeleft = 0;
  private float currentBurstSpeed;

  // Use this for initialization
  void Start () {
    //if (isLeadFish){
    //  setNextWaypoint(0);
    //}
  }
  
  // Update is called once per frame
  void Update () {
    if (needsNewWaypoint()){
      determineNextWaypoint();
    }
    moveTowardNextWaypoint();
    //if (isLeadFish){
    //}
    //} else {
    //  mimicLeadFish();
    //}
  }

  private void mimicLeadFish(){
    if (leadFish != null){
      //transform.rotation = leadFish.transform.rotation;
      Vector3 targetPosition = leadFish.transform.position - leadFishOffset;
      Vector3 direction = directionAfterAvoidingObstacles(targetPosition);
      moveInDirection(targetPosition, direction);
      //transform.position = Vector3.Lerp(transform.position, target, 0.5f);

      // TODO randomized micromovents will only work if they're applied when the waypoint is
      // determined, not throughout the regular movement updates
      //Vector3 exactPosition = leadFish.transform.position - leadFishOffset;
      //transform.position = new Vector3(exactPosition.x + Random.Range(-.1f, .1f),
      //                                 exactPosition.y + Random.Range(-.1f, .1f),
      //                                 exactPosition.z + Random.Range(-.1f, .1f));
    }
  }

  private void moveTowardNextWaypoint(){
    Vector3 targetPosition = nextWaypoint.position - leadFishOffset;
    Vector3 direction = directionAfterAvoidingObstacles(targetPosition);
    moveInDirection(targetPosition, direction);

    //transform.LookAt(direction);
    //if (justPassedWaypoint()){
    //  transform.position = LerpByDistance(transform.position, nextWaypoint.position, quickChangeOfDirectionDistance);
    //} else {
    //  transform.position = Vector3.MoveTowards(transform.position, nextWaypoint.position, speed * Time.deltaTime);
    //}
  }

  private void moveInDirection(Vector3 targetPosition, Vector3 direction){
    Quaternion rotation = Quaternion.LookRotation(direction);
    if (burstToNextWaypoint(false)){
      smoothlyLookAtNextWaypoint();
      transform.position += transform.forward * currentBurstSpeed * Time.deltaTime;
    } else {
      transform.rotation = Quaternion.Slerp(transform.rotation, rotation, obstacleAvoidanceRotationSpeed * Time.deltaTime);
      transform.position += transform.forward * forwardSpeed * Time.deltaTime;
    }
  }

  private void smoothlyLookAtNextWaypoint(){
    Vector3 targetPosition = nextWaypoint.position - leadFishOffset;
    Vector3 direction = (targetPosition - transform.position).normalized;
    Quaternion rotation = Quaternion.LookRotation(direction);
    transform.rotation = Quaternion.Slerp(transform.rotation, rotation, fastRotationSpeed * Time.deltaTime);
  }

  private Vector3 directionAfterAvoidingObstacles(Vector3 targetPosition){
    RaycastHit hit;
    Vector3 direction = (targetPosition - transform.position).normalized;
    float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
    float sensoryDistance = 10f;
    float hitSensitivity = 50f;

    Vector3 forwardRay = transform.forward * distanceToTarget;
    Vector3 leftRay = transform.forward * distanceToTarget;
    leftRay.x -= .5f;
    Vector3 rightRay = transform.forward * distanceToTarget;
    rightRay.x += .5f;

    // TODO if i can see my target that's where i'm looking (?)
    //if (Physics.Raycast(transform.position, direction * distanceToTarget, out hit, distanceToTarget)){
    //  if (hit.transform.position == targetPosition){
    //    //Debug.DrawLine(transform.position, hit.point, Color.magenta);
    //  }
    //}
    if (Physics.Raycast(transform.position, forwardRay, out hit, sensoryDistance)){
      if (hit.transform != transform){
        //Debug.DrawLine(forwardRay, hit.point, Color.red);
        direction += hit.normal * hitSensitivity;
      }
    }
    if (Physics.Raycast(transform.position, leftRay, out hit, sensoryDistance)){
      if (hit.transform != transform){
        //Debug.DrawLine(leftRay, hit.point, Color.green);
        direction += hit.normal * hitSensitivity;
      }
    }
    if (Physics.Raycast(transform.position, rightRay, out hit, sensoryDistance)){
      if (hit.transform != transform){
        //Debug.DrawLine(rightRay, hit.point, Color.blue);
        direction += hit.normal * hitSensitivity;
      }
    }

    return direction;
  }

  private bool justPassedWaypoint(){
    if (lastWaypoint != null && Vector3.Distance(transform.position, (lastWaypoint.position - leadFishOffset)) < 10f){
      return true;
    } else {
      return false;
    }
  }

  private bool needsNewWaypoint(){
    if (Vector3.Distance(transform.position, nextWaypoint.position - leadFishOffset) < .5f){
      return true;
    } else {
      return false;
    }
  }

  private void determineNextWaypoint(){
    nextWaypointIndex++;
    if (waypoints.Count <= nextWaypointIndex){
      nextWaypointIndex = 0;
    }
    setNextWaypoint(nextWaypointIndex);
    schoolOfFish.BroadcastNextWaypoint(nextWaypointIndex);
  }

  public Vector3 LerpByDistance(Vector3 from, Vector3 target, float distance){
    return (distance * Vector3.Normalize(target - from) + from);
  }

  public void setNextWaypoint(int index){
    nextWaypointIndex = index;
    lastWaypoint = nextWaypoint;
    nextWaypoint = waypoints[nextWaypointIndex].transform;
  }

  public void setLeadFish(GameObject fish){
    leadFish = fish;
    leadFishOffset = leadFish.transform.position - transform.position;
    leadFishDistance = Vector3.Distance(transform.position, leadFish.transform.position);
  }

  public bool burstToNextWaypoint(bool start){
    timeleft = start ? burstTimer : (timeleft -= Time.deltaTime);
    if (timeleft > 0f){
      currentBurstSpeed = (timeleft > (burstTimer * .8f)) ? burstSpeed : forwardSpeed;
      return true;
    } else {
      return false;
    }
  }
}
