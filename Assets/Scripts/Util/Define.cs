using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Define
{
    public class Tile
    {
        public const int InvalidTileIndex = -1;

        /// <summary>
        /// 직선 이동 방향
        /// </summary>
        public enum Direct
        {
            Start = -1,
            Up,
            Down,
            Left,
            Right,
            End,
        }

        /// <summary>
        /// 대각선 이동 방향
        /// </summary>
        public enum DiagonalDirect
        {
            Start = -1,
            LeftUp,
            RightUp,
            LeftDown,
            RightDown,
            End,
        }

        public enum PathFindAlgorithm
        {
            AStar,
            JPS,
        }
    }
}