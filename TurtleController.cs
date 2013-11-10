using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof (Animator))]
[RequireComponent(typeof (CapsuleCollider))]
[RequireComponent(typeof (Rigidbody))]
[RequireComponent(typeof (FollowingFish))]
[RequireComponent(typeof (BarrierController))]
public class TurtleController : MonoBehaviour {

  public float animSpeed = 1.5f;
  public float speed = 4f;
  public float gravity = 20f;
  public FollowingFish followingFish;
  [SerializeField]
  private float defaultRotateSpeed;
  [SerializeField]
  private float targetModeRotateSpeed;
  [SerializeField]
  private ThirdPersonCamera thirdPersonCamera;
  [SerializeField]
  private CutSceneManager manager;
  [SerializeField]
  private SunkenStaircase sunkenStaircase;

  private float speedInMedium = 8f;
  private Vector3 moveDirection = Vector3.zero;
  private CharacterController controller;
  private Animator anim;
  private CapsuleCollider col;
  private AnimatorStateInfo currentBaseState;
  private Vector3 previousPosition;
  private BarrierController barrierController;

  // physics
  private bool isNearSurface = false;
  private float currentLookPosY = 0f;
  [SerializeField]
  private float surfaceLevel = 177.50f;

  // temporary acceleration
  private float initialMinimumSpeed;
  private float minSpeedInMedium;
  private float maxSpeedInMedium;
  private float targetSpeedInMedium;
  private bool currentlyAccelerating;

  // sequential barriers
  [SerializeField]
  private List<GameObject> sequentialBarriers;
  private GameObject nextBarrier = null;
  private Barrier nextBarrierInstance = null;
  private bool schoolsMayLeave = false;

  // cut scenes
  private bool acceptRendezvousPointCutSceneReminder = true;
  private bool willShowPlayerInitialBarrier = false;

  void Start () {
    anim = GetComponent<Animator>();               
    col = GetComponent<CapsuleCollider>();          
    controller = GetComponent<CharacterController>();
    followingFish = GetComponent<FollowingFish>();
    barrierController = GetComponent<BarrierController>();
    speedInMedium = 16.5f;
    minSpeedInMedium = 16.5f;
    maxSpeedInMedium = 20f;
    targetSpeedInMedium = 16.5f;
    currentlyAccelerating = false;
    initialMinimumSpeed = minSpeedInMedium;
    nextBarrier = sequentialBarriers[0];
    nextBarrierInstance = barrierController.getBarrierInstanceFromBarrierGameObject(nextBarrier);
  }

  void FixedUpdate ()
  {
     float h = Input.GetAxis("Horizontal");
     float v = Input.GetAxis("Vertical");
     anim.SetFloat("Speed", v);
     anim.SetFloat("Direction", h);
     anim.speed = animSpeed;
     currentBaseState = anim.GetCurrentAnimatorStateInfo(0);
  }
  
  void Update () {
    calculateSpeedInMedium();
    previousPosition = transform.position;
    if (isUnderwater()){
      swim();
      anim.SetBool("Underwater", true); 
    } else if (isEmerging()){
      walk(slope: 60f, normalRay: Vector3.forward);
      anim.SetBool("Underwater", false); 
    } else {
      walk(slope: 45f, normalRay: Vector3.down);
      anim.SetBool("Underwater", false); 
    }
  }

  void walk(float slope, Vector3 normalRay){
    if (slope != null) controller.slopeLimit = slope;
    RaycastHit hit;
    float distance = 4f;
    Vector3 downRay = transform.TransformDirection(normalRay);
    Vector3 normal = transform.TransformDirection(Vector3.up);
    Vector3 lookDirection = transform.forward + transform.TransformDirection(new Vector3(Input.GetAxis("Horizontal"), 0, 0));
    if (Physics.Raycast(transform.position, downRay, out hit, distance))
      normal = hit.normal;
    Quaternion rotation = Quaternion.FromToRotation(transform.up, normal);
    rotation = rotation * Quaternion.LookRotation(lookDirection);
    transform.rotation = Quaternion.Slerp(transform.rotation, rotation, currentRotateSpeed() * .1f * Time.deltaTime);

    gravity = 80f;
    moveDirection = new Vector3(0, 0, Input.GetAxis("Vertical"));
    moveDirection = transform.TransformDirection(moveDirection);
    moveDirection *= speedInMedium;
    moveDirection.y -= gravity * Time.deltaTime;
    controller.Move(moveDirection * Time.deltaTime);
  }

