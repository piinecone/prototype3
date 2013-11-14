using UnityEngine;
using System.Collections;

public class ChaseBoundary : MonoBehaviour {

  [SerializeField]
  private TurtleController turtleController;

  private bool chaseInProgress = false;
  private float allowableTimeOutsideBoundary = 7f;
  private float elapsedTimeOutsideBoundary = 0f;
  private AudioSource chaseMusic;
  private float targetVolume = 0f;
  private float volumeOff = 0f;
  private float volumeOn = .7f;
  private float errorMargin = .005f;

  void Start () {
    chaseMusic = GetComponent<AudioSource>();
    chaseMusic.volume = volumeOff;
  }
  
  void Update () {
  }

  void LateUpdate(){
    if (chaseInProgress){
      elapsedTimeOutsideBoundary += Time.deltaTime;
      if (elapsedTimeOutsideBoundary >= allowableTimeOutsideBoundary){
        Debug.Log("outside bounds too long, ending chase");
        //turtleController.AbortChase();
        EndChase();
      }
    }

    if (chaseMusic.volume <= (targetVolume - errorMargin) ||
      chaseMusic.volume >= (targetVolume + errorMargin)){
        chaseMusic.volume = Mathf.SmoothStep(chaseMusic.volume, targetVolume, 2.2f * Time.deltaTime);
    }

    if (targetVolume == 0f && chaseMusic.volume <= (0f + errorMargin)) chaseMusic.Stop();
  }

  public void StartChase(){
    elapsedTimeOutsideBoundary = 0f;
    chaseInProgress = true;
    targetVolume = volumeOn;
    chaseMusic.Play();
  }

  public void EndChase(){
    chaseInProgress = false;
    elapsedTimeOutsideBoundary = 0f; // maybe don't reset this value
    targetVolume = volumeOff;
  }

  void OnTriggerStay(Collider collider){
    if (collider.gameObject.tag == "Player"){
      elapsedTimeOutsideBoundary = 0f;
    }
  }

  public void FadeInMusic(){
    // start coroutine
  }
}
