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
    flash.SetActive(false);
  }

  void OnTriggerEnter(Collider collider){
    if (collider.gameObject.tag == "Player"){
      flash.SetActive(true);
      stateController.PlayerPassedThroughArchway(archway);
    }
  }

  void OnTriggerStay(Collider collider){
    particleSystem.Play();
    if (collider.gameObject.tag == "Player"){
      flash.SetActive(false);
      stateController.PlayerPassedThroughArchway(archway);
    }
  }

  void OnTriggerExit(Collider collider){
      flash.SetActive(false);
    particleSystem.Stop();
  }
}
