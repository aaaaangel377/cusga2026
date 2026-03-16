using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

public class ItemController : MonoBehaviour
{
    [Header("File Settings")]
    [SerializeField] private string fileName;

    [Header("Attributes")]
    [SerializeField] private bool hasPosition = true;

    [SerializeField] private bool hasSpawn = false;

    [SerializeField] private bool hasSwitch = true;

    [SerializeField] private bool hasRotation = false;

    [SerializeField] private bool isCritical = false;

    [Header("References")]
    [SerializeField] private GameObject targetObject;

    private LevelFileManager _manager;

    private const int GRID_MAX_X = 11;
    private const int GRID_MAX_Y = 8;

    public string FileName => fileName;
    public bool IsCritical => isCritical;

    void Awake()
    {
        if (targetObject == null)
        {
            targetObject = this.gameObject;
        }
    }

    public void SetManager(LevelFileManager manager)
    {
        _manager = manager;
    }

    public void CreateDefaultFile(string folderPath)
    {
        string fullPath = Path.Combine(folderPath, $"{fileName}.txt");

        if (File.Exists(fullPath)) return;

        Vector3 currentPos = targetObject.transform.position;
        Vector2Int gridPos = ConvertToGridPosition(currentPos);
        string content = $"位置：{gridPos.x},{gridPos.y}";
        string rotationContent = $"旋转：{targetObject.transform.rotation.eulerAngles.z}";
        File.WriteAllText(fullPath, content);
        File.AppendAllText(fullPath, "\n" + rotationContent);
    }

    public void UpdateFromFile(string folderPath)
    {
        string fullPath = Path.Combine(folderPath, $"{fileName}.txt");
        bool exists = File.Exists(fullPath);

        if (!exists)
        {
            if (hasSwitch)
            {
                targetObject.SetActive(false);
            }
            return;
        }

        string content = File.ReadAllText(fullPath);

        if (hasSwitch)
        {
            targetObject.SetActive(true);
        }

        if (hasPosition)
        {
            Vector2Int? gridPos = ParsePosition(content);
            if (gridPos.HasValue && IsValidGrid(gridPos.Value))
            {
                Vector3 actualPos = ConvertToActualPosition(gridPos.Value);
                targetObject.transform.position = actualPos;
            }
        }

        if (hasRotation)
        {
            float? rotation = ParseRotation(content);
            if (rotation.HasValue)
            {
                targetObject.transform.rotation = Quaternion.Euler(0, 0, rotation.Value);
            }
        }
    }

    public void OnFileCopied(string newFileName, string content)
    {
        if (!hasSpawn) return;

        GameObject newObj = Instantiate(targetObject, targetObject.transform.parent);
        newObj.name = fileName + " - 副本";

        Vector2Int? gridPos = ParsePosition(content);
        if (gridPos.HasValue && IsValidGrid(gridPos.Value))
        {
            Vector3 actualPos = ConvertToActualPosition(gridPos.Value);
            newObj.transform.position = actualPos;
        }

        ItemController newItem = newObj.GetComponent<ItemController>();
        if (newItem == null)
        {
            newItem = newObj.AddComponent<ItemController>();
        }

        newItem.fileName = newFileName.Replace(".txt", "");
        newItem.hasSpawn = false;
        newItem.SetManager(_manager);
    }

    private Vector2Int? ParsePosition(string content)
    {
        if (string.IsNullOrEmpty(content)) return null;

        Match match = Regex.Match(content, @"位置：(-?\d+\.?\d*),(-?\d+\.?\d*)");

        if (match.Success)
        {
            float x = float.Parse(match.Groups[1].Value);
            float y = float.Parse(match.Groups[2].Value);

            if (IsInteger(x) && IsInteger(y))
            {
                return new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
            }
        }

        return null;
    }

    private float? ParseRotation(string content)
    {
        if (string.IsNullOrEmpty(content)) return null;

        Match match = Regex.Match(content, @"旋转：(-?\d+\.?\d*)");

        if (match.Success)
        {
            return float.Parse(match.Groups[1].Value);
        }

        return null;
    }

    private bool IsInteger(float value)
    {
        return Mathf.Approximately(value, Mathf.Round(value));
    }

    private bool IsValidGrid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x <= GRID_MAX_X &&
               pos.y >= 0 && pos.y <= GRID_MAX_Y;
    }

    private Vector3 ConvertToActualPosition(Vector2Int gridPos)
    {
        float actualX = (gridPos.x - 5.5f) * 1.1f;
        float actualY = (gridPos.y - 4f) * 1.1f;
        return new Vector3(actualX, actualY, 0);
    }

    private Vector2Int ConvertToGridPosition(Vector3 actualPos)
    {
        int gridX = Mathf.RoundToInt((actualPos.x / 1.1f) + 5.5f);
        int gridY = Mathf.RoundToInt((actualPos.y / 1.1f) + 4f);
        return new Vector2Int(gridX, gridY);
    }
}
