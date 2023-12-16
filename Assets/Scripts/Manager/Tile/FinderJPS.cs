using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;

using static Define.Tile;

/// <summary>
/// JPS 알고리즘으로 만들어진 길찾기
/// </summary>
public class FinderJPS : IPathFinder
{
    public class Node : FastPriorityQueueNode
    {
        private readonly int x;
        private readonly int y;

        public int X => x;
        public int Y => y;
        
        public int Index { get; private set; }

        public int F { get; private set; }
        public int G { get; private set; }
        public int H { get; private set; }

        public bool IsOpen { get; set; }
        public bool IsClose { get; set; }
        public bool IsObstacle { get; set; }
        
        /// <summary> 다른 캐릭터가 점거중인지 여부 </summary>
        public bool IsOccupied { get; set; }

        public Node Parent { get; private set; }

        public Node(int _index)
        {
            Index = _index;
            x = _index % PathFinder.Instance.WidthCount;
            y = _index / PathFinder.Instance.WidthCount;
        }

        public void Init()
        {
            Parent = null;
            H = 0;
            G = 0;
            F = 0;
            IsOpen = false;
            IsClose = false;
        }
        
        public void SetValue(Node _parent, int _destX, int _destY)
        {
            Parent = _parent;
            H = CalcH(_destX, _destY);
            G = CalcG(_parent);
            F = G + H;
        }

        private int CalcH(int _destX, int _destY)
        {
            return CalcH(this, _destX, _destY);
        }

        private int CalcG(Node _parent)
        {
            return CalcG(this, _parent);
        }

        public static int CalcH(Node _child, int _destX, int _destY)
        {
            return CalcH(_child.X, _child.Y, _destX, _destY);
        }

        public static int CalcH(int _x1, int _y1, int _x2, int _y2)
        {
            int x = Mathf.Abs(_x1 - _x2);
            int y = Mathf.Abs(_y1 - _y2);
            return (x + y) * 10;
        }

        public static int CalcG(Node _child, Node _parent)
        {
            if (_parent == null) return 0;
            
            if (_child.X == _parent.X || _child.Y == _parent.Y)
            {
                // 거리를 재야 한다.
                int dist = _child.X == _parent.X ? Mathf.Abs(_child.Y - _parent.Y) : Mathf.Abs(_child.X - _parent.X); 
                return _parent.G + 10 * dist;
            }
            else
            {
                // 거리를 재야 하는데, 대각선이기에 가로나 세로중 아무거나 쓰면 된다.
                int dist = Mathf.Abs(_child.X - _parent.X);
                return _parent.G + 14 * dist;
            }
        }
    }

    private List<Node> nodeList;
    
    /// <summary> 오픈 리스트 </summary>
    private FastPriorityQueue<Node> openList;

    private int destX;
    private int destY;

    public void Init()
    {
        openList = new FastPriorityQueue<Node>(PathFinder.Instance.TotalCount);
        nodeList = new List<Node>(PathFinder.Instance.TotalCount);
        for (int y = 0; y < PathFinder.Instance.HeightCount; ++y)
        {
            for (int x = 0; x < PathFinder.Instance.WidthCount; ++x)
            {
                var node = new Node(PathFinder.Instance.PosToIndex(x, y));
                node.Init();
                nodeList.Add(node);
            }
        }
    }
    
    public bool FindPath(int _startIndex, int _destIndex, out List<int> _path)
    {
        throw new System.NotImplementedException();
    }

