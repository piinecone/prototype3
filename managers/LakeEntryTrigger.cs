using UnityEngine;
using System.Collections;

public class LakeEntryTrigger : MonoBehaviour {

  [SerializeField]
  private CutSceneManager manager;

  private bool didPlay = false;

  void Start () {
  }

  void Update () {
  }

  void OnTriggerEnter(Collider collider){
    if (!didPlay && collider.gameObject.tag == "Player"){
      manager.playCutSceneFor("Lake Entry");
      manager.startReminders();
      didPlay = true;
    }
  }
}
