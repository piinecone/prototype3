using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof (TurtleController))]
public class FishMovement : MonoBehaviour {
  private bool debugging = false;

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
  private float rushRotationSpeed;
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
  private TurtleStateController playerStateController;
  private TurtleMovementController playerMovementController;
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
  public bool canFollowPlayer;
  private Vector3 randomizedPlayerOffset;
  private bool currentlyFollowingPlayer = false;
  private float patienceSeed = 7f;
  private float patienceLeft;
  private float patienceDistance = 27.5f;

  // barriers
  private Vector3 randomizedBarrierOffset;
  private Vector3 finishRushTargetPosition;
  private bool currentlyRushingABarrier = false;
  private bool currentlyFinishingRush = false;
  private GameObject targetedBarrier = null;
  private float scatterDistance = 12f;

  // barrier rendezvous points
  private bool currentlyMovingTowardRendezvousPoint = false;
  private GameObject rendezvousPoint = null;
  private float rendezvousDelayLeft = 5f;
  private bool isAborting = false;
  private bool hasReachedCurrentRendezvousPoint = false;

  // trapped + shoaling
  private bool isShoaling;
  private bool isTrapped;
  private Transform shoalPoint;

  // orbiting
  private bool orbitingUntilReleased = false;
  private GameObject orbitPoint;

  // corkscrew
  private bool isPerformingCorkscrew = false;
  private Vector3 corkscrewVector = Vector3.zero;
  private int corkscrewDirection = 0;
  private float corkscrewTimeLeft = 0f;
  private float corkscrewDuration = 0f;
  private float corkscrewOffset = 0f;
  private float corkscrewOffsetZ = 0f;

  // converting into energy
  private bool isConvertingIntoEnergy = false;

  // performance
  private bool shouldDoCalculations = false;
  private bool playerIsNearby = false;

  // particles
  private ParticleSystem particleEmitter;

  void Awake(){
    player = GameObject.FindWithTag("Player");
    turtleController = player.GetComponent<TurtleController>();
    playerStateController = player.GetComponent<TurtleStateController>();
    playerMovementController = player.GetComponent<TurtleMovementController>();
  }

  void Start () {
    forwardSpeed = 16f;
    shoalingSpeed = 7f;
    burstSpeed = 25f;
    followingSpeed = 16.1f;
    if (rushingSpeed == 0) rushingSpeed = 35f;
    if (fastRotationSpeed == 0) fastRotationSpeed = 75f;
    obstacleAvoidanceRotationSpeed = 1.5f;
    followingRotationSpeed = 1.6f;
    shoalingRotationSpeed = 1.8f;
    if (rushRotationSpeed == 0) rushRotationSpeed = 5f;
    quickChangeOfDirectionDistance = .75f;
    patienceLeft = patienceSeed;
    particleEmitter = GetComponent<ParticleSystem>();

    InvokeRepeating("checkIfShouldStartDoingCalculations", Random.Range(3,15), 5);
  }
  
