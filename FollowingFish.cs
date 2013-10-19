using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof (BarrierController))]
public class FollowingFish : MonoBehaviour {
  [SerializeField]
  private float targetingDistance;
  [SerializeField]
  private float nearbyBarrierDistanceThreshold;

  private BarrierController barrierController;
  private List<FishMovement> fishCurrentlyFollowingPlayer = new List<FishMovement>();
  private GameObject targetedBarrier = null;

  void Start () {
    nearbyBarrierDistanceThreshold = 75f;
    barrierController = GetComponent<BarrierController>();
  }
  
  void Update () {
    if (playerHasEnoughFish() && fishAreReadyToRush() && nearbyBarrierIsVisible()){
      fireTheFishiesAtTargetedBarrier();
    }
  }

  private bool playerHasEnoughFish(){
    return (fishCurrentlyFollowingPlayer.Count > 0);
  }

  private bool fishAreReadyToRush(){
    // FIXME check if the fish are currently rushing or finishing a rush
    return true;
  }

  private bool nearbyBarrierIsVisible(){
    targetedBarrier = null;
    Barrier nearestVisibleBarrier;
    float nearestBarrierDistance = nearbyBarrierDistanceThreshold;
    List<Barrier> barriers = barrierController.activeBarriers();
    foreach(Barrier barrier in barriers){
      if (!barrier.isViableTarget()) continue;
      Vector3 direction = (barrier.transform.position - transform.position).normalized;
      Vector3 forward = transform.forward;
      if (Vector3.Angle(direction, forward) < 45F){
        float distance = Vector3.Distance(transform.position, barrier.transform.position);
        if (distance < nearbyBarrierDistanceThreshold && distance < nearestBarrierDistance){
          nearestBarrierDistance = distance;
          nearestVisibleBarrier = barrier;
          targetedBarrier = nearestVisibleBarrier.trigger;
        }
      }
    }

    return (targetedBarrier != null);
  }

  private void fireTheFishiesAtTargetedBarrier(){
    int index = 0;
    List<GameObject> targetedBarriers = barrierController.getAllBarriersFor(targetedBarrier);
    for (int i = 0; i < targetedBarriers.Count; i++){
      barrierController.attemptToMarkBarrierAsDestroyed(targetedBarriers[i], fishCurrentlyFollowingPlayer.Count);
    }
    foreach(FishMovement fish in fishCurrentlyFollowingPlayer){
      fish.rushBarrier(targetedBarriers[index % targetedBarriers.Count]);
      index++;
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
