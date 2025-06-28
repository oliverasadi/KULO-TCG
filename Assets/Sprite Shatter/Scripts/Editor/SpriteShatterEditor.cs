namespace SpriteShatterEditor {
    using SpriteShatter;
    using System;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(Shatter))]
    public class SpriteShatterEditor : Editor {

        //Variables.
        static string lastUndoEvent = "";
        static int undoGroup = -1;
        static UnityEngine.Object previewOverlayTarget = null;
        static Texture2D previewOverlayTexture = null;
        static GUIStyle _versionStyle = null;
        static GUIStyle versionStyle {
            get {
                if (_versionStyle == null) {
                    _versionStyle = new GUIStyle(GUI.skin.label);
                    _versionStyle.fontStyle = FontStyle.Bold;
                    _versionStyle.normal.textColor = new Color(0, 0.4f, 0.8f);
                    _versionStyle.hover.textColor = new Color(0, 0.4f, 0.8f);
                    _versionStyle.active.textColor = new Color(0, 0.4f, 0.8f);
                    _versionStyle.focused.textColor = new Color(0, 0.4f, 0.8f);
                    _versionStyle.alignment = TextAnchor.MiddleRight;
                }
                return _versionStyle;
            }
        } 

        //On inspector GUI.
        public override void OnInspectorGUI() {
            Shatter shatter = (Shatter) target;

            //If sprite shatter needs upgrading, flag for the editor to call "upgrade()" on the next update (the undo functionality doesn't work correctly if it
            //is called from "OnInspectorGUI()").
            if (shatter.needsUpgrading()) {
                EditorApplication.update -= upgrade;
                EditorApplication.update += upgrade;
            }

            //Show an error message if the sprite renderer doesn't exist or doesn't have a valid sprite.
            SpriteRenderer spriteRenderer = shatter.gameObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null || spriteRenderer.sprite == null) {
                EditorGUILayout.HelpBox("Sprite Renderer does not have a valid sprite!", MessageType.Error);
                return;
            }

            //Keep track of whether the preview texture is dirty so it can be redrawn.
            bool previewOverlayDirty = previewOverlayTexture == null || previewOverlayTarget != target;

            //Shatter settings header.
            EditorGUILayout.LabelField("Shatter Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            //Shatter type header.
            EditorGUI.BeginChangeCheck();
            Shatter.ShatterType shatterType = (Shatter.ShatterType) EditorGUILayout.EnumPopup(new GUIContent("Type",
                    "The pattern in which to create the shattered pieces."), shatter.shatterDetails.shatterType);
            if (EditorGUI.EndChangeCheck()) {
                beginUndo("Change Sprite Shatter Type");
                shatter.shatterDetails.shatterType = shatterType;
                endUndo(true);
                previewOverlayDirty = true;
            }

            //Grid shatter type properties.
            if (shatter.shatterDetails.shatterType == Shatter.ShatterType.Grid) {

                //Horizontal cuts.
                EditorGUI.BeginChangeCheck();
                int horizontalCuts = EditorGUILayout.IntSlider(new GUIContent("Horizontal Cuts", "The number of cuts to make going horizontally across the " +
                        "sprite. More cuts will shatter the sprite into smaller pieces but will require more game objects."),
                        shatter.shatterDetails.horizontalCuts, 0, 32);
                if (EditorGUI.EndChangeCheck()) {
                    beginUndo("Change Number of Sprite Shatter Horizontal Cuts");
                    shatter.shatterDetails.horizontalCuts = horizontalCuts;
                    endUndo(false);
                    previewOverlayDirty = true;
                }

                //Vertical cuts.
                EditorGUI.BeginChangeCheck();
                int verticalCuts = EditorGUILayout.IntSlider(new GUIContent("Vertical Cuts", "The number of cuts to make going vertically down the sprite. " +
                        "More cuts will shatter the sprite into smaller pieces but will require more game objects."),
                        shatter.shatterDetails.verticalCuts, 0, 32);
                if (EditorGUI.EndChangeCheck()) {
                    beginUndo("Change Number of Sprite Shatter Vertical Cuts");
                    shatter.shatterDetails.verticalCuts = verticalCuts;
                    endUndo(false);
                    previewOverlayDirty = true;
                }

                //Show an error message if no cuts are selected.
                if (horizontalCuts == 0 && verticalCuts == 0)
                    EditorGUILayout.HelpBox("The sprite will not shatter without any cuts!", MessageType.Warning);

                //Horizontal Zigzags.
                EditorGUILayout.LabelField("Horizontal Zigzags", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                int horizontalZigzagPoints = EditorGUILayout.IntField(new GUIContent("Points",
                        "This number of points to create on the zigzags for each shattered piece."), shatter.shatterDetails.horizontalZigzagPoints);
                if (EditorGUI.EndChangeCheck()) {
                    beginUndo("Change Number of Sprite Shatter Horizontal Zigzag Points");
                    shatter.shatterDetails.horizontalZigzagPoints = horizontalZigzagPoints;
                    endUndo(false);
                    previewOverlayDirty = true;
                }
                EditorGUI.BeginChangeCheck();
                float horizontalZigzagSize = EditorGUILayout.Slider(new GUIContent("Size", "The size of the zigzags."),
                        shatter.shatterDetails.horizontalZigzagSize, 0, 1);
                if (EditorGUI.EndChangeCheck()) {
                    beginUndo("Change Size of Sprite Shatter Horizontal Zigzag Points");
                    shatter.shatterDetails.horizontalZigzagSize = horizontalZigzagSize;
                    endUndo(false);
                    previewOverlayDirty = true;
                }
                EditorGUI.indentLevel--;

                //Vertical Zigzags.
                EditorGUILayout.LabelField("Vertical Zigzags", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                int verticalZigzagPoints = EditorGUILayout.IntField(new GUIContent("Points",
                        "This number of points to create on the zigzags for each shattered piece."), shatter.shatterDetails.verticalZigzagPoints);
                if (EditorGUI.EndChangeCheck()) {
                    beginUndo("Change Number of Sprite Shatter Vertical Zigzag Points");
                    shatter.shatterDetails.verticalZigzagPoints = verticalZigzagPoints;
                    endUndo(false);
                    previewOverlayDirty = true;
                }
                EditorGUI.BeginChangeCheck();
                float verticalZigzagSize = EditorGUILayout.Slider(new GUIContent("Size", "The size of the zigzags."),
                        shatter.shatterDetails.verticalZigzagSize, 0, 1);
                if (EditorGUI.EndChangeCheck()) {
                    beginUndo("Change Size of Sprite Shatter Vertical Zigzag Points");
                    shatter.shatterDetails.verticalZigzagSize = verticalZigzagSize;
                    endUndo(false);
                    previewOverlayDirty = true;
                }
                EditorGUI.indentLevel--;
            }

            //Radial shatter type properties.
            else if (shatter.shatterDetails.shatterType == Shatter.ShatterType.Radial) {

                //Radial sectors.
                EditorGUI.BeginChangeCheck();
                int radialSectors = EditorGUILayout.IntSlider(new GUIContent("Sectors", "The number of sectors to split the sprite into when creating " +
                        "shattered pieces. Sectors go from the centre of the sprite to its edges."), shatter.shatterDetails.radialSectors, 1, 64);
                if (EditorGUI.EndChangeCheck()) {
                    beginUndo("Change Number of Sprite Shatter Radial Sectors");
                    shatter.shatterDetails.radialSectors = radialSectors;
                    endUndo(false);
                    previewOverlayDirty = true;
                }

                //Radials.
                EditorGUI.BeginChangeCheck();
                int radials = EditorGUILayout.IntSlider(new GUIContent("Radials", "The number of pieces to split each sector into."),
                        shatter.shatterDetails.radials, 1, 16);
                if (EditorGUI.EndChangeCheck()) {
                    beginUndo("Change Number of Sprite Shatter Radials");
                    shatter.shatterDetails.radials = radials;
                    endUndo(false);
                    previewOverlayDirty = true;
                }

                //Radial centre.
                EditorGUI.BeginChangeCheck();
                Vector2 radialCentre = EditorGUILayout.Vector2Field(new GUIContent("Centre",
                        "The centre point of the radial. X and Y co-ordinates can be between 0 and 1 with 0 being the left/top of the sprite and 1 being the " +
                        "right/bottom. The default is (0.5, 0.5) which is the centre."), shatter.shatterDetails.radialCentre);
                radialCentre.x = Mathf.Clamp01(radialCentre.x);
                radialCentre.y = Mathf.Clamp01(radialCentre.y);
                if (EditorGUI.EndChangeCheck()) {
                    beginUndo("Change Number of Sprite Shatter Radial Centre");
                    shatter.shatterDetails.radialCentre = radialCentre;
                    endUndo(false);
                    previewOverlayDirty = true;
                }
            }

            //Hexagons shatter type properties.
            else if (shatter.shatterDetails.shatterType == Shatter.ShatterType.Hexagons) {

                //Horizontal hexagons.
                EditorGUI.BeginChangeCheck();
                int horizontalHexagons = EditorGUILayout.IntSlider(new GUIContent("Horizontal",
                        "The number of hexagons to shatter the sprite into in the horizontal direction."), shatter.shatterDetails.horizontalHexagons, 1, 32);
                if (EditorGUI.EndChangeCheck()) {
                    beginUndo("Change Number of Sprite Shatter Horizontal Hexagons");
                    shatter.shatterDetails.horizontalHexagons = horizontalHexagons;
                    endUndo(false);
                    previewOverlayDirty = true;
                }

                //Vertical hexagons.
                EditorGUI.BeginChangeCheck();
                int verticalHexagons = EditorGUILayout.IntSlider(new GUIContent("Vertical",
                        "The number of hexagons to shatter the sprite into in the vertical direction."), shatter.shatterDetails.verticalHexagons, 1, 32);
                if (EditorGUI.EndChangeCheck()) {
                    beginUndo("Change Number of Sprite Shatter Vertical Hexagons");
                    shatter.shatterDetails.verticalHexagons = verticalHexagons;
                    endUndo(false);
                    previewOverlayDirty = true;
                }
            }

            //Triangles shatter type properties.
            else if (shatter.shatterDetails.shatterType == Shatter.ShatterType.Triangles) {

                //Triangle orientation.
                EditorGUI.BeginChangeCheck();
                Shatter.TriangleOrientation triangleOrientation = (Shatter.TriangleOrientation) EditorGUILayout.EnumPopup(new GUIContent("Orientation",
                        "Whether to arrange the triangular shatter pieces in a horizontal or vertical layout."), shatter.shatterDetails.triangleOrientation);
                if (EditorGUI.EndChangeCheck()) {
                    beginUndo("Change Sprite Shatter Triangle Orientation");
                    shatter.shatterDetails.triangleOrientation = triangleOrientation;
                    endUndo(true);
                    previewOverlayDirty = true;
                }

                //Horizontal triangles.
                EditorGUI.BeginChangeCheck();
                int horizontalTriangles = EditorGUILayout.IntSlider(new GUIContent("Horizontal",
                        "The number of triangles to shatter the sprite into in the horizontal direction."), shatter.shatterDetails.horizontalTriangles, 1, 32);
                if (EditorGUI.EndChangeCheck()) {
                    beginUndo("Change Number of Sprite Shatter Horizontal Triangles");
                    shatter.shatterDetails.horizontalTriangles = horizontalTriangles;
                    endUndo(false);
                    previewOverlayDirty = true;
                }

                //Vertical triangles.
                EditorGUI.BeginChangeCheck();
                int verticalTriangles = EditorGUILayout.IntSlider(new GUIContent("Vertical",
                        "The number of triangles to shatter the sprite into in the vertical direction."), shatter.shatterDetails.verticalTriangles, 1, 32);
                if (EditorGUI.EndChangeCheck()) {
                    beginUndo("Change Number of Sprite Shatter Vertical Triangles");
                    shatter.shatterDetails.verticalTriangles = verticalTriangles;
                    endUndo(false);
                    previewOverlayDirty = true;
                }
            }

            //Decrement the indent level after the shatter properties.
            EditorGUI.indentLevel--;

            //Randomness header.
            EditorGUILayout.GetControlRect();
            EditorGUILayout.LabelField("Randomness", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            //Randomize at run time.
            EditorGUI.BeginChangeCheck();
            bool randomizeAtRuntime = EditorGUILayout.Toggle(new GUIContent("Randomize at Run-Time", "Generate a completely random seed at run-time, meaning " +
                    "the layout of the shattered sprite pieces will be different every time the scene is run. This only has any effect if \"Randomness\", " +
                    "below, is set to greater than zero."), shatter.shatterDetails.randomizeAtRunTime);
            if (EditorGUI.EndChangeCheck()) {
                beginUndo("Change Sprite Shatter Randomize at Run-Time");
                shatter.shatterDetails.randomizeAtRunTime = randomizeAtRuntime;
                endUndo(true);
                previewOverlayDirty = true;
            }

            //Random seed.
            int randomSeed = shatter.shatterDetails.randomSeed;
            if (!randomizeAtRuntime) {
                EditorGUI.BeginChangeCheck();
                randomSeed = EditorGUILayout.IntField(new GUIContent("Random Seed", "The seed value to use when generating random numbers. The same seed " +
                        "will always generate shattered pieces in the same way, which is useful if you want multiple objects to shatter in an identical way. " +
                        "Otherwise, you can select \"Randomize at Runtime\" above to generate a different random set of cuts each time the game runs. The " +
                        "random seed only has any effect if \"Randomness\", below, is set to greater than zero."), shatter.shatterDetails.randomSeed);
                if (EditorGUI.EndChangeCheck()) {
                    beginUndo("Change Sprite Shatter Random Seed");
                    shatter.shatterDetails.randomSeed = randomSeed;
                    endUndo(true);
                    previewOverlayDirty = true;
                }
            }

            //Randomness.
            EditorGUI.BeginChangeCheck();
            float randomness = EditorGUILayout.Slider(new GUIContent("Randomness", "The amount of randomness to apply to the size and shape of the shattered " +
                    "pieces. A value of zero will result in uniform pieces, whereas a value of one will apply the maximum amount of randomness."),
                    shatter.shatterDetails.randomness, 0, 1);
            if (EditorGUI.EndChangeCheck()) {
                beginUndo("Change Sprite Shatter Randomness");
                shatter.shatterDetails.randomness = randomness;
                endUndo(false);
                previewOverlayDirty = true;
            }

            //Decrement the indent level after the randomness properties.
            EditorGUI.indentLevel--;

            //Physics header.
            EditorGUILayout.GetControlRect();
            EditorGUILayout.LabelField("Physics", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            //Collider types.
            EditorGUI.BeginChangeCheck();
            Shatter.ColliderType colliderType = (Shatter.ColliderType) EditorGUILayout.EnumPopup(new GUIContent("Collider Types", "The collider type to add " +
                    "to each of the shattered pieces of the sprite. Having no collider is the most efficient way of shattering a sprite, but the shattered " +
                    "pieces will not collide with anything. A polygon collider is the least efficient collider but produces the most accurate collisions."),
                    shatter.shatterDetails.colliderType);
            if (EditorGUI.EndChangeCheck()) {
                beginUndo("Change Sprite Shatter Collider Type");
                shatter.shatterDetails.colliderType = colliderType;
                endUndo(true);
            }

            //Collider scale (if a collider type is selected).
            if (shatter.shatterDetails.colliderType != Shatter.ColliderType.None) {
                EditorGUI.BeginChangeCheck();
                float collidersScale = EditorGUILayout.FloatField(new GUIContent("Collider Scale",
                        "The amount to scale the colliders. Set this to less than 1 if for example you want the shattered pieces to overlap."),
                        shatter.shatterDetails.collidersScale);
                if (EditorGUI.EndChangeCheck()) {
                    beginUndo("Change Sprite Shatter Colliders Scale");
                    shatter.shatterDetails.collidersScale = collidersScale;
                    endUndo(true);
                }
            }

            //Decrement the indent level after the physics properties.
            EditorGUI.indentLevel--;

            //Sprite shattering properties header.
            EditorGUILayout.GetControlRect();
            EditorGUILayout.LabelField("Sprite Shattering Properties", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            //Explode from.
            EditorGUI.BeginChangeCheck();
            Vector2 explodeFrom = EditorGUILayout.Vector2Field(new GUIContent("Explode From", "The position from which to apply the explosion force when " +
                    "exploding the shattered pieces of the sprite. The X co-ordinate should be 0 for the left edge of the sprite and 1 for the right edge, " +
                    "and the Y co-ordinate should be 0 for bottom of the sprite and 1 for the top, although the values can be outside this range. For " +
                    "example, to make the sprite explode up from the bottom, co-ordinates of (0.5, 0) could be used."), shatter.shatterDetails.explodeFrom);
            if (EditorGUI.EndChangeCheck()) {
                beginUndo("Change Sprite Shatter Explode From");
                shatter.shatterDetails.explodeFrom = explodeFrom;
                endUndo(true);
            }

            //Explosion force.
            EditorGUI.BeginChangeCheck();
            Vector2 explosionForce = EditorGUILayout.Vector2Field(new GUIContent("Explosion Force", "The force to apply in the X and Y direction when " +
                    "exploding the shattered pieces of the sprite. A force of zero will make the sprite just collapse in its current position."),
                    shatter.shatterDetails.explosionForce);
            if (EditorGUI.EndChangeCheck()) {
                beginUndo("Change Sprite Shatter Explosion Force");
                shatter.shatterDetails.explosionForce = explosionForce;
                endUndo(true);
            }

            //Decrement the indent level after the shattering properties.
            EditorGUI.indentLevel--;

            //Preview header.
            EditorGUILayout.GetControlRect();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            //Show the preview image.
            int textureWidth = (int) spriteRenderer.sprite.rect.width;
            int textureHeight = (int) spriteRenderer.sprite.rect.height;
            int previewTextureWidth = (int) (EditorGUIUtility.currentViewWidth * 0.75f);
            int previewTextureHeight = (int) ((textureHeight / (float) textureWidth) * EditorGUIUtility.currentViewWidth * 0.75f);
            if (previewOverlayDirty) {
                previewOverlayTarget = target;
                if (previewOverlayTexture != null)
                    DestroyImmediate(previewOverlayTexture);
                previewOverlayTexture = new Texture2D(previewTextureWidth, previewTextureHeight);
                previewOverlayTexture.name = "Sprite Shatter Inspector Preview Overlay";
                previewOverlayTexture.hideFlags = HideFlags.HideAndDontSave;
                bool[,] whitePixels = new bool[previewTextureWidth, previewTextureHeight];
                Vector2[][] shatterShapes = shatter.generateShatterShapes();
                for (int i = 0; i < shatterShapes.Length; i++)
                    for (int j = 0; j < shatterShapes[i].Length; j++) {
                        Vector2 from = new Vector2(shatterShapes[i][j].x * previewTextureWidth, shatterShapes[i][j].y * previewTextureHeight);
                        Vector2 to = new Vector2(shatterShapes[i][(j + 1) % shatterShapes[i].Length].x * previewTextureWidth,
                                shatterShapes[i][(j + 1) % shatterShapes[i].Length].y * previewTextureHeight);
                        for (float k = 0; k <= 1; k += 1 / (from - to).magnitude) {
                            Vector2 pixelPosition = Vector2.Lerp(from, to, k);
                            int pixelPositionX = Mathf.RoundToInt(Mathf.Clamp(pixelPosition.x, 1, previewTextureWidth - 1));
                            int pixelPositionY = Mathf.RoundToInt(Mathf.Clamp(pixelPosition.y, 1, previewTextureHeight - 1));
                            whitePixels[pixelPositionX, pixelPositionY] = true;
                        }
                    }
                for (int i = 1; i < previewTextureWidth - 1; i++)
                    for (int j = 1; j < previewTextureHeight - 1; j++) {
                        if (whitePixels[i, j])
                            previewOverlayTexture.SetPixel(i, j, Color.white);
                        else if (whitePixels[i - 1, j - 1] || whitePixels[i - 1, j] || whitePixels[i - 1, j + 1] || whitePixels[i, j - 1] || whitePixels[i, j] || whitePixels[i, j + 1] || whitePixels[i + 1, j - 1] || whitePixels[i + 1, j] || whitePixels[i + 1, j + 1])
                            previewOverlayTexture.SetPixel(i, j, Color.black);
                        else
                            previewOverlayTexture.SetPixel(i, j, Color.clear);
                    }
               previewOverlayTexture.Apply();
            }
            if (previewTextureWidth > 1) {
                if (spriteRenderer.flipY)
                    previewTextureHeight = -previewTextureHeight;
                Rect position = EditorGUILayout.GetControlRect(true, Math.Abs(previewTextureHeight));
                if (previewTextureHeight < 0) {
                    position.yMin -= previewTextureHeight + 1;
                    position.yMax -= previewTextureHeight + 1;
                }
                position.xMin += position.width * 0.125f;
                position.xMax -= position.width * 0.125f;
                if (spriteRenderer.flipX)
                    position = new Rect(position.xMax + 1, position.yMin, -position.width, position.height);
                position.height = previewTextureHeight;
                Rect textureRect = spriteRenderer.sprite.textureRect;
                textureRect = new Rect(
                    textureRect.xMin / spriteRenderer.sprite.texture.width,
                    textureRect.yMin / spriteRenderer.sprite.texture.height,
                    textureRect.width / spriteRenderer.sprite.texture.width,
                    textureRect.height / spriteRenderer.sprite.texture.height);
                if (Event.current.type == EventType.Repaint) {
                    Graphics.DrawTexture(position, spriteRenderer.sprite.texture, textureRect, 0, 0, 0, 0);
                    Graphics.DrawTexture(position, previewOverlayTexture);
                }
            }

            //Version details.
            EditorGUILayout.GetControlRect();
            Rect rect = EditorGUILayout.GetControlRect();
            if (GUI.Button(new Rect(rect.xMax - 100, rect.yMin, 100, rect.height), "Version " + Shatter.version, versionStyle)) {
                SpriteShatterVersionChanges versionChangesEditorWindow = EditorWindow.GetWindow<SpriteShatterVersionChanges>();
                versionChangesEditorWindow.minSize = new Vector2(800, 600);
                versionChangesEditorWindow.titleContent = new GUIContent("Sprite Shatter - Version Changes");
            }
        }

        //Begin an undo.
        void beginUndo(string eventName) {
            if (Undo.undoRedoPerformed == null)
                Undo.undoRedoPerformed += delegate { lastUndoEvent = ""; };
            Undo.RecordObject(target, eventName);
            if (eventName != lastUndoEvent) {
                undoGroup = Undo.GetCurrentGroup();
                lastUndoEvent = eventName;
            }
        }

        //End an undo.
        void endUndo(bool forceNewGroup) {
            EditorUtility.SetDirty(target);
            if (forceNewGroup)
                lastUndoEvent = "";
            else
                Undo.CollapseUndoOperations(undoGroup);
        }

        //Upgrade Sprite Shatter to the latest version.
        void upgrade() {
            Shatter shatter = (Shatter) target;
            Undo.RecordObject(shatter, "Upgraded Sprite Shatter component to version " + Shatter.version);
            shatter.upgrade(true);
            EditorApplication.update -= upgrade;
        }
    }
}