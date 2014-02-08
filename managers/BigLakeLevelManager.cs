using UnityEngine;
using System.Collections;

public class BigLakeLevelManager : MonoBehaviour {
  [SerializeField]
  private int numberOfFossilizedObjects = 0;

  private TurtleController deprecatedPlayerController;
  private int reanimatedObjectsCount = 0;

  void Start () {
    deprecatedPlayerController = GameObject.FindWithTag("Player").GetComponent<TurtleController>();
  }

  public void ReanimatedGameObject(GameObject fossilizedObject){
    reanimatedObjectsCount++;
    if (reanimatedObjectsCount >= numberOfFossilizedObjects)
      deprecatedPlayerController.ReanimateStaircase();
  }
}
