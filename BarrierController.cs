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

  public void applyForceVectorToBarrier(Vector3 forceVector, GameObject theBarrier){
    Barrier barrier = getBarrierInstanceFromBarrierGameObject(theBarrier);
    barrier.applyForceVector(forceVector);
  }

  public void attemptToMarkBarrierAsDestroyed(GameObject theBarrier, int attackStrength){
    Barrier barrier = getBarrierInstanceFromBarrierGameObject(theBarrier);
    barrier.willBeDestroyedByRushAttack(attackStrength);
  }

  public Barrier getBarrierInstanceFromBarrierGameObject(GameObject theBarrier){
    foreach(Barrier barrier in barriers){
      if (barrier.gameObject == theBarrier.transform.parent.gameObject){
        return barrier;
      }
    }
    return null;
  }
}
