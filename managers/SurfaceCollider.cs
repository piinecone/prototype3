using UnityEngine;
using System.Collections;

[RequireComponent(typeof (TurtleStateController))]

public class SurfaceCollider : MonoBehaviour {

  [SerializeField]
  private TurtleStateController stateController;

  void OnTriggerEnter(Collider collider){
    if (collider.gameObject.tag == "Player")
      stateController.PlayerIsNearSurface(true);
  }

  void OnTriggerStay(Collider collider){
    if (collider.gameObject.tag == "Player")
      stateController.PlayerIsNearSurface(true);
  }

  void OnTriggerExit(Collider collider){
    if (collider.gameObject.tag == "Player")
      stateController.PlayerIsNearSurface(false);
  }
}
