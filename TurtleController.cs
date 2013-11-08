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

  private float speedInMedium = 8f;
  private Vector3 moveDirection = Vector3.zero;
  private CharacterController controller;
  private Animator anim;
  private CapsuleCollider col;
  private AnimatorStateInfo currentBaseState;
  private Vector3 previousPosition;
  private BarrierController barrierController;

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

  void Start () {
    anim = GetComponent<Animator>();               
    col = GetComponent<CapsuleCollider>();          
    controller = GetComponent<CharacterController>();
    followingFish = GetComponent<FollowingFish>();
    barrierController = GetComponent<BarrierController>();
    speedInMedium = 16.5f;
    minSpeedInMedium = 16.5f;
    maxSpeedInMedium = 23.5f;
    targetSpeedInMedium = 16.5f;
    currentlyAccelerating = false;
    initialMinimumSpeed = minSpeedInMedium;
    nextBarrier = sequentialBarriers[0];
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
    swim(); // we're always underwater for now
  }

  void swim(){
    gravity = 30f;
    //speedInMedium = speed * 4.1f;
    moveDirection = new Vector3(Input.GetAxis("Horizontal") * 0.5f, 0, Input.GetAxis("Vertical"));
    moveDirection = transform.TransformDirection(moveDirection);
    moveDirection *= speedInMedium;

    Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
    //Debug.DrawRay(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z), mouseRay.direction, Color.red);
    Vector3 lookPos = mouseRay.direction;// - transform.position;
    lookPos.y *= currentYAxisMultiplier();
    Quaternion targetRotation = Quaternion.LookRotation(lookPos);
    // FIXME at some point in the future the currentRotateSpeed should be smoothed out based on the elapsed time since the 
    // camera state changed. So if the camera were in targeting mode, then the targeting button was released, the release time
    // would be recorded, decremented every update(), and used to calculate the rotation speed as follows:
    // if (1f - timeSinceRelease) == 1
    //   4 * slowRotationSpeed + 1 * fastRotationSpeed / 5
    // else if (1f - timeSinceRelease >= .75)
    //   3 * slowRotationSpeed + 2 * fastRotationSpeed / 5
    // else if (1f - timeSinceRelease >= .5)
    //   2 * slowRotationSpeed + 3 * fastRotationSpeed / 5
    // else if (1f - timeSinceRelease >= .25)
    //   1 * slowRotationSpeed + 4 * fastRotationSpeed / 5
    // else if (1f - timeSinceRelease >= 0)
    //   0 * slowRotationSpeed + 5 * fastRotationSpeed / 5
    // end
    // except do this intelligently with a function
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, currentRotateSpeed() * Time.deltaTime);
    moveDirection.y -= gravity * Time.deltaTime;

    controller.Move(moveDirection * Time.deltaTime);
  }

  public float velocity(){
    return (Vector3.Distance(transform.position, previousPosition)) / Time.deltaTime;
  }

  // FIXME this is just a wrapper for followingFish :/
  public void addFish(FishMovement fish){
    followingFish.addFish(fish);
    thirdPersonCamera.addObjectThatMustAlwaysRemainInFieldOfView(fish.transform.gameObject);
    updateMinimumSpeed();
  }

  public void removeFish(FishMovement fish){
    followingFish.removeFish(fish);
    thirdPersonCamera.removeObjectThatMustAlwaysRemainInFieldOfView(fish.transform.gameObject);
    updateMinimumSpeed();
  }

  // this is always assuming special rush attempts against sequential barriers
  public void applyForceVectorToBarrier(Vector3 forceVector, GameObject theBarrier, bool isLeadFish=false){
    bool success = barrierController.applyForceVectorToBarrier(forceVector, theBarrier, this.transform.position);
    if (isLeadFish){ // we only need to check once...ish (each school has a lead)
      Barrier barrier = barrierController.getBarrierInstanceFromBarrierGameObject(theBarrier);
      if (barrier.isDestroyed()){
        setNextBarrier(theBarrier);
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
    float desiredSpeed = initialMinimumSpeed + (followingFish.numberOfFollowingFish() / 10f);
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
    if (currentlyAccelerating && speedInMedium >= (targetSpeedInMedium - 1f) && speedInMedium <= (targetSpeedInMedium + 1f)){
      currentlyAccelerating = false;
    }

    if (currentlyAccelerating && speedInMedium < targetSpeedInMedium){
      speedInMedium = Mathf.SmoothStep(speedInMedium, targetSpeedInMedium, .2f);
    } else if (speedInMedium >= minSpeedInMedium) {
      speedInMedium = Mathf.SmoothStep(speedInMedium, minSpeedInMedium, .2f);
    } else if (speedInMedium < minSpeedInMedium) {
      speedInMedium = Mathf.SmoothStep(minSpeedInMedium, speedInMedium, .3f);
    }
  }

  public void rushNextSequentialBarrier(){
    if (nextBarrier != null){
      followingFish.rushBarrier(nextBarrier, special: true);
    } else {
      Debug.Log("can't rush a null barrier");
    }
  }

  private void setNextBarrier(GameObject currentBarrier){
    int indexOfNextBarrier = indexOfBarrier(currentBarrier) + 1;
    if (indexOfNextBarrier < sequentialBarriers.Count){
      nextBarrier = sequentialBarriers[indexOfNextBarrier];
    } else {
      // check if all sequential barriers have been destroyed and their
      // respective schools released
      // if so: you did it! Send the fish to the sunken staircase
    }
  }

  // make the first barrier the next one
  // trap all sequential fish, even if they were freed before, so the player
  // has to complete everything sequentially
  // TODO: LERP camera to cue player
  private void resetBarriers(GameObject abortedBarrier){
    nextBarrier = sequentialBarriers[0];
    foreach(GameObject barrier in sequentialBarriers)
      barrierController.trapSchoolForBarrier(barrier);
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
}