    public bool FindPath(List<int> _path, int _startIndex, int _destIndex, int _tempObstacleIndex)
    {
        _path.Clear();
        
        if (_startIndex == _destIndex)
        {
            Debug.Log("출발지와 목적지가 같음.");
            return false;
        }
        
        (destX, destY) = PathFinder.Instance.IndexToPos(_destIndex);
        (int startX, int startY) = PathFinder.Instance.IndexToPos(_startIndex);

        if (IsOutOfNode(startX, startY) || IsOutOfNode(destX, destY))
        {
            Debug.Log("출발지 혹은 목적지가 범위 밖임");
            return false;
        }

        if (nodeList[_startIndex].IsObstacle || nodeList[_destIndex].IsObstacle)
        {
            Debug.Log("출발지 혹은 목적지가 장애물임");
            return false;
        }
        
        if (_tempObstacleIndex != InvalidTileIndex)
        {
            if (IsObstacle(_tempObstacleIndex) || _tempObstacleIndex == _destIndex)
            {
                _tempObstacleIndex = InvalidTileIndex;
            }
            else
            {
                SetObstacle(_tempObstacleIndex, true);
            }
        }

        openList.Clear();
        for (int i = 0; i < PathFinder.Instance.TotalCount; ++i)
        {
            nodeList[i].Init();
        }
        
        Node curNode = nodeList[_startIndex];
        curNode.IsOpen = true;
        curNode.SetValue(null, destX, destY);
        openList.Enqueue(curNode, curNode.F);

        Node approNode = curNode;
        
        while (openList.Count > 0)
        {
            curNode = openList.Dequeue();
            curNode.IsOpen = false;
            curNode.IsClose = true;

            if (curNode.F <= approNode.F)
            {
                approNode = curNode;
            }
            
            // 목적지를 찾았다면 반환.
            if (curNode.Index == _destIndex) break;
            
            SearchPoint(curNode);
        }

        if (curNode.Index != _destIndex)
        {
            // 최종 타일이 목표 타일이 아닌경우, 목표와 가장 근사한 위치의 타일을 목표로 잡고 탐색한다.
            curNode = approNode;
        }

        while (curNode != null && curNode.Parent != null)
        {
            var parent = curNode.Parent;
            bool isDiagonal = curNode.X - parent.X != 0 && curNode.Y - parent.Y != 0;
            if (isDiagonal)
            {
                int x = curNode.X;
                int y = curNode.Y;
                var dir = PosToDiagonalDirect(parent.X - curNode.X, parent.Y - curNode.Y);
                (int dirX, int dirY) = diagonalLookup[(int)dir];
                while (x != parent.X || y != parent.Y)
                {
                    _path.Add(PathFinder.Instance.PosToIndex(x, y));
                    x += dirX;
                    y += dirY;
                }
            }
            else
            {
                int x = curNode.X;
                int y = curNode.Y;
                var dir = PosToDirect(parent.X - curNode.X, parent.Y - curNode.Y);
                (int dirX, int dirY) = directLookup[(int)dir];
                while (x != parent.X || y != parent.Y)
                {
                    _path.Add(PathFinder.Instance.PosToIndex(x, y));
                    x += dirX;
                    y += dirY;
                }
            }
            curNode = curNode.Parent;
        }

        // 목적지에서부터 출발지로 오는 경로이기 때문에 뒤집는다.
        _path.Reverse();
        
        if (_tempObstacleIndex != InvalidTileIndex)
        {
            SetObstacle(_tempObstacleIndex, false);
        }
        
        return true;
    }

    /// <summary>  상하좌우 방향으로 나아가는 룩업테이블  </summary>
    private readonly (int, int)[] directLookup =
    {
        (0, 1),
        (0, -1),
        (-1, 0),
        (1, 0)
    };
    
    /// <summary>  상하좌우 기준으로 장애물이 있는지 체크해야 하는 타일의 룩업테이블  </summary>
    private readonly (int, int)[] directObsLookup =
    {
        (-1, -1), (1, -1),
        (1, 1), (-1, 1),
        (1, -1), (1, 1),
        (-1, 1), (-1, -1)
    };
    
    /// <summary>  상하좌우 기준으로 지나갈 수 있는 곳인지 체크해야 하는 타일의 룩업테이블  </summary>
    private readonly (int, int)[] directOpenLookup =
    {
        (-1, 0), (1, 0),
        (1, 0), (-1, 0),
        (0, -1), (0, 1),
        (0, 1), (0, -1)
    };

    /// <summary>  좌상, 우상, 좌하, 우하 기준으로 진행방향의 룩업테이블.  </summary>
    private readonly (int, int)[] diagonalLookup =
    {
        (-1, 1),
        (1, 1),
        (-1, -1),
        (1, -1)
    };
    
