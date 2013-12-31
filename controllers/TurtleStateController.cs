using UnityEngine;
using System.Collections;

[RequireComponent(typeof (CapsuleCollider))]
[RequireComponent(typeof (Rigidbody))]

public class TurtleStateController : MonoBehaviour {

  [SerializeField]
  private float waterSurfaceLevel = 160f;

  private bool isNearSurface = false;
  private CapsuleCollider capsuleCollider;

  void Start () {
    capsuleCollider = GetComponent<CapsuleCollider>();
  }

  void Update () {
  }

  public bool PlayerIsEmergingFromWater(){
    return isTouchingTerrainFromSurface();
  }

  public bool PlayerIsUnderwater(){ // the player is completely submerged
    // y = y + the distance to the top of the turtle's shell
    //return (transform.position.y <= waterSurfaceLevel && !isTouchingTerrainFromSurface());
    return (transform.position.y <= waterSurfaceLevel);
  }

  public bool PlayerIsInWater(){ // the player is at least partially submerged
    // y = y - the distance to the turtle's underside / limbs
    return (transform.position.y <= waterSurfaceLevel);
  }

  public bool PlayerIsOnLand(){ // the player is completely on land
    // y = y - the distance to the turtle's underside / limbs
    // and player is "above" terrain, not water
    return (transform.position.y > waterSurfaceLevel);
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
}
