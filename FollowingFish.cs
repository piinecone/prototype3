using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FollowingFish : MonoBehaviour {
  [SerializeField]
  private float barrierTimeout = 1f;
  [SerializeField]
  private float targetingDistance = 100f;

  private List<FishMovement> fishCurrentlyFollowingPlayer = new List<FishMovement>();
  private float barrierTimeleft;

  void Start () {
  }
  
  void Update () {
    if (fishCurrentlyFollowingPlayer.Count > 0 && playerHasBeenLookingAtBarrierLongEnough()){
      Debug.Log("fire the fishies!");
    }
  }

  private bool playerHasBeenLookingAtBarrierLongEnough(){
    RaycastHit hit;
    Vector3 forwardRay = transform.forward * targetingDistance;
    bool targetingBarrier = false;

    if (Physics.Raycast(transform.position, forwardRay, out hit, targetingDistance)){
      if (hit.transform.gameObject.tag == "Barrier"){
        targetingBarrier = true;
        //Debug.DrawRay(transform.position, forwardRay, Color.red);
      }
    }

    barrierTimeleft = targetingBarrier ? barrierTimeleft - Time.deltaTime : barrierTimeout;
    return (barrierTimeleft > 0) ? false : true;
  }

  public void addFish(FishMovement fish){
    fishCurrentlyFollowingPlayer.Add(fish);
    // gainedFish(fish);
  }

  public void removeFish(FishMovement fish){
    fishCurrentlyFollowingPlayer.Remove(fish);
    // lostFish(fish);
  }
}
