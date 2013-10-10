using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FishMovement : MonoBehaviour {
  [SerializeField]
  private List<GameObject> waypoints;
  [SerializeField]
  private float forwardSpeed;
  [SerializeField]
  private float burstSpeed;
  [SerializeField]
  private float followingSpeed;
  [SerializeField]
  private float fastRotationSpeed;
  [SerializeField]
  private float obstacleAvoidanceRotationSpeed;
  [SerializeField]
  private float quickChangeOfDirectionDistance;
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

  void Start () {
    player = GameObject.FindWithTag("Player").transform;
    forwardSpeed = 16f;
    burstSpeed = 25f;
    followingSpeed = 16f;
    fastRotationSpeed = 75f;
    obstacleAvoidanceRotationSpeed = 1.5f;
    quickChangeOfDirectionDistance = .75f;
  }
  
  void Update () {
    if (needsNewWaypoint()){
      determineNextWaypoint();
    }
    moveTowardNextWaypoint();
  }

  private void mimicLeadFish(){
    if (leadFish != null){
      Vector3 targetPosition = leadFish.transform.position - leadFishOffset;
      Vector3 direction = directionAfterAvoidingObstacles(targetPosition);
      moveInDirection(targetPosition, direction);
    }
  }

  private void moveTowardNextWaypoint(){
    Vector3 targetPosition = nextWaypoint.position - leadFishOffset;
    Vector3 direction = directionAfterAvoidingObstacles(targetPosition);
    moveInDirection(targetPosition, direction);
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
    float forwardSensoryDistance = 10f;
    float angularSensoryDistance = 5f;
    float hitSensitivity = 75f;

    Vector3 forwardRay = transform.forward * forwardSensoryDistance;
    Vector3 leftRay = transform.forward * forwardSensoryDistance;
    Vector3 rightRay = transform.forward * forwardSensoryDistance;
    Vector3 topRay = transform.forward * angularSensoryDistance;
    Vector3 bottomRay = transform.forward * angularSensoryDistance;
    leftRay.x -= 2f;
    rightRay.x += 1f;
    topRay.y += 2f;
    bottomRay.y -= 2f;

    if (Physics.Raycast(transform.position, forwardRay, out hit, forwardSensoryDistance)){
      if (hit.transform != transform){
        direction += hit.normal * hitSensitivity;
      }
    }
    if (Physics.Raycast(transform.position, leftRay, out hit, forwardSensoryDistance)){
      if (hit.transform != transform){
        direction += hit.normal * hitSensitivity;
      }
    }
    if (Physics.Raycast(transform.position, rightRay, out hit, forwardSensoryDistance)){
      if (hit.transform != transform){
        direction += hit.normal * hitSensitivity;
      }
    }
    if (Physics.Raycast(transform.position, topRay, out hit, angularSensoryDistance)){
      if (hit.transform != transform){
        direction += hit.normal * hitSensitivity;
      }
    }
    if (Physics.Raycast(transform.position, bottomRay, out hit, angularSensoryDistance)){
      if (hit.transform != transform){
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
    if (Vector3.Distance(transform.position, nextWaypoint.position - leadFishOffset) < 4f){
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
