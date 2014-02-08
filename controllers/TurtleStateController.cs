using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof (CapsuleCollider))]
[RequireComponent(typeof (Rigidbody))]

public class TurtleStateController : MonoBehaviour {

  [SerializeField]
  private float waterSurfaceLevel;
  [SerializeField]
  private UnderWater environment;
  [SerializeField]
  private List<FishMovement> followingFish; // FIXME these should be FishControllers
  [SerializeField]
  private ParticleSystem splashEmitter;

  private CapsuleCollider capsuleCollider;
  private ParticleSystem particleEmitter;
  private CharacterController characterController;
  private string lastRecordedState;
  private string previousState;

  // surfacing
  private bool isNearSurface = false;
  private bool isCollidingWithBodyOfWater = false;
  private bool shouldLockVerticalPosition = false;
  private float verticalPositionMaximum = 0f;
  private GameObject currentRelevantBodyOfWater;

  // environmental forces
  private bool shouldApplyEnvironmentalForce = false;
  private Vector3 environmentalForceVector = Vector3.zero;

  // forward force override
  private bool shouldOverrideVelocity = false;
  private float minimumForwardAccelerationOverride = 0f;
  private float forwardAccelerationMultiplier = 0f;
  private float speedClampOverride = 0f;
  private float speedOffset = 20f;

  // constrain look direction
  private bool shouldConstrainLookDirection = false;
  private Vector3 constrainedLookDirectionVector = Vector3.zero;

  // debugging
  private bool drawDebugVectorsEnabled = false;

  // energy trail
  private TrailRenderer trailRenderer;
  private bool energyTrailIsCurrentlyActive = false;
  private float energyTrailTimeLeft = 0f;
  private float energyTrailDuration = 5f;
  private float energyTrailDurationPerFish = 2.5f;
  private float energyTrailWidthPerFish = .5f;

  void Start() {
    capsuleCollider = GetComponent<CapsuleCollider>();
    particleEmitter = GetComponent<ParticleSystem>();
    characterController = GetComponent<CharacterController>();
    splashEmitter.active = false;
    trailRenderer = GetComponent<TrailRenderer>();
  }

  void Update(){
    if (Input.GetKeyDown(KeyCode.V)) drawDebugVectorsEnabled = !drawDebugVectorsEnabled;
  }

  void LateUpdate(){
    if (energyTrailTimeLeft > 0f) energyTrailTimeLeft -= Time.deltaTime;
    else energyTrailExpired();
  }

  public bool DrawDebugVectors(){
    return drawDebugVectorsEnabled;
  }

  public string LastRecordedState(){
    return lastRecordedState;
  }

  public bool PlayerIsEmergingFromWater(){
    if (isApproachingTerrainNearTheSurface() && !isApproachingWater()){
      recordState("grounded");
      return true;
    }
    return false;
  }

  private void recordState(string state){
    if (lastRecordedState != state){
      previousState = lastRecordedState;
      lastRecordedState = state;
    }
  }

  public bool PlayerIsUnderwater(){ // the player is completely submerged
    if (playerIsPartiallySubmerged() && !isApproachingTerrainNearTheSurface()){
      recordState("underwater");
      return true;
    }
    return false;
  }

  public bool PlayerIsInWater(){ // the player is at least partially submerged
    // y = y - the distance to the turtle's underside / limbs
    if (playerIsPartiallySubmerged()){
      recordState("underwater");
      return true;
    }
    return false;
  }

  public bool PlayerIsOnLand(){ // the player is completely on land
    // y = y - the distance to the turtle's underside / limbs
    // and player is "above" terrain, not water
    if (playerIsTouchingTerrain() && playerHasCompletelyEmerged(range: .6f)){
      recordState("grounded");
      return true;
    }
    return false;
  }

  public bool PlayerIsAirborne(){
    if (PlayerIsNotTouchingAnything() && playerHasCompletelyEmerged(range: 0f)){
      recordState("airborne");
      return true;
    }
    return false;
  }

  public bool IsPlayerNearSurface(){
    return isNearSurface;
  }

  public bool PlayerIsRushingDownARiver(){
    return (currentRelevantBodyOfWater != null);
  }

  private bool playerIsCompletelySubmerged(float range){
    return (transform.position.y + range <= waterSurfaceLevel);
  }

  private bool playerHasCompletelyEmerged(float range){
    return (transform.position.y - range > waterSurfaceLevel && !isCollidingWithBodyOfWater);
  }

  private bool playerIsPartiallySubmerged(){
    return (isCollidingWithBodyOfWater || transform.position.y - .6f <= waterSurfaceLevel);
  }

