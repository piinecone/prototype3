using UnityEngine;
using System.Collections;

public class DirectionalCurrentBehavior : MonoBehaviour {

  [SerializeField]
  private float magnitude = 30f;
  [SerializeField]
  private float duration = 1.5f;

  private TurtleStateController stateController;
  private Vector3 forceVector = Vector3.zero;
  private float timeLeft = 0f;
  private bool colliding = false;
  private ParticleSystem particleSystem;

  void Start(){
    stateController = GameObject.FindWithTag("Player").GetComponent<TurtleStateController>();
    forceVector = transform.forward * magnitude;
    particleSystem = GetComponent<ParticleSystem>();
    particleSystem.Play();
  }

  void LateUpdate(){
    applyForce();
  }

  void OnTriggerEnter(Collider collider){
    if (collider.gameObject.tag == "Player"){
      colliding = true;
      resetTimer();
    }
  }

  void OnTriggerStay(Collider collider){
    if (collider.gameObject.tag == "Player"){
      colliding = true;
    }
  }

  void OnTriggerExit(Collider collider){
    if (collider.gameObject.tag == "Player"){
      colliding = false;
      resetTimer();
      stateController.IncreaseVelocity(false, magnitude);
      stateController.ApplyEnvironmentalForce(false, forceVector); // FIXME delay?
      //particleSystem.Stop();
    }
  }

  private void applyForce(){
    if (colliding && timeLeft <= 0f){
      movePlayer();
    } else if (colliding && timeLeft > 0f){
      timeLeft -= Time.deltaTime;
    }
  }

  private void resetTimer(){
    timeLeft = duration;
  }

  private void movePlayer(){
    stateController.IncreaseVelocity(true, magnitude);
    //stateController.RampUpMaximumSpeedTo(magnitude);
    stateController.ApplyEnvironmentalForce(true, forceVector);
    //particleSystem.Play();
  }
}
