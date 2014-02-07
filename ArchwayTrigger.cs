using UnityEngine;
using System.Collections;

public class ArchwayTrigger : MonoBehaviour {
  [SerializeField]
  private ArchwayBehavior archway;

  [SerializeField]
  private TurtleStateController stateController;

  void Start () {
  }

  void OnTriggerEnter(Collider collider){
    if (collider.gameObject.tag == "Player"){
      stateController.PlayerPassedThroughArchway(archway);
    }
  }
}
