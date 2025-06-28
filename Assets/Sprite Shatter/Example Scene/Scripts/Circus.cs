namespace SpriteShatterExampleScene {
    using SpriteShatter;
    using UnityEngine;

    public class Circus : MonoBehaviour {

        //Variables.
        bool shattered = false;

        //Update.
        void Update() {

            //If the user clicks the left mouse button, explode the circus!
            if (!shattered && Input.GetMouseButtonDown(0)) {
                GetComponent<Shatter>().shatter();
                shattered = true;
            }

            //If the user clicks the right mouse button, reset the circus!
            else if (shattered && Input.GetMouseButtonDown(1)) {
                GetComponent<Shatter>().reset();
                shattered = false;
            }
        }
    }
}