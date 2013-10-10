using UnityEngine;
using System.Collections;

public class Lens : MonoBehaviour {

   public TurtleState turtleState;
   private Color underWaterColor = Color.blue;
   private Color aboveWaterColor = Color.blue;
   
   // Use this for initialization
   void Start () {
      underWaterColor.a = 0.4f;
      aboveWaterColor.a = 0f;
      renderer.material.color = aboveWaterColor;
   }
   
   // Update is called once per frame
   void Update () {
       if (turtleState.cameraIsUnderwater() && renderer.material.color == aboveWaterColor){
         renderer.material.color = underWaterColor;
      } else if (turtleState.cameraIsUnderwater() == false && renderer.material.color == underWaterColor) {
         renderer.material.color = aboveWaterColor;
      }
   }

   public void Flash(Color color){
   }
}