  public bool PlayerIsNotTouchingAnything(){
    float distance = 2f; // FIXME may want to scale this back a touch
    RaycastHit hit;
    Vector3 downRay = Vector3.down;
    if (drawDebugVectorsEnabled) Debug.DrawRay(transform.position, downRay, Color.blue);
    if (Physics.Raycast(transform.position, downRay, out hit, distance))
      return false;
    return true;
  }

  public void WaterBodyGameObjectIsRelevant(GameObject waterBody, bool relevant=true){
    if (relevant){
      currentRelevantBodyOfWater = waterBody;
    } else if (!relevant && currentRelevantBodyOfWater == waterBody){
      isCollidingWithBodyOfWater = false;
      currentRelevantBodyOfWater = null;
      shouldApplyEnvironmentalForce = false;
      shouldConstrainLookDirection = false;
      shouldOverrideVelocity = false;
    }
  }

  public GameObject CurrentBodyOfWater(){
    return currentRelevantBodyOfWater;
  }

  private bool playerIsTouchingTerrain(){
    float distance = 2f;
    RaycastHit[] hits;
    Vector3 ray = Vector3.down * distance;
    if (drawDebugVectorsEnabled) Debug.DrawRay(transform.position, ray, Color.cyan);
    hits = Physics.RaycastAll(transform.position, ray, distance);
    foreach (RaycastHit hit in hits)
      if (hit.transform.gameObject.tag == "Terrain") return true;
    return false;
  }

  private bool isFacingNearbyWater(){
    float distance = 6f;
    RaycastHit hit;
    Vector3 forwardRay = transform.forward * distance;
    if (drawDebugVectorsEnabled) Debug.DrawRay(transform.position, forwardRay, Color.black);
    if (Physics.Raycast(transform.position, forwardRay, out hit, distance))
      return (hit.transform.gameObject.tag == "Water");
    return false;
  }

  private bool isApproachingWater(){
    return (lookingDownAndNotFacingTerrain());
  }

  private bool lookingDownAndNotFacingTerrain(){
    float distance = 4f;
    bool hitRecorded = false;
    RaycastHit hit;
    Vector3 ray = transform.forward * distance;
    if (drawDebugVectorsEnabled) Debug.DrawRay(transform.position, ray, Color.gray);
    hitRecorded = Physics.Raycast(transform.position, ray, out hit, distance);
    return (ray.z < 0f && (hitRecorded == false || (hit.transform.gameObject.tag == "Water" || hit.transform.gameObject.tag == "SurfaceCollider")));
  }

  private bool facingAndTouchingWater(){
    float distance = 5f;
    RaycastHit hit;
    Vector3 ray = transform.forward * distance;
    if (drawDebugVectorsEnabled) Debug.DrawRay(transform.position, ray, Color.yellow);
    return (ray.z < 0f && Physics.Raycast(transform.position, ray, out hit, distance));
  }

  private bool isApproachingTerrainNearTheSurface(){
    // fully submerged / emerged
    if (playerIsCompletelySubmerged(range: 1f) || playerHasCompletelyEmerged(range: 1f)) return false;

    return (touchingNearbyTerrain());
  }

  // FIXME make this generic and reusable
  private bool touchingNearbyTerrain(){
    float distance = 4f;
    int hitCount = 0;
    Vector3 forwardRay = transform.forward * distance;
    Vector3 intermediateRay = transform.TransformDirection(Vector3.forward) * distance * 1.5f;
    intermediateRay.y = 0f;
    Vector3 downRay = transform.up * -distance * .5f;
    RaycastHit[] forwardHits;
    RaycastHit[] intermediateHits;
    RaycastHit[] downHits;

    if (drawDebugVectorsEnabled){
      Debug.DrawRay(transform.position, forwardRay, Color.white);
      Debug.DrawRay(transform.position, intermediateRay, Color.white);
      Debug.DrawRay(transform.position, downRay, Color.white);
    }

    forwardHits = Physics.RaycastAll(transform.position, forwardRay, distance);
    intermediateHits = Physics.RaycastAll(transform.position, intermediateRay, distance);
    downHits = Physics.RaycastAll(transform.position, downRay, distance);

    foreach (RaycastHit hit in forwardHits)
      if (hit.transform.gameObject.tag == "Terrain") hitCount++;
    foreach (RaycastHit hit in intermediateHits)
      if (hit.transform.gameObject.tag == "Terrain") hitCount++;
    foreach (RaycastHit hit in downHits)
      if (hit.transform.gameObject.tag == "Terrain") hitCount++;

    return (hitCount >= 2);
  }

  public bool PlayerHasFollowingFish(){
    return (followingFish.Count > 0);
  }

  public int NumberOfFollowingFish(){
    return followingFish.Count;
  }

