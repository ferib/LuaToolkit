using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuaSharpVM.Disassembler;
using LuaSharpVM.Models;
using LuaSharpVM.Core;
using LuaSharpVM.Decompiler;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX;
using SharpDX.Mathematics.Interop;

namespace Graph
{
    public enum BlockColider
    {
        None = 0,
        Left,
        Top,
        Right,
        Bottom
    }


    public class GraphBlock
    {
        public RawRectangleF BoundryBox;
        public LuaScriptBlock Block;
        public string BlockInnerString;

        private int OffsetLeft;
        private int BlockHeight;
        private int LastGraphX;
        private int LastGraphY;

        public static int BlocksHeightOffset = 0;

        public GraphBlock()
        {

        }

        public GraphBlock(LuaScriptBlock block, int frmWidth, int frmHeight, int graphX = 0, int graphY = 0)
        {
            this.Block = block;
            this.BlockInnerString = "";
            this.BlockHeight = 0;
            this.LastGraphX = 0;
            this.LastGraphY = 0;
            this.OffsetLeft = 0;
            Initialize(frmWidth, frmHeight, graphX, graphY);
        }

        public void Initialize(int frmWidth, int frmHeight, int graphX, int graphY)
        {
            BoundryBox.Left = frmWidth / 2; // start center?
            BoundryBox.Top = 30 - (Block.Lines.Count * 7); // start center?
            //BoundryBox.Top = 30 - (Block.Lines[Block.Lines.Count-2].Text.Length * 7); // start center?
            BoundryBox.Bottom = 30 + (Block.Lines.Count * 7); // start center?
            //BoundryBox.Bottom = 30 + (Block.Lines[Block.Lines.Count - 2].Text.Length * 7); // start center?
            int longestData = 0;
            BlockInnerString = "";
            foreach (var b in Block.Lines)
            {
                string data = b.Text.Replace("\t", "");
                if (data.Length > longestData)
                {
                    BoundryBox.Left = (frmWidth / 2) - (data.Length * 3) - 2; // move to left
                    BoundryBox.Right = (frmWidth / 2) + (data.Length * 4) + 1; // mov to right
                    longestData = data.Length;
                }
                BlockInnerString += data;
            }

            Update(graphX, graphY);
        }

        public void Update(int graphX, int graphY)
        {
            ApplyGraphOffsets(graphX, graphY); // mouse movement
            ApplyHeightOffset(GetBlockHeightOffset()); // block height indexing
        }

        public void ApplyGraphOffsets(int x, int y)
        {
            BoundryBox.Left += LastGraphX; // rebase
            BoundryBox.Left -= x; // add offset
            BoundryBox.Right += LastGraphX; // rebase
            BoundryBox.Right -= x; // add offset
            BoundryBox.Top += LastGraphY; // rebase
            BoundryBox.Top -= y; // offset
            BoundryBox.Bottom += LastGraphY; // rebase
            BoundryBox.Bottom -= y; // offset

            // save last
            LastGraphX = x;
            LastGraphY = y;
        }

        public void ApplyHeightOffset(int newHeight)
        {
            // rebase blocks
            GraphBlock.BlocksHeightOffset -= this.BlockHeight;
            GraphBlock.BlocksHeightOffset += newHeight;
            this.BlockHeight = newHeight;
            BoundryBox.Top += GraphBlock.BlocksHeightOffset;
            BoundryBox.Bottom += GraphBlock.BlocksHeightOffset;
        }

        public bool IsVisible(int width, int height)
        {
            return this.BoundryBox.Top - LastGraphY < height || this.BoundryBox.Bottom - LastGraphY < 0;
            //|| this.BoundryBox.Left - LastGraphX < 0 || this.BoundryBox.Right - LastGraphX < width; // TODO: fix this when im sober again
        }

        public BlockColider CheckCollision(RawVector2 start, RawVector2 end)
        {
            //  4   sY
            //  3    |
            //  2   dY---------ds1
            //  1     _______  |
            //  0 ---|-------|-|------------------ 
            // -1    |b 0xB00| |
            // -2    |_______| sX---------dX
            // -3        |
            // -4        |
            //  * -4 -3 -2 -1  0  1  2  3  4
            //     sX     |                                   eX
            // this is such brainfuck.. Think you can do better? Be my guest!


            if (start.Y < BoundryBox.Top && BoundryBox.Top < end.Y && CollidesHorizontal()) // collision Top   
                return BlockColider.Top;
            else if (start.Y > BoundryBox.Bottom && BoundryBox.Bottom > end.Y && CollidesHorizontal()) // collides Bottom
                return BlockColider.Bottom;
            else if (start.X > BoundryBox.Left && BoundryBox.Left > start.X && CollidesVertical()) // collides Left
                return BlockColider.Left;
            else if (start.X < BoundryBox.Right && BoundryBox.Right < end.X && CollidesVertical()) // collides Right
                return BlockColider.Right;

            return BlockColider.None;

            bool CollidesHorizontal()
            {
                return BoundryBox.Right > end.X && BoundryBox.Left < end.X;
            }

            bool CollidesVertical()
            {
                return BoundryBox.Top > end.Y && BoundryBox.Bottom < end.Y;
            }
        }

        public int GetBlockHeightOffset()
        {
            return (int)(this.BoundryBox.Bottom - this.BoundryBox.Top) + 40;
        }

        public RawVector2 GetTopCenter()
        {
            return new RawVector2(this.BoundryBox.Left + (GetBlockWidth() / 2), this.BoundryBox.Top);
        }

        public RawVector2 GetBottomCenter()
        {
            return new RawVector2(this.BoundryBox.Left + (GetBlockWidth() / 2), this.BoundryBox.Bottom);
        }

        public int GetBlockHeight()
        {
            int sum = (int)(this.BoundryBox.Top - this.BoundryBox.Bottom);
            if (sum >= 0)
                return sum;
            else
                return (int)(this.BoundryBox.Bottom - this.BoundryBox.Top);
        }

        public int GetBlockWidth()
        {
            int sum = (int)(this.BoundryBox.Left - this.BoundryBox.Right);
            if (sum >= 0)
                return sum;
            else
                return (int)(this.BoundryBox.Right - this.BoundryBox.Left);
        }
    }
}