  void swim(){
    gravity = 15f;

    // apply keyboard
    moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
    if (moveDirection.z < 0f) moveDirection.z = 0f;
    moveDirection = transform.TransformDirection(moveDirection);
    moveDirection *= speedInMedium;

    // apply mouselook
    Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
    Vector3 lookPos = mouseRay.direction;// - transform.position;
    lookPos.y *= currentYAxisMultiplier();

    if (transform.position.y >= surfaceLevel && lookPos.y >= 0f){
      lookPos.y = currentLookPosY;
      lookPos = Vector3.Lerp(lookPos, new Vector3(lookPos.x, 0f, lookPos.z), 10f * Time.deltaTime);
    }

    Quaternion targetRotation = Quaternion.LookRotation(lookPos);
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, currentRotateSpeed() * Time.deltaTime);
    if (!isNearSurface) moveDirection.y -= gravity * Time.deltaTime;
    controller.Move(moveDirection * Time.deltaTime);

    if (isNearSurface)
      if (transform.position.y > surfaceLevel) transform.position = new Vector3(transform.position.x, surfaceLevel, transform.position.z);
    currentLookPosY = transform.forward.y;
  }

  private bool isTouchingTerrainFromSurface(){
    if (transform.position.y < (surfaceLevel - 2f)) return false;

    float distance = 5f;
    RaycastHit hit;
    Vector3 forwardRay = transform.forward * distance;
    Debug.DrawRay(transform.position, forwardRay, Color.red);
    if (Physics.Raycast(transform.position, forwardRay, out hit, distance)){
      return (hit.normal.y > .3);
    } else {
      return false;
    }
  }

  // Uncomment if you want to immediately raise the staircase
  //void LateUpdate(){
  //  if (followingFish.numberOfFollowingFish() > 5 && Time.time > 5f && Time.time < 5.5f){
  //    followingFish.beginOrbiting(sunkenStaircase.getFocalPoint());
  //    sunkenStaircase.scheduleRaise();
  //    manager.cutTo(sunkenStaircase.getFocalPoint(), 40f, new Vector3(-10f, 10f, -50f));
  //  }
  //}

  //void legacySwim(){
  //  gravity = 20f;
  //  //speedInMedium = speed * 4.1f;
  //  //moveDirection = new Vector3(Input.GetAxis("Horizontal") * 0.5f, 0, Input.GetAxis("Vertical")); // slower left/right rotation
  //  moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
  //  if (moveDirection.z < 0f) moveDirection.z = 0f;
  //  moveDirection = transform.TransformDirection(moveDirection);
  //  moveDirection *= speedInMedium;

  //  Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
  //  //Debug.DrawRay(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z), mouseRay.direction, Color.red);
  //  Vector3 lookPos = mouseRay.direction;// - transform.position;
  //  lookPos.y *= currentYAxisMultiplier();
  //  Quaternion targetRotation = Quaternion.LookRotation(lookPos);
  //  // FIXME at some point in the future the currentRotateSpeed should be smoothed out based on the elapsed time since the 
  //  // camera state changed. So if the camera were in targeting mode, then the targeting button was released, the release time
  //  // would be recorded, decremented every update(), and used to calculate the rotation speed as follows:
  //  // if (1f - timeSinceRelease) == 1
  //  //   4 * slowRotationSpeed + 1 * fastRotationSpeed / 5
  //  // else if (1f - timeSinceRelease >= .75)
  //  //   3 * slowRotationSpeed + 2 * fastRotationSpeed / 5
  //  // else if (1f - timeSinceRelease >= .5)
  //  //   2 * slowRotationSpeed + 3 * fastRotationSpeed / 5
  //  // else if (1f - timeSinceRelease >= .25)
  //  //   1 * slowRotationSpeed + 4 * fastRotationSpeed / 5
  //  // else if (1f - timeSinceRelease >= 0)
  //  //   0 * slowRotationSpeed + 5 * fastRotationSpeed / 5
  //  // end
  //  // except do this intelligently with a function
  //  transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, currentRotateSpeed() * Time.deltaTime);
  //  moveDirection.y -= gravity * Time.deltaTime;

  //  controller.Move(moveDirection * Time.deltaTime);
  //}

  public float velocity(){
    return (Vector3.Distance(transform.position, previousPosition)) / Time.deltaTime;
  }

  // FIXME this is just a wrapper for followingFish :/
  public void addFish(FishMovement fish){
    followingFish.addFish(fish);
    if (!fish.isSpecial()) thirdPersonCamera.addObjectThatMustAlwaysRemainInFieldOfView(fish.transform.gameObject);
    updateMinimumSpeed();
    if (fish.isTheLeadFish() && fish.parentSchool().isGameWinner()){
      freedGameWinningFish();
    }
  }

  public void removeFish(FishMovement fish){
    followingFish.removeFish(fish);
    if (!fish.isSpecial()) thirdPersonCamera.removeObjectThatMustAlwaysRemainInFieldOfView(fish.transform.gameObject);
    updateMinimumSpeed();
  }

  // this is always assuming special rush attempts against sequential barriers
  public void applyForceVectorToBarrier(Vector3 forceVector, GameObject theBarrier, bool isLeadFish=false){
    bool success = barrierController.applyForceVectorToBarrier(forceVector, theBarrier, this.transform.position);
    if (isLeadFish){ // we only need to check once...ish (each school has a lead)
      Barrier barrier = barrierController.getBarrierInstanceFromBarrierGameObject(theBarrier);
      // FIXME re-enable game logic
      if (barrier.isDestroyed()){
        setNextBarrier(theBarrier);
      // FIXME fix bug with fish rushing the wrong barrier and causing aborts
      } else {
        followingFish.abortRushAttempt(special: true);
        resetBarriers(theBarrier);
      }
    }
  }

  private float currentRotateSpeed(){
    return thirdPersonCamera.getCamState() == "Behind" ? defaultRotateSpeed : targetModeRotateSpeed;
  }

  private float currentYAxisMultiplier(){
    return thirdPersonCamera.getCamState() == "Behind" ? 1.5f : 0.5f;
  }

  public void updateMinimumSpeed(){
    float desiredSpeed = initialMinimumSpeed + (followingFish.numberOfFollowingFish() / 30f);
    minSpeedInMedium = desiredSpeed > maxSpeedInMedium ? maxSpeedInMedium : desiredSpeed;
  }

  public void accelerateToward(Vector3 targetPosition, int strength){
    //Vector3 force = transform.InverseTransformDirection(targetPosition);
    //rigidbody.AddRelativeForce(force, ForceMode.Impulse);
    //float speed = speedInMedium + (strength * 1.5f);
    //targetSpeedInMedium = speed > maxSpeedInMedium ? maxSpeedInMedium : speed;
    //currentlyAccelerating = true;
  }

  private void calculateSpeedInMedium(){
    if (transform.position.y > surfaceLevel){
      speedInMedium = 12f;
    } else {
      if (currentlyAccelerating && speedInMedium >= (targetSpeedInMedium - 1f) && speedInMedium <= (targetSpeedInMedium + 1f))
        currentlyAccelerating = false;

      if (currentlyAccelerating && speedInMedium < targetSpeedInMedium){
        speedInMedium = Mathf.SmoothStep(speedInMedium, targetSpeedInMedium, .2f);
      } else if (speedInMedium >= minSpeedInMedium) {
        speedInMedium = Mathf.SmoothStep(speedInMedium, minSpeedInMedium, .2f);
      } else if (speedInMedium < minSpeedInMedium) {
        speedInMedium = Mathf.SmoothStep(minSpeedInMedium, speedInMedium, .3f);
      }
    }
  }

  public void rushNextSequentialBarrier(bool force=false){
    if (nextBarrier != null){
      if (!allRequiredSchoolsAreInPlace()) forceStragglersIntoPlace();
      followingFish.rushBarrier(nextBarrier, special: true, force: true);
    } else {
      Debug.Log("can't rush a null barrier");
    }
  }

  private void setNextBarrier(GameObject currentBarrier){
    // get the next non-destroyed barrier
    schoolsMayLeave = false;
    foreach(GameObject barrierGameObject in sequentialBarriers){
      Barrier barrier = barrierController.getBarrierInstanceFromBarrierGameObject(barrierGameObject);
      if (!barrier.isDestroyed()){
        nextBarrier = barrierGameObject;
        nextBarrierInstance = barrierController.getBarrierInstanceFromBarrierGameObject(nextBarrier);
        rushNextSequentialBarrier(force: true);
        break;
      }
    }
  }

  private void freedGameWinningFish(){
    if (!sunkenStaircase.isReadyToRaise() && allSequentialBarriersDestroyed()) {
      followingFish.beginOrbiting(sunkenStaircase.getFocalPoint());
      sunkenStaircase.scheduleRaise();
      manager.cutTo(sunkenStaircase.getFocalPoint(), 40f, new Vector3(-10f, 10f, -50f));
    }
  }

  public void tellFollowingFishToLeaveStaircase(){
    followingFish.stopOrbiting();
  }

  private bool allSequentialBarriersDestroyed(){
    foreach(GameObject theBarrier in sequentialBarriers){
      Barrier barrier = barrierController.getBarrierInstanceFromBarrierGameObject(theBarrier);
      if (!barrier.isDestroyed()) return false;
    }
    return true;
  }

  // make the first barrier the next one
  // trap all sequential fish, even if they were freed before, so the player
  // has to complete everything sequentially
  // TODO: LERP camera to cue player
  private void resetBarriers(GameObject abortedBarrier){
    nextBarrier = sequentialBarriers[0];
    nextBarrierInstance = barrierController.getBarrierInstanceFromBarrierGameObject(nextBarrier);
    foreach(GameObject barrier in sequentialBarriers)
      barrierController.resurrectBarrier(barrier);
    willShowPlayerInitialBarrier = true;
  }

  private int indexOfBarrier(GameObject aBarrier){
    for (int i = 0; i < sequentialBarriers.Count; i++){
      if (aBarrier != null){
        // super blargh :/
        if (sequentialBarriers[i].transform.parent.gameObject != null){
          GameObject den = sequentialBarriers[i].transform.parent.gameObject;
          if (aBarrier.transform.parent.gameObject != null){
            GameObject currentBarrier = aBarrier.transform.parent.gameObject;
            if (den == currentBarrier) return i;

            // vomit
            if (currentBarrier != null && currentBarrier.transform.parent != null){
              if (den == currentBarrier.transform.parent.gameObject) return i;
            }
          }
        }
      }
    }

    return sequentialBarriers.Count;
  }

  public int numberOfFollowingFish(){
    return followingFish.numberOfFollowingFish();
  }

  public bool needsRendezvousPointReminder(){
    return acceptRendezvousPointCutSceneReminder;
  }

  public void rendezvousPointReached(GameObject point){
    manager.cutTo(point, 10f, new Vector3(10f, 10f, -25f));
    acceptRendezvousPointCutSceneReminder = false;
  }

  public void showPlayerInitialBarrier(SchoolOfFishMovement school){
    if (willShowPlayerInitialBarrier && manager.InitialBarrier().schoolOfFish() == school){
      manager.playCutSceneFor("Abort Barrier");
      willShowPlayerInitialBarrier = false;
    }
  }

  public bool allRequiredSchoolsAreInPlace(){
    if (schoolsMayLeave){
      return true;
    } else {
      foreach(SchoolOfFishMovement school in nextBarrierInstance.requiredSchools){
        if (!school.mayLeaveRendezvousPoint()){
          schoolsMayLeave = false;
          return schoolsMayLeave;
        }
      }
      return true;
    }
  }

  public void forceStragglersIntoPlace(){
    followingFish.forceStragglersToRushBarrier(nextBarrier, nextBarrierInstance.rendezvousPoint);
  }

  public void rushRequiredSchools(){
    foreach(SchoolOfFishMovement school in nextBarrierInstance.requiredSchools)
      school.RushBarrier();
  }

  public void playerIsNearTheSurface(bool state=true){
    isNearSurface = state;
  }

  public bool isUnderwater(){
    return (transform.position.y <= surfaceLevel && !isTouchingTerrainFromSurface());
  }

  public bool isEmerging(){
    return isTouchingTerrainFromSurface();
  }
}
