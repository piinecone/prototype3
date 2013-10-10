using UnityEngine;
using System.Collections;

public class RockArchTrigger : MonoBehaviour {

   [SerializeField]
   private JetstreamTrigger jetstreams;

   // Use this for initialization
   void Start () {
   
   }
   
   // Update is called once per frame
   void Update () {
   
   }

   void OnTriggerEnter(Collider collider){
     if (collider.gameObject.tag == "Player"){
       jetstreams.activate();
     }
   }
}
