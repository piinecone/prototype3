using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SchoolOfFishMovement : MonoBehaviour {
  [SerializeField]
  private FishMovement leadFish;
  [SerializeField]
  private List<FishMovement> fish;
  [SerializeField]
  private List<GameObject> waypoints = new List<GameObject>();

  void Start () {
    collectFish();
    if (waypoints.Count == 0){
      // randomize waypoints
    }
    foreach(FishMovement f in fish){
      f.setLeadFish(leadFish);
      f.setWaypoints(waypoints);
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

  void collectFish(){
    FishMovement[] fishies = GetComponentsInChildren<FishMovement>();
    for (int i = 0; i < fishies.Length; i++){
      fish.Add(fishies[i]);
    }
  }
}
