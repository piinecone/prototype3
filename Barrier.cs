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
      int times = Random.Range(1, 2);
      for (int i = 0; i < times && barrierComponents.Count > 0; i++){
        int index = Random.Range(0, barrierComponents.Count - 1);
        Rigidbody component = barrierComponents[index];
        jettisonComponent(component, forceVector);
      }

      disableTrigger();
      freeTrappedSchool();
    }
  }

  private void jettisonComponent(Rigidbody component, Vector3 forceVector){
    component.useGravity = true;
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
    if (trappedSchool != null)
      trappedSchool.free();
  }

  public bool willBeDestroyedByRushAttack(int attackStrength){
    if (strength <= attackStrength){
      destroyed = true;
      return true;
    } else {
      return false;
    }
  }

  public bool isViableTarget(){
    return !destroyed;
  }
}
