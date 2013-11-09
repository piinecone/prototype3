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
  private Barrier initialBarrier;
  [SerializeField]
  private bool disabled = false;

  public Barrier InitialBarrier(){
    return initialBarrier;
  }

  public void startReminders(){
    InvokeRepeating("RemindPlayerToVisitForest", underwaterForestReminderInterval, underwaterForestReminderInterval);
    InvokeRepeating("ShowPlayerWhichBarrierToVisit", 5, 45);
  }

  public void playCutSceneFor(string sceneName){
    if (!disabled){
      switch(sceneName){
        case "Lake Entry":
          camera.cutTo(bigTree, 8f, new Vector3(-10f, 4f, -50f));
          break;
        case "Underwater Forest":
          camera.cutTo(underwaterForest, 6f, new Vector3(0f, -10f, -140f));
          break;
        case "Initial Barrier":
          camera.cutTo(initialBarrier.gameObject, 7f, new Vector3(0f, 0f, 10f));
          break;
        case "Abort Barrier":
          camera.cutTo(initialBarrier.gameObject, 11f, new Vector3(0f, 0f, 10f));
          break;
      }
    }
  }

  public void cutTo(GameObject aGameObject, float duration, Vector3 offsetVector){
    if (!disabled) camera.cutTo(aGameObject, duration, offsetVector);
  }

  private void RemindPlayerToVisitForest(){
    if (shouldRemindPlayerAboutForest()) playCutSceneFor("Underwater Forest");
  }

  private void ShowPlayerWhichBarrierToVisit(){
    if (shouldRemindPlayerAboutInitialBarrier()) playCutSceneFor("Initial Barrier");
  }

  private bool shouldRemindPlayerAboutForest(){
    return !initialBarrier.isDestroyed() &&
      turtleController.numberOfFollowingFish() < initialBarrier.Strength() &&
      Vector3.Distance(turtleController.transform.position, underwaterForest.transform.position) > 120f;
  }

  private bool shouldRemindPlayerAboutInitialBarrier(){
    return !initialBarrier.isDestroyed() &&
      turtleController.numberOfFollowingFish() >= initialBarrier.Strength() &&
      Vector3.Distance(turtleController.transform.position, initialBarrier.transform.position) > 100f;
  }
}
