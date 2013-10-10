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
  private float followingRotationSpeed;
  [SerializeField]
  private float quickChangeOfDirectionDistance;
  [SerializeField]
  public bool isLeadFish = false;
  [SerializeField]
  private SchoolOfFishMovement schoolOfFish;
  [SerializeField]
  private Transform player;

  private Transform nextWaypoint;
  private Transform lastWaypoint;
  private int nextWaypointIndex;
  private Vector3 randomizedPlayerOffset;
  private GameObject leadFish;
  private Vector3 leadFishOffset;
  private float leadFishDistance;
  private Vector3 adjustedTarget;
  private float burstTimer = 1.5f;
  private float timeleft = 0;
  private float currentBurstSpeed;
  private bool currentlyFollowingPlayer = false;

  void Start () {
    player = GameObject.FindWithTag("Player").transform;
    forwardSpeed = 16f;
    burstSpeed = 25f;
    followingSpeed = 16.1f;
    fastRotationSpeed = 75f;
    obstacleAvoidanceRotationSpeed = 1.5f;
    followingRotationSpeed = 1.6f;
    quickChangeOfDirectionDistance = .75f;
  }
  
  void Update () {
    if (!currentlyFollowingPlayer){
      if (needsNewWaypoint()) determineNextWaypoint();
      moveTowardNextWaypoint();
    } else {
      moveTowardPlayer();
    }
  }

  void OnTriggerEnter(Collider collider){
    if (collider.gameObject.tag == "PlayerInfluence" && playerIsInFront()){
      if (!currentlyFollowingPlayer){ // then look at the player
        Vector3 direction = player.forward.normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, fastRotationSpeed * Time.deltaTime);
        randomizedPlayerOffset = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(-3f, 1f), Random.Range(0f, 0f));
      }
      currentlyFollowingPlayer = true;
    }
  }

  private void moveTowardPlayer(){
    Vector3 targetPosition = (player.position - player.InverseTransformDirection(randomizedPlayerOffset));
    Vector3 direction = directionAfterAvoidingObstacles(targetPosition);
    Quaternion rotation = Quaternion.LookRotation(direction);
    //transform.rotation = Quaternion.Slerp(transform.rotation, rotation, obstacleAvoidanceRotationSpeed * Time.deltaTime);
    transform.rotation = Quaternion.Slerp(transform.rotation, rotation, followingRotationSpeed * Time.deltaTime);

    // make speed proportional to distance
    //float speed = (Vector3.Distance(transform.position, player.position) < 8f) ? 8f : followingSpeed;
    //float distanceFromPlayer = Vector3.Distance(transform.position, player.position);
    float speedMultiplier = 1;
    //if (distanceFromPlayer >= 8f && distanceFromPlayer <= 16f){
    //  speedMultiplier = distanceFromPlayer / 8f;
    //  if (speedMultiplier < .75f) speedMultiplier = .75f;
    //} else if (distanceFromPlayer < 8f) {
    //  speedMultiplier = .5f;
    //}
    float speed = followingSpeed * speedMultiplier;
    transform.position += transform.forward * speed * Time.deltaTime;
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
      if (transformShouldBeAvoided(hit.transform)){
        direction += hit.normal * hitSensitivity;
      }
    }
    if (Physics.Raycast(transform.position, leftRay, out hit, forwardSensoryDistance)){
      if (transformShouldBeAvoided(hit.transform)){
        direction += hit.normal * hitSensitivity;
      }
    }
    if (Physics.Raycast(transform.position, rightRay, out hit, forwardSensoryDistance)){
      if (transformShouldBeAvoided(hit.transform)){
        direction += hit.normal * hitSensitivity;
      }
    }
    if (Physics.Raycast(transform.position, topRay, out hit, angularSensoryDistance)){
      if (transformShouldBeAvoided(hit.transform)){
        direction += hit.normal * hitSensitivity;
      }
    }
    if (Physics.Raycast(transform.position, bottomRay, out hit, angularSensoryDistance)){
      if (transformShouldBeAvoided(hit.transform)){
        direction += hit.normal * hitSensitivity;
      }
    }

    return direction;
  }

  private bool transformShouldBeAvoided(Transform hitTransform){
    if (hitTransform != transform && hitTransform != player){
      return true;
    } else {
      return false;
    }
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
    if (currentlyFollowingPlayer) return false;

    timeleft = start ? burstTimer : (timeleft -= Time.deltaTime);
    if (timeleft > 0f){
      currentBurstSpeed = (timeleft > (burstTimer * .8f)) ? burstSpeed : forwardSpeed;
      return true;
    } else {
      return false;
    }
  }

  private bool playerIsInFront(){
    Vector3 direction = (player.position - transform.position).normalized;
    Vector3 forward = transform.forward;
    float angle = Vector3.Angle(direction, forward);
    return (angle < 45F) ? true : false;
  }
}