  public void PlayerIsNearSurface(bool value=true){
    isNearSurface = value;
  }

  public void PlayerIsCollidingWithBodyOfWater(bool value=true){
    isCollidingWithBodyOfWater = value;

    if (isCollidingWithBodyOfWater)
      environment.SwitchToUnderwaterEnvironment();
    else
      environment.SwitchToAboveWaterEnvironment();
  }

  public void IncreaseVelocity(bool state, float magnitude){
    shouldOverrideVelocity = state;
    speedClampOverride = magnitude + speedOffset;
    minimumForwardAccelerationOverride = speedOffset;
    forwardAccelerationMultiplier = magnitude;
  }

  public bool ShouldOverrideVelocity(){
    return shouldOverrideVelocity;
  }

  public float SpeedClampOverride(){
    return speedClampOverride;
  }

  public float ForwardAccelerationMultiplier(){
    return forwardAccelerationMultiplier;
  }

  public float MinimumForwardAcceleration(){
    return minimumForwardAccelerationOverride;
  }

  public void ApplyEnvironmentalForce(bool state, Vector3 forceVector){
    shouldApplyEnvironmentalForce = state;
    environmentalForceVector = forceVector;
  }

  public bool ShouldApplyEnvironmentalForce(){
    return shouldApplyEnvironmentalForce;
  }

  public Vector3 EnvironmentalForceVector(){
    return environmentalForceVector;
  }

  public void ConstrainLookDirection(bool value, Vector3 direction){
    shouldConstrainLookDirection = value;
    constrainedLookDirectionVector = direction;
  }

  public bool ShouldConstrainLookDirection(){
    return shouldConstrainLookDirection;
  }

  public Vector3 ConstrainedLookDirectionVector(){
    return constrainedLookDirectionVector;
  }

  // FIXME replace FishMovement with FishController
  public void AddFollowingFish(FishMovement fish){
    followingFish.Add(fish);
  }

  // FIXME replace FishMovement with FishController
  public void RemoveFollowingFish(FishMovement fish){
    followingFish.Remove(fish);
  }

  public void PerformCorkscrewLaunch(int direction, float duration){
    foreach (FishMovement fish in followingFish)
      fish.PerformCorkscrewManeuver(direction, duration);
  }

  public bool FollowingFishAreNearby(){
    float meanDistance = 0f;
    foreach (FishMovement fish in followingFish)
      meanDistance += Vector3.Distance(transform.position, fish.transform.position);
    return ((meanDistance / followingFish.Count) < 30f);
  }

  public void EmitBubbleTrail(){
    particleEmitter.Play();
  }

  public void EmitSplashTrail(Vector3 direction){
    splashEmitter.active = true;
    splashEmitter.transform.rotation = Quaternion.LookRotation(direction);
    splashEmitter.Play();
  }

  public void StopSplashTrailEmission(){
    splashEmitter.Stop();
    splashEmitter.Pause();
    splashEmitter.active = false;
  }

  public string CurrentState(string state=""){
    if (state == "") state = lastRecordedState;
    if (state == "grounded") return "Walking";
    else if (state == "underwater") return "Swimming";
    else if (state == "airborne") return "Falling";
    else return "Undefined";
  }

  public string PreviousState(){
    return CurrentState(previousState);
  }

  public void PlayerPassedThroughArchway(ArchwayBehavior archway){
    if (PlayerHasFollowingFish()){
      activateEnergyTrail();
      foreach (FishMovement fish in followingFish)
        fish.ConvertIntoEnergy();
    }
  }

  private void activateEnergyTrail(){
    energyTrailIsCurrentlyActive = true;
    trailRenderer.enabled = true;
    energyTrailTimeLeft = energyTrailDuration;
  }

  public void FishDidConvertIntoEnergy(){
    if (energyTrailIsCurrentlyActive){
      trailRenderer.startWidth += energyTrailWidthPerFish;
      trailRenderer.endWidth += energyTrailWidthPerFish;
      energyTrailTimeLeft += energyTrailDurationPerFish;
    }
  }

  private void energyTrailExpired(){
    energyTrailIsCurrentlyActive = false;
    trailRenderer.enabled = false;
    foreach (FishMovement fish in followingFish)
      fish.Enable();
  }

  public bool EnergyTrailIsCurrentlyActive(){
    return energyTrailIsCurrentlyActive;
  }

  public float EnergyTrailTimeLeft(){
    return energyTrailTimeLeft;
  }

  public void PlayerIsNearFossilizedGameObject(FossilizedBehavior fossilizedObject){
    if (energyTrailIsCurrentlyActive)
      fossilizedObject.Reanimate();
  }
}
