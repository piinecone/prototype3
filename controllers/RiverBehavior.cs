using UnityEngine;
using System.Collections;

public class RiverBehavior : MonoBehaviour {

  [SerializeField]
  private float force = 0f;

  [SerializeField]
  private int legNumber = 0;

  [SerializeField]
  private TurtleStateController playerStateController;

  private Vector3 forceVector = Vector3.zero;
  private bool hasAlreadyCollidedWithPlayer = false;
  private bool colliding = false;
  private bool areaIsRelevant = false;
  private bool areaIsActive = true;

  void Start () {
    forceVector = transform.up * 15f;
  }

  void Update(){
    if (!colliding){
      determineIfAreaIsRelevant();
      if (areaIsRelevant) fallTowardSurface();
    } else {
      playerStateController.ApplyEnvironmentalForce(true, forceVector);
    }
  }

  private void determineIfAreaIsRelevant(){
    areaIsRelevant = false;
    float distance = 300f;
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

  private void fallTowardSurface(){
    playerStateController.PlayerIsNearSurface(false);
    playerStateController.PlayerIsCollidingWithBodyOfWater(false);
    playerStateController.ApplyEnvironmentalForce(false, Vector3.zero);
  }

  void OnTriggerEnter(Collider collider){
    if (collider.gameObject.tag == "Player"){
      colliding = true;
      areaIsRelevant = true;
      hasAlreadyCollidedWithPlayer = true;
      playerStateController.PlayerIsNearSurface(false);
      playerStateController.PlayerIsCollidingWithBodyOfWater(true);
      setAsRelevantBodyOfWater();
    }
  }

  void OnTriggerStay(Collider collider){
    if (collider.gameObject.tag == "Player"){
      colliding = true;
      areaIsRelevant = true;
      hasAlreadyCollidedWithPlayer = true;
      playerStateController.PlayerIsNearSurface(false);
      playerStateController.PlayerIsCollidingWithBodyOfWater(true);
      setAsRelevantBodyOfWater();
    }
  }

  void OnTriggerExit(Collider collider){
    if (collider.gameObject.tag == "Player"){
      colliding = false;
      fallTowardSurface();
    }
  }

  private void setAsRelevantBodyOfWater(){
    if (!areaIsRelevant) playerStateController.WaterBodyGameObjectIsRelevant(gameObject, true);
  }
}