    /// <summary>  좌상, 우상, 좌하, 우하 기준으로 장애물이 있는지 체크해야 하는 타일의 룩업테이블  </summary>
    private readonly (int, int)[] diagonalObsLookup =
    {
        (-1, -1), (1, 1),
        (-1, 1), (1, -1),
        (1, -1), (-1, 1),
        (1, 1), (-1, -1)
    };
    
    /// <summary>  좌상, 우상, 좌하, 우하 기준으로 지나갈 수 있는 곳인지 체크해야 하는 타일의 룩업테이블  </summary>
    private readonly (int, int)[] diagonalOpenLookup =
    {
        (-1, 0), (0, 1),
        (0, 1), (1, 0),
        (0, -1), (-1, 0),
        (1, 0), (0, -1)
    };

    /// <summary>  좌상, 우상, 좌하, 우하 기준으로 보조 탐색을 할 방향의 룩업테이블  </summary>
    private readonly (Direct, Direct)[] diagonalSubLookup =
    {
        (Direct.Left, Direct.Up),
        (Direct.Up, Direct.Right),
        (Direct.Down, Direct.Left),
        (Direct.Right, Direct.Down)
    };

    /// <summary>
    /// 포인트를 찾기위해 주변 노드를 탐색한다.
    /// </summary>
    private void SearchPoint(Node _node)
    {
        if (_node.Parent != null)
        {
            // 가지치기를 위한 차이값을 구한다.
            int diffX = _node.X - _node.Parent.X;
            int diffY = _node.Y - _node.Parent.Y;

            if (diffX == 0 || diffY == 0) // 직선으로 이동한 경우
            {
                // 이동중인 방향
                Direct dir = PosToDirect(diffX, diffY);
                int dirIndex = (int)dir;
                
                // 부모노드에서 부터 이동중안 방향으로 탐색한다.
                AddOpenList(_node, SearchDirectOpenNode(_node, dir));
                
                // 특정 위치에 장애물이 있으면 탐색범위가 커져야 하기 때문에 검사해야 한다. 
                (int leftObsX, int leftObsY) = directObsLookup[dirIndex * 2];
                (int rightObsX, int rightObsY) = directObsLookup[dirIndex * 2 + 1];

                if (IsObstacleNode(_node.X + leftObsX, _node.Y + leftObsY))
                {
                    // 좌측에 장애물이 있는경우, 좌측을 추가로 탐색한다.
                    AddOpenList(_node, SearchDirectOpenNode(_node, dir.TurnLeft()));
                    AddOpenList(_node, SearchDiagonalDirectOpenNode(_node, GetDiagnoalDirect(dir, dir.TurnLeft())));
                }
                
                if (IsObstacleNode(_node.X + rightObsX, _node.Y + rightObsY))
                {
                    // 우측에 장애물이 있는경우, 우측을 추가로 탐색한다.
                    AddOpenList(_node, SearchDirectOpenNode(_node, dir.TurnRight()));
                    AddOpenList(_node, SearchDiagonalDirectOpenNode(_node, GetDiagnoalDirect(dir, dir.TurnRight())));
                }
            }
            else // 대각선으로 이동한 경우
            {
                // 부모로부터 이동중인 방향과 좌우를 추가로 검사한다.
                Direct verticalDir = diffY < 0 ? Direct.Down : Direct.Up;
                Direct horizontalDir = diffX < 0 ? Direct.Left : Direct.Right;
                DiagonalDirect diagonalDir = GetDiagnoalDirect(verticalDir, horizontalDir);
                
                AddOpenList(_node, SearchDirectOpenNode(_node, verticalDir));
                AddOpenList(_node, SearchDirectOpenNode(_node, horizontalDir));
                AddOpenList(_node, SearchDiagonalDirectOpenNode(_node, diagonalDir));
            }
        }
        else
        {
            // 부모 노드가 null이라면 가지치기를 하지 않고 모든 방향을 검사한다.
            for (var dir = Direct.Start + 1; dir < Direct.End; ++dir)
            {
                AddOpenList(_node, SearchDirectOpenNode(_node, dir));
            }

            for (var dir = DiagonalDirect.Start + 1; dir < DiagonalDirect.End; ++dir)
            {
                AddOpenList(_node, SearchDiagonalDirectOpenNode(_node, dir));
            }
        }
    }

