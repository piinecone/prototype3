using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof (TurtleController))]
public class FishMovement : MonoBehaviour {
  [SerializeField]
  private List<GameObject> waypoints;
  [SerializeField]
  private float forwardSpeed;
  [SerializeField]
  private float shoalingSpeed;
  [SerializeField]
  private float burstSpeed;
  [SerializeField]
  private float followingSpeed;
  [SerializeField]
  private float fastRotationSpeed;
  [SerializeField]
  private float rushingSpeed;
  [SerializeField]
  private float obstacleAvoidanceRotationSpeed;
  [SerializeField]
  private float followingRotationSpeed;
  [SerializeField]
  private float shoalingRotationSpeed;
  [SerializeField]
  private float quickChangeOfDirectionDistance;
  [SerializeField]
  public bool isLeadFish = false;
  [SerializeField]
  private SchoolOfFishMovement schoolOfFish;
  [SerializeField]
  private GameObject player;
  [SerializeField]
  private TurtleController turtleController;
  [SerializeField]
  private AudioSource startedFollowingSound;
  [SerializeField]
  private AudioSource stoppedFollowingSound;

  // waypoints
  private Transform nextWaypoint;
  private Transform lastWaypoint;
  private int nextWaypointIndex;

  // lead fish
  private GameObject leadFish;
  private Vector3 leadFishOffset;
  private float leadFishDistance;

  // bursts
  private float burstTimer = 1.5f;
  private float timeleft = 0;
  private float currentBurstSpeed;

  // player following
  private Vector3 randomizedPlayerOffset;
  private bool currentlyFollowingPlayer = false;
  private float patienceSeed = 7f;
  private float patienceLeft;
  private float patienceDistance = 20f;

  // barriers
  private Vector3 randomizedBarrierOffset;
  private Vector3 finishRushTargetPosition;
  private bool currentlyRushingABarrier = false;
  private bool currentlyFinishingRush = false;
  private GameObject targetedBarrier = null;
  private float rushRotationSpeed;
  private float scatterDistance = 12f;

  // trapped + shoaling
  private bool isShoaling;
  private bool isTrapped;
  private Transform shoalPoint;

  void Start () {
    player = GameObject.FindWithTag("Player");
    turtleController = player.GetComponent<TurtleController>();
    forwardSpeed = 16f;
    shoalingSpeed = 7f;
    burstSpeed = 25f;
    followingSpeed = 16.1f;
    rushingSpeed = 35f;
    fastRotationSpeed = 75f;
    obstacleAvoidanceRotationSpeed = 1.5f;
    followingRotationSpeed = 1.6f;
    shoalingRotationSpeed = 1.8f;
    rushRotationSpeed = 5f;
    quickChangeOfDirectionDistance = .75f;
    patienceLeft = patienceSeed;
  }
  
  void Update () {
    if (currentlyRushingABarrier){
      rushTowardBarrier();
    } else if (currentlyFinishingRush) {
      finishRushBehavior();
    } else if (currentlyFollowingPlayer){
      if (boredByPlayer()){
        currentlyFollowingPlayer = false;
        turtleController.removeFish(this);
        stoppedFollowingSound.Play();
      } else {
        moveTowardPlayer();
      }
    } else if (isShoaling) {
      shoalAroundShoalPoint();
    } else {
      if (needsNewWaypoint()) determineNextWaypoint();
      moveTowardNextWaypoint();
    }
  }

  private float distanceFromPlayer(){
    return Vector3.Distance(transform.position, player.transform.position);
  }

  private bool boredByPlayer(){
    if (turtleController.velocity() < 10f || distanceFromPlayer() > patienceDistance){
      patienceLeft -= Time.deltaTime;
    } else {
      patienceLeft = Random.Range(.75f, 2f) * patienceSeed;
    }
    return (patienceLeft <= 0f) ? true : false;
  }

