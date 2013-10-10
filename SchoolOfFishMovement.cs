using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SchoolOfFishMovement : MonoBehaviour {
  [SerializeField]
  private GameObject leadFish;
  [SerializeField]
  private List<FishMovement> fish;

  void Start () {
    foreach(FishMovement f in fish){
      f.setLeadFish(leadFish);
    }
    BroadcastNextWaypoint(0);
  }

  void Update () {
  }

  public void BroadcastNextWaypoint(int waypointIndex){
    foreach(FishMovement f in fish){
      f.setNextWaypoint(waypointIndex);
      f.burstToNextWaypoint(true);
    }
  }
}