    /// <summary>
    /// 오픈리스트에 넣는다.
    /// </summary>
    void AddOpenList(Node _parent, Node _child)
    {
        // 포인트를 찾지 못했다면 반환.
        if (_child == null) return;
        
        // 닫혀있는 노드인경우 반환.
        if (_child.IsClose) return;
        
        if (_child.IsOpen)
        {
            if (Node.CalcH(_child, destX, destY) + Node.CalcG(_child, _parent) < _child.F)
            {
                // 이미 오픈리스트에 있고, F값이 더 나을경우 갱신한다.
                _child.SetValue(_parent, destX, destY);
                openList.UpdatePriority(_child, _child.F);
            }
        }
        else
        {
            _child.IsOpen = true;
            _child.SetValue(_parent, destX, destY);
            openList.Enqueue(_child, _child.F);   
        }
    }

    /// <summary>
    /// 직선 방향에 포인트가 없는지 탐색 
    /// </summary>
    Node SearchDirectOpenNode(Node _node, Direct _direct)
    {
        int directIndex = (int)_direct;
        
        (int dirX, int dirY) = directLookup[directIndex];
        
        int curX = _node.X + dirX;
        int curY = _node.Y + dirY;

        // 현재 위치가 이동 불가능한 위치라면 탐색을 중지한다.
        if (IsMovableNode(curX, curY) == false) return null;
        
        (int leftObsX, int leftObsY) = directObsLookup[directIndex * 2];
        (int rightObsX, int rightObsY) = directObsLookup[directIndex * 2 + 1];
            
        (int leftOpenX, int leftOpenY) = directOpenLookup[directIndex * 2];
        (int rightOpenX, int rightOpenY) = directOpenLookup[directIndex * 2 + 1];

        // 왼쪽이든 오른쪽이든 OutOfNode라면 그 쪽은 탐색하는 내내 쭉 OutOfNode이기 때문에 미리 검사한다.
        bool isLeftOutOfNode = IsOutOfNode(curX + leftObsX, curY + leftObsY);
        bool isRightOutOfNode = IsOutOfNode(curX + rightObsX, curY + rightObsY);

        while (true)
        {
            // 목적지에 도착했다면, 현재 노드를 반환한다.
            if (curX == destX && curY == destY) return nodeList[PathFinder.Instance.PosToIndex(curX, curY)];

            if (isLeftOutOfNode == false)
            {
                if (nodeList[PathFinder.Instance.PosToIndex(curX + leftObsX, curY + leftObsY)].IsObstacle &&
                    nodeList[PathFinder.Instance.PosToIndex(curX + leftOpenX, curY + leftOpenY)].IsObstacle == false)
                {
                    // 포인트 조건에 맞다면 반환.
                    return nodeList[PathFinder.Instance.PosToIndex(curX, curY)];
                }
            }

            if (isRightOutOfNode == false)
            {
                if (nodeList[PathFinder.Instance.PosToIndex(curX + rightObsX, curY + rightObsY)].IsObstacle &&
                    nodeList[PathFinder.Instance.PosToIndex(curX + rightOpenX, curY + rightOpenY)].IsObstacle == false)
                {
                    // 포인트 조건에 맞다면 반환.
                    return nodeList[PathFinder.Instance.PosToIndex(curX, curY)];
                }
            }
            
            // 다음위치가 이동 불가능한 곳이라면 탐색을 중지한다.
            if (IsMovableNode(curX + dirX, curY + dirY) == false)
            {
                return null;
            }

            // 다음 노드로 이동.
            curX += dirX;
            curY += dirY;
        }
    }

