using UnityEngine;
using System.Collections;

[RequireComponent(typeof (Animator))]
[RequireComponent(typeof (CapsuleCollider))]
[RequireComponent(typeof (Rigidbody))]
public class TurtleController : MonoBehaviour {
   public float animSpeed = 1.5f;
   public float speed = 4f;
   public float gravity = 20f;

   private float speedInMedium = 8f;
   private float rotateSpeed = 3f;
   private Vector3 moveDirection = Vector3.zero;
   private CharacterController controller;
   private Animator anim;
   private CapsuleCollider col;
   private AnimatorStateInfo currentBaseState;
   private Vector3 previousPosition;

   void Start () {
      anim = GetComponent<Animator>();               
      col = GetComponent<CapsuleCollider>();          
      controller = GetComponent<CharacterController>();
   }

   void FixedUpdate ()
   {
      float h = Input.GetAxis("Horizontal");
      float v = Input.GetAxis("Vertical");
      anim.SetFloat("Speed", v);
      anim.SetFloat("Direction", h);
      anim.speed = animSpeed;
      currentBaseState = anim.GetCurrentAnimatorStateInfo(0);
   }
   
   void Update () {
     previousPosition = transform.position;
     swim(); // we're always underwater for now
   }

   void swim(){
     gravity = 40f;
     speedInMedium = speed * 4;
     moveDirection = new Vector3(Input.GetAxis("Horizontal") * 0.5f, 0, Input.GetAxis("Vertical"));
     moveDirection = transform.TransformDirection(moveDirection);
     moveDirection *= speedInMedium;

     Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
     Debug.DrawRay(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z), mouseRay.direction, Color.red);
     Vector3 lookPos = mouseRay.direction;// - transform.position;
     Quaternion targetRotation = Quaternion.LookRotation(lookPos);
     transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 30f * rotateSpeed * Time.deltaTime);
     moveDirection.y -= gravity * Time.deltaTime;

     controller.Move(moveDirection * Time.deltaTime);
   }

   public float velocity(){
     return (Vector3.Distance(transform.position, previousPosition)) / Time.deltaTime;
   }
}
