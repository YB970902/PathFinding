using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Define;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public interface IPathFinder
{
    /// <summary>
    /// 초기화
    /// </summary>
    public void Init();

    /// <summary>
    /// 장애물 설정
    /// </summary>
    public void SetObstacle(int _index, bool _isObstacle);

    /// <summary>
    /// 장애물인지 여부
    /// </summary>
    /// <returns></returns>
    public bool IsObstacle(int _index);

    /// <summary>
    /// 경로 탐색 성공 여부를 반환하고, 경로는 매개변수를 통해 반환한다.
    /// </summary>
    public bool FindPath(int _startIndex, int _destIndex, out List<int> _path);

    /// <summary>
    /// 목표 인덱스의 주변에서 시작 인덱스와 가장 가까운 열린 노드를 반환한다.
    /// </summary>
    public int GetNearOpenNode(int _startIndex, int _targetIndex);
}

/// <summary>
/// 타일과 관련된 비즈니스 로직이 있는 클래스이다.
/// 인게임에만 쓰이는 요소이므로, BattleManager에 의존적이다.
/// 길찾기 탐색 요청 응답 기능과 인덱스를 타일 위치로 바꿔주는 등의 기능을 제공한다.
/// </summary>
public class PathFinder : MonoSingleton<PathFinder>
{
    [SerializeField] private int widthCount = 20;
    [SerializeField] private int heightCount = 20;

    public int WidthCount => widthCount;
    public int HeightCount => heightCount;
    public int TotalCount => totalCount;

    [SerializeField] private Tile.PathFindAlgorithm algorithm = Tile.PathFindAlgorithm.AStar;
    
    private int totalCount;

    [SerializeField] private Vector2 tileSize = Vector2.one;
    private Vector2 tileHalfSize;

    /// <summary> 화면에 위치할 수 있는 캐릭터의 최대 수. </summary>
    private const int CharacterPoolCount = 100;

    /// <summary> 경로 탐색기 </summary>
    private IPathFinder pathFinder;

    private void Awake()
    {
        totalCount = widthCount * heightCount;
        tileHalfSize = tileSize * 0.5f;

        switch (algorithm)
        {
            case Tile.PathFindAlgorithm.AStar:
                pathFinder = new FinderAstar();
                break;
            case Tile.PathFindAlgorithm.JPS:
                pathFinder = new FinderJPS();
                break;
            default:
                pathFinder = new FinderAstar();
                break;
        }
        pathFinder.Init();
    }

    /// <summary>
    /// 길찾기를 요청한다. pathGuide는 pool에 들어가며, 매 프레임마다 조금씩 길찾기를 수행한다. 
    /// </summary>
    public void RequestPathFind(int _startIndex, int _destIndex, out List<int> _path)
    {
        pathFinder.FindPath(_startIndex, _destIndex, out _path);
    }

    /// <summary>
    /// 인덱스에 맞는 타일의 위치 반환.
    /// </summary>
    public Vector2 GetTilePosition(int _index)
    {
        if (_index < 0 || _index >= totalCount)
        {
            Debug.LogError("Out of range");
            return Vector2.zero;
        }
        
        (int x, int y) = IndexToPosition(_index);
        
        return new Vector2(tileSize.x * x + tileHalfSize.x, tileSize.y * y + tileHalfSize.y);
    }

    private (int, int) IndexToPosition(int _index)
    {
        if (_index < 0 || _index >= totalCount)
        {
            Debug.LogError("Out of range");
            return (0, 0);
        }

        return (_index % widthCount, _index / widthCount);
    }

    public int GetNearOpenNode(int _startIndex, int _destIndex)
    {
        return pathFinder.GetNearOpenNode(_startIndex, _destIndex);
    }
    
    public (int, int) IndexToPos(int _index)
    {
        return (_index % widthCount, _index / heightCount);
    }
    
    public int PosToIndex(int _x, int _y)
    {
        return _x + _y * widthCount;
    }

    public void SetObstacle(int _index, bool _isOccupied)
    {
        pathFinder.SetObstacle(_index, _isOccupied);
    }

    public bool IsObstacle(int _index)
    {
        return pathFinder.IsObstacle(_index);
    }
}
