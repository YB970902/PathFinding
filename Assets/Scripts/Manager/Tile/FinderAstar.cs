using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;

using static Define.Tile;

/// <summary>
/// 에이스타 알고리즘으로 만들어진 길찾기
/// </summary>
public class FinderAstar : IPathFinder
{
    /// <summary>
    /// 타일 정보
    /// </summary>
    public class Node : FastPriorityQueueNode
    {
        /// <summary> 타일의 인덱스 </summary>
        public int Index { get; private set; }

        /// <summary> 목표까지의 대략적인 거리 </summary>
        public int H { get; private set; }

        /// <summary> 지금까지 이동한 거리 </summary>
        public int G { get; private set; }

        /// <summary> 목표까지의 거리 + 이동한 거리 </summary>
        public int F => H + G;

        /// <summary> 오픈 리스트에 있는지 유무 </summary>
        public bool IsOpen { get; set; }

        /// <summary> 클로즈 리스트에 있는지 유무 </summary>
        public bool IsClose { get; set; }

        /// <summary> 장애물인지 여부 </summary>
        public bool IsObstacle { get; set; }

        public Node Parent { get; set; }

        /// <summary> 직선 값 </summary>
        public const int DirectValue = 10;

        /// <summary> 대각선 값 </summary>
        public const int DiagonalValue = 14;

        /// <summary>
        /// 초기화
        /// </summary>
        public void Init(int _index)
        {
            Index = _index;
            IsObstacle = false;

            Reset();
        }

        /// <summary>
        /// 내용 초기화
        /// </summary>
        public void Reset()
        {
            H = 0;
            G = 0;

            IsOpen = false;
            IsClose = false;

            Parent = null;
        }

        /// <summary>
        /// H값을 계산한다음 대입한다
        /// </summary>
        public void SetH(int _destX, int _destY)
        {
            H = CalcH(_destX, _destY);
        }

        /// <summary>
        /// G값을 계산한다음 대입한다
        /// </summary>
        public void SetG(Node _prevTile, bool _isDiagonal)
        {
            G = CalcG(_prevTile, _isDiagonal);
        }

        /// <summary>
        /// 현재 경로보다 매개변수로 전달받은 경로가 더 짧은지 여부
        /// </summary>
        public bool IsShortPath(int _destX, int _destY, Node _prevTile, bool _isDiagonal)
        {
            int h = CalcH(_destX, _destY);
            int g = CalcG(_prevTile, _isDiagonal);

            return F > h + g;
        }

        /// <summary>
        /// H값을 계산한 후 반환한다
        /// </summary>
        private int CalcH(int _destX, int _destY)
        {
            return CalcH(Index % PathFinder.Instance.WidthCount, Index / PathFinder.Instance.WidthCount, _destX, _destY);
        }

        public static int CalcH(int _x1, int _y1, int _x2, int _y2)
        {
            int x = Mathf.Abs(_x1 - _x2);
            int y = Mathf.Abs(_y1 - _y2);

            return (x + y) * 10;
        }

        /// <summary>
        /// G값을 계산한 후 반환한다
        /// </summary>
        private int CalcG(Node _prevTile, bool _isDiagonal)
        {
            return _isDiagonal ? _prevTile.G + DiagonalValue : _prevTile.G + DirectValue;
        }
    }

    /// <summary> 타일의 리스트 </summary>
    private List<Node> tileList;

    /// <summary> 오픈 리스트 </summary>
    private FastPriorityQueue<Node> openList;

    /// <summary> 목적지 위치 X </summary>
    private int destX;

    /// <summary> 목적지 위치 Y </summary>
    private int destY;

    // 상하좌우 방향을 빠르게 찾기 위한 룩업테이블
    static readonly int[] dtX = { 0, 0, -1, 1 };
    static readonly int[] dtY = { 1, -1, 0, 0 };
    static readonly bool[] dirOpen = { false, false, false, false };

    // 대각선 방향을 빠르게 찾기 위한 룩업테이블
    static readonly int[] dgX = { -1, 1, -1, 1 };
    static readonly int[] dgY = { 1, 1, -1, -1 };

    static readonly (int, int)[] dgB =
    {
        ((int)Direct.Left, (int)Direct.Up),
        ((int)Direct.Right, (int)Direct.Up),
        ((int)Direct.Left, (int)Direct.Down),
        ((int)Direct.Right, (int)Direct.Down)
    };

