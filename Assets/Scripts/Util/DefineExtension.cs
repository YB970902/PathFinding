using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DefineExtension
{
    public static Define.Tile.Direct TurnLeft(this Define.Tile.Direct _dir)
    {
        switch (_dir)
        {
            case Define.Tile.Direct.Up:
                return Define.Tile.Direct.Left;
            case Define.Tile.Direct.Down:
                return Define.Tile.Direct.Right;
            case Define.Tile.Direct.Left:
                return Define.Tile.Direct.Down;
            case Define.Tile.Direct.Right:
                return Define.Tile.Direct.Up;
        }

        return Define.Tile.Direct.End;
    }
    
    public static Define.Tile.Direct TurnRight(this Define.Tile.Direct _dir)
    {
        switch (_dir)
        {
            case Define.Tile.Direct.Up:
                return Define.Tile.Direct.Right;
            case Define.Tile.Direct.Down:
                return Define.Tile.Direct.Left;
            case Define.Tile.Direct.Left:
                return Define.Tile.Direct.Up;
            case Define.Tile.Direct.Right:
                return Define.Tile.Direct.Down;
        }

        return Define.Tile.Direct.End;
    }
}