    /// <summary>
    /// 대각선 방향의 포인트가 없는지 탐색
    /// </summary>
    Node SearchDiagonalDirectOpenNode(Node _node, DiagonalDirect _direct)
    {
        int directIndex = (int)_direct;
        
        (int dirX, int dirY) = diagonalLookup[directIndex];
        
        int curX = _node.X;
        int curY = _node.Y;
    
        (int leftOpenX, int leftOpenY) = diagonalOpenLookup[directIndex * 2];
        (int rightOpenX, int rightOpenY) = diagonalOpenLookup[directIndex * 2 + 1];
        
        // 다음 위치로 이동 불가능하다면 탐색을 취소한다.
        if (IsMovableNode(curX + dirX, curY + dirY) == false ||
            IsMovableNode(curX + leftOpenX, curY + leftOpenY) == false ||
            IsMovableNode(curX + rightOpenX, curY + rightOpenY) == false) return null;

        curX += dirX;
        curY += dirY;
        
        (int leftObsX, int leftObsY) = diagonalObsLookup[directIndex * 2];
        (int rightObsX, int rightObsY) = diagonalObsLookup[directIndex * 2 + 1];
        
        (Direct leftDir, Direct rightDir) = diagonalSubLookup[directIndex];

        while (true)
        {
            var curNode = nodeList[PathFinder.Instance.PosToIndex(curX, curY)];

            // 목적지를 발견한 경우, 현재 노드를 반환한다.
            if (curX == destX && curY == destY)
            {
                return nodeList[PathFinder.Instance.PosToIndex(curX, curY)];
            }
            
            if (IsObstacleNode(curX + leftObsX, curY + leftObsY) &&
                IsMovableNode(curX + leftOpenX, curY + leftOpenY))
            {
                // 포인트를 찍는 조건에 만족하면 노드 반환.
                return curNode;
            }

            if (IsObstacleNode(curX + rightObsX, curY + rightObsY) &&
                IsMovableNode(curX + rightOpenX, curY + rightOpenY))
            {
                // 포인트를 찍는 조건에 만족하면 노드 반환.
                return curNode;
            }

            // 대각선에서 포인트를 못찍었다면 보조 탐색을 한다.
            
            // 왼쪽 보조 탐색에서 포인트를 찍었다면 현재 노드를 반환한다.
            if (SearchDirectOpenNode(curNode, leftDir) != null) return curNode;

            // 오른쪽 보조 탐색에서 포인트를 찍었다면 현재 노드를 반환한다.
            if (SearchDirectOpenNode(curNode, rightDir) != null) return curNode;


            // 다음 노드가 장애물이어서 이동할 수 없다면 탐색을 중지한다.
            if (IsMovableNode(curX + dirX, curY + dirY) == false) return null;

            // 다음 노드로 지나갈 경로가 장애물로 막혀있다면 현재 노드를 반환한다.
            if (IsMovableNode(curX + leftOpenX, curY + leftOpenY) == false ||
                IsMovableNode(curX + rightOpenX, curY + rightOpenY) == false)
            {
                return nodeList[PathFinder.Instance.PosToIndex(curX, curY)];
            }

            // 다음 노드로 이동.
            curX += dirX;
            curY += dirY;
        }
    }
    
    public void SetObstacle(int _index, bool _isObstacle)
    {
        nodeList[_index].IsObstacle = _isObstacle;
    }

    public bool IsObstacle(int _index)
    {
        return nodeList[_index].IsObstacle;
    }

    

    private bool IsOutOfNode(int _x, int _y)
    {
        return _x < 0 || _x >= PathFinder.Instance.WidthCount || _y < 0 || _y >= PathFinder.Instance.HeightCount;
    }

    /// <summary>
    /// 노드가 범위내에 있으며, 이동가능한 노드인지 여부
    /// </summary>
    private bool IsMovableNode(int _x, int _y)
    {
        if (IsOutOfNode(_x, _y)) return false;

        return nodeList[PathFinder.Instance.PosToIndex(_x, _y)].IsObstacle == false;
    }

    /// <summary>
    /// 노드가 범위내에 있으며, 장애물인지 여부
    /// </summary>
    private bool IsObstacleNode(int _x, int _y)
    {
        if (IsOutOfNode(_x, _y)) return false;
        
        return nodeList[PathFinder.Instance.PosToIndex(_x, _y)].IsObstacle;
    }

