using UnityEngine;
using System.Collections;

public class RiverBehavior : MonoBehaviour {

  [SerializeField]
  private float force = 0f;

  [SerializeField]
  private int legNumber = 0;

  [SerializeField]
  private TurtleStateController playerStateController;

  private bool hasAlreadyCollidedWithPlayer = false;
  private bool colliding = false;
  private bool areaIsRelevant = false;
  private bool areaIsActive = true;
  private Vector3 forceVector;

  void Start () {
    forceVector = transform.forward.normalized * force;
    forceVector.y = 4f;
  }

  void FixedUpdate(){
    if (!colliding){
      determineIfAreaIsRelevant();
      if (areaIsRelevant) determineIfPlayerShouldLockVerticalPosition();
    } else {
      playerStateController.ApplyEnvironmentalForce(true, forceVector);
    }
  }

  private void determineIfAreaIsRelevant(){
    areaIsRelevant = false;
    float distance = 100f;
    Vector3 ray = Vector3.down * distance;
    RaycastHit[] hits;
    Vector3 raycastFrom = playerStateController.transform.position;
    raycastFrom.y += 5f;
    Debug.DrawRay(raycastFrom, ray, Color.magenta);
    hits = Physics.RaycastAll(raycastFrom, ray, distance);
    if (hits.Length > 0){
      foreach (RaycastHit hit in hits){
        if (hit.transform.gameObject.tag == "RiverLeg"){
          if (hit.transform == transform) areaIsRelevant = true;
          break;
        }
      }
    }
    playerStateController.WaterBodyGameObjectIsRelevant(gameObject, areaIsRelevant);
  }

  private void determineIfPlayerShouldLockVerticalPosition(){
    float distance = 5f;
    Vector3 ray = Vector3.down * 5f;
    RaycastHit hit;
    if (hasAlreadyCollidedWithPlayer && Physics.Raycast(playerStateController.transform.position, ray, out hit, distance)){
      playerStateController.LockVerticalPosition(true, position: (playerStateController.transform.position.y - hit.distance + 1f));
      playerStateController.ApplyEnvironmentalForce(true, forceVector);
    } else { // allow player to fall
      playerStateController.LockVerticalPosition(false);
      playerStateController.PlayerIsCollidingWithBodyOfWater(false);
      playerStateController.ApplyEnvironmentalForce(false, Vector3.zero);
    }
  }

  void OnTriggerEnter(Collider collider){
    if (collider.gameObject.tag == "Player"){
      colliding = true;
      areaIsRelevant = true;
      hasAlreadyCollidedWithPlayer = true;
      playerStateController.PlayerIsCollidingWithBodyOfWater(true);
      setAsRelevantBodyOfWater();
    }
  }

  void OnTriggerStay(Collider collider){
    if (collider.gameObject.tag == "Player"){
      colliding = true;
      areaIsRelevant = true;
      hasAlreadyCollidedWithPlayer = true;
      playerStateController.LockVerticalPosition(false);
      playerStateController.PlayerIsCollidingWithBodyOfWater(true);
      setAsRelevantBodyOfWater();
    }
  }

  void OnTriggerExit(Collider collider){
    if (collider.gameObject.tag == "Player")
      colliding = false;
  }

  private void setAsRelevantBodyOfWater(){
    if (!areaIsRelevant) playerStateController.WaterBodyGameObjectIsRelevant(gameObject, true);
  }
}
