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

    private List<Renderer>[,] _gameObjectRenderers;
    private List<Renderer> _sharedGameObjectRenderersToRemove;

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
            var renderer = child.GetComponent<Renderer>();

            if (renderer == null)
            {
                continue;
            }

            if (CanAddRenderer(renderer))
            {
                AddRenderer(renderer);
            }
        }
    }

    /// <summary>
    /// 更新
    /// </summary>
    private void Update()
    {
        if (_gameObjectRenderers is not null)
        {
            var selfWorldX = transform.position.x;
            var selfWorldZ = transform.position.z;
            var containerArraySize = _gameObjectRenderers.GetLength(0);
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

        _gameObjectRenderers = new List<Renderer>[arraySize, arraySize];

        for (int i = 0; i < arraySize; i++)
        {
            for (int j = 0; j < arraySize; j++)
            {
                _gameObjectRenderers[i, j] = new List<Renderer>();
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
        if (_gameObjectRenderers is null)
        {
            InitContainer(size, depth);
            return;
        }

        var arraySize = _gameObjectRenderers.GetLength(0);
        var allGameObjectRenderers = new List<Renderer>();

        for (int i = 0; i < arraySize; i++)
        {
            for (int j = 0; j < arraySize; j++)
            {
                allGameObjectRenderers.AddRange(_gameObjectRenderers[i, j]);
            }
        }

        InitContainer(size, depth);
        foreach (var renderer in allGameObjectRenderers)
        {
            AddRenderer(renderer);
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
        if (_sharedGameObjectRenderersToRemove is null)
        {
            _sharedGameObjectRenderersToRemove = new();
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
                        var currentBlockGameObjects = _gameObjectRenderers[i, z];

                        foreach (var renderer in currentBlockGameObjects)
                        {
                            if (renderer != null)
                            {
                                renderer.enabled = true;
                            }
                            else
                            {
                                _sharedGameObjectRenderersToRemove.Add(renderer);
                            }
                        }

                        foreach (var gameObject in _sharedGameObjectRenderersToRemove)
                        {
                            currentBlockGameObjects.Remove(gameObject);
                        }

                        _sharedGameObjectRenderersToRemove.Clear();
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
                    var currentBlockGameObjectRenderers = _gameObjectRenderers[i, z];

                    foreach (var renderer in currentBlockGameObjectRenderers)
                    {
                        if (renderer != null)
                        {
                            renderer.enabled = false;
                        }
                        else
                        {
                            _sharedGameObjectRenderersToRemove.Add(renderer);
                        }
                    }

                    foreach (var gameObject in _sharedGameObjectRenderersToRemove)
                    {
                        currentBlockGameObjectRenderers.Remove(gameObject);
                    }

                    _sharedGameObjectRenderersToRemove.Clear();
                }
            }
        }
    }

    private void CheckObjectsMovement(int containerArraySize)
    {
        if (_sharedGameObjectRenderersToRemove is null)
        {
            _sharedGameObjectRenderersToRemove = new();
        }

        for (int i = 0; i < containerArraySize; i++)
        {
            for (int j = 0; j < containerArraySize; j++)
            {
                var currentBlockGameObjects = _gameObjectRenderers[i, j];

                _sharedGameObjectRenderersToRemove.Clear();
                foreach (var renderer in currentBlockGameObjects)
                {
                    if (!CalculateBestBlock(renderer, out var newI, out var newJ))
                    {
                        _sharedGameObjectRenderersToRemove.Add(renderer);
                        continue;
                    }

                    if (newI != i ||
                        newJ != j)
                    {
                        _sharedGameObjectRenderersToRemove.Add(renderer);
                        AddRenderer(renderer);
                    }
                }

                foreach (var renderer in _sharedGameObjectRenderersToRemove)
                {
                    currentBlockGameObjects.Remove(renderer);
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

            if (enabled)
            {
                Gizmos.color = Color.red;
            }
            else
            {
                Gizmos.color = Color.yellow;
            }

            Gizmos.DrawWireCube(centerPoint, size);
        }
    }

    public bool CalculateBestBlock(Renderer renderer, out int i, out int j)
    {
        if (_gameObjectRenderers is null)
        {
            i = 0;
            j = 0;
            return false;
        }

        var relativePosition = renderer.transform.position - transform.position;

        var halfSize = _size / 2;
        var containerArraySize = _gameObjectRenderers.GetLength(0);
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

    public bool CanAddRenderer(Renderer renderer)
    {
        return CalculateBestBlock(renderer, out _, out _);
    }

    public void AddRenderer(Renderer gameObjectRenderer)
    {
        if (!CalculateBestBlock(gameObjectRenderer, out var i, out var j))
        {
            throw new System.InvalidOperationException();
        }

        _gameObjectRenderers[i, j].Add(gameObjectRenderer);
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
