using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BigLakeLevelManager : MonoBehaviour {
  [SerializeField]
  private int numberOfFossilizedObjects = 0;
  [SerializeField]
  private ParticleSystem plagueParticleSystem;

  private TurtleController deprecatedPlayerController;
  private List<GameObject> reanimatedObjects = new List<GameObject>();

  void Start () {
    deprecatedPlayerController = GameObject.FindWithTag("Player").GetComponent<TurtleController>();
  }

  public void ReanimatedGameObject(GameObject reanimatedObject){
    if (reanimatedObjects.Contains(reanimatedObject)) return;

    reanimatedObjects.Add(reanimatedObject);
    if (reanimatedObjects.Count >= numberOfFossilizedObjects){
      plagueParticleSystem.Stop();
      deprecatedPlayerController.ReanimateStaircase();
    }
  }
}