    private Direct PosToDirect(int _x, int _y)
    {
        if (_x != 0 && _y != 0) return Direct.End;
        
        if (_x < 0) return Direct.Left; 
        if (_x > 0) return  Direct.Right; 
        if (_y < 0) return Direct.Down;
        if (_y > 0) return Direct.Up;
        
        return Direct.End;
    }

    private DiagonalDirect PosToDiagonalDirect(int _x, int _y)
    {
        if (_x < 0 && _y < 0) return DiagonalDirect.LeftDown;
        if (_x < 0 && _y > 0) return DiagonalDirect.LeftUp;
        if (_x > 0 && _y < 0) return DiagonalDirect.RightDown;
        if (_x > 0 && _y > 0) return DiagonalDirect.RightUp;
        return DiagonalDirect.End;
    }

    /// <summary>
    /// 직선방향 두 개로 대각선 방향 반환.
    /// </summary>
    private DiagonalDirect GetDiagnoalDirect(Direct _dir1, Direct _dir2)
    {
        if ((_dir1 == Direct.Left && _dir2 == Direct.Up) ||
            (_dir2 == Direct.Left && _dir1 == Direct.Up))
            return DiagonalDirect.LeftUp;
        else if ((_dir1 == Direct.Right && _dir2 == Direct.Up) ||
                 (_dir2 == Direct.Right && _dir1 == Direct.Up))
            return DiagonalDirect.RightUp;
        else if ((_dir1 == Direct.Left && _dir2 == Direct.Down) ||
                 (_dir2 == Direct.Left && _dir1 == Direct.Down))
            return DiagonalDirect.LeftDown;
        else if ((_dir1 == Direct.Right && _dir2 == Direct.Down) ||
                 (_dir2 == Direct.Right && _dir1 == Direct.Down))
            return DiagonalDirect.RightDown;

        return DiagonalDirect.End;
    }

    private static readonly Direct[] LookupStepDirect = new Direct[] {
        Direct.Right,
        Direct.Down,
        Direct.Left,
        Direct.Up
    };
    
    public int GetNearOpenNode(int _startIndex, int _targetIndex)
    {
        // 목표 지점이 장애물이 아니면, 그대로 반환한다.
        if (nodeList[_targetIndex].IsObstacle == false) return _targetIndex;

        // 시작지점
        (int startX, int startY) = PathFinder.Instance.IndexToPos(_startIndex);
        // 목표지점
        (int targetX, int targetY) = PathFinder.Instance.IndexToPos(_targetIndex);
        
        // 탐색을 시작할 지점
        int x = targetX;
        int y = targetY;
        
        // 한 방향으로 탐색할 횟수. 2회씩 늘어난다.
        int stepCount = 0;

        int tileSize = Mathf.Max(PathFinder.Instance.WidthCount, PathFinder.Instance.HeightCount); 
        while (stepCount <= tileSize)
        {
            // 탐색 시작 위치가 왼쪽위로 한칸씩 움직인다.
            x -= 1;
            y += 1;
            
            // 한 방향으로 탐색하는 범위인 스텝은 2칸씩 늘어난다
            stepCount += 2;
            
            // 탐색이 가능한 노드를 찾았는지 여부
            bool isFindOpenNode = false;
            // 시작위치로부터 H값이 가장 낮은 인덱스를 반환해야 하기 때문에 저장하는 H값과 인덱스.
            int minH = int.MaxValue;
            int minIndex = 0;
            
            for (int i = 0; i < 4; ++i)
            {
                int curDirectIndex = (int)LookupStepDirect[i];
                (int diffX, int diffY) = directLookup[curDirectIndex];
                
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
        }

        return _targetIndex;
    }

    public void SetOccupied(int _index, bool _isOccupied)
    {
        nodeList[_index].IsOccupied = _isOccupied;
    }

    public bool IsOccupied(int _index)
    {
        return nodeList[_index].IsOccupied;
    }

    private bool IsObstacle(int _x, int _y)
    {
        if (IsOutOfNode(_x, _y)) return true;
        return nodeList[PathFinder.Instance.PosToIndex(_x, _y)].IsObstacle;
    }
}
