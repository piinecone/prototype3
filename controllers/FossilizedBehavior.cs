using UnityEngine;
using System.Collections;

public class FossilizedBehavior : MonoBehaviour {

  [SerializeField]
  private float sicknessFactor = 0f;
  [SerializeField]
  private float healingStep = .5f;
  [SerializeField]
  private Material recoveringMaterial;
  [SerializeField]
  private Material healthyMaterial;
  [SerializeField]
  private BigLakeLevelManager levelManager;

  private GameObject player;
  private TurtleStateController playerStateController;
  private bool colliding = false;

  void Start () {
    player = GameObject.FindWithTag("Player");
    playerStateController = player.GetComponent<TurtleStateController>();
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
    sicknessFactor -= healingStep;
    if (sicknessFactor <= 0f){
      renderer.material = healthyMaterial;
      levelManager.ReanimatedGameObject(gameObject);
    } else if (sicknessFactor < 10f){
      renderer.material = recoveringMaterial;
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
