using UnityEngine;
using System.Collections;

public class MediumManager : MonoBehaviour {

  [SerializeField]
  private SurfaceCollider surfaceCollider;
  [SerializeField]
  private TurtleController turtleController;

  void Start(){
  }

  void Update(){
  }

  public void playerIsNearTheSurface(bool state=true){
    turtleController.playerIsNearTheSurface(state);
  }
}
