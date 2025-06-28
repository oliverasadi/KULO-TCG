namespace SpriteShatterEditor {
    using UnityEngine;
    using UnityEditor;

    public class SpriteShatterVersionChanges : EditorWindow {

        //Variables.
        Vector2 scrollPosition = Vector2.zero;
        GUIStyle _headerLabel = null;
        GUIStyle headerLabel {
            get {
                if (_headerLabel == null) {
                    _headerLabel = new GUIStyle(EditorStyles.boldLabel);
                    _headerLabel.alignment = TextAnchor.MiddleCenter;
                    _headerLabel.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
                }
                return _headerLabel;
            }
        }
        GUIStyle _subHeaderLabel = null;
        GUIStyle subHeaderLabel {
            get {
                if (_subHeaderLabel == null) {
                    _subHeaderLabel = new GUIStyle(EditorStyles.boldLabel);
                    _subHeaderLabel.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
                }
                return _subHeaderLabel;
            }
        }
        GUIStyle _boldWrappedLabel = null;
        GUIStyle boldWrappedLabel {
            get {
                if (_boldWrappedLabel == null) {
                    _boldWrappedLabel = new GUIStyle(EditorStyles.boldLabel);
                    _boldWrappedLabel.wordWrap = true;
                }
                return _boldWrappedLabel;
            }
        }
        GUIStyle _wrappedLabel = null;
        GUIStyle wrappedLabel {
            get {
                if (_wrappedLabel == null) {
                    _wrappedLabel = new GUIStyle(EditorStyles.label);
                    _wrappedLabel.padding = new RectOffset(25, 0, 0, 0);
                    _wrappedLabel.wordWrap = true;
                }
                return _wrappedLabel;
            }
        }
        Texture2D _bulletPointTexture = null;
        Texture2D bulletPointTexture {
            get {
                if (_bulletPointTexture == null)
                    _bulletPointTexture = getCircularTexture(EditorStyles.label.normal.textColor, 16);
                return _bulletPointTexture;
            }
        }
        GUIStyle _bulletPoint = null;
        GUIStyle bulletPoint {
            get {
                if (_bulletPoint == null) {
                    _bulletPoint = new GUIStyle(EditorStyles.label);
                    _bulletPoint.normal.background = bulletPointTexture;
                }
                return _bulletPoint;
            }
        }

        //Draw the GUI.
        void OnGUI() {

            //Display the version change text.
            EditorGUILayout.LabelField("Sprite Shatter Version Changes", headerLabel);
            EditorGUILayout.GetControlRect();
            EditorGUILayout.LabelField("If you have any comments or suggestions as to how we could improve Sprite Shatter, or if you want to report a bug in " +
                    "the software, feel free to e-mail us on info@battenbergsoftware.com and we'll get back to you. Thanks!", boldWrappedLabel);
            EditorGUILayout.GetControlRect();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.LabelField("Version 2.0.5", subHeaderLabel);
            EditorGUILayout.LabelField(
                    "Added a new triangles shatter type for shattering the sprite into a configurable number of tessellating triangle shapes.",
                    wrappedLabel);
            addBulletPoint();
            EditorGUILayout.GetControlRect();
            EditorGUILayout.LabelField("Version 2.0.4", subHeaderLabel);
            EditorGUILayout.LabelField("Added the ability to scale colliders.", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.LabelField("Added assembly definition files for build and editor scripts.", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.GetControlRect();
            EditorGUILayout.LabelField("Version 2.0.3", subHeaderLabel);
            EditorGUILayout.LabelField(
                    "Added a new hexagons shatter type for shattering the sprite into a configurable number of tessellating hexagonal shapes.", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.GetControlRect();
            EditorGUILayout.LabelField("Version 2.0.2", subHeaderLabel);
            EditorGUILayout.LabelField("Radial sprite shatters can now be shattered from any point on the sprite instead of just the centre.", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.LabelField(
                    "Improved the efficiency of the Sprite Shatter editor script, making the preview on the component in the inspector run much quicker.",
                    wrappedLabel);
            addBulletPoint();
            EditorGUILayout.GetControlRect();
            EditorGUILayout.LabelField("Version 2.0.1", subHeaderLabel);
            EditorGUILayout.LabelField("Minor fix to add some bounds checking to prevent Sprite Shatter from throwing an out of bounds error for some " +
                    "textures that have transparent borders.", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.GetControlRect();
            EditorGUILayout.LabelField("Version 2.0.0", subHeaderLabel);
            EditorGUILayout.LabelField("Improved the way Sprite Shatter works - shattered pieces now use the original sprite's texture and are shaped using " +
                    "sprite vertices instead of by copying the texture pixel by pixel. This reduces both the time taken to generate the shattered pieces " +
                    "and the memory overhead, and means sprites no longer need their read/write flags set.", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.LabelField("Added a radial shatter type which generates shattered pieces going outwards from the centre of the sprite, a bit " +
                    "like smashed glass.", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.LabelField("The zigzag cuts on grid shatter types can now be configured independently in the horizontal and vertical directions.",
                    wrappedLabel);
            addBulletPoint();
            EditorGUILayout.LabelField("Circle, box and polygon colliders can now be low, medium or high quality. Lower quality colliders are less accurate " +
                    "but are generated faster at run-time.", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.LabelField("Added this version changes window.", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.GetControlRect();
            EditorGUILayout.LabelField("Version 1.3.1", subHeaderLabel);
            EditorGUILayout.LabelField("Added support for flipped sprite renderers (Unity 5.3 onwards).", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.LabelField("Fixed an editor issue where sprites generated at runtime were considered invalid.", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.GetControlRect();
            EditorGUILayout.LabelField("Version 1.3.0", subHeaderLabel);
            EditorGUILayout.LabelField("Now allows zero cuts, horizontally or vertically, on a sprite to allow the sprite to be cut in only one direction. " +
                    "Displays a warning, however, if zero cuts are selected for both the horizontal and vertical direction, which means the sprite won't " +
                    "actually be shattered.", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.LabelField("Implemented pixel perfect shattering where applicable. Very small sprites can now be shattered into individual " +
                    "pixels if required, with colliders included as long as the colliders aren't too small for the physics engine.", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.LabelField("Improved the look of the cuts on the preview texture within the inspector for more pixelated sprites.", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.LabelField("Fixed slight issue with explosion parameter labels overlapping their respective fields in the Shatter component " +
                    "editor.", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.LabelField("The dynamic game objects created for the shattered pieces of a sprite are now placed on the same layer as the " +
                    "original game object instead of on the default layer.", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.LabelField("Shows an error message if an invalid sprite is selected for the sprite renderer, rather than an editor script crash.",
                    wrappedLabel);
            addBulletPoint();
            EditorGUILayout.GetControlRect();
            EditorGUILayout.LabelField("Version 1.2.0", subHeaderLabel);
            EditorGUILayout.LabelField("Sprite Shatter now has full prefab support! To achieve this, the shatter properties are now stored against the game " +
                    "object and are set up in the inspector window. The algorithm to create the shattered pieces then occurs at run-time, rather than at " +
                    "design-time. Because this is such a major update, you will have to perform minor updates to sprites that were created in a previous " +
                    "version of Sprite Shatter, namely: 1) Remove the auto-generated \"Sprite Shatter\" game object underneath any sprites that were " +
                    "previously automatically created at design-time. 2) Set the \"Shatter\" parameters again on sprites in the inspector window.",
                    wrappedLabel);
            addBulletPoint();
            EditorGUILayout.GetControlRect();
            EditorGUILayout.LabelField("Version 1.1.0", subHeaderLabel);
            EditorGUILayout.LabelField("The Sprite Shatter process can now be run on multiple game objects at once.", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.LabelField("Shows a helpful message if the sprite texture has an invalid format. Only formats that work with \"GetPixels()\" are " +
                    "supported - namely ARGB32, RGBA32, BGRA32, RGB24, Alpha8 or DXT. Thanks to Michael Span for spotting this issue!", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.GetControlRect();
            EditorGUILayout.LabelField("Version 1.0.0", subHeaderLabel);
            EditorGUILayout.LabelField("Initial release.", wrappedLabel);
            EditorGUILayout.EndScrollView();
        }

        //Adds a bullet point before the label that has just been added.
        void addBulletPoint() {
            Rect rect = GUILayoutUtility.GetLastRect();
            EditorGUI.LabelField(new Rect(17, rect.yMin + 4, 5, 5), "", bulletPoint);
        }

        //Get a circular texture for the bullet point.
        Texture2D getCircularTexture(Color colour, int size) {
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            Vector2 centre = new Vector2((size / 2f) - 0.5f, (size / 2f) - 0.5f);
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++) {
                    Vector2 point = new Vector2(i, j);
                    float alpha = Mathf.Clamp01((size / 2f) - Vector2.Distance(point, centre) - 1f);
                    texture.SetPixel(i, j, new Color(colour.r, colour.g, colour.b, alpha));
                }
            texture.Apply();
            return texture;
        }

        //Called when the version changes window is closed - destroy the bullet point texture if it has been generated.
        void OnDestroy() {
            if (_bulletPointTexture != null)
                DestroyImmediate(_bulletPointTexture);
        }
    }
}
