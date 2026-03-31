using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using OpenCvSharp;

public class CollisionImageItem : MonoBehaviour
{
    [Header("文件设置")]
    [SerializeField] private string fileName;
    [SerializeField] private Sprite sourceSprite;

    [Header("碰撞设置")]
    [Range(0, 255)]
    [SerializeField] private float threshold = 128f;
    [Range(0, 10)]
    [SerializeField] private float simplification = 2f;

    [Header("调试")]
    [SerializeField] private bool showDebugInfo;
    [SerializeField] private int contourPointsCount;

    private SpriteRenderer _spriteRenderer;
    private PolygonCollider2D _collider;
    private DateTime _lastModifiedTime;
    private Vector2 _originalImageSize;
    private string _folderPath;
    private LevelFileManager _manager;
    private Texture2D _currentTexture;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<PolygonCollider2D>();

        if (_spriteRenderer == null)
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (_collider == null)
        {
            _collider = gameObject.AddComponent<PolygonCollider2D>();
        }
    }

    public void SetManager(LevelFileManager manager)
    {
        _manager = manager;
        string gameRoot = Directory.GetParent(Application.dataPath).FullName;
        _folderPath = Path.Combine(gameRoot, "level", manager.GetLevelIndex().ToString());

        if (!Directory.Exists(_folderPath))
        {
            Directory.CreateDirectory(_folderPath);
        }
    }

    public void Initialize()
    {
        string fullPath = Path.Combine(_folderPath, $"{fileName}.png");

        if (!File.Exists(fullPath))
        {
            CreateDefaultImage();
        }

        LoadImageFromFile(fullPath);
        ProcessImageToCollision();
    }

    public void CheckImageFile(string folderPath)
    {
        string fullPath = Path.Combine(folderPath, $"{fileName}.png");

        if (!File.Exists(fullPath))
        {
            CreateDefaultImage();
            return;
        }

        DateTime newTime = File.GetLastWriteTime(fullPath);
        if (newTime != _lastModifiedTime)
        {
            _lastModifiedTime = newTime;
            LoadImageFromFile(fullPath);
            ProcessImageToCollision();
        }
    }

    private void CreateDefaultImage()
    {
        if (sourceSprite == null)
        {
            Debug.LogWarning($"[CollisionImageItem] {gameObject.name}: Source Sprite is null, cannot create default image");
            return;
        }

        string fullPath = Path.Combine(_folderPath, $"{fileName}.png");

        if (File.Exists(fullPath)) return;

        try
        {
            Texture2D tex = sourceSprite.texture;
            Color32[] pixels = tex.GetPixels32();

            Texture2D newTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            newTex.SetPixels32(pixels);
            newTex.Apply();

            byte[] bytes = newTex.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);

            DestroyImmediate(newTex);

            Debug.Log($"[CollisionImageItem] Created default image: {fullPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[CollisionImageItem] Failed to create default image: {e.Message}");
        }
    }

    private void LoadImageFromFile(string fullPath)
    {
        try
        {
            byte[] fileBytes = File.ReadAllBytes(fullPath);

            if (_currentTexture != null)
            {
                Destroy(_currentTexture);
            }

            _currentTexture = new Texture2D(2, 2);
            _currentTexture.LoadImage(fileBytes);

            if (_originalImageSize == Vector2.zero)
            {
                _originalImageSize = new Vector2(_currentTexture.width, _currentTexture.height);
            }

            Vector2 currentSize = new Vector2(_currentTexture.width, _currentTexture.height);
            if (currentSize != _originalImageSize && _originalImageSize != Vector2.zero)
            {
                Vector2 scaleRatio = new Vector2(
                    _originalImageSize.x / currentSize.x,
                    _originalImageSize.y / currentSize.y
                );
                transform.localScale = new Vector3(scaleRatio.x, scaleRatio.y, 1f);
            }

            if (_spriteRenderer != null)
            {
                UnityEngine.Rect rect = new UnityEngine.Rect(0, 0, _currentTexture.width, _currentTexture.height);
                Vector2 pivot = new Vector2(0.5f, 0.5f);
                Sprite newSprite = Sprite.Create(_currentTexture, rect, pivot, 100f);
                _spriteRenderer.sprite = newSprite;
            }

            Debug.Log($"[CollisionImageItem] Loaded image: {fullPath} ({_currentTexture.width}x{_currentTexture.height})");
        }
        catch (Exception e)
        {
            Debug.LogError($"[CollisionImageItem] Failed to load image: {e.Message}");
        }
    }

    private void ProcessImageToCollision()
    {
        if (_currentTexture == null)
        {
            Debug.LogWarning("[CollisionImageItem] No texture to process");
            return;
        }

        if (_collider == null)
        {
            Debug.LogWarning("[CollisionImageItem] No PolygonCollider2D found");
            return;
        }

        Mat image = null;
        Mat gray = null;
        Mat binary = null;
        Point[][] contours;

        try
        {
            Color32[] pixels = _currentTexture.GetPixels32();
            int width = _currentTexture.width;
            int height = _currentTexture.height;

            byte[] bytes = new byte[pixels.Length * 4];
            for (int i = 0; i < pixels.Length; i++)
            {
                bytes[i * 4] = pixels[i].b;
                bytes[i * 4 + 1] = pixels[i].g;
                bytes[i * 4 + 2] = pixels[i].r;
                bytes[i * 4 + 3] = pixels[i].a;
            }

            image = new Mat(height, width, MatType.CV_8UC4, bytes);

            gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGRA2GRAY);

            binary = new Mat();
            Cv2.Threshold(gray, binary, threshold, 255, ThresholdTypes.BinaryInv);

            Cv2.FindContours(binary, out contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            if (contours == null || contours.Length == 0)
            {
                _collider.pathCount = 0;
                contourPointsCount = 0;
                Debug.Log("[CollisionImageItem] No dark regions found, collision cleared");
                return;
            }

            List<Point> allPoints = new List<Point>();
            foreach (Point[] contour in contours)
            {
                if (contour.Length < 3) continue;

                Point[] simplified = Cv2.ApproxPolyDP(
                    contour,
                    simplification,
                    true
                );

                if (simplified.Length >= 3)
                {
                    allPoints.AddRange(simplified);
                }
            }

            if (allPoints.Count < 3)
            {
                _collider.pathCount = 0;
                contourPointsCount = 0;
                Debug.Log("[CollisionImageItem] Not enough points after simplification");
                return;
            }

            Point[] hull = Cv2.ConvexHull(allPoints.ToArray());

            Vector2[] points = ConvertPointsToUnity(hull, width, height);

            _collider.pathCount = 1;
            _collider.SetPath(0, points);

            contourPointsCount = points.Length;

            if (showDebugInfo)
            {
                Debug.Log($"[CollisionImageItem] Generated collision with {points.Length} points from {contours.Length} contours");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CollisionImageItem] Failed to process image: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            image?.Dispose();
            gray?.Dispose();
            binary?.Dispose();
        }
    }

    private Vector2[] ConvertPointsToUnity(Point[] points, int imageWidth, int imageHeight)
    {
        Vector2[] result = new Vector2[points.Length];

        float spriteWidth = _originalImageSize.x;
        float spriteHeight = _originalImageSize.y;

        for (int i = 0; i < points.Length; i++)
        {
            float x = (points[i].X / (float)imageWidth - 0.5f) * spriteWidth;
            float y = (points[i].Y / (float)imageHeight - 0.5f) * spriteHeight;
            result[i] = new Vector2(x, -y);
        }

        return result;
    }

    void OnDestroy()
    {
        if (_currentTexture != null)
        {
            Destroy(_currentTexture);
        }
    }

    void OnValidate()
    {
        if (_collider != null && showDebugInfo)
        {
            contourPointsCount = 0;
            for (int i = 0; i < _collider.pathCount; i++)
            {
                contourPointsCount += _collider.GetPath(i).Length;
            }
        }
    }
}
