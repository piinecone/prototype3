using UnityEngine;
using System.Collections;

public class TurtleState : MonoBehaviour {

   public Camera camera;
   private float waterLevel = 162f;

   // Use this for initialization
   void Start () {
   
   }
   
   // Update is called once per frame
   void Update () {
   
   }
   
   public bool isUnderwater(){
      if (transform.position.y < waterLevel){
        return true;
      } else {
        return false;
      }
   }

   public bool cameraIsUnderwater(){
     if (camera.transform.position.y < waterLevel){
       return true;
     } else {
       return false;
     }
   }
}
