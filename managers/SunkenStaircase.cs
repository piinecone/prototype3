using UnityEngine;
using System.Collections;

public class SunkenStaircase : MonoBehaviour {

  [SerializeField]
  private TurtleController turtleController;
  [SerializeField]
  private GameObject focalPoint;
  [SerializeField]
  private AudioSource music;
  [SerializeField]
  private AudioSource snap;

  private Vector3 finalPosition = new Vector3(438.2f, 206.8f, 651f);
  private Quaternion finalRotation = new Quaternion(0f, 0f, 0f, 1f);
  private bool readyToRaise = false;
  private float timeLeftUntilRaise = 5f;
  private bool toldDemFish = false;

  void LateUpdate () {
    if (closeToFinalPosition() && !toldDemFish){
      turtleController.tellFollowingFishToLeaveStaircase();
      toldDemFish = true;
      Invoke("PlaySnapSound", 5.5f);
    }
    // FIXME this requires that startCoroutine use a string method name, but limits the call to 
    // one argument and has a higher performance overhead
    //  StopCoroutine("SmoothlyMoveStaircase");
    if (readyToRaise) {
      timeLeftUntilRaise -= Time.deltaTime;
      if (timeLeftUntilRaise <= 0f){
        raiseStaircase();
        readyToRaise = false;
      }
    }
  }

  private bool closeToFinalPosition(){
    float distance = Vector3.Distance(transform.position, finalPosition);
    float angle = Quaternion.Angle(transform.rotation, finalRotation);

    return (distance < 10f && angle < 5f);
  }

  private void PlaySnapSound(){
    snap.Play();
  }

  IEnumerator SmoothlyMoveStaircase(float duration){
    float step = 0f;
    while (step <= 1f) {
      step += Time.deltaTime / duration;
      transform.position = Vector3.Lerp(transform.position, finalPosition, Mathf.SmoothStep(0f, 1f, step));
      transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, Mathf.SmoothStep(0f, 1f, step));
      yield return true;
    }
  }

  private void raiseStaircase(){
    StartCoroutine(SmoothlyMoveStaircase(200f));
  }

  public void scheduleRaise(){
    music.Play();
    readyToRaise = true;
    timeLeftUntilRaise = 15f;
  }

  public GameObject getFocalPoint(){
    return focalPoint;
  }

  public bool isReadyToRaise(){
    return readyToRaise;
  }
}
