using UnityEngine;
using System.Collections;

public class ArchwayTrigger : MonoBehaviour {
  [SerializeField]
  private ArchwayBehavior archway;
  [SerializeField]
  private GameObject flash;
  [SerializeField]
  private TurtleStateController stateController;

  private ParticleSystem particleSystem;

  void Start () {
    particleSystem = GetComponent<ParticleSystem>();
    toggleFlash(false);
  }

  void OnTriggerEnter(Collider collider){
    if (collider.gameObject.tag == "Player"){
      toggleFlash(true);
      stateController.PlayerPassedThroughArchway(archway);
    }
  }

  void OnTriggerStay(Collider collider){
    particleSystem.Play();
    if (collider.gameObject.tag == "Player"){
      toggleFlash(false);
      stateController.PlayerPassedThroughArchway(archway);
    }
  }

  void OnTriggerExit(Collider collider){
    toggleFlash(false);
    particleSystem.Stop();
  }

  void toggleFlash(bool visible=true){
    flash.GetComponent<MeshRenderer>().enabled = visible;
  }
}
