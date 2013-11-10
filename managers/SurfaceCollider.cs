using UnityEngine;
using System.Collections;

public class SurfaceCollider : MonoBehaviour {

  [SerializeField]
  private MediumManager mediumManager;

  void Start(){

  }

  void OnTriggerEnter(Collider collider){
    if (collider.gameObject.tag == "Player"){
      mediumManager.playerIsNearTheSurface(true);
    }
  }

  void OnTriggerStay(Collider collider){
    if (collider.gameObject.tag == "Player"){
      mediumManager.playerIsNearTheSurface(true);
    }
  }

  void OnTriggerExit(Collider collider){
    if (collider.gameObject.tag == "Player"){
      mediumManager.playerIsNearTheSurface(false);
    }
  }
}
