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

  void Update () {
  }

  public string LastRecordedState(){
    return lastRecordedState;
  }

  public bool PlayerIsEmergingFromWater(){
    return isTouchingTerrainFromSurface();
  }

  public bool PlayerIsUnderwater(){ // the player is completely submerged
    // y = y + the distance to the top of the turtle's shell
    //return (transform.position.y <= waterSurfaceLevel && !isTouchingTerrainFromSurface());
    if (transform.position.y <= waterSurfaceLevel){
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
    if (characterController.isGrounded && transform.position.y > waterSurfaceLevel){
      lastRecordedState = "grounded";
      return true;
    }
    return false;
  }

  public bool PlayerIsAirborne(){
    if (!characterController.isGrounded && transform.position.y > waterSurfaceLevel){
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

  private bool isTouchingTerrainFromSurface(){
    if (transform.position.y < (waterSurfaceLevel - 2f)) return false;

    float distance = 6f;
    RaycastHit hit;
    Vector3 forwardRay = transform.forward * distance;
    if (Physics.Raycast(transform.position, forwardRay, out hit, distance))
      return (hit.normal.y > .3);
    else
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
