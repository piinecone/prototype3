using UnityEngine;
using System.Collections;

public class ArchwayTrigger : MonoBehaviour {
  [SerializeField]
  private ArchwayBehavior archway;
  [SerializeField]
  private TurtleStateController stateController;

  private ParticleSystem particleSystem;

  void Start () {
    particleSystem = GetComponent<ParticleSystem>();
  }

  void OnTriggerEnter(Collider collider){
    if (collider.gameObject.tag == "Player"){
      stateController.PlayerPassedThroughArchway(archway);
    }
  }

  void OnTriggerStay(Collider collider){
    particleSystem.Play();
    if (collider.gameObject.tag == "Player"){
      stateController.PlayerPassedThroughArchway(archway);
    }
  }

  void OnTriggerExit(Collider collider){
    particleSystem.Stop();
  }
}
