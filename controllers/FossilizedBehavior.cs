using UnityEngine;
using System.Collections;

public class FossilizedBehavior : MonoBehaviour {

  [SerializeField]
  private float sicknessFactor = 0f;
  [SerializeField]
  private float healingStep = .5f;
  [SerializeField]
  private Material healthyMaterial;
  [SerializeField]
  private BigLakeLevelManager levelManager;

  private ParticleSystem particleSystem ;
  private GameObject player;
  private TurtleStateController playerStateController;
  private bool colliding = false;

  void Start () {
    player = GameObject.FindWithTag("Player");
    playerStateController = player.GetComponent<TurtleStateController>();
    particleSystem = GetComponent<ParticleSystem>();
  }

  void OnTriggerEnter(Collider collider){
    if (!Healthy() && collidingWithPlayer(collider))
      playerStateController.PlayerIsNearFossilizedGameObject(this);
  }

  void OnTriggerStay(Collider collider){
    if (!Healthy() && collidingWithPlayer(collider))
      playerStateController.PlayerIsNearFossilizedGameObject(this);
  }

  public void Reanimate(){
    particleSystem.Play();
    sicknessFactor -= healingStep;
    if (sicknessFactor <= 0f){
      particleSystem.Stop();
      renderer.material = healthyMaterial;
      levelManager.ReanimatedGameObject(gameObject);
    }
  }

  public bool Healthy(){
    return (sicknessFactor <= 0f);
  }

  private bool collidingWithPlayer(Collider collider){
    string tag = collider.gameObject.tag;
    return (tag == "Player" || tag == "PlayerInfluence");
  }
}
