using UnityEngine;
using System.Collections;

public class Jetstream : MonoBehaviour {

   [SerializeField]
   private GameObject target;
   [SerializeField]
   private TurtleControl turtle;

   private Vector3 startPosition;
   private Vector3 targetPosition;
   private GameObject player;

	// Use this for initialization
	void Start () {
     startPosition = transform.position;
     player = GameObject.FindWithTag("Player");
     targetPosition = target.transform.position;
	}
	
	// Update is called once per frame
	void FixedUpdate () {
     //if (Vector3.Distance(transform.position, target.transform.position) < 10f){
     //  transform.position = startPosition;
     //} else {
     //  transform.position = Vector3.Lerp(transform.position, target.transform.position, 2f * Time.deltaTime);
     //}
	}

   void OnTriggerEnter(Collider collider){
     if (collider.gameObject.tag == "Player"){
       turtle.launchTo(targetPosition);
     }
   }

   //void OnTriggerStay(Collider collider){
   //  //if (collider.gameObject.tag == "Player" && Vector3.Distance(player.transform.position, transform.position) > 10f){
   //  if (collider.gameObject.tag == "Player"){
   //    //Debug.Log("LAUNCH THAT TURTLE" + Time.time);
   //    player.transform.LookAt(targetPosition);
   //    player.transform.position = Vector3.Lerp(player.transform.position, targetPosition, Time.deltaTime);
   //  }
   //}
}
