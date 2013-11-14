using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BarrierController : MonoBehaviour {
  [SerializeField]
  private List<Barrier> barriers = new List<Barrier>();

  void Start () {
  }

  void Update () {
  }

  public bool applyForceVectorToBarrier(Vector3 forceVector, GameObject theBarrier, Vector3 playerPosition){
    Barrier barrier = getBarrierInstanceFromBarrierGameObject(theBarrier);
    if (!barrier.isSpecial() || (barrier.isSpecial() && playerIsCloseToBarrierOrSibling(barrier, playerPosition))){
      barrier.markAsDestroyed();
      barrier.applyForceVector(forceVector);
      return true;
    }
    return false;
  }

  private bool playerIsCloseToBarrierOrSibling(Barrier barrier, Vector3 playerPosition){
    float maxDistance = 100f;
    return (Vector3.Distance(barrier.transform.position, playerPosition) < maxDistance ||
              (barrier.sibling != null && Vector3.Distance(barrier.sibling.transform.position, playerPosition) < maxDistance));
  }

  public void attemptToMarkBarrierAsDestroyed(GameObject theBarrier, int attackStrength, bool special=false){
    Barrier barrier = getBarrierInstanceFromBarrierGameObject(theBarrier);
    barrier.willBeDestroyedByRushAttack(attackStrength, special);
  }

  public List<GameObject> getAllBarriersFor(GameObject theBarrier){
    Barrier barrier = getBarrierInstanceFromBarrierGameObject(theBarrier);
    List<GameObject> allBarriers = new List<GameObject>();
    allBarriers.Add(theBarrier);
    if (barrier.sibling != null) allBarriers.Add(barrier.sibling);

    return allBarriers;
  }

  public List<Barrier> activeBarriers(){
    return barriers;
  }

  public GameObject rendezvousPointForBarrier(GameObject theBarrier){
    Barrier barrier = getBarrierInstanceFromBarrierGameObject(theBarrier);
    return barrier.rendezvousPoint;
  }

  public void resurrectBarrier(GameObject aBarrier){
    Barrier barrier = getBarrierInstanceFromBarrierGameObject(aBarrier);
    barrier.markAsDestroyed(false);
    barrier.trapSchool();
  }

  public Barrier getBarrierInstanceFromBarrierGameObject(GameObject theBarrier){
    foreach(Barrier barrier in barriers){
      if (barrier.gameObject == theBarrier || barrier.gameObject == theBarrier.transform.parent.gameObject){
        return barrier;
      }
    }
    return null;
  }
}
