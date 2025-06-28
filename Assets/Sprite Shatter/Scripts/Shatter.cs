namespace SpriteShatter {
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class Shatter : MonoBehaviour {

        //Constants.
        public const string version = "2.0.5";

        //Enumerated Types.
        public enum ShatterType {
            Grid = 0,
            Hexagons = 2,
            Radial = 1,
            Triangles = 3
        };
        public enum ColliderType {
            None,
            CircleLowQuality, CircleMediumQuality, CircleHighQuality,
            BoxLowQuality, BoxMediumQuality, BoxHighQuality,
            PolygonLowQuality, PolygonMediumQuality, PolygonHighQuality
        };
        public enum TriangleOrientation {
            Horizontal = 0,
            Vertical = 1
        }

        //Classes;
        [Serializable]
        public class ShatterDetails {

            //Version.
            public string version = "";

            //Shatter type.
            public ShatterType shatterType = ShatterType.Grid;

            //Grid shattering properties.
            public int horizontalCuts = 8, verticalCuts = 8;
            [Obsolete("Use \"horizontalZigzagPoints\" and \"verticalZigzagPoints\" instead.", true)]
            public float zigzagFrequency = 0;
            [Obsolete("Use \"horizontalZigzagSize\" and \"verticalZigzagSize\" instead.", true)]
            public float zigzagAmplitude = 0;
            public int horizontalZigzagPoints = 0;
            public float horizontalZigzagSize = 0;
            public int verticalZigzagPoints = 0;
            public float verticalZigzagSize = 0;

            //Radial shattering properties.
            public int radialSectors = 16;
            public int radials = 1;
            public Vector2 radialCentre = new Vector2(0.5f, 0.5f);

            //Hexagons shattering properties.
            public int horizontalHexagons = 4;
            public int verticalHexagons = 4;

            //Triangles shattering properties.
            public TriangleOrientation triangleOrientation = TriangleOrientation.Horizontal;
            public int horizontalTriangles = 4;
            public int verticalTriangles = 4;

            //Randomness.
            public bool randomizeAtRunTime = false;
            public int randomSeed = 0;
            public float randomness = 0.5f;

            //Physics.
            public ColliderType colliderType;
            public float collidersScale = 1f;

            //Shattering.
            public Vector2 explodeFrom = new Vector2(0.5f, 0.5f);
            public Vector2 explosionForce = new Vector2(0, 5);
        }

        //Properties.
        public ShatterDetails shatterDetails;

        //Variables.
        Vector3[] originalShatterPieceLocations;
        Quaternion[] originalShatterPieceRotations;
        Transform shatterGameObjectTransform = null;
        bool error = false;

        //Reset.
        void Reset() {
            shatterDetails = new ShatterDetails();
            shatterDetails.version = version;
        }

        //Start.
        void Start() {

            //Check whether the Sprite Shatter version needs upgrading.
            if (needsUpgrading())
                upgrade(true);

            //Get the sprite renderer and texture sizes. Return immediately if there is no sprite renderer or it doesn't have a sprite associated with it.
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null || spriteRenderer.sprite == null) {
                error = true;
                return;
            }

            //Determine the collider type.
            bool isCircleCollider =
                shatterDetails.colliderType == ColliderType.CircleHighQuality ||
                shatterDetails.colliderType == ColliderType.CircleMediumQuality ||
                shatterDetails.colliderType == ColliderType.CircleLowQuality;
            bool isBoxCollider =
                shatterDetails.colliderType == ColliderType.BoxHighQuality ||
                shatterDetails.colliderType == ColliderType.BoxMediumQuality ||
                shatterDetails.colliderType == ColliderType.BoxLowQuality;
            bool isPolygonCollider =
                shatterDetails.colliderType == ColliderType.PolygonHighQuality ||
                shatterDetails.colliderType == ColliderType.PolygonMediumQuality ||
                shatterDetails.colliderType == ColliderType.PolygonLowQuality;
            int colliderQualityDivisor =
                shatterDetails.colliderType == ColliderType.CircleLowQuality || shatterDetails.colliderType == ColliderType.BoxLowQuality ||
                    shatterDetails.colliderType == ColliderType.PolygonLowQuality ? 4 :
                (shatterDetails.colliderType == ColliderType.CircleMediumQuality || shatterDetails.colliderType == ColliderType.BoxMediumQuality ||
                    shatterDetails.colliderType == ColliderType.PolygonMediumQuality ? 2 : 1);

            //Get the texture size.
            int textureWidth = Mathf.RoundToInt(spriteRenderer.sprite.textureRect.width) / colliderQualityDivisor;
            int textureHeight = Mathf.RoundToInt(spriteRenderer.sprite.textureRect.height) / colliderQualityDivisor;

            //Randomize the random seed if required at runtime.
            if (shatterDetails.randomizeAtRunTime)
                shatterDetails.randomSeed = new System.Random().Next();

            //Create the sprite shatter parent object, getting the next free name.
            int nextFreeSpriteShatterParentNumber = 0;
            string spriteShatterParentName = name + " - Sprite Shatter";
            Transform[] childTransforms = GetComponentsInChildren<Transform>(true);
            while (transformArrayContainsGameObject(childTransforms, spriteShatterParentName)) {
                nextFreeSpriteShatterParentNumber++;
                spriteShatterParentName = name + " - Sprite Shatter (" + (nextFreeSpriteShatterParentNumber + 1) + ")";
            }
            GameObject shatterGameObject = new GameObject(spriteShatterParentName);
            shatterGameObject.layer = gameObject.layer;
            shatterGameObjectTransform = shatterGameObject.transform;
            shatterGameObjectTransform.SetParent(transform);
            shatterGameObjectTransform.localPosition = Vector3.zero;
            shatterGameObjectTransform.localRotation = Quaternion.identity;
            Vector3 scale = Vector3.one;
            if (spriteRenderer.flipX)
                scale.x = -scale.x;
            if (spriteRenderer.flipY)
                scale.y = -scale.y;
            shatterGameObjectTransform.localScale = scale;
            shatterGameObject.SetActive(false);

            //Create a render texture containing the sprite's texture so its pixel data can be read to create polygon colliders (if required).
            Texture2D texture = null;
            Color[] pixels = null;
            if (shatterDetails.colliderType != ColliderType.None) {
                RenderTexture renderTexture = new RenderTexture(textureWidth, textureHeight, 16, RenderTextureFormat.ARGB32);
                renderTexture.hideFlags = HideFlags.HideAndDontSave;
                RenderTexture oldActiveRenderTexture = RenderTexture.active;
                RenderTexture.active = renderTexture;
                GL.PushMatrix();
                GL.LoadPixelMatrix(0, textureWidth, textureHeight, 0);
                GL.Clear(true, true, Color.clear);
                Graphics.DrawTexture(
                    new Rect(
                        -spriteRenderer.sprite.textureRect.xMin / colliderQualityDivisor,
                        (spriteRenderer.sprite.textureRect.yMax - spriteRenderer.sprite.texture.height) / colliderQualityDivisor,
                        spriteRenderer.sprite.texture.width / colliderQualityDivisor,
                        spriteRenderer.sprite.texture.height / colliderQualityDivisor),
                    spriteRenderer.sprite.texture);
                GL.PopMatrix();
                texture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
                texture.Apply();
                pixels = texture.GetPixels();
                RenderTexture.active = oldActiveRenderTexture;
                Destroy(renderTexture);
            }

            //Generate the shattered shapes and loop over them.
            Vector2[][] shapes = generateShatterShapes();
            int pieceIndex = 0;
            List<Vector3> originalShatterPieceLocationsList = new List<Vector3>();
            List<Quaternion> originalShatterPieceRotationsList = new List<Quaternion>();
            for (int i = 0; i < shapes.Length; i++) {

                //Create a game object for this shatter piece and add a sprite renderer whose properties match the original sprite and a 2D rigid body.
                GameObject spriteShatterGameObject = new GameObject("Piece " + pieceIndex);
                spriteShatterGameObject.layer = gameObject.layer;
                spriteShatterGameObject.transform.SetParent(shatterGameObjectTransform);
                spriteShatterGameObject.transform.localScale = Vector3.one;
                spriteShatterGameObject.transform.localRotation = Quaternion.identity;
                SpriteRenderer shatterSpriteRenderer = spriteShatterGameObject.AddComponent<SpriteRenderer>();
                shatterSpriteRenderer.color = spriteRenderer.color;
                shatterSpriteRenderer.sharedMaterial = spriteRenderer.sharedMaterial;
                shatterSpriteRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                shatterSpriteRenderer.sortingOrder = spriteRenderer.sortingOrder;
                spriteShatterGameObject.AddComponent<Rigidbody2D>();

                //Get the bounds of the shape and position it so it is centred on them.
                float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
                for (int j = 0; j < shapes[i].Length; j++) {
                    shapes[i][j].x *= textureWidth;
                    shapes[i][j].y *= textureHeight;
                    minX = Mathf.Min(minX, shapes[i][j].x);
                    maxX = Mathf.Max(maxX, shapes[i][j].x);
                    minY = Mathf.Min(minY, shapes[i][j].y);
                    maxY = Mathf.Max(maxY, shapes[i][j].y);
                }
                Vector2 centre = new Vector2(((minX + maxX) / 2) * colliderQualityDivisor, ((minY + maxY) / 2) * colliderQualityDivisor);
                spriteShatterGameObject.transform.localPosition =
                    new Vector3(
                        (centre.x - spriteRenderer.sprite.pivot.x) + spriteRenderer.sprite.textureRectOffset.x,
                        (centre.y - spriteRenderer.sprite.pivot.y) + spriteRenderer.sprite.textureRectOffset.y,
                        0) / spriteRenderer.sprite.pixelsPerUnit;

                //Get the vertices and triangles for this shatter piece.
                Vector2[] vertices = shapes[i];
                ushort[] triangles = generateMeshTriangles(shapes[i]);

                //Create a polygon collider to determine whether any pixels actually exist in this shattered piece, and to get the bounds of them if they do.
                //The polygon collider can then be removed if it isn't required.
                if (shatterDetails.colliderType != ColliderType.None) {
                    int x = Mathf.RoundToInt(minX), y = Mathf.RoundToInt(minY), width = Mathf.RoundToInt(maxX - minX), height = Mathf.RoundToInt(maxY - minY);
                    if (x > 0) {
                        x--;
                        width++;
                    }
                    if (y > 0) {
                        y--;
                        height++;
                    }
                    if (x + width < textureWidth)
                        width++;
                    if (y + height < textureHeight)
                        height++;
                    int pieceMinX = int.MaxValue, pieceMaxX = int.MinValue, pieceMinY = int.MaxValue, pieceMaxY = int.MinValue;
                    float[] triangleVertexPositions = new float[triangles.Length * 2];
                    for (int j = 0; j < triangles.Length; j++) {
                        triangleVertexPositions[j * 2] = vertices[triangles[j]].x;
                        triangleVertexPositions[(j * 2) + 1] = vertices[triangles[j]].y;
                    }
                    Color[] piecePixels = null;
                    if (isPolygonCollider)
                        piecePixels = new Color[width * height];
                    int pixelIndex = 0;
                    for (int k = 0; k < height; k++) {
                        int pixelOffset = ((y + k) * textureWidth) + x;
                        for (int j = 0; j < width; j++) {
                            bool pointInTriangle = false;
                            for (int l = 0; l < triangleVertexPositions.Length; l += 6)
                                if (MathsFunctions.pointIn2DTriangle(
                                        triangleVertexPositions[l], triangleVertexPositions[l + 1],
                                        triangleVertexPositions[l + 2], triangleVertexPositions[l + 3],
                                        triangleVertexPositions[l + 4], triangleVertexPositions[l + 5],
                                        x + j, y + k)) {
                                    pointInTriangle = true;
                                    break;
                                }
                            if (pointInTriangle) {
                                if (isPolygonCollider)
                                    piecePixels[pixelIndex] = pixels[pixelOffset + j];
                                if (pixels[pixelOffset + j].a > 0.001f) {
                                    pieceMinX = Math.Min(pieceMinX, x + j);
                                    pieceMaxX = Math.Max(pieceMaxX, x + j);
                                    pieceMinY = Math.Min(pieceMinY, y + k);
                                    pieceMaxY = Math.Max(pieceMaxY, y + k);
                                }
                            }
                            pixelIndex++;
                        }
                    }
                    if (pieceMinX < pieceMaxX && pieceMinY < pieceMaxY) {
                        if (isPolygonCollider) {
                            const float pentagonAngle = 1.2566370614359172953850573533118f;
                            texture.SetPixels(x, y, width, height, piecePixels);
                            texture.Apply();
                            shatterSpriteRenderer.sprite = Sprite.Create(texture, new Rect(minX, minY, Mathf.Floor(maxX - minX), Mathf.Floor(maxY - minY)),
                                    new Vector2(0.5f, 0.5f), spriteRenderer.sprite.pixelsPerUnit / colliderQualityDivisor, 0, SpriteMeshType.FullRect);
                            PolygonCollider2D polygonCollider = spriteShatterGameObject.AddComponent<PolygonCollider2D>();
                            bool colliderCouldNotBeCreated = false;
                            if (polygonCollider.points.Length == 5) {
                                colliderCouldNotBeCreated = true;
                                Vector2 colliderCentre = Vector2.zero;
                                for (int k = 0; k < 5; k++)
                                    colliderCentre += polygonCollider.points[k];
                                colliderCentre /= 5;
                                for (int k = 0; k < 4; k++) {
                                    float x1 = polygonCollider.points[k].x - colliderCentre.x;
                                    float y1 = polygonCollider.points[k].y - colliderCentre.y;
                                    float x2 = polygonCollider.points[k + 1].x - colliderCentre.x;
                                    float y2 = polygonCollider.points[k + 1].y - colliderCentre.y;
                                    if (Math.Abs(Math.Acos(((x1 * x2) + (y1 * y2)) / (Math.Sqrt(Math.Pow(x1, 2) + Math.Pow(y1, 2)) *
                                            Math.Sqrt(Math.Pow(x2, 2) + Math.Pow(y2, 2)))) - pentagonAngle) > 0.001f) {
                                        colliderCouldNotBeCreated = false;
                                        break;
                                    }
                                }
                            }
                            if (colliderCouldNotBeCreated) {
                                Destroy(spriteShatterGameObject);
                                continue;
                            }

                            //Scale the collider points.
                            else if (Mathf.Abs(shatterDetails.collidersScale - 1) > 0.0001f) {
                                Vector2 colliderCentre = Vector2.zero;
                                for (int j = 0; j < polygonCollider.points.Length; j++)
                                    colliderCentre += polygonCollider.points[j];
                                colliderCentre /= polygonCollider.points.Length;
                                Vector2[] path = new Vector2[polygonCollider.points.Length];
                                for (int j = 0; j < polygonCollider.points.Length; j++)
                                    path[j] =
                                        new Vector2(
                                            ((polygonCollider.points[j].x - colliderCentre.x) * shatterDetails.collidersScale) + colliderCentre.x,
                                            ((polygonCollider.points[j].y - colliderCentre.y) * shatterDetails.collidersScale) + colliderCentre.y
                                        );
                                polygonCollider.SetPath(0, path);
                            }
                        }
                        else {
                            Vector2 offset = (new Vector2(((pieceMinX + pieceMaxX) - (minX + maxX)) / 2, ((pieceMinY + pieceMaxY) - (minY + maxY)) / 2) *
                                    colliderQualityDivisor) / spriteRenderer.sprite.pixelsPerUnit;
                            if (isBoxCollider) {
                                BoxCollider2D boxCollider = spriteShatterGameObject.AddComponent<BoxCollider2D>();
                                boxCollider.offset = offset;
                                boxCollider.size = new Vector2(
                                        ((pieceMaxX - pieceMinX) * colliderQualityDivisor) / spriteRenderer.sprite.pixelsPerUnit,
                                        ((pieceMaxY - pieceMinY) * colliderQualityDivisor) / spriteRenderer.sprite.pixelsPerUnit) *
                                        shatterDetails.collidersScale;
                            }
                            else if (isCircleCollider) {
                                CircleCollider2D circleCollider = spriteShatterGameObject.AddComponent<CircleCollider2D>();
                                circleCollider.offset = offset;
                                circleCollider.radius = ((((pieceMaxX - pieceMinX) + (pieceMaxY - pieceMinY)) * colliderQualityDivisor) /
                                        (spriteRenderer.sprite.pixelsPerUnit * 4)) * shatterDetails.collidersScale;
                            }
                        }
                    }
                    else {
                        Destroy(spriteShatterGameObject);
                        continue;
                    }
                }

                //Create the sprite and associate it with the sprite renderer.
                float spriteRectX = Mathf.Max((minX * colliderQualityDivisor) + spriteRenderer.sprite.textureRect.xMin, 0);
                float spriteRectY = Mathf.Max((minY * colliderQualityDivisor) + spriteRenderer.sprite.textureRect.yMin, 0);
                float spriteRectWidth = (maxX - minX) * colliderQualityDivisor;
                float spriteRectHeight = (maxY - minY) * colliderQualityDivisor;
                spriteRectWidth -= Mathf.Max(0, spriteRectX + spriteRectWidth - spriteRenderer.sprite.textureRect.xMax);
                spriteRectHeight -= Mathf.Max(0, spriteRectY + spriteRectHeight - spriteRenderer.sprite.textureRect.yMax);
                Sprite sprite = Sprite.Create(
                    spriteRenderer.sprite.texture,
                    new Rect(spriteRectX, spriteRectY, spriteRectWidth, spriteRectHeight),
                    new Vector2(0.5f, 0.5f),
                    spriteRenderer.sprite.pixelsPerUnit,
                    0,
                    SpriteMeshType.Tight);
                for (int j = 0; j < vertices.Length; j++) {
                    vertices[j].x = Mathf.Clamp((vertices[j].x - minX) * colliderQualityDivisor, 0, spriteRectWidth);
                    vertices[j].y = Mathf.Clamp((vertices[j].y - minY) * colliderQualityDivisor, 0, spriteRectHeight);
                }
                sprite.OverrideGeometry(vertices, triangles);
                shatterSpriteRenderer.sprite = sprite;

                //Add the piece position and rotation to the list of original positions and rotations so the sprite can be restored after it has been shattered.
                originalShatterPieceLocationsList.Add(spriteShatterGameObject.transform.localPosition);
                originalShatterPieceRotationsList.Add(spriteShatterGameObject.transform.localRotation);

                //Move onto the next shatter piece index.
                pieceIndex++;
            }

            //If no shattered pieces were created (probably because the image was entirely transparent or the pieces were too small), display an error.
            if (pieceIndex == 0) {
                Debug.LogError("Sprite Shatter (" + name + "): No shattered pieces were created. This is probably because the pieces are too small for the " +
                        "sprite, or the sprite has too much transparency.");
                error = true;
            }

            //Get the original position and rotation of the shatter objects so they can be reset.
            else {
                originalShatterPieceLocations = originalShatterPieceLocationsList.ToArray();
                originalShatterPieceRotations = originalShatterPieceRotationsList.ToArray();
            }

            //Destroy the temporary texture.
            if (texture != null)
                Destroy(texture);
        }

        //Generate the shapes of the shattered pieces.
        public Vector2[][] generateShatterShapes() {
            Vector2[][] shapes;

            //Initialise the random instance, using the random seed.
            System.Random random = new System.Random(shatterDetails.randomSeed);

            //Generate shapes for a grid-style shatter.
            if (shatterDetails.shatterType == ShatterType.Grid) {

                //Initialise an array of shapes that will be generated by shattering the sprite.
                shapes = new Vector2[(shatterDetails.horizontalCuts + 1) * (shatterDetails.verticalCuts + 1)][];

                //Get the horizontal and vertical offsets for the grid cuts to shatter the sprite, applying randomness if required.
                float[,] randomHorizontalCutOffsets = new float[shatterDetails.horizontalCuts, 2];
                for (int i = 0; i < shatterDetails.horizontalCuts; i++)
                    for (int j = 0; j < 2; j++)
                        randomHorizontalCutOffsets[i, j] = ((float) ((random.NextDouble() - 0.5f) / (shatterDetails.horizontalCuts + 1)) *
                                shatterDetails.randomness * 0.99f);
                float[,] randomVerticalCutOffsets = new float[shatterDetails.verticalCuts, 2];
                for (int i = 0; i < shatterDetails.verticalCuts; i++)
                    for (int j = 0; j < 2; j++)
                        randomVerticalCutOffsets[i, j] = ((float) ((random.NextDouble() - 0.5f) / (shatterDetails.verticalCuts + 1)) *
                                shatterDetails.randomness * 0.99f);

                //Get the vertices where the grid lines meet, adjusting them based on the randomness.
                Vector2[,] vertices = new Vector2[shatterDetails.verticalCuts + 2, shatterDetails.horizontalCuts + 2];
                vertices[0, 0] = Vector2.zero;
                for (int i = 0; i <= shatterDetails.verticalCuts + 1; i++)
                    for (int j = 0; j <= shatterDetails.horizontalCuts + 1; j++) {
                        float x = (float) i / (shatterDetails.verticalCuts + 1);
                        float y = (float) j / (shatterDetails.horizontalCuts + 1);
                        vertices[i, j] = MathsFunctions._2DLineIntersectionPoint(
                            i == 0 ? 0 : (i == shatterDetails.verticalCuts + 1 ? 1 : (x + randomVerticalCutOffsets[i - 1, 0])),
                            -0.0001f,
                            i == 0 ? 0 : (i == shatterDetails.verticalCuts + 1 ? 1 : (x + randomVerticalCutOffsets[i - 1, 1])),
                            1.0001f,
                            -0.0001f,
                            j == 0 ? 0 : (j == shatterDetails.horizontalCuts + 1 ? 1 : (y + randomHorizontalCutOffsets[j - 1, 0])),
                            1.0001f,
                            j == 0 ? 0 : (j == shatterDetails.horizontalCuts + 1 ? 1 : (y + randomHorizontalCutOffsets[j - 1, 1]))
                        );
                    }

                //Determine whether zigzags are enabled and get the size of them if they are.
                bool usingHorizontalZigzag = shatterDetails.horizontalZigzagPoints > 0 && shatterDetails.horizontalZigzagSize > 0.0001f;
                bool usingVerticalZigzag = shatterDetails.verticalZigzagPoints > 0 && shatterDetails.verticalZigzagSize > 0.0001f;
                float zigzagSizeX = (shatterDetails.horizontalZigzagSize / (shatterDetails.horizontalCuts + 1)) * 0.25f;
                float zigzagSizeY = (shatterDetails.verticalZigzagSize / (shatterDetails.verticalCuts + 1)) * 0.25f;

                //Loop over the pieces in the grid and create the shapes.
                List<Vector2> shapeVertices = new List<Vector2>();
                for (int i = 0; i <= shatterDetails.verticalCuts; i++)
                    for (int j = 0; j <= shatterDetails.horizontalCuts; j++) {
                        shapeVertices.Clear();
                        Vector2 start = vertices[i, j], end = vertices[i + 1, j];
                        for (int l = 0; l < 4; l++) {
                            bool enableZigzag;
                            if (l == 0)
                                enableZigzag = j > 0;
                            else {
                                start = end;
                                if (l == 1) {
                                    end = vertices[i + 1, j + 1];
                                    enableZigzag = i < shatterDetails.verticalCuts;
                                }
                                else if (l == 2) {
                                    end = vertices[i, j + 1];
                                    enableZigzag = j < shatterDetails.horizontalCuts;
                                }
                                else {
                                    end = vertices[i, j];
                                    enableZigzag = i > 0;
                                }
                            }
                            shapeVertices.Add(start);

                            //Add zigzag vertices if enabled.
                            if (enableZigzag && ((l % 2 == 0 && usingHorizontalZigzag) || (l % 2 == 1 && usingVerticalZigzag))) {
                                float zigzagSize = l % 2 == 0 ? zigzagSizeX : zigzagSizeY;

                                //Adjust the number of zigzags in this particular piece depending on the piece size.
                                int zigzagPoints;
                                if (l % 2 == 0)
                                    zigzagPoints = Mathf.RoundToInt(shatterDetails.horizontalZigzagPoints);
                                else
                                    zigzagPoints = Mathf.RoundToInt(shatterDetails.verticalZigzagPoints);

                                //Add the zigzag vertices.
                                Vector2 zigzagDirection = new Vector2(-(end.y - start.y), end.x - start.x).normalized *
                                        (l < 2 || zigzagPoints % 2 == 0 ? -zigzagSize : zigzagSize);
                                for (int k = 0; k < zigzagPoints; k++) {
                                    zigzagDirection = -zigzagDirection;
                                    float amount = (k + 0.5f) / zigzagPoints;
                                    Vector2 vertex = new Vector2(Mathf.Lerp(start.x, end.x, amount), Mathf.Lerp(start.y, end.y, amount));
                                    if (l == 0 || l == 2)
                                        vertex += new Vector2(
                                            zigzagDirection.x,
                                            zigzagDirection.y *
                                                ((l == 0 && i == 0) || (l == 2 && i == shatterDetails.verticalCuts) ? 1 :
                                                    Mathf.Clamp01(Mathf.Abs(vertex.x - start.x) / (zigzagSize * 1.25f))) *
                                                ((l == 0 && i == shatterDetails.verticalCuts) || (l == 2 && i == 0) ? 1 :
                                                    Mathf.Clamp01(Mathf.Abs(vertex.x - end.x) / (zigzagSize * 1.25f))));
                                    else
                                        vertex += new Vector2(
                                            zigzagDirection.x *
                                                ((l == 1 && j == 0) || (l == 3 && j == shatterDetails.horizontalCuts) ? 1 :
                                                    Mathf.Clamp01(Mathf.Abs(vertex.y - start.y) / (zigzagSize * 1.25f))) *
                                                ((l == 1 && j == shatterDetails.horizontalCuts) || (l == 3 && j == 0) ? 1 :
                                                    Mathf.Clamp01(Mathf.Abs(vertex.y - end.y) / (zigzagSize * 1.25f))),
                                            zigzagDirection.y);
                                    shapeVertices.Add(new Vector2(Mathf.Clamp01(vertex.x), Mathf.Clamp01(vertex.y)));
                                }
                            }
                        }
                        shapes[(i * (shatterDetails.horizontalCuts + 1)) + j] = shapeVertices.ToArray();
                    }
            }

            //Generate shapes for a radial-style shatter.
            else if (shatterDetails.shatterType == ShatterType.Radial) {

                //Initialise an array of shapes that will be generated by shattering the sprite.
                shapes = new Vector2[shatterDetails.radialSectors * shatterDetails.radials][];

                //Set the rotation amounts for the radial sectors, applying randomness if required.
                float[] rotationAmounts = new float[shatterDetails.radialSectors];
                float rotationAmount = -(Mathf.PI * 2) / shatterDetails.radialSectors;
                for (int i = 0; i < shatterDetails.radialSectors; i++)
                    rotationAmounts[i] = (i * rotationAmount) + ((float) (random.NextDouble() - 0.5f) * rotationAmount * shatterDetails.randomness * 0.9f);

                //Set the co-ordinates of the radial lines going out from the centre, working out where they collide with the edge of the sprite and which edge
                //they collide with.
                int[] edgeCollision = new int[shatterDetails.radialSectors];
                Vector2[] intersectionPoints = new Vector2[shatterDetails.radialSectors];
                for (int i = 0; i < shatterDetails.radialSectors; i++) {
                    Vector2 radialVector = MathsFunctions.rotateVector(Vector2.up, rotationAmounts[i]);
                    for (int j = 0; j < 4; j++) {
                        Vector2 boundaryStart = new Vector2(j == 0 || j == 3 ? -0.5f : 0.5f, j < 2 ? -0.5f : 0.5f);
                        Vector2 boundaryEnd = new Vector2(j >= 2 ? -0.5f : 0.5f, j == 0 || j == 3 ? -0.5f : 0.5f);
                        if (MathsFunctions._2DLinesIntersect(0, 0, radialVector.x, radialVector.y, boundaryStart.x, boundaryStart.y, boundaryEnd.x,
                                boundaryEnd.y)) {
                            intersectionPoints[i] = MathsFunctions._2DLineIntersectionPoint(0, 0, radialVector.x, radialVector.y, boundaryStart.x,
                                    boundaryStart.y, boundaryEnd.x, boundaryEnd.y);
                            if (intersectionPoints[i].x > -0.5001f && intersectionPoints[i].x < 0.5001f && intersectionPoints[i].y > -0.5001f &&
                                    intersectionPoints[i].y < 0.5001f) {
                                edgeCollision[i] = j;
                                break;
                            }
                        }
                    }
                }

                //Set the random offsets 
                float[,] radialSizeOffsets = new float[shatterDetails.radialSectors, shatterDetails.radials];
                for (int i = 0; i < shatterDetails.radialSectors; i++)
                    for (int j = 0; j < shatterDetails.radials; j++)
                        radialSizeOffsets[i, j] = j == shatterDetails.radials - 1 ? 1 :
                                ((float) (j + 1) / shatterDetails.radials) + (float) ((random.NextDouble() - 0.5f) * (1f / shatterDetails.radials) *
                                (shatterDetails.randomness * 0.9f));

                //Create the shattered pieces, splitting the sectors up into the number of radials required.
                int shapeIndex = 0;
                for (int i = 0; i < shatterDetails.radialSectors; i++) {
                    for (int j = 0; j < shatterDetails.radials; j++) {
                        Vector2 intersectionPoint, nextIntersectionPoint;
                        List<Vector2> shapeVertices = new List<Vector2>();
                        if (j == 0)
                            shapeVertices.Add(new Vector2(shatterDetails.radialCentre.x, 1 - shatterDetails.radialCentre.y));
                        else {
                            intersectionPoint = intersectionPoints[i] * radialSizeOffsets[i, j - 1];
                            nextIntersectionPoint = intersectionPoints[(i + 1) % shatterDetails.radialSectors] *
                                    radialSizeOffsets[(i + 1) % shatterDetails.radialSectors, j - 1];
                            shapeVertices.Add(new Vector2(Mathf.Clamp01(nextIntersectionPoint.x + 0.5f), Mathf.Clamp01(nextIntersectionPoint.y + 0.5f)));
                            shapeVertices.Add(new Vector2(Mathf.Clamp01(intersectionPoint.x + 0.5f), Mathf.Clamp01(intersectionPoint.y + 0.5f)));
                        }
                        intersectionPoint = intersectionPoints[i] * radialSizeOffsets[i, j];
                        nextIntersectionPoint = intersectionPoints[(i + 1) % shatterDetails.radialSectors] *
                                radialSizeOffsets[(i + 1) % shatterDetails.radialSectors, j];
                        shapeVertices.Add(new Vector2(Mathf.Clamp01(intersectionPoint.x + 0.5f), Mathf.Clamp01(intersectionPoint.y + 0.5f)));

                        //Add corner vertices to the final radial to ensure the mesh goes right into the corners of the sprite.
                        if (j == shatterDetails.radials - 1) {
                            int nextSector = (i + 1) % shatterDetails.radialSectors;
                            if (edgeCollision[i] == 0 && edgeCollision[nextSector] == 1)
                                shapeVertices.Add(new Vector2(1, 0));
                            else if (edgeCollision[i] == 1 && edgeCollision[nextSector] == 2)
                                shapeVertices.Add(Vector2.one);
                            else if (edgeCollision[i] == 2 && edgeCollision[nextSector] == 3)
                                shapeVertices.Add(new Vector2(0, 1));
                            else if (edgeCollision[i] == 3 && edgeCollision[nextSector] == 0)
                                shapeVertices.Add(Vector2.zero);
                        }
                        shapeVertices.Add(new Vector2(Mathf.Clamp01(nextIntersectionPoint.x + 0.5f), Mathf.Clamp01(nextIntersectionPoint.y + 0.5f)));
                        shapes[shapeIndex++] = shapeVertices.ToArray();
                    }
                }
            }

            //Generate shapes for a shatter into hexagonal pieces.
            else if (shatterDetails.shatterType == ShatterType.Hexagons) {
                List<Vector2[]> shapesList = new List<Vector2[]>();
                float top;
                float height = 1f / shatterDetails.verticalHexagons;
                float left = 0;
                float width = 1 / ((shatterDetails.horizontalHexagons * 1.5f) - 0.5f);
                shapesList.Add(
                    new Vector2[] {
                        Vector2.zero,
                        new Vector2(width / 4, 0),
                        new Vector2(0, height / 2)
                    }
                );
                shapesList.Add(
                    new Vector2[] {
                        new Vector2(width / 4, 1),
                        new Vector2(0, 1),
                        new Vector2(0, 1 - (height / 2))
                    }
                );
                top = 1 - height / 2;
                for (int i = 0; i < shatterDetails.verticalHexagons - 1; i++) {
                    shapesList.Add(
                        new Vector2[] {
                            new Vector2(width / 4, top - (height / 2)),
                            new Vector2(0, top),
                            new Vector2(0, top - height)
                        }
                    );
                    top -= height;
                }
                shapesList.Add(
                    new Vector2[] {
                        new Vector2(1, 0),
                        new Vector2(1, height / 2),
                        new Vector2(1 - (width / 4), 0)
                    }
                );
                shapesList.Add(
                    new Vector2[] {
                        new Vector2(1 - (width / 4), 1),
                        new Vector2(1, 1 - (height / 2)),
                        Vector2.one
                    }
                );
                top = 1 - height / 2;
                for (int i = 0; i < shatterDetails.verticalHexagons - 1; i++) {
                    shapesList.Add(
                        new Vector2[] {
                            new Vector2(1 - (width / 4), top - (height / 2)),
                            new Vector2(1, top - height),
                            new Vector2(1, top)
                        }
                    );
                    top -= height;
                }
                for (int j = 0; j < shatterDetails.horizontalHexagons; j++) {
                    for (int k = 0; k < (j == shatterDetails.horizontalHexagons - 1 ? 1 : 2); k++) {
                        top = k == 0 ? 0 : (height / 2);
                        for (int i = 0; i < shatterDetails.verticalHexagons - (k == 1 ? 1 : 0); i++) {
                            shapesList.Add(
                                new Vector2[] {
                                    new Vector2(left, top + (height / 2)),
                                    new Vector2(left + (width / 4), top),
                                    new Vector2(left + ((width * 3) / 4), top),
                                    new Vector2(left + width, top + (height / 2)),
                                    new Vector2(left + ((width * 3) / 4), top + height),
                                    new Vector2(left + (width / 4), top + height)
                                }
                            );
                            top += height;
                        }
                        if (k == 1) {
                            shapesList.Add(
                                new Vector2[] {
                                    new Vector2(left, 0),
                                    new Vector2(left + width, 0),
                                    new Vector2(left + ((width * 3) / 4), height / 2),
                                    new Vector2(left + (width / 4), height / 2)
                                }
                            );
                            shapesList.Add(
                                new Vector2[] {
                                    new Vector2(left + (width / 4), 1 - (height / 2)),
                                    new Vector2(left + ((width * 3) / 4), 1 - (height / 2)),
                                    new Vector2(left + width, 1),
                                    new Vector2(left, 1)
                                }
                            );
                        }
                        left += width * 0.75f;
                    }
                }
                shapes = shapesList.ToArray();
                randomizePoints(random, shapes, Mathf.Min(width / 2, height / 2));
            }

            //Generate shapes for a shatter into triangular pieces.
            else if (shatterDetails.shatterType == ShatterType.Triangles) {
                List<Vector2[]> shapesList = new List<Vector2[]>();
                if (shatterDetails.triangleOrientation == TriangleOrientation.Horizontal)
                    for (int i = 0; i < shatterDetails.verticalTriangles; i++) {
                        float yFrom = (float) i / shatterDetails.verticalTriangles;
                        float yTo = (float) (i + 1) / shatterDetails.verticalTriangles;
                        for (int j = 0; j < shatterDetails.horizontalTriangles; j++) {
                            float xLeft = Mathf.Max((float) (j - 1) / shatterDetails.horizontalTriangles, 0f);
                            float xMiddle = (float) j / shatterDetails.horizontalTriangles;
                            float xRight = Mathf.Min((float) (j + 1) / shatterDetails.horizontalTriangles, 1f);
                            if (j % 2 == 0)
                                shapesList.Add(
                                    new Vector2[] {
                                        new Vector2(xLeft, yTo),
                                        new Vector2(xMiddle, yFrom),
                                        new Vector2(xRight, yTo),
                                        new Vector2(xMiddle, yTo)
                                    }
                                );
                            else
                                shapesList.Add(
                                    new Vector2[] {
                                        new Vector2(xLeft, yFrom),
                                        new Vector2(xMiddle, yFrom),
                                        new Vector2(xRight, yFrom),
                                        new Vector2(xMiddle, yTo)
                                    }
                                );
                        }
                    }
                else
                    for (int i = 0; i < shatterDetails.horizontalTriangles; i++) {
                        float xFrom = (float) i / shatterDetails.horizontalTriangles;
                        float xTo = (float) (i + 1) / shatterDetails.horizontalTriangles;
                        for (int j = 0; j < shatterDetails.verticalTriangles; j++) {
                            float yBottom = Mathf.Max((float) (j - 1) / shatterDetails.verticalTriangles, 0f);
                            float yMiddle = (float) j / shatterDetails.verticalTriangles;
                            float yTop = Mathf.Min((float) (j + 1) / shatterDetails.verticalTriangles, 1f);
                            if (j % 2 == 0)
                                shapesList.Add(
                                    new Vector2[] {
                                        new Vector2(xTo, yBottom),
                                        new Vector2(xTo, yMiddle),
                                        new Vector2(xTo, yTop),
                                        new Vector2(xFrom, yMiddle)
                                    }
                                );
                            else
                                shapesList.Add(
                                    new Vector2[] {
                                        new Vector2(xFrom, yBottom),
                                        new Vector2(xTo, yMiddle),
                                        new Vector2(xFrom, yTop),
                                        new Vector2(xFrom, yMiddle)
                                    }
                                );
                        }
                    }
                shapes = shapesList.ToArray();
                randomizePoints(random, shapes, Mathf.Min(1f / shatterDetails.horizontalTriangles, 1f / shatterDetails.verticalTriangles) / 2f);
            }

            //Throw an exception if the shatter type is unknown.
            else
                throw new Exception($"Unknown shatter type: {shatterDetails.shatterType}.");

            //Return the generated shapes.
            return shapes;
        }

        //Returns whether a transform array contains a transform associated with a game object with a given name
        static bool transformArrayContainsGameObject(Transform[] transformArray, string gameObjectName) {
            for (int i = 0; i < transformArray.Length; i++)
                if (transformArray[i].gameObject.name == gameObjectName)
                    return true;
            return false;
        }

        /// <summary>
        /// Shatter a sprite for which a "Sprite Shatter" has already been initialised. This method hides the original sprite by disabling its sprite renderer
        /// (if you want to disable other components of the original game object, such as a collider, you must do this manually) and activates its shatter
        /// pieces, applying any specified forces to them. The sprite will then shatter as the pieces react to gravity. To reset the sprite back to its original
        /// state and to hide the shatter pieces, call "reset()".
        /// </summary>
        public void shatter() {
            if (error)
                return;
            SpriteRenderer parentSpriteRenderer = null;
            if (shatterGameObjectTransform != null)
                parentSpriteRenderer = shatterGameObjectTransform.parent.gameObject.GetComponent<SpriteRenderer>();
            if (shatterGameObjectTransform == null || parentSpriteRenderer == null)
                Debug.LogError("Sprite Shatter (" + name + "): The Sprite Shatter game object or its Sprite Renderer could not be found. Please initialise " +
                        "the \"Sprite Shatter\" component again.");
            else {

                //Disable the sprite renderer of the original object to hide it and activate the shatter pieces game objects.
                parentSpriteRenderer.enabled = false;
                shatterGameObjectTransform.gameObject.SetActive(true);

                //Apply forces to the pieces.
                if (Math.Abs(shatterDetails.explosionForce.x) > 0.0001f || Math.Abs(shatterDetails.explosionForce.y) > 0.0001f) {
                    Vector2 explosionSource = new Vector2(
                            (parentSpriteRenderer.bounds.extents.x * shatterDetails.explodeFrom.x * 2) + parentSpriteRenderer.bounds.center.x -
                                parentSpriteRenderer.bounds.extents.x,
                            (parentSpriteRenderer.bounds.extents.y * shatterDetails.explodeFrom.y * 2) + parentSpriteRenderer.bounds.center.y -
                                parentSpriteRenderer.bounds.extents.y);
                    for (int i = 0; i < shatterGameObjectTransform.childCount; i++) {
                        Transform shatterObject = shatterGameObjectTransform.GetChild(i);
                        Vector2 normalizedVectorFromExplosionSource = (new Vector2(shatterObject.position.x - explosionSource.x, shatterObject.position.y -
                                explosionSource.y)).normalized;
                        if (shatterObject.GetComponent<Rigidbody2D>() != null)
#if UNITY_6000_0_OR_NEWER
                            shatterObject.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(normalizedVectorFromExplosionSource.x *
                                    shatterDetails.explosionForce.x, normalizedVectorFromExplosionSource.y * shatterDetails.explosionForce.y);
#else
                            shatterObject.GetComponent<Rigidbody2D>().velocity = new Vector2(normalizedVectorFromExplosionSource.x *
                                    shatterDetails.explosionForce.x, normalizedVectorFromExplosionSource.y * shatterDetails.explosionForce.y);
#endif
                    }
                }
            }
        }

        /// <summary>
        /// Resets a previously shattered sprite. Reactivates the main sprite and deactivates the "shatter" pieces, placing them back at their original
        /// positions with their original rotations, ready for another shatter.
        /// </summary>
        public void reset() {
            if (error)
                return;
            SpriteRenderer parentSpriteRenderer = null;
            if (shatterGameObjectTransform != null)
                parentSpriteRenderer = shatterGameObjectTransform.parent.gameObject.GetComponent<SpriteRenderer>();
            if (shatterGameObjectTransform == null || parentSpriteRenderer == null)
                Debug.LogError("Sprite Shatter (" + name + "): The Sprite Shatter game object or its Sprite Renderer could not be found. Please initialise " +
                        "the \"Sprite Shatter\" component again.");
            else {
                parentSpriteRenderer.enabled = true;
                shatterGameObjectTransform.gameObject.SetActive(false);

                //Restore the original positions of the "shatter" pieces.
                for (int i = 0; i < shatterGameObjectTransform.childCount; i++) {
                    shatterGameObjectTransform.GetChild(i).transform.localPosition = originalShatterPieceLocations[i];
                    shatterGameObjectTransform.GetChild(i).transform.localRotation = originalShatterPieceRotations[i];
                }
            }
        }

        //Use the "ear clipping" method for generating triangles for a series of ordered vertices.
        ushort[] generateMeshTriangles(Vector2[] vertices) {

            //Construct a linked list of vertices and flag whether they are reflex.
            LinkedList<ushort> allVertices = new LinkedList<ushort>();
            for (int i = 0; i < vertices.Length; i++)
                allVertices.AddLast((ushort) (i + (MathsFunctions.isAngleReflex(vertices[(i + vertices.Length - 1) % vertices.Length],
                        vertices[(i + 1) % vertices.Length], vertices[i]) ? 32768 : 0)));

            //Initialise the triangles array.
            ushort[] triangles = new ushort[(vertices.Length - 2) * 3];
            int triangleIndex = 0;

            //Repeatedly loop over the linked list of vertices and use an "ear clipping" method to remove polygon ears. Loop while there is at least another
            //triangle remaining (and the algorithm hasn't failed).
            LinkedListNode<ushort> currentNode = allVertices.First;
            int failedAttempts = 0;
            while (allVertices.Count > 2 && failedAttempts < allVertices.Count) {

                //Get the next convex point.
                while (currentNode.Value >= 32768 && failedAttempts < allVertices.Count) {
                    currentNode = currentNode.Next;
                    if (currentNode == null)
                        currentNode = allVertices.First;
                    failedAttempts++;
                }
                if (failedAttempts == allVertices.Count)
                    break;

                //Check that no other (reflex) points are in the triangle made by this convex point and its neighbours. If there are no other points inside the
                //triangle, we have an ear.
                ushort previousIndex = (ushort) ((currentNode.Previous == null ? allVertices.Last.Value : currentNode.Previous.Value) % 32768);
                ushort thisIndex = currentNode.Value;
                ushort nextIndex = (ushort) ((currentNode.Next == null ? allVertices.First.Value : currentNode.Next.Value) % 32768);
                bool ear = true;
                float minX = Mathf.Min(Mathf.Min(vertices[previousIndex].x, vertices[thisIndex].x), vertices[nextIndex].x);
                float maxX = Mathf.Max(Mathf.Max(vertices[previousIndex].x, vertices[thisIndex].x), vertices[nextIndex].x);
                float minY = Mathf.Min(Mathf.Min(vertices[previousIndex].y, vertices[thisIndex].y), vertices[nextIndex].y);
                float maxY = Mathf.Max(Mathf.Max(vertices[previousIndex].y, vertices[thisIndex].y), vertices[nextIndex].y);
                for (LinkedListNode<ushort> i = allVertices.First; i != null; i = i.Next) {
                    int vertexIndex = i.Value - 32768;
                    if (vertexIndex >= 0 && vertexIndex != previousIndex && vertexIndex != thisIndex && vertexIndex != nextIndex &&
                            vertices[vertexIndex].x >= minX && vertices[vertexIndex].x <= maxX &&
                            vertices[vertexIndex].y >= minY && vertices[vertexIndex].y <= maxY &&
                            MathsFunctions.pointIn2DTriangle(vertices[previousIndex].x, vertices[previousIndex].y, vertices[thisIndex].x, vertices[thisIndex].y,
                                    vertices[nextIndex].x, vertices[nextIndex].y, vertices[vertexIndex].x, vertices[vertexIndex].y)) {
                        ear = false;
                        break;
                    }
                }

                //Create the ear triangle and remove the vertex at the centre of it.
                if (ear) {
                    triangles[triangleIndex++] = nextIndex;
                    triangles[triangleIndex++] = thisIndex;
                    triangles[triangleIndex++] = previousIndex;
                    LinkedListNode<ushort> newNodeToCalculate = (currentNode.Previous == null ? allVertices.Last : currentNode.Previous);
                    allVertices.Remove(currentNode);
                    if (allVertices.Count <= 2)
                        break;
                    for (int i = 0; i < 2; i++) {
                        if (newNodeToCalculate.Value >= 32768 &&
                                !MathsFunctions.isAngleReflex(vertices[(newNodeToCalculate.Previous == null ? allVertices.Last.Value :
                                newNodeToCalculate.Previous.Value) % 32768], vertices[(newNodeToCalculate.Next == null ? allVertices.First.Value :
                                newNodeToCalculate.Next.Value) % 32768], vertices[newNodeToCalculate.Value - 32768]))
                            newNodeToCalculate.Value -= 32768;
                        if (i == 0)
                            newNodeToCalculate = (newNodeToCalculate.Next == null ? allVertices.First : newNodeToCalculate.Next);
                    }
                    currentNode = newNodeToCalculate;
                    failedAttempts = 0;
                }
                else
                    currentNode = currentNode.Next == null ? allVertices.First : currentNode.Next;
            }

            //Return the triangles array.
            return triangles;
        }

        //Randomize shatter shapes.
        void randomizePoints(System.Random random, Vector2[][] shapes, float randomMultiplier) {
            if (shatterDetails.randomness > 0.0001f) {
                bool[][][] shapesProcessed = new bool[2][][];
                shapesProcessed[0] = new bool[shapes.Length][];
                shapesProcessed[1] = new bool[shapes.Length][];
                for (int k = 0; k < 2; k++)
                    for (int i = 0; i < shapes.Length; i++) {
                        shapesProcessed[k][i] = new bool[shapes[i].Length];
                        for (int j = 0; j < shapesProcessed[k][i].Length; j++)
                            shapesProcessed[k][i][j] = false;
                    }
                randomMultiplier *= shatterDetails.randomness;
                for (int i = 0; i < shapes.Length; i++)
                    for (int j = 0; j < shapes[i].Length; j++) {
                        float x = shapes[i][j].x;
                        float y = shapes[i][j].y;
                        if (!shapesProcessed[0][i][j] && x > 0.000001f && x < 0.999999f) {
                            float randomChange = (float) ((random.NextDouble() - 0.5) * randomMultiplier);
                            for (int k = 0; k < shapes.Length; k++)
                                for (int l = 0; l < shapes[k].Length; l++)
                                    if (!shapesProcessed[0][k][l] && Math.Abs(shapes[k][l].x - x) < 0.000001f && Math.Abs(shapes[k][l].y - y) < 0.000001f) {
                                        shapesProcessed[0][k][l] = true;
                                        shapes[k][l].x += randomChange;
                                    }
                        }
                        if (!shapesProcessed[1][i][j] && y > 0.000001f && y < 0.999999f) {
                            float randomChange = (float) ((random.NextDouble() - 0.5) * randomMultiplier);
                            for (int k = 0; k < shapes.Length; k++)
                                for (int l = 0; l < shapes[k].Length; l++)
                                    if (!shapesProcessed[1][k][l] && Math.Abs(shapes[k][l].x - x) < 0.000001f && Math.Abs(shapes[k][l].y - y) < 0.000001f) {
                                        shapesProcessed[1][k][l] = true;
                                        shapes[k][l].y += randomChange;
                                    }
                        }
                    }
            }
        }

        //Upgrade Sprite Shatter to the latest version.
        public bool needsUpgrading() => shatterDetails.version != version;
        public void upgrade(bool log) {
            string oldVersion = shatterDetails.version;
            string newVersion = oldVersion;

            //Perform the upgrade steps between the various versions.
            if (newVersion == "") {
                switch ((int) shatterDetails.colliderType) {
                    case 1:
                        shatterDetails.colliderType = ColliderType.CircleMediumQuality;
                        break;
                    case 2:
                        shatterDetails.colliderType = ColliderType.BoxMediumQuality;
                        break;
                    case 3:
                        shatterDetails.colliderType = ColliderType.PolygonMediumQuality;
                        break;
                }
                try {
                    float zigzagFrequency = (float) typeof(ShatterDetails).GetField("zigzagFrequency").GetValue(shatterDetails);
                    shatterDetails.horizontalZigzagPoints = Mathf.RoundToInt((1f / (1 - (zigzagFrequency * 0.975f))) / shatterDetails.horizontalCuts);
                    shatterDetails.verticalZigzagPoints = Mathf.RoundToInt((1f / (1 - (zigzagFrequency * 0.975f))) / shatterDetails.verticalCuts);
                }
                catch {
                    shatterDetails.horizontalZigzagPoints = 0;
                    shatterDetails.verticalZigzagPoints = 0;
                }
                try {
                    float zigzagAmplitude = (float) typeof(ShatterDetails).GetField("zigzagAmplitude").GetValue(shatterDetails);
                    shatterDetails.horizontalZigzagSize = zigzagAmplitude;
                    shatterDetails.verticalZigzagSize = zigzagAmplitude;
                }
                catch {
                    shatterDetails.horizontalZigzagSize = 0;
                    shatterDetails.verticalZigzagSize = 0;
                }
                newVersion = "2.0.0";
            }
            if (newVersion == "2.0.0")
                newVersion = "2.0.1";
            if (newVersion == "2.0.1")
                newVersion = "2.0.2";
            if (newVersion == "2.0.2")
                newVersion = "2.0.3";
            if (newVersion == "2.0.3")
                newVersion = "2.0.4";
            if (newVersion == "2.0.4") {
                shatterDetails.triangleOrientation = TriangleOrientation.Horizontal;
                shatterDetails.horizontalTriangles = 4;
                shatterDetails.verticalTriangles = 4;
                newVersion = "2.0.5";
            }

            //If the version number has changed, set it in the shatter details and indicate the upgrade has occurred in the debug log if required.
            if (oldVersion != newVersion) {
                shatterDetails.version = newVersion;
                if (log)
                    Debug.Log("Upgraded Sprite Shatter component from version " + (oldVersion == "" ? "Pre-2.0.0" : oldVersion) + " to " + newVersion + ".");
            }
        }
    }
}