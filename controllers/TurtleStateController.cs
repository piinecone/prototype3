using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof (CapsuleCollider))]
[RequireComponent(typeof (Rigidbody))]

public class TurtleStateController : MonoBehaviour {

  [SerializeField]
  private float waterSurfaceLevel;
  [SerializeField]
  private List<FishMovement> followingFish; // FIXME these should be FishControllers
  [SerializeField]
  private ParticleSystem splashEmitter;

  private bool isNearSurface = false;
  private CapsuleCollider capsuleCollider;
  private ParticleSystem particleEmitter;
  private CharacterController characterController;
  private string lastRecordedState;

  void Start () {
    capsuleCollider = GetComponent<CapsuleCollider>();
    particleEmitter = GetComponent<ParticleSystem>();
    characterController = GetComponent<CharacterController>();
    splashEmitter.active = false;
  }

  public string LastRecordedState(){
    return lastRecordedState;
  }

  public bool PlayerIsEmergingFromWater(){
    if (isApproachingTerrainNearTheSurface() && !isApproachingWater()){
      lastRecordedState = "grounded";
      return true;
    }
    return false;
  }

  public bool PlayerIsUnderwater(){ // the player is completely submerged
    if (playerIsPartiallySubmerged() && !isApproachingTerrainNearTheSurface()){
      lastRecordedState = "underwater";
      return true;
    }
    return false;
  }

  public bool PlayerIsInWater(){ // the player is at least partially submerged
    // y = y - the distance to the turtle's underside / limbs
    if (playerIsPartiallySubmerged()){
      lastRecordedState = "underwater";
      return true;
    }
    return false;
  }

  public bool PlayerIsOnLand(){ // the player is completely on land
    // y = y - the distance to the turtle's underside / limbs
    // and player is "above" terrain, not water
    if (playerIsTouchingTerrain() && playerHasCompletelyEmerged(range: .6f)){
      lastRecordedState = "grounded";
      return true;
    }
    return false;
  }

  public bool PlayerIsAirborne(){
    if (PlayerIsNotTouchingAnything() && playerHasCompletelyEmerged(range: 0f)){
      lastRecordedState = "airborne";
      return true;
    }
    return false;
  }

  public bool IsPlayerNearSurface(){
    return isNearSurface;
  }

  private bool playerIsCompletelySubmerged(float range){
    return (transform.position.y + range <= waterSurfaceLevel);
  }

  private bool playerHasCompletelyEmerged(float range){
    return (transform.position.y - range > waterSurfaceLevel);
  }

  private bool playerIsPartiallySubmerged(){
    return (isCollidingWithBodyOfWater || transform.position.y - .6f <= waterSurfaceLevel);
  }

  public bool PlayerIsNotTouchingAnything(){
    float distance = 2f; // FIXME may want to scale this back a touch
    RaycastHit hit;
    Vector3 downRay = transform.TransformDirection(Vector3.down);
    if (Physics.Raycast(transform.position, downRay, out hit, distance))
      return false;
    return true;
  }

  private bool playerIsTouchingTerrain(){
    float distance = 5f;
    RaycastHit[] hits;
    Vector3 ray = Vector3.down * distance;
    hits = Physics.RaycastAll(transform.position, ray, distance);
    foreach (RaycastHit hit in hits)
      if (hit.transform.gameObject.tag == "Terrain") return true;
    return false;
  }

  private bool isFacingNearbyWater(){
    float distance = 6f;
    RaycastHit hit;
    Vector3 forwardRay = transform.forward * distance;
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
    Debug.DrawRay(transform.position, ray, Color.red);
    hitRecorded = Physics.Raycast(transform.position, ray, out hit, distance);
    return (ray.z < 0f && (hitRecorded == false || (hit.transform.gameObject.tag == "Water" || hit.transform.gameObject.tag == "SurfaceCollider")));
  }

  private bool facingAndTouchingWater(){
    float distance = 5f;
    RaycastHit hit;
    Vector3 ray = transform.forward * distance;
    Debug.DrawRay(transform.position, ray, Color.red);
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

  public void LockVerticalPosition(bool value=true, float position=0f){
    shouldLockVerticalPosition = value;
    verticalPositionMaximum = position;
  }

  public bool ShouldLockVerticalPosition(){
    return shouldLockVerticalPosition;
  }

  public float VerticalPositionMaximum(){
    return verticalPositionMaximum;
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
}
