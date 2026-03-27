using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using OpenCvSharp;

public class ImageColliderFile : MonoBehaviour
{
    [Header("文件设置")]
    [SerializeField] private string fileName;
    [SerializeField] private float checkInterval = 0.5f;

    [Header("Resource 设置")]
    [SerializeField] private string resourcePath;
    [SerializeField] private bool useResourceAsDefault = true;

    [Header("碰撞箱设置")]
    [SerializeField] private Transform colliderChild;
    [SerializeField] private bool autoAddCollider = false;

    [Header("碰撞设置")]
    [Range(0, 255)]
    [SerializeField] private float threshold = 128f;
    [Range(0, 10)]
    [SerializeField] private float curveAccuracy = 2f;
    [SerializeField] private float minArea = 100f;

    [Header("调试")]
    [SerializeField] private bool showDebugInfo;
    [SerializeField] private int contourCount;
    [SerializeField] private int totalPointsCount;

    private SpriteRenderer _spriteRenderer;
    private PolygonCollider2D _collider;
    private DateTime _lastModifiedTime;
    private Vector2 _originalImageSize;
    private string _folderPath;
    private LevelFileManager _manager;
    private Texture2D _currentTexture;
    private string _lastFileHash;
    private float _timer = 0f;
    private bool _isInitialized;
    private bool _isFileActive = true;
    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (colliderChild != null)
        {
            _collider = colliderChild.GetComponent<PolygonCollider2D>();
            if (_collider == null && autoAddCollider)
            {
                _collider = colliderChild.gameObject.AddComponent<PolygonCollider2D>();
            }
        }
        else
        {
            _collider = GetComponent<PolygonCollider2D>();
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<PolygonCollider2D>();
            }
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
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = gameObject.name;
        }

        string fullPath = Path.Combine(_folderPath, $"{fileName}.png");

        if (File.Exists(fullPath))
        {
            _lastModifiedTime = File.GetLastWriteTime(fullPath);
            _lastFileHash = CalculateFileHash(fullPath);
            LoadImageFromFile(fullPath);
        }
        else if (useResourceAsDefault && !string.IsNullOrEmpty(resourcePath))
        {
            Sprite resourceSprite = ResourceImageLoader.LoadSpriteFromResource(resourcePath);
            if (resourceSprite != null)
            {
                Texture2D tex = ResourceImageLoader.SpriteToTexture2D(resourceSprite);
                if (tex != null)
                {
                    _currentTexture = tex;
                    
                    if (_originalImageSize == Vector2.zero)
                    {
                        _originalImageSize = new Vector2(_currentTexture.width, _currentTexture.height);
                    }
                    
                    if (_spriteRenderer != null)
                    {
                        UnityEngine.Rect rect = new UnityEngine.Rect(0, 0, _currentTexture.width, _currentTexture.height);
                        Vector2 pivot = new Vector2(0.5f, 0.5f);
                        Sprite newSprite = Sprite.Create(_currentTexture, rect, pivot, 100f);
                        _spriteRenderer.sprite = newSprite;
                    }
                    
                    ResourceImageLoader.SaveTextureAsPng(_currentTexture, fullPath);
                    _lastModifiedTime = File.GetLastWriteTime(fullPath);
                    _lastFileHash = CalculateFileHash(fullPath);
                    Debug.Log($"[ImageColliderFile] Loaded from Resource and created external file: {fullPath}");
                }
            }
            else
            {
                Debug.LogWarning($"[ImageColliderFile] Resource not found: {resourcePath}, skipping initialization");
                _isInitialized = true;
                return;
            }
        }
        else
        {
            Debug.LogWarning($"[ImageColliderFile] No external file and Resource not configured, skipping initialization");
            _isInitialized = true;
            return;
        }

        ProcessImageToCollision();
        _isInitialized = true;
    }

    void Update()
    {
        if (!_isInitialized || _manager == null) return;

        _timer += Time.deltaTime;
        if (_timer >= checkInterval)
        {
            _timer = 0f;
            CheckImageFile(_folderPath);
        }
    }

    public void CheckImageFile(string folderPath)
    {
        string fullPath = Path.Combine(folderPath, $"{fileName}.png");

        if (!File.Exists(fullPath))
        {
            if (_isFileActive)
            {
                OnFileDeleted();
                _isFileActive = false;
            }
            return;
        }

        if (!_isFileActive)
        {
            OnFileCreated();
            _isFileActive = true;
        }

        string currentHash = CalculateFileHash(fullPath);
        if (currentHash != _lastFileHash)
        {
            _lastFileHash = currentHash;
            _lastModifiedTime = File.GetLastWriteTime(fullPath);
            LoadImageFromFile(fullPath);
            ProcessImageToCollision();
            OnFileUpdated();
        }
    }

    private string CalculateFileHash(string filePath)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            byte[] hashBytes = md5.ComputeHash(fileBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    private void CreateDefaultImage()
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            Debug.LogWarning($"[ImageColliderFile] {gameObject.name}: Resource path is not set, cannot create default image");
            return;
        }

        string fullPath = Path.Combine(_folderPath, $"{fileName}.png");

        if (File.Exists(fullPath)) return;

        Sprite resourceSprite = ResourceImageLoader.LoadSpriteFromResource(resourcePath);
        if (resourceSprite == null)
        {
            Debug.LogError($"[ImageColliderFile] {gameObject.name}: Failed to load Sprite from Resource: {resourcePath}");
            return;
        }

        ResourceImageLoader.SaveSpriteAsPng(resourceSprite, fullPath);
        
        if (File.Exists(fullPath))
        {
            _lastModifiedTime = File.GetLastWriteTime(fullPath);
            _lastFileHash = CalculateFileHash(fullPath);
        }
    }

    private void LoadImageFromFile(string fullPath)
    {
        try
        {
            gameObject.SetActive(true);

            byte[] fileBytes = File.ReadAllBytes(fullPath);

            if (_currentTexture != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_currentTexture);
                }
                else
                {
                    DestroyImmediate(_currentTexture);
                }
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
                    currentSize.x / _originalImageSize.x,
                    currentSize.y / _originalImageSize.y
                );
                transform.localScale = new Vector3(scaleRatio.x, scaleRatio.y, 1f);
            }

            if (_spriteRenderer != null)
            {
                UnityEngine.Rect rect = new UnityEngine.Rect(0, 0, _currentTexture.width, _currentTexture.height);
                Vector2 pivot = new(0.5f, 0.5f);
                float pixelsPerUnit = 100f;
                Sprite newSprite = Sprite.Create(_currentTexture, rect, pivot, pixelsPerUnit);
                _spriteRenderer.sprite = newSprite;
            }

            Debug.Log($"[ImageColliderFile] Loaded image: {fullPath} ({_currentTexture.width}x{_currentTexture.height})");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ImageColliderFile] Failed to load image: {e.Message}");
        }
    }

    private void ProcessImageToCollision()
    {
        if (_currentTexture == null)
        {
            Debug.LogWarning("[ImageColliderFile] No texture to process");
            return;
        }

        if (_collider == null)
        {
            Debug.LogWarning("[ImageColliderFile] No PolygonCollider2D found");
            return;
        }

        Mat image = null;
        Mat gray = null;
        Mat binary = null;
        Point[][] contours;
        HierarchyIndex[] hierarchy;

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

            Cv2.FindContours(binary, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            if (contours == null || contours.Length == 0)
            {
                _collider.pathCount = 0;
                contourCount = 0;
                totalPointsCount = 0;
                Debug.Log("[ImageColliderFile] No dark regions found, collision cleared");
                return;
            }

            List<Vector2[]> allPaths = new List<Vector2[]>();

            foreach (Point[] contour in contours)
            {
                if (contour.Length < 3) continue;

                double area = Cv2.ContourArea(contour);
                if (area < minArea) continue;

                Point[] simplified = Cv2.ApproxPolyDP(contour, curveAccuracy, true);

                if (simplified.Length >= 3)
                {
                    Vector2[] points = ConvertPointsToUnity(simplified, width, height);
                    allPaths.Add(points);
                }
            }

            _collider.pathCount = allPaths.Count;
            for (int i = 0; i < allPaths.Count; i++)
            {
                _collider.SetPath(i, allPaths[i]);
            }

            contourCount = allPaths.Count;
            totalPointsCount = 0;
            foreach (var path in allPaths)
            {
                totalPointsCount += path.Length;
            }

            if (showDebugInfo)
            {
                Debug.Log($"[ImageColliderFile] Generated {contourCount} collision paths with {totalPointsCount} total points");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ImageColliderFile] Failed to process image: {e.Message}\n{e.StackTrace}");
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

        for (int i = 0; i < points.Length; i++)
        {
            float x = (points[i].X / (float)imageWidth - 0.5f);
            float y = (points[i].Y / (float)imageHeight - 0.5f);
            result[i] = new Vector2(x, -y);
        }

        return result;
    }

    protected virtual void OnFileCreated()
    {
        gameObject.SetActive(true);
        Debug.Log($"[ImageColliderFile] File created: {gameObject.name}");
    }

    protected virtual void OnFileUpdated()
    {
        Debug.Log($"[ImageColliderFile] File updated: {gameObject.name}");
    }

    protected virtual void OnFileDeleted()
    {
        gameObject.SetActive(false);
        Debug.Log($"[ImageColliderFile] File deleted, object deactivated: {gameObject.name}");
    }

    void OnDestroy()
    {
        if (_currentTexture != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_currentTexture);
            }
            else
            {
                DestroyImmediate(_currentTexture);
            }
        }
    }

    void OnValidate()
    {
        if (_collider != null && showDebugInfo)
        {
            contourCount = _collider.pathCount;
            totalPointsCount = 0;
            for (int i = 0; i < _collider.pathCount; i++)
            {
                totalPointsCount += _collider.GetPath(i).Length;
            }
        }
    }
}
