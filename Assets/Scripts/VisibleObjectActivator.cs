using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisibleObjectActivator : MonoBehaviour
{
    [SerializeField]
    private Camera _targetCamera;

    [SerializeField]
    private float _size = 100;

    [SerializeField]
    private int _depth = 5;

    [SerializeField]
    private bool _checkMovement;

    private List<GameObject>[,] _gameObjects;
    private List<GameObject> _sharedGameObjectsToRemove;

    private float Size
    {
        get => _size;
        set
        {
            _size = value;
            ChangeContainer(_size, _depth);
        }
    }
    public int Depth
    {
        get => _depth;
        set
        {
            _depth = value;
            ChangeContainer(_size, _depth);
        }
    }


    public Camera TargetCamera { get => _targetCamera; set => _targetCamera = value; }
    public bool CheckMovement { get => _checkMovement; set => _checkMovement = value; }

    private void Awake()
    {
        InitContainer(_size, _depth);

        var childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            var child = transform.GetChild(i).gameObject;

            if (CanAddGameObject(child))
            {
                AddGameObject(child);
            }
        }
    }

    /// <summary>
    /// 更新
    /// </summary>
    private void Update()
    {
        if (_gameObjects is not null)
        {
            var selfWorldX = transform.position.x;
            var selfWorldZ = transform.position.z;
            var containerArraySize = _gameObjects.GetLength(0);
            var blockSize = _size / containerArraySize;

            SetAreaActiveState(selfWorldX, selfWorldZ, _size / 2, blockSize, 0, containerArraySize, 0, containerArraySize);

            if (_checkMovement)
            {
                CheckObjectsMovement(containerArraySize);
            }
        }
    }

    /// <summary>
    /// 初始化容器
    /// </summary>
    /// <param name="size"></param>
    /// <param name="depth"></param>
    /// <exception cref="System.ArgumentOutOfRangeException"></exception>
    private void InitContainer(float size, int depth)
    {
        if (size <= 1)
        {
            throw new System.ArgumentOutOfRangeException(nameof(size));
        }

        if (depth < 1)
        {
            throw new System.ArgumentOutOfRangeException(nameof(depth));
        }

        var arraySize = 1;
        for (int i = 0; i < depth; i++)
        {
            arraySize *= 2;
        }

        _gameObjects = new List<GameObject>[arraySize, arraySize];

        for (int i = 0; i < arraySize; i++)
        {
            for (int j = 0; j < arraySize; j++)
            {
                _gameObjects[i, j] = new List<GameObject>();
            }
        }
    }

    /// <summary>
    /// 改变容器
    /// </summary>
    /// <param name="size"></param>
    /// <param name="depth"></param>
    private void ChangeContainer(float size, int depth)
    {
        if (_gameObjects is null)
        {
            InitContainer(size, depth);
            return;
        }

        var arraySize = _gameObjects.GetLength(0);
        var allGameObjects = new List<GameObject>();

        for (int i = 0; i < arraySize; i++)
        {
            for (int j = 0; j < arraySize; j++)
            {
                allGameObjects.AddRange(_gameObjects[i, j]);
            }
        }

        InitContainer(size, depth);
        foreach (var gameObject in allGameObjects)
        {
            AddGameObject(gameObject);
        }
    }

    /// <summary>
    /// 判断一个矩形是否在相机可视范围内
    /// </summary>
    /// <param name="point00"></param>
    /// <param name="point10"></param>
    /// <param name="point01"></param>
    /// <param name="point11"></param>
    /// <returns></returns>
    private bool RectInCamera(Vector2 point00, Vector2 point10, Vector2 point01, Vector2 point11)
    {
        // 来自 https://blog.csdn.net/YasinXin/article/details/105918308 的魔法代码
        static int ComputeOutCode(Camera camera, Vector4 projectionPos)
        {
            int _code = 0;
            if (projectionPos.x < -projectionPos.w)
                _code |= 1;
            if (projectionPos.x > projectionPos.w)
                _code |= 2;
            if (projectionPos.y < -projectionPos.w)
                _code |= 4;
            if (projectionPos.y > projectionPos.w)
                _code |= 8;
            if (projectionPos.z < camera.nearClipPlane)
                _code |= 16;
            if (projectionPos.z > camera.farClipPlane)
                _code |= 32;

            return _code;
        }


        Camera camera = _targetCamera;

        if (camera == null)
        {
            camera = Camera.main;
        }

        if (camera == null)
        {
            return false;
        }

        int outCode = 63;
        outCode &= ComputeOutCode(camera, camera.projectionMatrix * camera.worldToCameraMatrix * new Vector4(point00.x, 0, point00.y, 1));
        outCode &= ComputeOutCode(camera, camera.projectionMatrix * camera.worldToCameraMatrix * new Vector4(point10.x, 0, point10.y, 1));
        outCode &= ComputeOutCode(camera, camera.projectionMatrix * camera.worldToCameraMatrix * new Vector4(point01.x, 0, point01.y, 1));
        outCode &= ComputeOutCode(camera, camera.projectionMatrix * camera.worldToCameraMatrix * new Vector4(point11.x, 0, point11.y, 1));

        return outCode == 0;
    }

    private void SetAreaActiveState(
        float selfWorldX, float selfWorldZ,
        float halfSize, float blockSize,
        int iStart, int iEnd,
        int jStart, int jEnd)
    {
        if (_sharedGameObjectsToRemove is null)
        {
            _sharedGameObjectsToRemove = new();
        }

        Vector2 point00 = new Vector2(selfWorldX + blockSize * iStart - halfSize, selfWorldZ + blockSize * jStart - halfSize);
        Vector2 point10 = new Vector2(selfWorldX + blockSize * iEnd - halfSize, selfWorldZ + blockSize * jStart - halfSize);
        Vector2 point01 = new Vector2(selfWorldX + blockSize * iStart - halfSize, selfWorldZ + blockSize * jEnd - halfSize);
        Vector2 point11 = new Vector2(selfWorldX + blockSize * iEnd - halfSize, selfWorldZ + blockSize * jEnd - halfSize);

        var inCamera = RectInCamera(point00, point10, point01, point11);

        if (inCamera)
        {
            if (iEnd - iStart <= 1 ||
                jEnd - jStart <= 1)
            {
                for (int i = iStart; i < iEnd; i++)
                {
                    for (int z = jStart; z < jEnd; z++)
                    {
                        var currentBlockGameObjects = _gameObjects[i, z];

                        foreach (var gameObject in currentBlockGameObjects)
                        {
                            if (gameObject != null)
                            {
                                gameObject.SetActive(true);
                            }
                            else
                            {
                                _sharedGameObjectsToRemove.Add(gameObject);
                            }
                        }

                        foreach (var gameObject in _sharedGameObjectsToRemove)
                        {
                            currentBlockGameObjects.Remove(gameObject);
                        }

                        _sharedGameObjectsToRemove.Clear();
                    }
                }
            }
            else
            {
                var iOffset = iEnd - iStart;
                var jOffset = jEnd - jStart;
                var iHalfOffset = iOffset / 2;
                var jHalfOffset = jOffset / 2;

                SetAreaActiveState(selfWorldX, selfWorldZ, halfSize, blockSize, iStart, iStart + iHalfOffset, jStart, jStart + jHalfOffset);
                SetAreaActiveState(selfWorldX, selfWorldZ, halfSize, blockSize, iStart + iHalfOffset, iEnd, jStart, jStart + jHalfOffset);
                SetAreaActiveState(selfWorldX, selfWorldZ, halfSize, blockSize, iStart, iStart + iHalfOffset, jStart + jHalfOffset, jEnd);
                SetAreaActiveState(selfWorldX, selfWorldZ, halfSize, blockSize, iStart + iHalfOffset, iEnd, jStart + jHalfOffset, jEnd);
            }
        }
        else
        {
            for (int i = iStart; i < iEnd; i++)
            {
                for (int z = jStart; z < jEnd; z++)
                {
                    var currentBlockGameObjects = _gameObjects[i, z];

                    foreach (var gameObject in currentBlockGameObjects)
                    {
                        if (gameObject != null)
                        {
                            gameObject.SetActive(false);
                        }
                        else
                        {
                            _sharedGameObjectsToRemove.Add(gameObject);
                        }
                    }

                    foreach (var gameObject in _sharedGameObjectsToRemove)
                    {
                        currentBlockGameObjects.Remove(gameObject);
                    }

                    _sharedGameObjectsToRemove.Clear();
                }
            }
        }
    }

    private void CheckObjectsMovement(int containerArraySize)
    {
        if (_sharedGameObjectsToRemove is null)
        {
            _sharedGameObjectsToRemove = new();
        }

        for (int i = 0; i < containerArraySize; i++)
        {
            for (int j = 0; j < containerArraySize; j++)
            {
                var currentBlockGameObjects = _gameObjects[i, j];

                _sharedGameObjectsToRemove.Clear();
                foreach (var gameObject in currentBlockGameObjects)
                { 
                    if (!CalculateBestBlock(gameObject, out var newI, out var newJ))
                    {
                        _sharedGameObjectsToRemove.Add(gameObject);
                        continue;
                    }

                    if (newI != i ||
                        newJ != j)
                    {
                        _sharedGameObjectsToRemove.Add(gameObject);
                        AddGameObject(gameObject);
                    }
                }

                foreach (var gameObject in _sharedGameObjectsToRemove)
                {
                    currentBlockGameObjects.Remove(gameObject);
                }
            }
        }
    }

    private void GizmosArea(
        float selfWorldX, float selfWorldY, float selfWorldZ,
        float halfSize, float blockSize,
        int iStart, int iEnd,
        int jStart, int jEnd)
    {
        Vector2 point00 = new Vector2(selfWorldX + blockSize * iStart - halfSize, selfWorldZ + blockSize * jStart - halfSize);
        Vector2 point10 = new Vector2(selfWorldX + blockSize * iEnd - halfSize, selfWorldZ + blockSize * jStart - halfSize);
        Vector2 point01 = new Vector2(selfWorldX + blockSize * iStart - halfSize, selfWorldZ + blockSize * jEnd - halfSize);
        Vector2 point11 = new Vector2(selfWorldX + blockSize * iEnd - halfSize, selfWorldZ + blockSize * jEnd - halfSize);

        var inCamera = RectInCamera(point00, point10, point01, point11);

        if (inCamera)
        {
            if (iEnd - iStart <= 1 ||
                jEnd - jStart <= 1)
            {
                var centerPoint = new Vector3(selfWorldX + blockSize * (((float)iEnd + iStart) / 2) - halfSize, selfWorldY, selfWorldZ + blockSize * (((float)jEnd + jStart) / 2) - halfSize);
                var size = new Vector3(blockSize * (iEnd - iStart), 0, blockSize * (jEnd - jStart));

                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(centerPoint, size);
            }
            else
            {
                var iOffset = iEnd - iStart;
                var jOffset = jEnd - jStart;
                var iHalfOffset = iOffset / 2;
                var jHalfOffset = jOffset / 2;

                GizmosArea(selfWorldX, selfWorldY, selfWorldZ, halfSize, blockSize, iStart, iStart + iHalfOffset, jStart, jStart + jHalfOffset);
                GizmosArea(selfWorldX, selfWorldY, selfWorldZ, halfSize, blockSize, iStart + iHalfOffset, iEnd, jStart, jStart + jHalfOffset);
                GizmosArea(selfWorldX, selfWorldY, selfWorldZ, halfSize, blockSize, iStart, iStart + iHalfOffset, jStart + jHalfOffset, jEnd);
                GizmosArea(selfWorldX, selfWorldY, selfWorldZ, halfSize, blockSize, iStart + iHalfOffset, iEnd, jStart + jHalfOffset, jEnd);
            }
        }
        else
        {
            var centerPoint = new Vector3(selfWorldX + blockSize * (((float)iEnd + iStart) / 2) - halfSize, selfWorldY, selfWorldZ + blockSize * (((float)jEnd + jStart) / 2) - halfSize);
            var size = new Vector3(blockSize * (iEnd - iStart), 0, blockSize * (jEnd - jStart));

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(centerPoint, size);
        }
    }

    public bool CalculateBestBlock(GameObject gameObject, out int i, out int j)
    {
        if (_gameObjects is null)
        {
            i = 0;
            j = 0;
            return false;
        }

        var relativePosition = gameObject.transform.position - transform.position;

        var halfSize = _size / 2;
        var containerArraySize = _gameObjects.GetLength(0);
        var blockSize = _size / containerArraySize;
        i = (int)((relativePosition.x + halfSize) / blockSize);
        j = (int)((relativePosition.z + halfSize) / blockSize);

        if (i < 0 || i >= containerArraySize ||
            j < 0 || j >= containerArraySize)
        {
            return false;
        }

        return true;
    }

    public bool CanAddGameObject(GameObject gameObject)
    {
        return CalculateBestBlock(gameObject, out _, out _);
    }

    public void AddGameObject(GameObject gameObject)
    {
        if (!CalculateBestBlock(gameObject, out var i, out var j))
        {
            throw new System.InvalidOperationException();
        }

        _gameObjects[i, j].Add(gameObject);
    }




    private void OnDrawGizmos()
    {
        var containerArraySize = 1;
        for (int i = 0; i < _depth; i++)
        {
            containerArraySize *= 2;
        }

        var selfWorldX = transform.position.x;
        var selfWorldY = transform.position.y;
        var selfWorldZ = transform.position.z;
        var blockSize = _size / containerArraySize;

        GizmosArea(selfWorldX, selfWorldY, selfWorldZ, _size / 2, blockSize, 0, containerArraySize, 0, containerArraySize);
    }
}
