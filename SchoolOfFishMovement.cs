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
  [SerializeField]
  private GameObject seedPoint;
  [SerializeField]
  private float verticalRange = 10f;
  [SerializeField]
  private Transform shoalPoint;
  [SerializeField]
  private bool trapped;
  [SerializeField]
  private bool shoaling;
  [SerializeField]
  private bool canFollowPlayer;

  void Start () {
    collectFish();
    initializeWaypoints();
    initializeFish();
    BroadcastNextWaypoint(0);
  }

  void initializeWaypoints(){
    if (seedPoint != null && waypoints.Count == 0){
      for (int i = 0; i < 20; i++){
        GameObject waypoint = new GameObject();
        Vector3 randomWaypoint = seedPoint.transform.position + Random.insideUnitSphere * 80;
        float y = seedPoint.transform.position.y;
        randomWaypoint.y = Random.Range(y - verticalRange, y + verticalRange);
        waypoint.transform.position = randomWaypoint;
        waypoints.Add(waypoint);
      }
    }
  }

  void initializeFish(){
    foreach(FishMovement f in fish){
      f.setLeadFish(leadFish);
      f.toggleShoaling(shoaling);
      f.setTrapped(trapped);
      f.canFollowPlayer = canFollowPlayer;
      if (shoalPoint != null) {
        f.setShoalPoint(shoalPoint);
      } else {
        f.setWaypoints(waypoints);
      }
    }
  }

  void Update () {
  }

  public void BroadcastNextWaypoint(int waypointIndex){
    foreach(FishMovement f in fish){
      f.setNextWaypoint(waypointIndex);
      if (!trapped) f.burstToNextWaypoint(true);
    }
  }

  void collectFish(){
    FishMovement[] fishies = GetComponentsInChildren<FishMovement>();
    for (int i = 0; i < fishies.Length; i++){
      fish.Add(fishies[i]);
    }
  }

  public void free(){
    trapped = false;
    foreach(FishMovement f in fish){
      f.setTrapped(trapped);
    }
  }
}