  void OnTriggerEnter(Collider collider){
    if (collider.gameObject.tag == "PlayerInfluence" && shouldFollowPlayer()){
      if (!currentlyFollowingPlayer && !isTrapped){ // then look at the player
        Vector3 direction = player.transform.forward.normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, fastRotationSpeed * Time.deltaTime);
        randomizedPlayerOffset = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(-3f, 1f), Random.Range(0f, 0f));
        turtleController.addFish(this);
        startedFollowingSound.Play();
      }
      currentlyFollowingPlayer = true;
    }
  }

  private void rushTowardBarrier(){
    if (shouldContinueRushingBarrier()){
      Vector3 targetPosition = (targetedBarrier.transform.position - randomizedBarrierOffset);
      Vector3 direction = directionAfterAvoidingObstacles(targetPosition);
      Quaternion rotation = Quaternion.LookRotation(direction);
      transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rushRotationSpeed * Time.deltaTime);
      transform.position += transform.forward * rushingSpeed * Time.deltaTime;
      //Debug.DrawRay(transform.position, targetPosition, Color.blue);
    } else {
      if (!currentlyFinishingRush) beginFinishingBarrierRush(targetedBarrier);
      currentlyRushingABarrier = false;
      targetedBarrier = null;
    }
  }

  private void beginFinishingBarrierRush(GameObject barrier){
    finishRushTargetPosition = (transform.position + transform.up * scatterDistance);
    float xComponent = finishRushTargetPosition.x;
    finishRushTargetPosition.x = Random.Range(xComponent - 30f, xComponent + 30f);
    quicklyLookAt(finishRushTargetPosition);
    currentlyFinishingRush = true;
    turtleController.applyForceVectorToBarrier(finishRushTargetPosition, barrier);
  }

  private void finishRushBehavior(){
    if (shouldFinishRushBehavior()){
      // NOTE assuming "scatter" behavior by default for now;
      // eventually this will be determined by the barrier itself
      Vector3 direction = directionAfterAvoidingObstacles(finishRushTargetPosition);
      Quaternion rotation = Quaternion.LookRotation(direction);
      transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rushRotationSpeed * Time.deltaTime);
      transform.position += transform.forward * rushingSpeed * Time.deltaTime;
      //Debug.DrawRay(transform.position, finishRushTargetPosition, Color.blue);
    } else {
      currentlyFinishingRush = false;
    }
  }

  private bool shouldContinueRushingBarrier(){
    if (Vector3.Distance(transform.position, targetedBarrier.transform.position) > 15f){
      return true;
    } else {
      return false;
    }
  }

  private bool shouldFinishRushBehavior(){
    if (Vector3.Distance(transform.position, finishRushTargetPosition) > 5f){
      return true;
    } else {
      return false;
    }
  }

  private void moveTowardPlayer(){
    Vector3 targetPosition = (player.transform.position - player.transform.InverseTransformDirection(randomizedPlayerOffset));
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

  private void shoalAroundShoalPoint(){
    Vector3 targetPosition = shoalPoint.position;
    Vector3 direction = directionAfterAvoidingObstacles(targetPosition, 200f, 1f, 1f);
    Quaternion rotation = Quaternion.LookRotation(direction);
    transform.rotation = Quaternion.Slerp(transform.rotation, rotation, shoalingRotationSpeed * Time.deltaTime);
    transform.position += transform.forward * shoalingSpeed * Time.deltaTime;
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

  private void quicklyLookAt(Vector3 position){
    Vector3 direction = (position - transform.position).normalized;
    Quaternion rotation = Quaternion.LookRotation(direction);
    transform.rotation = Quaternion.Slerp(transform.rotation, rotation, fastRotationSpeed * Time.deltaTime);
  }

  private Vector3 directionAfterAvoidingObstacles(Vector3 targetPosition, float hitSensitivity=75f, float forwardSensoryDistance=10f, float angularSensoryDistance=5f){
    RaycastHit hit;
    Vector3 direction = (targetPosition - transform.position).normalized;
    float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

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
    if (hitTransform != transform && hitTransform != player.transform && hitTransform.gameObject != targetedBarrier){
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
    if (nextWaypointIndex < waypoints.Count) nextWaypoint = waypoints[nextWaypointIndex].transform;
  }

  public void setLeadFish(FishMovement fish){
    leadFish = fish.transform.gameObject;
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

  private bool shouldFollowPlayer(){
    return (isShoaling || playerIsInFront());
  }

  private bool playerIsInFront(){
    Vector3 direction = (player.transform.position - transform.position).normalized;
    Vector3 forward = transform.forward;
    float angle = Vector3.Angle(direction, forward);
    return (angle < 45F) ? true : false;
  }

  public void rushBarrier(GameObject barrier){
    if (!currentlyRushingABarrier && !currentlyFinishingRush){
      Vector3 direction = barrier.transform.position;
      Quaternion rotation = Quaternion.LookRotation(direction);
      transform.rotation = Quaternion.Slerp(transform.rotation, rotation, fastRotationSpeed * Time.deltaTime);
      randomizedBarrierOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
      currentlyRushingABarrier = true;
      targetedBarrier = barrier;
    }
  }

  // should be for lead fish only
  // TODO give leadfish their own behavior class
  public void setWaypoints(List<GameObject> theWaypoints){
    waypoints = theWaypoints;
  }

  public void setTrapped(bool trapped){
    isTrapped = trapped;
  }

  public void setShoalPoint(Transform theShoalPoint){
    shoalPoint = theShoalPoint;
  }

  public void toggleShoaling(bool shoaling){
    isShoaling = shoaling;
  }
}
