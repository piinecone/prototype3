using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof (BarrierController))]
public class FollowingFish : MonoBehaviour {
  [SerializeField]
  private float barrierTimeout;
  [SerializeField]
  private float targetingDistance;
  [SerializeField]
  private float minimumDistance;

  private BarrierController barrierController;
  private List<FishMovement> fishCurrentlyFollowingPlayer = new List<FishMovement>();
  private float barrierTimeleft;
  private GameObject targetedBarrier = null;

  void Start () {
    minimumDistance = 35f;
    barrierController = GetComponent<BarrierController>();
  }
  
  void Update () {
    if (fishCurrentlyFollowingPlayer.Count > 0 && playerHasBeenLookingAtBarrierLongEnough()){
        fireTheFishiesAtTargetedBarrier();
    }
  }

  private bool playerHasBeenLookingAtBarrierLongEnough(){
    RaycastHit hit;
    Vector3 forwardRay = transform.forward * targetingDistance;
    bool targetingBarrier = false;
    Debug.DrawRay(transform.position, forwardRay, Color.green);

    if (Physics.Raycast(transform.position, forwardRay, out hit, targetingDistance)){
      if (hit.transform.gameObject.tag == "Barrier" && Vector3.Distance(transform.position, hit.transform.position) >= minimumDistance){
        targetingBarrier = true;
        targetedBarrier = hit.transform.gameObject;
        Debug.DrawRay(transform.position, forwardRay, Color.red);
      } else {
        targetedBarrier = null;
      }
    }

    barrierTimeleft = targetingBarrier ? barrierTimeleft - Time.deltaTime : barrierTimeout;
    return (barrierTimeleft > 0) ? false : true;
  }

  private void fireTheFishiesAtTargetedBarrier(){
    List<GameObject> targetedBarriers = barrierController.getAllBarriersFor(targetedBarrier);
    for (int i = 0; i < targetedBarriers.Count; i++){
      barrierController.attemptToMarkBarrierAsDestroyed(targetedBarriers[i], fishCurrentlyFollowingPlayer.Count);
    }
    foreach(FishMovement fish in fishCurrentlyFollowingPlayer){
      int index = Random.Range(0,targetedBarriers.Count-1);
      fish.rushBarrier(targetedBarriers[index]);
    }
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
