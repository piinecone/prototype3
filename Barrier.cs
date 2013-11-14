using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Barrier : MonoBehaviour {
  [SerializeField]
  private GameObject barrierComponentsContainer;
  [SerializeField]
  private int strength;
  [SerializeField]
  public GameObject trigger;
  [SerializeField]
  private SchoolOfFishMovement trappedSchool;
  [SerializeField]
  public GameObject sibling;
  [SerializeField]
  public bool special;
  [SerializeField]
  public List<SchoolOfFishMovement> requiredSchools = new List<SchoolOfFishMovement>();
  [SerializeField]
  public GameObject rendezvousPoint;
  [SerializeField]
  public ChaseBoundary chaseBoundary;

  private List<Rigidbody> barrierComponents = new List<Rigidbody>();
  private Rigidbody[] barrierComponentsArray;
  private bool destroyed = false;

  void Start () {
    barrierComponentsArray = barrierComponentsContainer.GetComponentsInChildren<Rigidbody>();
    foreach(Rigidbody barrierComponent in barrierComponentsArray){
      barrierComponents.Add(barrierComponent);
    }
  }

  void Update () {
  }

  public void applyForceVector(Vector3 forceVector){
    if (destroyed){
      jettisonBarrierComponents(forceVector);
      disableTrigger();
      freeTrappedSchool();
    }
  }

  private void jettisonBarrierComponents(Vector3 forceVector){
    int times = Random.Range(1, 4);
    for (int i = 0; i < times && barrierComponents.Count > 0; i++){
      int index = Random.Range(0, barrierComponents.Count - 1);
      Rigidbody component = barrierComponents[index];
      jettisonComponent(component, forceVector);
    } 
  }

  private void jettisonComponent(Rigidbody component, Vector3 forceVector){
    component.useGravity = true;
    component.collider.isTrigger = true;
    component.constraints = RigidbodyConstraints.None;
    Vector3 force = transform.InverseTransformDirection(forceVector);
    force.x = 0;
    float originalZ = force.z;
    float zComponent = Random.Range(force.z - 120f, force.z + 120f);
    force.z = zComponent;
    component.transform.Rotate(transform.right, (zComponent - originalZ)/2f);
    component.AddRelativeForce(force, ForceMode.Impulse);
    barrierComponents.Remove(component);
  }

  private void disableTrigger(){
    trigger.SetActive(false);
  }

  private void freeTrappedSchool(){
    if (trappedSchool != null) trappedSchool.free();
  }

  public void trapSchool(){
    if (trappedSchool != null) trappedSchool.trap();
  }

  public SchoolOfFishMovement schoolOfFish(){
    return trappedSchool;
  }

  public bool willBeDestroyedByRushAttack(int attackStrength, bool special=false){
    if (special && isSpecial()){
      return false;
    } else {
      if (strength <= attackStrength){
        destroyed = true;
        return true;
      } else {
        return false;
      }
    }
  }

  public bool isViableTarget(){
    return !destroyed && !special;
  }

  public bool isSpecial(){
    return special;
  }

  public bool isDestroyed(){
    return destroyed;
  }

  public void markAsDestroyed(bool isDestroyed=true){
    destroyed = isDestroyed;
  }

  public int Strength(){
    return strength;
  }

  public ChaseBoundary getChaseBoundary(){
    return chaseBoundary;
  }
}
