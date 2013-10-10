using UnityEngine;
using System.Collections;

public class Movable : MonoBehaviour {

   [SerializeField]
   private GameObject proxySubject;

   void Start () {
     Color color = Color.magenta;
     color.a = 0.5f;
     renderer.material.color = color;
   }
   
   void Update () {
   
   }

   void OnCollisionStay(Collision collision){
     if (collision.gameObject.tag == "Player"){
       GameObject player = collision.gameObject;
       rigidbody.constraints = RigidbodyConstraints.None;
       rigidbody.AddForce(player.transform.forward * 10);
       //Debug.DrawRay(new Vector3(transform.position.x, transform.position.y + 2f, transform.position.z), player.transform.forward * 50, Color.red);
       Vector3 proxySubjectForward = transform.InverseTransformDirection(player.transform.forward);
       //Debug.DrawRay(new Vector3(transform.position.x, transform.position.y + 2f, transform.position.z), proxySubjectForward * 10, Color.blue);
       Vector3 force = proxySubjectForward * 100000;
       proxySubject.rigidbody.constraints = RigidbodyConstraints.None;
       proxySubject.rigidbody.AddForce(force);
       //Debug.DrawRay(proxySubject.transform.position, proxySubjectForward * 100, Color.blue);
       //Vector3 proxySubjectForward = proxySubject.transform.InverseTransformDirection(proxyForward);
       //float angle = Quaternion.Angle(transform.rotation, proxySubject.transform.rotation);
       //Quaternion rotation = Quaternion.AngleAxis(angle, proxySubject.transform.up);
       //Debug.Log(angle);
       //Vector3 force = (rotation * player.transform.forward);
       //Debug.DrawRay(proxySubject.transform.position, force * 50, Color.red);
     }
   }
}
