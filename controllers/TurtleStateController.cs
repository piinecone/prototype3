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

  // emerging
  private bool beganToEmerge = false;
  private float emergenceBeganAtPositionY = 0f;

  // submerging
  private bool beganToSubmerge = false;
  private float submergenceBeganAtPositionY = 0f;

  void Start () {
    capsuleCollider = GetComponent<CapsuleCollider>();
    particleEmitter = GetComponent<ParticleSystem>();
    characterController = GetComponent<CharacterController>();
    splashEmitter.active = false;
  }

  void LateUpdate () {
    checkForEmergence();
    //checkForSubmergence();
  }

  private void checkForEmergence(){
    if (!beganToEmerge && isTouchingTerrainFromSurface()){
      beganToEmerge = true;
      emergenceBeganAtPositionY = transform.position.y;
    }

    if (beganToEmerge && (transform.position.y + 1f) < waterSurfaceLevel)
      beganToEmerge = false;

    if (beganToEmerge && (transform.position.y - 2f) > waterSurfaceLevel)
      beganToEmerge = false;
  }

  private void checkForSubmergence(){
    if (!beganToSubmerge && isApproachingWaterSurfaceFromAbove()){
      beganToSubmerge = true;
      beganToEmerge = false;
      submergenceBeganAtPositionY = transform.position.y;
    }

    if (beganToSubmerge && transform.position.y < waterSurfaceLevel)
      beganToSubmerge = false;
  }

  public string LastRecordedState(){
    return lastRecordedState;
  }

  public bool PlayerIsEmergingFromWater(){
    return beganToEmerge;
  }

  public bool PlayerIsSubmergingIntoWater(){
    return beganToSubmerge;
  }

  public bool PlayerIsUnderwater(){ // the player is completely submerged
    // y = y + the distance to the top of the turtle's shell
    //return (transform.position.y <= waterSurfaceLevel && !isTouchingTerrainFromSurface());
    if (transform.position.y <= waterSurfaceLevel && !isTouchingTerrainFromSurface()){
      lastRecordedState = "underwater";
      return true;
    }
    return false;
  }

  public bool PlayerIsInWater(){ // the player is at least partially submerged
    // y = y - the distance to the turtle's underside / limbs
    if (transform.position.y - .6f <= waterSurfaceLevel){
      lastRecordedState = "underwater";
      return true;
    }
    return false;
  }

  public bool PlayerIsOnLand(){ // the player is completely on land
    // y = y - the distance to the turtle's underside / limbs
    // and player is "above" terrain, not water
    if (playerIsTouchingTerrain() && (transform.position.y - .6f) > waterSurfaceLevel){
      lastRecordedState = "grounded";
      return true;
    }
    return false;
  }

  public bool PlayerIsAirborne(){
    if (PlayerIsNotTouchingAnything() && transform.position.y > waterSurfaceLevel){
      lastRecordedState = "airborne";
      return true;
    }
    return false;
  }

  public void PlayerIsNearSurface(bool value=true){
    isNearSurface = value;
  }

  public bool IsPlayerNearSurface(){
    return isNearSurface;
  }

  public bool PlayerIsNotTouchingAnything(){
    float distance = 2f; // FIXME may want to scale this back a touch
    RaycastHit hit;
    Vector3 downRay = transform.TransformDirection(Vector3.down);
    if (Physics.Raycast(transform.position, downRay, out hit, distance))
      return false;
    return true;
  }

  public bool PlayerIsNotTouchingWater(){
    float distance = 2f; // FIXME may want to scale this back a touch
    RaycastHit hit;
    Vector3 downRay = transform.TransformDirection(Vector3.down);
    if (Physics.Raycast(transform.position, downRay, out hit, distance))
      return (hit.transform.gameObject.tag != "Water");
    return false;
  }

  private bool isTouchingTerrainFromSurface(){
    if (transform.position.y < (waterSurfaceLevel - 2f) || transform.position.y > (waterSurfaceLevel + 1f)) return false;

    return (isFacingNearbyTerrain() || isAlignedWithTerrain());
  }

  private bool isFacingNearbyTerrain(){
    float distance = 3f;
    RaycastHit hit;
    Vector3 forwardRay = transform.forward * distance;
    if (Physics.Raycast(transform.position, forwardRay, out hit, distance))
      return (hit.normal.y > .3);
    else
      return false;
  }

  private bool isAlignedWithTerrain(){
    float distance = 1.5f;
    RaycastHit[] hits;
    Vector3 ray = transform.forward * distance;
    ray.y = 0;
    hits = Physics.RaycastAll(transform.position, ray, distance);
    foreach (RaycastHit hit in hits)
      if (hit.transform.gameObject.tag == "Terrain") return true;
    return false;
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

  private bool isApproachingWaterSurfaceFromAbove(){
    if (transform.position.y < (waterSurfaceLevel - 1f) || transform.position.y > (waterSurfaceLevel + 1f)) return false;

    return (isFacingNearbyWater() || isAlignedWithNearbyWater());
  }

  private bool isFacingNearbyWater(){
    float distance = 6f;
    RaycastHit hit;
    Vector3 forwardRay = transform.forward * distance;
    if (Physics.Raycast(transform.position, forwardRay, out hit, distance))
      return (hit.transform.gameObject.tag == "Water");
    return false;
  }

  private bool isAlignedWithNearbyWater(){
    return false;
  }

  public bool PlayerHasFollowingFish(){
    return (followingFish.Count > 0);
  }

  public int NumberOfFollowingFish(){
    return followingFish.Count;
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
