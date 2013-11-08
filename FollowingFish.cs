using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof (BarrierController))]
[RequireComponent(typeof (TurtleController))]
public class FollowingFish : MonoBehaviour {
  [SerializeField]
  private float targetingDistance;
  [SerializeField]
  private float nearbyBarrierDistanceThreshold;
  [SerializeField]
  private TurtleController turtleController;

  private BarrierController barrierController;
  private List<FishMovement> fishCurrentlyFollowingPlayer = new List<FishMovement>();
  private GameObject targetedBarrier = null;

  void Start () {
    turtleController = GetComponent<TurtleController>();
    nearbyBarrierDistanceThreshold = 75f;
    barrierController = GetComponent<BarrierController>();
  }

  void Update () {
    if (playerHasEnoughFish() && fishAreReadyToRush() && nearbyBarrierIsVisible()){
      rushBarrier();
    }
  }

  public void rushBarrier(GameObject theBarrier=null, bool special=false){
    GameObject barrier = theBarrier == null ? targetedBarrier : theBarrier;
    List<GameObject> targetedBarriers = barrierController.getAllBarriersFor(barrier);
    fireTheFishiesAtTargetedBarriers(targetedBarriers, special);
    //acceleratePlayerTowardTargetedBarrierPosition(targetedBarriers[0].transform.position);
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

  private void fireTheFishiesAtTargetedBarriers(List<GameObject> targetedBarriers, bool special=false){
    int index = 0;
    GameObject rendezvousPoint = barrierController.rendezvousPointForBarrier(targetedBarriers[0]);
    for (int i = 0; i < targetedBarriers.Count; i++){
      barrierController.attemptToMarkBarrierAsDestroyed(targetedBarriers[i], fishCurrentlyFollowingPlayer.Count, special);
    }
    foreach(FishMovement fish in fishCurrentlyFollowingPlayer){
      if (special && !fish.isSpecial()) continue;
      fish.rushBarrier(targetedBarriers[index % targetedBarriers.Count], rendezvousPoint);
      index++;
    }
  }

  public void abortRushAttempt(bool special=false){
    List<FishMovement> fishToRemoveFromFollowers = new List<FishMovement>();
    foreach(FishMovement fish in fishCurrentlyFollowingPlayer){
      if (special && !fish.isSpecial()) continue;
      fish.abortRushAttempt();
      fishToRemoveFromFollowers.Add(fish);
    }
    foreach(FishMovement fish in fishToRemoveFromFollowers){
      fish.stopFollowingPlayer();
    }
  }


  private void acceleratePlayerTowardTargetedBarrierPosition(Vector3 position){
    int fishCount = fishCurrentlyFollowingPlayer.Count;
    int strength = fishCount > 10 ? 10 : fishCount;
    turtleController.accelerateToward(position, strength);
  }

  public void addFish(FishMovement fish){
    fishCurrentlyFollowingPlayer.Add(fish);
  }

  public void removeFish(FishMovement fish){
    fishCurrentlyFollowingPlayer.Remove(fish);
  }

  public int numberOfFollowingFish(){
    return fishCurrentlyFollowingPlayer.Count;
  }

  public void beginOrbiting(GameObject aGameObject){
    foreach(FishMovement fish in fishCurrentlyFollowingPlayer)
      fish.BeginToOrbit(aGameObject);
  }

  public void stopOrbiting(){
    foreach(FishMovement fish in fishCurrentlyFollowingPlayer)
      fish.StopOrbiting();
  }
}
