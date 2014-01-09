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
  private string lastRecordedState;

  // swim
  private float maximumSwimSpeed;
  private float defaultMaximumSwimSpeed = 16.5f;
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
  [SerializeField]
  private float walkSpeedMultiplier = 5f;
  private Vector3 defaultTerrainRay = Vector3.down;
  float defaultSlope = 90f;
  private Vector3 moveDirection = Vector3.zero;
  private float speedOnLand = 10f;

  // surfacing
  private float lastLookDirectionYValue = 0f;

  // submerging
  private bool isCurrentlySubmerging = false;
  private float submergeTimeLeft = 0f;
  private float submergeDuration = 1f;
  private float submersionDirection = 1f;

  // input
  private Vector3 mouseInput;
  private Vector3 keyboardInput;
  private float rawForwardValue = 0f;
  private float rawHorizontalValue = 0f;
  private float rawPitchValue = 0f;
  private float rawYawValue = 0f;
  private float rawRollValue = 0f;
  private float previousRollValue = 0f;

  // barrel roll
  private float barrelRollCaptureTimeLeft = 0f;
  private float barrelRollCaptureTime = .5f;
  private int barrelRollDirection = 0;
  private bool barrelRollArmed = false;
  private bool performingBarrelRoll = false;
  private float rollRotationOffset = 0f;
  private Vector3 rollPositionVector = Vector3.zero;
  private float barrelRollSpeed = 10f;

  // corkscrew launch
  private bool preparingForCorkscrewLaunch = false;
  private bool performingCorkscrewLaunch = false;
  private bool finishingACorkscrewLaunch = false;
  private int corkscrewDirection = 0;
  private float corkscrewLaunchSpeed = 150f;
  private float corkscrewPerformanceRollSpeed = 30f;
  private float corkscrewPreparationTimeLeft = 0;
  private float corkscrewPreparationDuration = 2f;
  private float corkscrewPerformanceTimeLeft = 0;
  private float corkscrewPerformanceDuration = .1f;
  private float corkscrewResidualSpeedDuration = 4f;
  private float corkscrewCompletionRollSpeed = 10f;

  // splash
  private bool didJustSplashIntoWater = false;

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
    maximumSwimSpeed = defaultMaximumSwimSpeed;
  }

  void FixedUpdate(){
    mapInputParameters();
    handleStateChange();

    if (isCurrentlySubmerging || isUnderwater())
      swim();
    else if (isEmerging())
      emerge();
    else if (stateController.PlayerIsOnLand())
      walk(slope: defaultSlope, terrainRay: defaultTerrainRay);
    else if (isFalling())
      fall();
  }

  void Update() {
    lastKnownPosition = transform.position;
    updateTransformPositionAndRotation();
  }

  private void updateTransformPositionAndRotation(){
    if (isCurrentlySubmerging) adjustPlayerPositionForSubmersion();
    characterController.Move(positionVector * Time.deltaTime);
    if (!isEmerging() && !isCurrentlySubmerging && isSwimming()) adjustPlayerPositionNearWaterSurface();
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeedInMedium());
    lastLookDirectionYValue = transform.forward.y;
  }

  private void adjustPlayerPositionNearWaterSurface(){
    if (stateController.ShouldLockVerticalPosition()){
      transform.position = new Vector3(transform.position.x, Mathf.Min(transform.position.y, stateController.VerticalPositionMaximum()), transform.position.z);
    } else {
      float angle = Vector3.Angle(positionVector, Vector3.up);
      float magnitude = Vector3.ClampMagnitude(underwaterMovementVectorInWorldSpace, maximumSwimSpeed).magnitude;
      if (isNearSurface() && (transform.position.y + .5f) >= waterSurfaceLevel && angle <= 90f && magnitude <= defaultMaximumSwimSpeed)
        transform.position = new Vector3(transform.position.x, waterSurfaceLevel - .5f, transform.position.z);
    }
  }

  private void adjustPlayerPositionForSubmersion(){
    positionVector = transform.forward * 20f * submersionDirection;
    positionVector.y = Mathf.Min(waterSurfaceLevel - 2f, positionVector.y - 1f);
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

    previousRollValue = rawRollValue;        // store roll value slope
    rawForwardValue = rawKeyboardInput.z;    // forward thrust
    rawHorizontalValue = rawKeyboardInput.x; // lateral thrust
    rawPitchValue = mouseInput.y;            // pitch
    rawRollValue = rawKeyboardInput.x;       // roll
    rawYawValue = mouseInput.x;              // yaw

    adjustRawInputValues();
    updateAnimatorStates();
    if (isUnderwater()) captureBarrelRoll();
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

    if (isSwimming()){
      if (rawForwardValue > .4f)
        animator.speed = Mathf.Max(5f * (1 - rawForwardValue), 1.6f);
      else
        animator.speed = 1.2f;
    } else {
      animator.speed = 1.9f;
    }
  }

  private void captureBarrelRoll(){
    if (performingBarrelRoll) return;

    if (Mathf.Abs(rawRollValue) > Mathf.Abs(previousRollValue)){
      if (barrelRollCaptureTimeLeft > 0f && barrelRollArmed && rollDirectionFromInput() == barrelRollDirection)
        performBarrelRoll();
      else
        primeBarrelRoll();
    } else if (barrelRollCaptureTimeLeft > 0f && Mathf.Abs(rawRollValue) < Mathf.Abs(previousRollValue)){
      barrelRollArmed = true;
    }

    barrelRollCaptureTimeLeft -= Time.deltaTime;
  }

  private int rollDirectionFromInput(){
    return (rawRollValue > 0f ? 1 : -1);
  }

  private void performBarrelRoll(){
    performingBarrelRoll = true;
    barrelRollArmed = false;
    barrelRollCaptureTimeLeft = barrelRollCaptureTime;
    rollRotationOffset = Mathf.Abs(appliedRollValue);
    rollPositionVector = transform.right * barrelRollDirection * 12f;
    attemptCorkscrewLaunch();
  }

  private void primeBarrelRoll(){
    barrelRollDirection = rollDirectionFromInput();
    barrelRollCaptureTimeLeft = barrelRollCaptureTime;
    barrelRollArmed = false;
    performingBarrelRoll = false;
  }

  private bool bankingForMoreThan(float seconds){
    return (rawRollInputTimeElapsed >= seconds);
  }

  private bool bankingForLessThan(float seconds){
    return (rawRollInputTimeElapsed <= seconds);
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

  private Quaternion determineAirborneRotation(){
    Quaternion rotation = Quaternion.identity;
    calculateAppliedRollValue();
    Quaternion lookRotation = Quaternion.LookRotation(transform.TransformDirection(Vector3.down));
    float x = transform.rotation.eulerAngles.x + 2f;
    if (x < 90f && x > 45f) x = 45f;
    rotation.eulerAngles = new Vector3(x, transform.rotation.eulerAngles.y, appliedRollValue);

    return rotation;
  }

  private Quaternion currentLookRotation(){
    Vector3 mousePosition = Input.mousePosition;
    Ray mouseRay = Camera.main.ScreenPointToRay(mousePosition);
    Vector3 lookDirection = mouseRay.direction;
    lookDirection.y *= currentYAxisMultiplier();
    lookDirection = adjustedLookDirectionNearWaterSurface(lookDirection);
    lookDirection = adjustedLookDirectionDuringSubmersion(lookDirection);

    return Quaternion.LookRotation(lookDirection);
  }

  private Vector3 adjustedLookDirectionNearWaterSurface(Vector3 lookDirection){
    if (transform.position.y + .6f >= waterSurfaceLevel && lookDirection.y >= 0f){
      lookDirection.y = lastLookDirectionYValue;
      Vector3 adjustedLookDirection = new Vector3(lookDirection.x, .125f, lookDirection.z);
      lookDirection = Vector3.Lerp(lookDirection, adjustedLookDirection, 30f * Time.deltaTime);
    }

    return lookDirection;
  }

  private Vector3 adjustedLookDirectionDuringSubmersion(Vector3 lookDirection){
    if (isCurrentlySubmerging)
      return new Vector3(lookDirection.x, -.125f, lookDirection.z);
    return lookDirection;
  }

  private void calculateAppliedRollValue(){
    if (performingBarrelRoll)
      calculateAppliedRollValueForBarrelRoll();
    else if (preparingForCorkscrewLaunch)
      calculateAppliedRollValueForCorkscrewPreparation();
    else if (performingCorkscrewLaunch)
      calculateAppliedRollValueForCorkscrewPerformance();
    else if (finishingACorkscrewLaunch)
      calculateAppliedRollValueForCorkscrewCompletion();
    else
      calculateAppliedRollValueForBanking();
  }

  private void calculateAppliedRollValueForBarrelRoll(){
    if (Mathf.Abs(appliedRollValue) < (360f + rollRotationOffset)){
      appliedRollValue -= (barrelRollDirection * barrelRollSpeed);
    } else if (Mathf.Abs(appliedRollValue) >= (360f + rollRotationOffset)){
      appliedRollValue -= (appliedRollValue + (barrelRollDirection * rollRotationOffset));
      performingBarrelRoll = false;
    }
  }

  private void calculateAppliedRollValueForCorkscrewPreparation(){
    appliedRollValue -= (corkscrewDirection * barrelRollSpeed * 1.5f);
  }

  private void calculateAppliedRollValueForCorkscrewPerformance(){
    appliedRollValue -= (corkscrewDirection * corkscrewPerformanceRollSpeed);
  }

  private void calculateAppliedRollValueForCorkscrewCompletion(){
    float maximumRotation = 5f * 360f;
    if (Mathf.Abs(appliedRollValue) < (maximumRotation + rollRotationOffset)){
      appliedRollValue -= (corkscrewDirection * corkscrewCompletionRollSpeed);
    } else if (Mathf.Abs(appliedRollValue) >= (maximumRotation + rollRotationOffset)){
      appliedRollValue -= (appliedRollValue + (corkscrewDirection * rollRotationOffset));
      finishingACorkscrewLaunch = false;
      StopCoroutine("decreaseCorkscrewCompletionRollSpeed");
    }
  }

  private void calculateAppliedRollValueForBanking(){
    float forwardStep = Time.deltaTime * 6f;
    float backwardStep = Time.deltaTime * 3f;
    if (rawRollValue != 0f)
      appliedRollValue = Mathf.SmoothStep(appliedRollValue, rawRollValue * -maxRollRotationAngle, forwardStep);
    else
      appliedRollValue = Mathf.SmoothStep(appliedRollValue, 0f, backwardStep);
  }

  private void calculatePositionInWater(){
    positionVector = underwaterThrustVector();
    if (!isNearSurface()) positionVector.y -= gravity * Time.deltaTime;
  }

  private Vector3 underwaterThrustVector(){
    calculateCurrentDragCoefficientInWater();
    calculateDragDampener();
    underwaterMovementVectorInWorldSpace *= currentDragCoefficientInWater * dragDampener;
    calculateForwardAccelerationUnderwater();
    underwaterMovementVectorInWorldSpace += transform.forward * forwardAccelerationUnderwater;
    accountForSpecialMoves();

    return Vector3.ClampMagnitude(underwaterMovementVectorInWorldSpace, maximumSwimSpeed);
  }

  private void accountForSpecialMoves(){
    if (isCurrentlySubmerging) continueSubmersion();
    if (performingBarrelRoll && !preparingForCorkscrewLaunch) underwaterMovementVectorInWorldSpace += rollPositionVector;
    if (preparingForCorkscrewLaunch || performingCorkscrewLaunch) performCorkscrewLaunch();
  }

  private void continueSubmersion(){
    if (submergeTimeLeft > 0f)
      submergeTimeLeft -= Time.deltaTime;
    else
      isCurrentlySubmerging = false;
  }

  private void performCorkscrewLaunch(){
    if (preparingForCorkscrewLaunch){
      stateController.EmitBubbleTrail();
      if (corkscrewPreparationTimeLeft <= 0f){
        performingCorkscrewLaunch = true;
        preparingForCorkscrewLaunch = false;
        maximumSwimSpeed = corkscrewLaunchSpeed;
        corkscrewPerformanceTimeLeft = corkscrewPerformanceDuration;
      } else {
        corkscrewPreparationTimeLeft -= Time.deltaTime;
      }
    }

    if (performingCorkscrewLaunch){
      if (corkscrewPerformanceTimeLeft <= 0f){
        performingCorkscrewLaunch = false;
        finishingACorkscrewLaunch = true;
        corkscrewCompletionRollSpeed = corkscrewPerformanceRollSpeed / 4f;
        StartCoroutine(returnMaximumSwimSpeedToNormal(corkscrewResidualSpeedDuration));
        StartCoroutine("decreaseCorkscrewCompletionRollSpeed");
      } else {
        Mathf.SmoothStep(maximumSwimSpeed, defaultMaximumSwimSpeed, Time.deltaTime * 5f);
        underwaterMovementVectorInWorldSpace += (transform.forward).normalized * 500f;
        corkscrewPerformanceTimeLeft -= Time.deltaTime;
      }
    }
  }

  IEnumerator returnMaximumSwimSpeedToNormal(float duration){
    float step = 0f;
    while (step <= 1f) {
      step += Time.deltaTime / duration;
      maximumSwimSpeed = Mathf.SmoothStep(maximumSwimSpeed, defaultMaximumSwimSpeed, Mathf.SmoothStep(0f, 1f, step));
      yield return true;
    }
  }

  IEnumerator decreaseCorkscrewCompletionRollSpeed(){
    float step = 0f;
    float duration = corkscrewResidualSpeedDuration * 3f;
    while (step <= 1f) {
      step += Time.deltaTime / duration;
      corkscrewCompletionRollSpeed = Mathf.SmoothStep(corkscrewCompletionRollSpeed, 3f, Mathf.SmoothStep(0f, 1f, step));
      yield return true;
    }
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
    if (didJustSplashIntoWater){
      forwardAccelerationUnderwater = 0f;
      stateController.EmitSplashTrail(positionVector * -1f);
      if (positionVector.magnitude < 5f){
        didJustSplashIntoWater = false;
        stateController.StopSplashTrailEmission();
      }
    } else {
      forwardAccelerationUnderwater = Mathf.Max(rawForwardValue, 0f) * 10f;
    }
  }

  private void emerge(){
    appliedRollValue = 0f;
    animator.SetBool("Underwater", false);
    characterController.slopeLimit = 120f;
    Vector3 terrainRay = transform.TransformDirection(Vector3.forward);
    alignPlayerWithTerrain(terrainRay: terrainRay);
    constrainPlayerMovementToEmergence();
  }

  private void constrainPlayerMovementToEmergence(){
    gravity = 80f;
    positionVector = new Vector3(0, 0, Input.GetAxis("Vertical"));
    positionVector = transform.TransformDirection(positionVector);
    positionVector *= speedOnLand;
    positionVector.y -= gravity * Time.deltaTime;
  }

  private void submerge(){
    animator.SetBool("Underwater", false);
    characterController.slopeLimit = 120f;
    alignPlayerWithTerrain(terrainRay: transform.TransformDirection(Vector3.down));
    constrainPlayerMovementToSubmergence();
  }

  private void constrainPlayerMovementToSubmergence(){
    positionVector = new Vector3(0, 0, Input.GetAxis("Vertical"));
    positionVector = transform.TransformDirection(positionVector);
    positionVector *= speedOnLand;
  }

  private void walk(float slope, Vector3 terrainRay){
    appliedRollValue = 0f;
    animator.SetBool("Underwater", false);
    slope = slope == null ? 90f : slope;
    terrainRay = terrainRay == null ? Vector3.down : terrainRay;
    terrainRay = transform.TransformDirection(terrainRay);
    characterController.slopeLimit = slope;
    alignPlayerWithTerrain(terrainRay: terrainRay);
    movePlayerOnLand();
  }

  private void alignPlayerWithTerrain(Vector3 terrainRay){
    targetRotation = rotationForAlignmentWithTerrain(terrainRay: terrainRay);
  }

  private void movePlayerOnLand(){
    gravity = 80f; // FIXME this is weird!
    positionVector = new Vector3(0, 0, Input.GetAxis("Vertical"));
    positionVector = transform.TransformDirection(positionVector);
    positionVector.y -= gravity * Time.deltaTime;
    positionVector *= speedOnLand;
  }

  private Quaternion rotationForAlignmentWithTerrain(Vector3 terrainRay){
    RaycastHit hit;
    float terrainCheckDistance = 8f;
    Vector3 normal = transform.TransformDirection(Vector3.up);
    Vector3 lookDirection = transform.forward + transform.TransformDirection(new Vector3(Input.GetAxis("Horizontal"), 0, 0));
    if (Physics.Raycast(transform.position, terrainRay, out hit, terrainCheckDistance))
      normal = hit.normal;
    Quaternion rotation = Quaternion.FromToRotation(transform.up, normal);
    return (rotation * Quaternion.LookRotation(lookDirection));
  }

  private void fall(){
    animator.SetBool("Underwater", false);
    gravity = 80f;
    positionVector.y -= gravity * Time.deltaTime;
    underwaterMovementVectorInWorldSpace = positionVector;
    targetRotation = determineAirborneRotation();
  }

  private float terrainAlignmentRotationSpeed(){
    return (20f * .05f * Time.deltaTime);
  }

  private float rotationSpeedInMedium(){
    if (isEmerging()) return rotationSpeedWhileEmerging();
    else if (isUnderwater()) return rotationSpeedInWater();
    else if (stateController.PlayerIsOnLand()) return rotationSpeedOnLand();
    else if (isFalling()) return rotationSpeedInAir();
    else return 1f;
  }

  // FIXME parameterize the following values
  private float rotationSpeedWhileEmerging(){
    return 2f * Time.deltaTime;
  }

  private float rotationSpeedInWater(){
    return 20f * Time.deltaTime;
  }

  private float rotationSpeedInAir(){
    return 20f * Time.deltaTime;
  }

  private float rotationSpeedOnLand(){
    return 4f * Time.deltaTime;
  }

  private float currentYAxisMultiplier(){
    return 1.2f;
  }

  public float Velocity(){
    return Vector3.Distance(transform.position, lastKnownPosition) / Time.deltaTime;
  }

  public bool isNearSurface(){
    return (stateController.IsPlayerNearSurface());
  }

  private void attemptCorkscrewLaunch(){
    if (stateController.PlayerHasFollowingFish() && stateController.FollowingFishAreNearby()){
      corkscrewDirection = barrelRollDirection;
      stateController.PerformCorkscrewLaunch(corkscrewDirection, corkscrewPreparationDuration);
      corkscrewPreparationTimeLeft = corkscrewPreparationDuration;
      StopCoroutine("decreaseCorkscrewCompletionRollSpeed");
      preparingForCorkscrewLaunch = true;
      performingCorkscrewLaunch = false;
      finishingACorkscrewLaunch = false;
    }
  }

  private bool isUnderwater(){
    return (stateController.PlayerIsUnderwater());
  }

  private bool isSwimming(){
    return (stateController.PlayerIsInWater());
  }

  private bool isEmerging(){
    return (stateController.PlayerIsEmergingFromWater());
  }

  private bool isSubmerging(){
    return false; // (stateController.PlayerIsSubmergingIntoWater());
  }

  private bool isFalling(){
    return (stateController.PlayerIsAirborne());
  }

  private void handleStateChange(){
    string previousState = lastRecordedState;
    lastRecordedState = stateController.LastRecordedState();
    if (lastRecordedState == "underwater" && previousState == "airborne")
      didJustSplashIntoWater = true;
    if (lastRecordedState == "underwater" && previousState == "grounded"){
      isCurrentlySubmerging = true;
      submergeTimeLeft = submergeDuration;
      float angle = Vector3.Angle(transform.forward, Vector3.up);
      submersionDirection = angle <= 90f ? -1f : 1f;
    }
  }
}
