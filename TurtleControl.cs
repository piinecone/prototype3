using UnityEngine;
using System.Collections;

[RequireComponent(typeof (Animator))]
[RequireComponent(typeof (CapsuleCollider))]
[RequireComponent(typeof (Rigidbody))]
public class TurtleControl : MonoBehaviour {

   public float animSpeed = 1.5f;            // a public setting for overall animator animation speed
   public float lookSmoother = 3f;           // a smoothing setting for camera motion
   public TurtleState turtleState;
   public bool useCurves;                 // a setting for teaching purposes to show use of curves
   public float speed = 4f;
   public float gravity = 20f;
   private float speedInMedium = 8f;
   private float upwardForce = 0f;
   private float rotateSpeed = 3f;
   private Vector3 moveDirection = Vector3.zero;
   
   private Animator anim;                    // a reference to the animator on the character
   private AnimatorStateInfo currentBaseState;     // a reference to the current state of the animator, used for base layer
   private AnimatorStateInfo layer2CurrentState;   // a reference to the current state of the animator, used for layer 2
   private CapsuleCollider col;              // a reference to the capsule collider of the character
   private CharacterController controller;
   private bool isCurrentlyLaunching = false;
   private Vector3 launchTargetPosition;

   static int idleState = Animator.StringToHash("Base Layer.Idle");  
   static int locoState = Animator.StringToHash("Base Layer.Locomotion");        // these integers are references to our animator's states

   void Start () {
      anim = GetComponent<Animator>();               
      col = GetComponent<CapsuleCollider>();          
      controller = GetComponent<CharacterController>();
   }
   
   void FixedUpdate ()
   {
      float h = Input.GetAxis("Horizontal");          // setup h variable as our horizontal input axis
      float v = Input.GetAxis("Vertical");            // setup v variables as our vertical input axis
      anim.SetFloat("Speed", v);                   // set our animator's float parameter 'Speed' equal to the vertical input axis            
      anim.SetFloat("Direction", h);                  // set our animator's float parameter 'Direction' equal to the horizontal input axis      
      anim.speed = animSpeed;                      // set the speed of our animator to the public variable 'animSpeed'
      currentBaseState = anim.GetCurrentAnimatorStateInfo(0);  // set our currentState variable to the current state of the Base Layer (0) of animation
   }
   
   void Update () {
     if (isCurrentlyLaunching == false){
       if (controller.isGrounded){
         anim.SetBool("Underwater", false); 
         walk();
       } else if (turtleState.isUnderwater()){
         anim.SetBool("Underwater", true); 
         swim();
       } else {
         anim.SetBool("Underwater", false); 
         idle();
       }
       // Move forward / backward
       //Vector3 forward = transform.TransformDirection(Vector3.forward);
       //float curSpeed = speed * Input.GetAxis ("Vertical");
       //controller.SimpleMove(forward * curSpeed);
     } else {
       if (Vector3.Distance(transform.position, launchTargetPosition) < 100f){
         isCurrentlyLaunching = false;
       } else {
         //rigidbody.AddForce(transform.forward * 10f);
         transform.position = Vector3.Lerp(transform.position, launchTargetPosition, Time.deltaTime);
       }
     }
   }

   void walk(){
     gravity = 50f;
     speedInMedium = speed;
     upwardForce = 0f;
     moveDirection = new Vector3(0, 0, Input.GetAxis("Vertical"));
     moveDirection = transform.TransformDirection(moveDirection);
     moveDirection *= speedInMedium;
     transform.Rotate(0, Input.GetAxis ("Horizontal") * rotateSpeed, 0);
     moveDirection.y -= gravity * Time.deltaTime;
     controller.Move(moveDirection * Time.deltaTime);
   }

   void swim(){
     gravity = 40f;
     speedInMedium = speed * 2;
     //upwardForce = Input.GetAxis("Vertical") * 4.7f;
     //if (upwardForce >= 4.5) upwardForce = 4.5f;
     moveDirection = new Vector3(Input.GetAxis("Horizontal") * 0.5f, 0, Input.GetAxis("Vertical"));
     moveDirection = transform.TransformDirection(moveDirection);
     moveDirection *= speedInMedium;
     //moveDirection.y += upwardForce * Time.deltaTime;

     Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
     Debug.DrawRay(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z), mouseRay.direction, Color.red);
     Vector3 lookPos = mouseRay.direction;// - transform.position;
     //lookPos.y = 0;
     Quaternion targetRotation = Quaternion.LookRotation(lookPos);
     transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 30f * rotateSpeed * Time.deltaTime);
     //transform.Rotate(0, Input.GetAxis ("Horizontal") * rotateSpeed, 0);
     //Quaternion referentialShift = Quaternion.FromToRotation(transform.forward, mouseRay.direction);
     //transform.Rotate(referentialShift.ToEulerAngles());
     moveDirection.y -= gravity * Time.deltaTime;
     controller.Move(moveDirection * Time.deltaTime);
   }

   void idle(){
     gravity = 20f;
     speedInMedium = speed;
     upwardForce = 0f;
     transform.Rotate(0, Input.GetAxis ("Horizontal") * rotateSpeed, 0);
     moveDirection.y -= gravity * Time.deltaTime;
     controller.Move(moveDirection * Time.deltaTime);
   }

   public void launchTo(Vector3 targetPosition){
     isCurrentlyLaunching = true;
     //rigidbody.isKinematic = false;
     launchTargetPosition = targetPosition;
     transform.LookAt(launchTargetPosition);
   }

   void launchCompleted(){
     isCurrentlyLaunching = false;
     //rigidbody.isKinematic = true;
   }
}
