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
    if (!barrier.isSpecial() || (barrier.isSpecial() && Vector3.Distance(barrier.transform.position, playerPosition) < 30f)){
      barrier.applyForceVector(forceVector);
      return true;
    }
    return false;
  }

  public void attemptToMarkBarrierAsDestroyed(GameObject theBarrier, int attackStrength){
    Barrier barrier = getBarrierInstanceFromBarrierGameObject(theBarrier);
    barrier.willBeDestroyedByRushAttack(attackStrength);
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

  private Barrier getBarrierInstanceFromBarrierGameObject(GameObject theBarrier){
    foreach(Barrier barrier in barriers){
      if (barrier.gameObject == theBarrier || barrier.gameObject == theBarrier.transform.parent.gameObject){
        return barrier;
      }
    }
    return null;
  }
}
