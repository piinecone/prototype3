using UnityEngine;
using System.Collections;

[RequireComponent(typeof (Animator))]
[RequireComponent(typeof (CapsuleCollider))]
[RequireComponent(typeof (Rigidbody))]
[RequireComponent(typeof (FollowingFish))]
[RequireComponent(typeof (BarrierController))]
public class TurtleController : MonoBehaviour {

  public float animSpeed = 1.5f;
  public float speed = 4f;
  public float gravity = 20f;
  public FollowingFish followingFish;
  [SerializeField]
  private float defaultRotateSpeed;
  [SerializeField]
  private float targetModeRotateSpeed;
  [SerializeField]
  private ThirdPersonCamera thirdPersonCamera;

  private float speedInMedium = 8f;
  private Vector3 moveDirection = Vector3.zero;
  private CharacterController controller;
  private Animator anim;
  private CapsuleCollider col;
  private AnimatorStateInfo currentBaseState;
  private Vector3 previousPosition;
  private BarrierController barrierController;

  void Start () {
    anim = GetComponent<Animator>();               
    col = GetComponent<CapsuleCollider>();          
    controller = GetComponent<CharacterController>();
    followingFish = GetComponent<FollowingFish>();
    barrierController = GetComponent<BarrierController>();
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
    //Debug.DrawRay(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z), mouseRay.direction, Color.red);
    Vector3 lookPos = mouseRay.direction;// - transform.position;
    lookPos.y *= currentYAxisMultiplier();
    Quaternion targetRotation = Quaternion.LookRotation(lookPos);
    // FIXME at some point in the future the currentRotateSpeed should be smoothed out based on the elapsed time since the 
    // camera state changed. So if the camera were in targeting mode, then the targeting button was released, the release time
    // would be recorded, decremented every update(), and used to calculate the rotation speed as follows:
    // if (1f - timeSinceRelease) == 1
    //   4 * slowRotationSpeed + 1 * fastRotationSpeed / 5
    // else if (1f - timeSinceRelease >= .75)
    //   3 * slowRotationSpeed + 2 * fastRotationSpeed / 5
    // else if (1f - timeSinceRelease >= .5)
    //   2 * slowRotationSpeed + 3 * fastRotationSpeed / 5
    // else if (1f - timeSinceRelease >= .25)
    //   1 * slowRotationSpeed + 4 * fastRotationSpeed / 5
    // else if (1f - timeSinceRelease >= 0)
    //   0 * slowRotationSpeed + 5 * fastRotationSpeed / 5
    // end
    // except do this intelligently with a function
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, currentRotateSpeed() * Time.deltaTime);
    moveDirection.y -= gravity * Time.deltaTime;

    controller.Move(moveDirection * Time.deltaTime);
  }

  public float velocity(){
    return (Vector3.Distance(transform.position, previousPosition)) / Time.deltaTime;
  }

  // FIXME this is just a wrapper for followingFish :/
  public void addFish(FishMovement fish){
    followingFish.addFish(fish);
    thirdPersonCamera.addObjectThatMustAlwaysRemainInFieldOfView(fish.transform.gameObject);
  }

  public void removeFish(FishMovement fish){
    followingFish.removeFish(fish);
    thirdPersonCamera.removeObjectThatMustAlwaysRemainInFieldOfView(fish.transform.gameObject);
  }

  public void applyForceVectorToBarrier(Vector3 forceVector, GameObject barrier){
    barrierController.applyForceVectorToBarrier(forceVector, barrier);
  }

  private float currentRotateSpeed(){
    return thirdPersonCamera.getCamState() == "Behind" ? defaultRotateSpeed : targetModeRotateSpeed;
  }

  private float currentYAxisMultiplier(){
    return thirdPersonCamera.getCamState() == "Behind" ? 1.5f : 0.5f;
  }
}
