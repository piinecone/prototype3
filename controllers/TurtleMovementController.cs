using UnityEngine;
using System.Collections;

[RequireComponent(typeof (Animator))]
[RequireComponent(typeof (CapsuleCollider))]
[RequireComponent(typeof (Rigidbody))]
[RequireComponent(typeof (TurtleStateController))]

public class TurtleMovementController : MonoBehaviour {
  private Animator animator;
  private CapsuleCollider capsuleCollider;
  private CharacterController characterController;
  private TurtleStateController stateController;

  // configuration options
  [SerializeField]
  private bool invertedYAxis = false;
  [SerializeField]
  private float waterSurfaceLevel = 177.5f;
  [SerializeField]
  private float gravity = 20f;
  [SerializeField]
  private float animationRate = 1.5f;

  // stabilize the look position when the avatar is partially submerged
  private float currentYValueOfLookPosition = 0f;

  // movement
  private Vector3 lastKnownPosition;
  private Vector3 positionVector;
  private Quaternion targetRotation;

  // swim
  private float maximumSwimSpeed = 16.5f;
  private float forwardAccelerationUnderwater = 0f;
  private Vector3 underwaterMovementVectorInWorldSpace = Vector3.zero;
  private float currentDragCoefficientInWater;
  private float highSpeedDragCoefficientInWater = .98f;
  private float lowSpeedDragCoefficientInWater = .97f;
  private float dragDampener;
  private float maximumDragDampener = 1f;
  private float minimumDragDampener = .7f;
  private float appliedRollValue = 0f;
  private float maxRollRotationAngle = 52.5f;
  private float bankThresholdInSeconds = 1.35f;

  // walk
  Vector3 defaultTerrainRay = Vector3.down;
  float defaultSlope = 90f;
  private Vector3 moveDirection = Vector3.zero;
  [SerializeField]
  private float walkSpeedMultiplier = 5f;

  // input
  private Vector3 mouseInput;
  private Vector3 keyboardInput;
  private float rawForwardValue = 0f;
  private float rawHorizontalValue = 0f;
  private float rawPitchValue = 0f;
  private float rawYawValue = 0f;
  private float rawRollValue = 0f;

  // timing
  private float rawRollInputTimeElapsed = 0f;
  private float rawForwardInputTimeElapsed = 0f;

  void Start () {
    animator = GetComponent<Animator>();
    capsuleCollider = GetComponent<CapsuleCollider>();
    characterController = GetComponent<CharacterController>();
    stateController = GetComponent<TurtleStateController>();
    currentDragCoefficientInWater = lowSpeedDragCoefficientInWater;
    dragDampener = minimumDragDampener;
  }

  void FixedUpdate(){
    mapInputParameters();

    if (stateController.PlayerIsInWater())
      handleMovementInWater();
    else if (stateController.PlayerIsOnLand())
      walk(slope: defaultSlope, terrainRay: defaultTerrainRay);
  }

  void Update() {
    lastKnownPosition = transform.position;
    UpdateTransformPositionAndRotation();
  }