  //void Update () {
  void LateUpdate () {
    if (debugging){
      patienceLeft = 200f;
      if (Time.time > 2f && Time.time < 4.5f && turtleController.followingFish.numberOfFollowingFish() < 10){
        turtleController.addFish(this);
        transform.position = player.transform.position;
        currentlyFollowingPlayer = true;
      }
    }

    if (orbitingUntilReleased){
      orbitAroundOrbitPoint();
    } else if (currentlyRushingABarrier){
      rushTowardBarrier();
    } else if (currentlyFinishingRush) {
      finishRushBehavior();
    } else if (currentlyMovingTowardRendezvousPoint) {
      moveTowardRendezvousPoint();
    } else if (currentlyFollowingPlayer){
      if (isPerformingCorkscrew){
        performCorkscrew();
      } else if (isConvertingIntoEnergy){
        ConvertIntoEnergy();
      //} else if (boredByPlayer()){
      //  stopFollowingPlayer();
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

  private float currentDistanceFromPlayer(){
    return distanceFromPlayer();
  }

  private float distanceFromPlayer(){
    return Vector3.Distance(transform.position, player.transform.position);
  }

  private bool boredByPlayer(){
    if (turtleController.isFrozen()) return false;

    if (playerMovementController.Velocity() < 10f || distanceFromPlayer() > patienceDistance){
      patienceLeft -= Time.deltaTime;
    } else {
      patienceLeft = Random.Range(.75f, 2f) * patienceSeed;
    }
    return (patienceLeft <= 0f) ? true : false;
  }

  void OnTriggerEnter(Collider collider){
    if (collider.gameObject.tag == "PlayerInfluence"){
      if (shouldFollowPlayer() && playerStateController.NumberOfFollowingFish() < 50f){
        if (!currentlyFollowingPlayer && !isTrapped){
          Vector3 direction = player.transform.forward.normalized;
          Quaternion rotation = Quaternion.LookRotation(direction);
          transform.rotation = Quaternion.Slerp(transform.rotation, rotation, fastRotationSpeed * Time.deltaTime);
          randomizedPlayerOffset = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(-3f, 1f), Random.Range(0f, 0f));
          turtleController.addFish(this);
          startedFollowingSound.Play();
        }
        currentlyFollowingPlayer = true;
      } else if (!canFollowPlayer && !isTrapped && !isAborting){
        hurryTowardTheNextSequentialBarrier();
      }
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
    currentlyFinishingRush = true;
    finishRushTargetPosition = (transform.position + transform.up * scatterDistance);
    float xComponent = finishRushTargetPosition.x;
    finishRushTargetPosition.x = Random.Range(xComponent - 30f, xComponent + 30f);
    quicklyLookAt(finishRushTargetPosition);
    turtleController.applyForceVectorToBarrier(finishRushTargetPosition, barrier, isLeadFish: isLeadFish);
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
    return (Vector3.Distance(transform.position, targetedBarrier.transform.position) > 10f);
  }

  private bool shouldFinishRushBehavior(){
    return (Vector3.Distance(transform.position, finishRushTargetPosition) > 5f);
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
    float speed = Mathf.Max(followingSpeed, playerMovementController.Velocity() - 1f) * speedMultiplier;
    transform.position += transform.forward * speed * Time.deltaTime;
  }

  private void orbitAroundOrbitPoint(){
    Vector3 targetPosition = orbitPoint.transform.position;
    float distance = Vector3.Distance(transform.position, targetPosition);
    Vector3 direction = directionAfterAvoidingObstacles(targetPosition);
    Quaternion rotation = Quaternion.LookRotation(direction);
    transform.rotation = Quaternion.Slerp(transform.rotation, rotation, obstacleAvoidanceRotationSpeed * Time.deltaTime);
    transform.position += transform.forward * forwardSpeed * 2f * Time.deltaTime;
  }

  private void shoalAroundShoalPoint(){
    Vector3 targetPosition = shoalPoint.position;
    float distance = Vector3.Distance(transform.position, targetPosition);
    Vector3 direction = Vector3.zero;
    if (isAborting){
      direction = directionAfterAvoidingObstacles(targetPosition);
    } else {
      direction = directionAfterAvoidingObstacles(targetPosition, 200f, 1f, 1f);
    }
    Quaternion rotation = Quaternion.LookRotation(direction);
    transform.rotation = Quaternion.Slerp(transform.rotation, rotation, shoalingRotationSpeed * Time.deltaTime);
    float speed = distance >= 20f ? forwardSpeed : shoalingSpeed;
    transform.position += transform.forward * speed * Time.deltaTime;
    if (isAborting && distance < 20f){
      isAborting = false;
      turtleController.showPlayerInitialBarrier(schoolOfFish);
    }
  }

  private void moveTowardNextWaypoint(){
    Vector3 targetPosition = nextWaypoint.position - leadFishOffset;
    Vector3 direction = directionAfterAvoidingObstacles(targetPosition);
    moveInDirection(targetPosition, direction);
  }

  private void moveTowardRendezvousPoint(){
    if (rendezvousDelayLeft <= 0f){
      rendezvousDelayLeft = 1.5f;
      currentlyMovingTowardRendezvousPoint = false;
      currentlyRushingABarrier = false;
      currentlyFinishingRush = false;
      turtleController.rushRequiredSchools();
    } else {
      float distanceFromPlayer = currentDistanceFromPlayer();
      float distanceFromPoint = Vector3.Distance(transform.position, rendezvousPoint.transform.position);
      if (distanceFromPoint < 20f){
        hasReachedCurrentRendezvousPoint = true;
        if (turtleController.needsRendezvousPointReminder()) turtleController.rendezvousPointReached(rendezvousPoint);
      }

      if (distanceFromPlayer < 20f && turtleController.allRequiredSchoolsAreInPlace()){
        if (hasReachedCurrentRendezvousPoint){
          rendezvousDelayLeft -= Time.deltaTime;
        } else {
          rendezvousDelayLeft = 1.5f;
        }
      }

      Vector3 targetPosition = rendezvousPoint.transform.position - leadFishOffset;
      Vector3 direction = directionAfterAvoidingObstacles(targetPosition);
      moveInDirection(targetPosition, direction);
    }
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
    // only perform AI steering when visible
    if (!shouldDoCalculations) return direction;

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
    if (hitTransform != transform && hitTransform != player.transform && hitTransform.gameObject != targetedBarrier && !hitTransform.collider.isTrigger){
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
    if (waypoints.Count <= nextWaypointIndex)
      nextWaypointIndex = 0;
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
    return (canFollowPlayer && (isShoaling || playerIsInFront()));
  }

  private bool playerIsInFront(){
    return true;
    //Vector3 direction = (player.transform.position - transform.position).normalized;
    //Vector3 forward = transform.forward;
    //float angle = Vector3.Angle(direction, forward);
    //return (angle < 45F) ? true : false;
  }

  public void rushBarrier(GameObject barrier, GameObject aRendezvousPoint=null, bool force=false){
    if (force || (!currentlyRushingABarrier && !currentlyFinishingRush)){
      targetedBarrier = barrier;
      if (aRendezvousPoint != null){
        currentlyMovingTowardRendezvousPoint = true;
        if (rendezvousPoint != aRendezvousPoint){
          hasReachedCurrentRendezvousPoint = false;
          rendezvousPoint = aRendezvousPoint;
          schoolOfFish.rendezvousFor(barrier, aRendezvousPoint); // may not need this
        }
      } else {
        currentlyRushingABarrier = true;
        Vector3 direction = barrier.transform.position;
        Quaternion rotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, fastRotationSpeed * Time.deltaTime);
        randomizedBarrierOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        emitBubbleTrail();
        //Invoke("emitBubbleTrail", 1f);
      }
    }
  }

  private void emitBubbleTrail(){
    particleEmitter.Play();
  }

  public void rushTargetedBarrier(){
    if (targetedBarrier != null) rushBarrier(targetedBarrier);
  }

  public void abortRushAttempt(){
    if (isSpecial()){
      isShoaling = true;
      isAborting = true;
      currentlyRushingABarrier = false;
      currentlyFinishingRush = false;
      currentlyMovingTowardRendezvousPoint = false;
    } else {
      currentlyRushingABarrier = false;
      currentlyFinishingRush = false;
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

  private void hurryTowardTheNextSequentialBarrier(){
    foreach(FishMovement fish in schoolOfFish.allFish())
      turtleController.addFish(fish);
    turtleController.rushNextSequentialBarrier();
  }

  public void stopFollowingPlayer(bool playSound=true){
    currentlyFollowingPlayer = false;
    turtleController.removeFish(this);
    if (playSound) stoppedFollowingSound.Play();
  }

  public bool isSpecial(){
    return !canFollowPlayer;
  }

  public void BeginToOrbit(GameObject aGameObject){
    orbitPoint = aGameObject;
    orbitingUntilReleased = true;
  }

  public void StopOrbiting(){
    orbitingUntilReleased = false;
  }

  public bool isTheLeadFish(){
    return isLeadFish;
  }

  public SchoolOfFishMovement parentSchool(){
    return schoolOfFish;
  }

  public bool isNearRendezvousPoint(){
    return hasReachedCurrentRendezvousPoint;
  }

  public void forceRushWithRendezvous(GameObject aBarrier, GameObject aRendezvousPoint){
    currentlyRushingABarrier = false;
    currentlyFinishingRush = false;
    currentlyMovingTowardRendezvousPoint = true;
    if (rendezvousPoint != aRendezvousPoint){
      hasReachedCurrentRendezvousPoint = false;
      rendezvousPoint = aRendezvousPoint;
    }
  }

  void checkIfShouldStartDoingCalculations(){
    shouldDoCalculations = ((playerIsNearby && renderer.isVisible) || performingNecessaryMovement());
  }

  bool performingNecessaryMovement(){
    return (currentlyRushingABarrier || currentlyFinishingRush ||
        currentlyFollowingPlayer || currentlyMovingTowardRendezvousPoint);
  }

  public void playerIsClose(bool state=true){
    playerIsNearby = state;
  }

  public void PerformCorkscrewManeuver(int direction, float duration){
    corkscrewDirection = direction;
    corkscrewVector = transform.forward;
    corkscrewOffset = Random.Range(0f,2f);
    corkscrewOffsetZ = Random.Range(-15f,-5f);
    isPerformingCorkscrew = true;
    corkscrewDuration = duration;
    corkscrewTimeLeft = corkscrewDuration;
    InvokeRepeating("emitBubbleTrail", Random.Range(0f, 1f), Random.Range(.6f, 1f));
  }

  private void performCorkscrew(){
    if (corkscrewTimeLeft > 0f){
      float speed = 20f;
      float radius = 5f;
      float seed = Time.time + corkscrewOffset;
      float zOffset = corkscrewOffsetZ + (corkscrewDuration - corkscrewTimeLeft) * 10f;
      corkscrewVector.z = player.transform.forward.z + zOffset;
      if (corkscrewDirection == 1){
        corkscrewVector.x = Mathf.Sin(seed * speed) * radius;
        corkscrewVector.y = Mathf.Cos(seed * speed) * radius;
      } else {
        corkscrewVector.x = Mathf.Cos(seed * speed) * radius;
        corkscrewVector.y = Mathf.Sin(seed * speed) * radius;
      }
      corkscrewVector = player.transform.TransformDirection(corkscrewVector);
      Vector3 targetPosition = player.transform.position + corkscrewVector;
      Vector3 direction = (targetPosition - transform.position).normalized;
      Quaternion rotation = Quaternion.LookRotation(direction);
      transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 30f * Time.deltaTime);
      transform.position += direction * 45f * Time.deltaTime;
      corkscrewTimeLeft -= Time.deltaTime;
    } else {
      CancelInvoke("emitBubbleTrail");
      isPerformingCorkscrew = false;
    }
  }

  public void ConvertIntoEnergy(){
    if (currentDistanceFromPlayer() > 1f){
      isConvertingIntoEnergy = true;
      Vector3 targetPosition = player.transform.position;
      Vector3 direction = (targetPosition - transform.position).normalized;
      Quaternion rotation = Quaternion.LookRotation(direction);
      transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 15f * Time.deltaTime);
      transform.position += direction * 45f * Time.deltaTime;
    } else if (isConvertingIntoEnergy) {
      playerStateController.FishDidConvertIntoEnergy();
      isConvertingIntoEnergy = false;
      Disable();
    }
  }

  public void Disable(){
    this.enabled = false;
    GetComponent<MeshRenderer>().enabled = false;
  }

  public void Enable(bool forcePosition = false){
    this.enabled = true;
    GetComponent<MeshRenderer>().enabled = true;
    if (forcePosition) transform.position = player.transform.position;
  }

  public void SetAsSpecial(bool special){
    if (special) turtleController.AddSpecialFish(this);
  }
}
