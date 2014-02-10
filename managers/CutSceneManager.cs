using UnityEngine;
using System.Collections;

public class CutSceneManager : MonoBehaviour {

  [SerializeField]
  private TurtleController turtleController;
  [SerializeField]
  private ThirdPersonCamera camera;
  [SerializeField]
  private GameObject bigTree;
  [SerializeField]
  private GameObject underwaterForest;
  [SerializeField]
  private float underwaterForestReminderInterval;
  [SerializeField]
  private GameObject sunkenStaircase;
  [SerializeField]
  private Barrier initialBarrier;
  [SerializeField]
  private bool disabled = false;
  [SerializeField]
  private AudioSource openingLoop;
  [SerializeField]
  private AudioSource aboveWaterMusic;
  [SerializeField]
  private AudioSource underWaterMusic;

  private bool musicCanPlay = true;
  private bool hasBeenUnderwaterAlready = false;
  private bool playerIsUnderwater = false;

  private bool musicPaused = false;
  private bool musicFading = false;
  private float volumeOn = .35f;
  private float volumeOff= 0f;
  private float targetVolume = 1f;
  private float errorMargin = .005f;

  void Start(){
    // FIXME re-enable
    openingLoop.volume = volumeOn;
    openingLoop.Play();
    underWaterMusic.Play();
  }

  void LateUpdate(){
    if (!musicCanPlay){
      openingLoop.Stop();
      underWaterMusic.Stop();
      aboveWaterMusic.Stop();
    }

    if (musicFading && musicCanPlay){
      if (underWaterMusic.volume <= (targetVolume - errorMargin) || underWaterMusic.volume >= (targetVolume + errorMargin)){
        underWaterMusic.volume = Mathf.SmoothStep(underWaterMusic.volume, targetVolume, 2.2f * Time.deltaTime);
      } else {
        musicFading = false;
        underWaterMusic.volume = targetVolume;
      }
    }
  }

  public Barrier InitialBarrier(){
    return initialBarrier;
  }

  public void startReminders(){
    InvokeRepeating("RemindPlayerToVisitForest", underwaterForestReminderInterval, underwaterForestReminderInterval);
    InvokeRepeating("ShowPlayerWhichBarrierToVisit", 5, 40);
  }

  public void playCutSceneFor(string sceneName){
    if (!disabled){
      switch(sceneName){
        case "Lake Entry":
          float duration = 8f;
          camera.cutTo(bigTree, duration, new Vector3(-10f, 4f, -50f));
          Invoke("playCutSceneForSunkenStaircase", duration + .2f);
          managePlayer(duration);
          break;
        case "Sunken Staircase":
          camera.cutTo(sunkenStaircase, 8f, new Vector3(0f, 30f, 80f));
          managePlayer(8f);
          break;
        case "Underwater Forest":
          camera.cutTo(underwaterForest, 6f, new Vector3(0f, -10f, -140f));
          managePlayer(6f);
          break;
        case "Initial Barrier":
          camera.cutTo(initialBarrier.gameObject, 8f, new Vector3(0f, 0f, 10f));
          managePlayer(8f);
          break;
        case "Abort Barrier":
          camera.cutTo(initialBarrier.gameObject, 12f, new Vector3(0f, 0f, 10f));
          managePlayer(12f);
          break;
      }
    }
  }

  private void playCutSceneForSunkenStaircase(){
    playCutSceneFor("Sunken Staircase");
  }

  public void cutTo(GameObject aGameObject, float duration, Vector3 offsetVector){
    if (!disabled){
      camera.cutTo(aGameObject, duration, offsetVector);
      managePlayer(duration);
    }
  }

  private void managePlayer(float duration){
    turtleController.FreezePlayer(duration);
  }

  private void RemindPlayerToVisitForest(){
    //if (shouldRemindPlayerAboutForest()) playCutSceneFor("Underwater Forest");
  }

  private void ShowPlayerWhichBarrierToVisit(){
    //if (shouldRemindPlayerAboutInitialBarrier()) playCutSceneFor("Initial Barrier");
  }

  //private bool shouldRemindPlayerAboutForest(){
  //  return !initialBarrier.isDestroyed() &&
  //    turtleController.numberOfFollowingFish() < initialBarrier.Strength() &&
  //    Vector3.Distance(turtleController.transform.position, underwaterForest.transform.position) > 120f;
  //}

  //private bool shouldRemindPlayerAboutInitialBarrier(){
  //  return !initialBarrier.isDestroyed() &&
  //    turtleController.numberOfFollowingFish() >= initialBarrier.Strength() &&
  //    Vector3.Distance(turtleController.transform.position, initialBarrier.transform.position) > 100f;
  //}

  public void PlayerIsUnderwater(bool value=true){
    playerIsUnderwater = value;
    if (!musicCanPlay) return;

    if (playerIsUnderwater){
      adjustMusicVolumeForUnderWater();
      if (!hasBeenUnderwaterAlready){
        hasBeenUnderwaterAlready = true;
        underWaterMusic.Play();
        aboveWaterMusic.Play();
        openingLoop.Stop();
      }
    } else {
      adjustMusicVolumeForAboveWater();
    }
  }

  private void adjustMusicVolumeForAboveWater(){
    if (!musicCanPlay) return;

    if (!musicPaused && !musicFading){
      if (underWaterMusic.volume != volumeOff) underWaterMusic.volume = volumeOff;
      if (aboveWaterMusic.volume != volumeOn) aboveWaterMusic.volume = volumeOn;
    }
  }

  private void adjustMusicVolumeForUnderWater(){
    if (!musicCanPlay) return;

    if (!musicPaused && !musicFading){
      if (underWaterMusic.volume != volumeOn) underWaterMusic.volume = volumeOn;
      if (aboveWaterMusic.volume != volumeOff) aboveWaterMusic.volume = volumeOff;
    }
  }

  private void turnOffMusic(){
    if (underWaterMusic.volume != volumeOff || aboveWaterMusic.volume != volumeOff){
      underWaterMusic.volume = volumeOff;
      aboveWaterMusic.volume = volumeOff;
    }
  }

  public void ResumeLevelMusic(){
    if (!musicCanPlay) return;

    musicPaused = false;
    musicFading = true;
    FadeInMusic();
  }

  public void PauseLevelMusic(){
    if (!musicCanPlay) return;

    musicPaused = true;
    musicFading = true;
    //if (underWaterMusic.volume == 1f)
    FadeOutMusic();
  }

  IEnumerator fadeInMusic(){
    float step = 0f;
    while (step < 1f) {
      step += Time.deltaTime / 15f;
      underWaterMusic.volume = Mathf.SmoothStep(0f, 1f, step);
      yield return true;
    }
  }

  IEnumerator fadeOutMusic(){
    float step = 0f;
    while (musicPaused = true && step > 0f) {
      step += Time.deltaTime / 15f;
      underWaterMusic.volume = Mathf.SmoothStep(1f, 0f, step);
      yield return true;
    }
  }

  public void FadeInMusic(){
    targetVolume = volumeOn;
    //StartCoroutine(fadeInMusic());
  }

  public void FadeOutMusic(){
    targetVolume = volumeOff;
    //StartCoroutine(fadeOutMusic());
  }

  public void StopLevelMusic(){
    FadeOutMusic();
    //Invoke("disableMusic", 7f);
    Invoke("disableMusic", 0f);
  }

  private void disableMusic(){
    musicCanPlay = false;
    underWaterMusic.Stop();
    aboveWaterMusic.Stop();
  }
}