    public void Init()
    {
        int width = PathFinder.Instance.WidthCount;
        int height = PathFinder.Instance.HeightCount;

        tileList = new List<Node>(PathFinder.Instance.TotalCount);
        openList = new FastPriorityQueue<Node>(PathFinder.Instance.TotalCount);

        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                var tile = new Node();
                tile.Init(y * width + x);
                tileList.Add(tile);
            }
        }
    }

    public void SetObstacle(int _index, bool _isObstacle)
    {
        tileList[_index].IsObstacle = _isObstacle;
    }

    public bool IsObstacle(int _index)
    {
        return tileList[_index].IsObstacle;
    }

    public bool FindPath(int _startIndex, int _destIndex, out List<int> _path)
    {
        _path = new List<int>();
        
        // 타일이 범위를 벗어난 경우
        if (IsOutOfTile(_startIndex) || IsOutOfTile(_destIndex))
        {
            Debug.Log("Index is out of Tile");
            return false;
        }

        // 이미 목적지에 도착한 경우
        if (_startIndex == _destIndex)
        {
            Debug.Log("Already at destination");
            return false;
        }

        tileList.ForEach(tile => tile.Reset());
        openList.Clear();

        openList.Enqueue(tileList[_startIndex], 0);

        destX = _destIndex % PathFinder.Instance.WidthCount;
        destY = _destIndex / PathFinder.Instance.WidthCount;

        Node curTile = null;

        //목표에 근사한 노드로, 만약 목적지를 찾지 못했을 경우 사용된다.
        Node approTile = null;

        while (openList.Count > 0)
        {
            curTile = openList.Dequeue();

            if (approTile == null || approTile.F >= curTile.F)
            {
                approTile = curTile;
            }

            // 경로를 찾았다.
            if (curTile.Index == _destIndex)
            {
                break;
            }

            curTile.IsClose = true;
            curTile.IsOpen = false;

            var nearNode = FindNearTile(curTile);
            nearNode.ForEach(tile => AddToOpenList(tile, curTile));

        }

        if (curTile.Index != _destIndex)
        {
            // 최종 타일이 목표 타일이 아닌경우, 목표와 가장 근사한 위치의 타일을 목표로 잡고 탐색한다.
            curTile = approTile;
        }

        while (curTile != null)
        {
            _path.Add(curTile.Index);
            curTile = curTile.Parent;
        }

        _path.Reverse();

        return true;   
    }

    /// <summary> 주변 타일 반환용 리스트. FindNearTile에서만 사용된다. </summary>
    private List<Node> nearTileResult = new List<Node>(8);

    /// <summary>
    /// 주변 노드를 반환한다.
    /// </summary>
    private List<Node> FindNearTile(Node curTile)
    {
        nearTileResult.Clear();

        int curX = curTile.Index % PathFinder.Instance.WidthCount;
        int curY = curTile.Index / PathFinder.Instance.WidthCount;

        // 상하좌우 검사부터 한다.
        for (Direct i = Direct.Start + 1; i < Direct.End; ++i)
        {
            int index = (int)i;
            int x = curX + dtX[index];
            int y = curY + dtY[index];

            dirOpen[index] = IsOpenableTile(x, y);
            if (dirOpen[index]) nearTileResult.Add(tileList[x + y * PathFinder.Instance.WidthCount]);
        }

        // 대각선 검사를 한다.
        for (DiagonalDirect i = DiagonalDirect.Start + 1; i < DiagonalDirect.End; ++i)
        {
            int index = (int)i;
            int x = curX + dgX[index];
            int y = curY + dgY[index];

            if (dirOpen[dgB[index].Item1] &&
                dirOpen[dgB[index].Item2] &&
                IsOpenableTile(x, y)) nearTileResult.Add(tileList[x + y * PathFinder.Instance.WidthCount]);
        }

        return nearTileResult;
    }

    /// <summary>
    /// 타일을 오픈 리스트에 넣는다.
    /// </summary>
    private void AddToOpenList(Node tile, Node parentTile)
    {
        // 닫혀있거나 장애물이면 오픈노드가 될 수 없다. 
        if (tile.IsClose || tile.IsObstacle) return;

        if (tile.IsOpen)
        {
            // 이미 오픈리스트에 들어있는 경우.
            if (tile.IsShortPath(destX, destY, parentTile, IsDiagonal(parentTile, tile)))
            {
                // 현재 경로가 더 짧은 경로라면 값을 갱신한다.
                tile.SetH(destX, destY);
                tile.SetG(parentTile, IsDiagonal(parentTile, tile));
                tile.Parent = parentTile;
                openList.UpdatePriority(tile, tile.F);
            }
        }
        else
        {
            tile.SetH(destX, destY);
            tile.SetG(parentTile, IsDiagonal(parentTile, tile));
            tile.IsOpen = true;
            tile.Parent = parentTile;
            openList.Enqueue(tile, tile.F);
        }
    }

    /// <summary>
    /// 인덱스가 타일 범위 밖인지 여부
    /// </summary>
    private bool IsOutOfTile(int index)
    {
        if (index < 0 || index >= PathFinder.Instance.TotalCount) return true;
        return false;
    }

    private bool IsOutOfTile(int x, int y)
    {
        if (x < 0 || x >= PathFinder.Instance.WidthCount ||
            y < 0 || y >= PathFinder.Instance.HeightCount) return true;
        return false;
    }

    /// <summary>
    /// 오픈리스트에 넣을 수 있는 타일인지 여부
    /// </summary>
    private bool IsOpenableTile(int x, int y)
    {
        if (IsOutOfTile(x, y)) return false;

        var tile = tileList[x + y * PathFinder.Instance.WidthCount];
        if (tile.IsClose || tile.IsObstacle) return false;
        return true;
    }

    /// <summary>
    /// 두 타일이 대각선에 위치한지 여부
    /// </summary>
    private bool IsDiagonal(Node a, Node b)
    {
        int aX = a.Index % PathFinder.Instance.WidthCount;
        int aY = a.Index / PathFinder.Instance.WidthCount;

        int bX = b.Index % PathFinder.Instance.WidthCount;
        int bY = b.Index / PathFinder.Instance.WidthCount;

        return aX != bX && aY != bY;
    }

    private static readonly Direct[] dtStepDirect = new Direct[] {
        Direct.Right,
        Direct.Down,
        Direct.Left,
        Direct.Up
    };
    
    public int GetNearOpenNode(int _startIndex, int _targetIndex)
    {
        // 목표 지점이 맵 밖에 있다면, 그대로 반환한다.
        if (IsOutOfTile(_targetIndex)) return _targetIndex;

        // 목표 지점이 장애물이 아니면, 그대로 반환한다.
        if (tileList[_targetIndex].IsObstacle == false) return _targetIndex;

        // 시작지점
        (int startX, int startY) = PathFinder.Instance.IndexToPos(_startIndex);
        // 목표지점
        (int targetX, int targetY) = PathFinder.Instance.IndexToPos(_targetIndex);
        
        // 탐색을 시작할 지점
        int x = targetX;
        int y = targetY;
        
        // 한 방향으로 탐색할 횟수. 2회씩 늘어난다.
        int stepCount = 2;
        
        while (stepCount <= PathFinder.Instance.WidthCount)
        {
            // 탐색 시작 위치가 왼쪽위로 한칸씩 움직인다.
            x -= 1;
            y += 1;
            
            // 탐색이 가능한 노드를 찾았는지 여부
            bool isFindOpenNode = false;
            // 시작위치로부터 H값이 가장 낮은 인덱스를 반환해야 하기 때문에 저장하는 H값과 인덱스.
            int minH = int.MaxValue;
            int minIndex = 0;
            
            for (int i = 0; i < 4; ++i)
            {
                int curDirectIndex = (int)dtStepDirect[i];
                int diffX = dtX[curDirectIndex];
                int diffY = dtY[curDirectIndex];
                
                for (int j = 0; j < stepCount; ++j)
                {
                    if (IsObstacle(x, y) == false)
                    {
                        isFindOpenNode = true;
                        int h = Node.CalcH(x, y, startX, startY);
                        if (h < minH)
                        {
                            minH = h;
                            minIndex = PathFinder.Instance.PosToIndex(x, y);
                        }
                    }

                    x += diffX;
                    y += diffY;
                }
            }

            if (isFindOpenNode) return minIndex;

            stepCount += 2;
        }

        return _targetIndex;
    }

    private bool IsObstacle(int _x, int _y)
    {
        if (IsOutOfTile(_x, _y)) return false;
        return tileList[PathFinder.Instance.PosToIndex(_x, _y)].IsObstacle;
    }
}
