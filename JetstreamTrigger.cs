using UnityEngine;
using System.Collections;

public class JetstreamTrigger : MonoBehaviour {

   [SerializeField]
   private GameObject startingJetstreams;
   [SerializeField]
   private GameObject hiddenJetstreams;
   [SerializeField]
   private GameObject superHiddenJetstreams;

   private bool streamsNotYetActivated;

   // Use this for initialization
   void Start () {
     startingJetstreams.SetActive(false);
     hiddenJetstreams.SetActive(false);
     superHiddenJetstreams.SetActive(false);
     streamsNotYetActivated = true;
   }

   // Update is called once per frame
   void Update () {
   }

   void OnTriggerEnter(Collider collider){
     if (collider.gameObject.tag == "Player"){
       hiddenJetstreams.SetActive(true);
       streamsNotYetActivated = false;
     }
   }

   public void activate(){
     if (streamsNotYetActivated){
       startingJetstreams.SetActive(true);
     }
   }

   public void PowerUpCollected(){
     startingJetstreams.SetActive(false);
     hiddenJetstreams.SetActive(false);
     superHiddenJetstreams.SetActive(true);
   }
}