  private void UpdateTransformPositionAndRotation(){
    characterController.Move(positionVector * Time.deltaTime);
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeedInWater());
  }

  private void mapInputParameters(){
    float horizontalValue = Input.GetAxis("Horizontal");
    float verticalValue = Input.GetAxis("Vertical");

    // mouse input
    Vector3 mousePosition = Input.mousePosition;
    Ray mouseRay = Camera.main.ScreenPointToRay(mousePosition);
    mouseInput = mouseRay.direction;

    // keyboard input
    // FIXME no need for a vector3 here, right?
    Vector3 rawKeyboardInput = new Vector3(horizontalValue, 0, verticalValue);

    rawForwardValue = rawKeyboardInput.z;    // forward thrust
    rawHorizontalValue = rawKeyboardInput.x; // lateral thrust
    rawPitchValue = mouseInput.y;            // pitch
    rawRollValue = rawKeyboardInput.x;       // roll
    rawYawValue = mouseInput.x;              // yaw

    adjustRawInputValues();
    updateAnimatorStates();
  }

  private void adjustRawInputValues(){
    rawForwardInputTimeElapsed += Time.deltaTime;
    if (rawForwardValue == 0f)
      rawForwardInputTimeElapsed = Mathf.SmoothStep(rawForwardInputTimeElapsed, 0f, Time.deltaTime);

    rawRollInputTimeElapsed += Time.deltaTime;
    if (rawRollValue == 0 || bankingForMoreThan(2f * bankThresholdInSeconds)) rawRollInputTimeElapsed = 0f;

    if (bankingForMoreThan(bankThresholdInSeconds) && bankingForLessThan(2f * bankThresholdInSeconds) && rawForwardValue > .5f)
      rawForwardValue = 0f;
  }

  private void updateAnimatorStates(){
    animator.SetFloat("Speed", rawForwardValue);
    animator.SetFloat("Direction", rawHorizontalValue);

    if (rawForwardValue > .4f)
      animator.speed = Mathf.Max(5f * (1 - rawForwardValue), 1.6f);
    else
      animator.speed = 1.2f;
  }

  private bool bankingForMoreThan(float seconds){
    return (rawRollInputTimeElapsed >= seconds);
  }

  private bool bankingForLessThan(float seconds){
    return (rawRollInputTimeElapsed <= seconds);
  }

  private void handleMovementInWater(){
    if (stateController.PlayerIsEmergingFromWater())
      walk(slope: 120f, terrainRay: Vector3.forward);
    else if (stateController.PlayerIsUnderwater())
      swim();
  }

  private void swim(){
    animator.SetBool("Underwater", true);
    calculateRotationInWater();
    calculatePositionInWater();
  }

  private void calculateRotationInWater(){
    targetRotation = determineUnderwaterRotationFromInput();
  }

  private Quaternion determineUnderwaterRotationFromInput(){
    Quaternion rotation = Quaternion.identity;
    calculateAppliedRollValue();
    Quaternion lookRotation = currentLookRotation();
    rotation.eulerAngles = new Vector3(lookRotation.eulerAngles.x, lookRotation.eulerAngles.y, appliedRollValue);

    return rotation;
  }

  private Quaternion currentLookRotation(){
    Vector3 mousePosition = Input.mousePosition;
    Ray mouseRay = Camera.main.ScreenPointToRay(mousePosition);
    Vector3 lookDirection = mouseRay.direction;
    lookDirection.y *= currentYAxisMultiplier();

    return Quaternion.LookRotation(lookDirection);
  }

  private void calculateAppliedRollValue(){
    float forwardStep = Time.deltaTime * 6f;
    float backwardStep = Time.deltaTime * 3f;
    if (rawRollValue != 0f) {
      // to allow corkscrewing, multiply the max by the active time elapsed
      // appliedRollValue = Mathf.SmoothStep(appliedRollValue, rawRollValue * -maxRollRotationAngle * rawRollInputTimeElapsed (this is often zero), step * 1.5f);
      appliedRollValue = Mathf.SmoothStep(appliedRollValue, rawRollValue * -maxRollRotationAngle, forwardStep);
    } else {
      // to undo a corkscrew: instead of stepping to 0f, step to the next lowest upright rotation (modulo 2pi probably)
      appliedRollValue = Mathf.SmoothStep(appliedRollValue, 0f, backwardStep);
    }
  }

  private void calculatePositionInWater(){
    positionVector = underwaterThrustVector();
  }

  private Vector3 underwaterThrustVector(){
    calculateCurrentDragCoefficientInWater();
    calculateDragDampener();
    underwaterMovementVectorInWorldSpace *= currentDragCoefficientInWater * dragDampener;
    calculateForwardAccelerationUnderwater();
    underwaterMovementVectorInWorldSpace += transform.forward * forwardAccelerationUnderwater;

    return Vector3.ClampMagnitude(underwaterMovementVectorInWorldSpace, maximumSwimSpeed);
  }

  private void calculateCurrentDragCoefficientInWater(){
    float step = Time.deltaTime * 5f;
    if (rawForwardValue > 0.01f)
      currentDragCoefficientInWater = Mathf.SmoothStep(currentDragCoefficientInWater, highSpeedDragCoefficientInWater, step);
    else
      currentDragCoefficientInWater = Mathf.SmoothStep(currentDragCoefficientInWater, lowSpeedDragCoefficientInWater, step);
  }

  private void calculateDragDampener(){
    if (rawForwardValue > 0.01f)
      dragDampener = Mathf.SmoothStep(dragDampener, maximumDragDampener, Time.deltaTime * 5f);
    else
      dragDampener = Mathf.SmoothStep(dragDampener, minimumDragDampener, Time.deltaTime * 1f);
  }

  private void calculateForwardAccelerationUnderwater(){
    forwardAccelerationUnderwater = Mathf.Max(rawForwardValue, 0f) * 10f;
  }

  private void walk(float slope, Vector3 terrainRay){
    animator.SetBool("Underwater", false);
    slope = slope == null ? 90f : slope;
    terrainRay = terrainRay == null ? Vector3.down : terrainRay;
    characterController.slopeLimit = slope;
    alignPlayerWithTerrain(terrainRay: terrainRay);
    movePlayerOnLand();
  }

  private void alignPlayerWithTerrain(Vector3 terrainRay){
    Quaternion targetRotation = rotationForAlignmentWithTerrain(terrainRay: terrainRay);
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, terrainAlignmentRotationSpeed());
  }

  private void movePlayerOnLand(){
    gravity = 80f;
    moveDirection = new Vector3(0, 0, Input.GetAxis("Vertical"));
    moveDirection = transform.TransformDirection(moveDirection);
    moveDirection.y -= gravity * Time.deltaTime;
    characterController.Move(moveDirection * Time.deltaTime);
  }

  private Quaternion rotationForAlignmentWithTerrain(Vector3 terrainRay){
    RaycastHit hit;
    float terrainCheckDistance = 6f;
    Vector3 terrainCheckRay = transform.TransformDirection(terrainRay);
    Vector3 normal = transform.TransformDirection(Vector3.up);
    Vector3 lookDirection = transform.forward + transform.TransformDirection(new Vector3(Input.GetAxis("Horizontal"), 0, 0));
    if (Physics.Raycast(transform.position, terrainCheckRay, out hit, terrainCheckDistance))
      normal = hit.normal;
    Quaternion rotation = Quaternion.FromToRotation(transform.up, normal);
    return (rotation * Quaternion.LookRotation(lookDirection));
  }

  private float terrainAlignmentRotationSpeed(){
    return (currentRotateSpeed() * .05f * Time.deltaTime);
  }

  private float rotationSpeedInWater(){
    return currentRotateSpeed() * Time.deltaTime;
  }

  private float currentRotateSpeed(){
    return 20f;
  }

  private float currentYAxisMultiplier(){
    return 1.2f;
  }

  public float Velocity(){
    return Vector3.Distance(transform.position, lastKnownPosition) / Time.deltaTime;
  }
}
