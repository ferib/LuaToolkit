using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuaToolkit.Disassembler;
using LuaToolkit.Models;
using LuaToolkit.Core;
using LuaToolkit.Decompiler;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX;
using SharpDX.Mathematics.Interop;

namespace Graph
{
    public class GraphArrow
    {
        public int Start;
        public int End;
        public RawVector2 Start2D; // unused?
        public RawVector2 End2D;
        public Brush Brush;

        public int StartPixelOffsetH; // shift blocks left/right for arrow lines
        public int EndPixelOffsetH;

        private int LastGraphX;
        private int LastGraphY;

        private List<GraphBlock> GBlocks;

        public GraphArrow(ref List<GraphBlock> gblocks)
        {
            GBlocks = gblocks;
            SetGraphOffsets(0, 0);
        }

        public void SetGraphOffsets(int x, int y)
        {
            // save last
            LastGraphX = x;
            LastGraphY = y;
        }


        public List<RawVector2> CalculatePaths()
        {
            List<RawVector2> result = new List<RawVector2>();

            if (this.GBlocks == null)
                return result;

            int startIndex = this.GBlocks.IndexOf(this.GBlocks.Find(x => x.Block.StartAddress == this.Start));
            int endIndex = this.GBlocks.IndexOf(this.GBlocks.Find(x => x.Block.StartAddress == this.End));

            if (startIndex < 0 || endIndex < 0 || this.GBlocks.Count <= startIndex || this.GBlocks.Count <= endIndex)
                return result;

            // TODO: add caching
            // TODO: implement

            // start from bottom
            var source = GBlocks[startIndex].GetBottomCenter();
            var destination = GBlocks[endIndex].GetTopCenter();

            // add small up/down stub at Start
            var sourceStart = new RawVector2(source.X, source.Y);
            if (destination.Y > source.Y)
                sourceStart.Y += 7;
            else
                sourceStart.Y -= 7;
            result.Add(source);
            result.Add(sourceStart);

            // add small up/down stub at End
            var destinationPreEnd = new RawVector2(destination.X, destination.Y);
            if (source.Y > destination.Y)
                destinationPreEnd.Y += 7;
            else
                destinationPreEnd.Y -= 7;


            // TODO: decide to left/right
            int MinLeft = (int)sourceStart.X;
            int MaxRight = (int)sourceStart.X;

            // scan range, select all located in between the start and end block
            for (int i = 0; i < GBlocks.Count; i++)
            {
                if (GBlocks[i].BoundryBox.Top > source.Y && GBlocks[i].BoundryBox.Top < destination.Y)
                {
                    // box in between line, get left/right
                    var sum = GBlocks[i].BoundryBox.Left - 10;
                    if (sum < MinLeft)
                        MinLeft = (int)sum;

                    sum = GBlocks[i].BoundryBox.Right + 10;
                    if (GBlocks[i].BoundryBox.Right > MaxRight)
                        MaxRight = (int)sum;
                }
            }

            // check most block to the left/right
            result.Add(new RawVector2(MinLeft, sourceStart.Y));
            result.Add(new RawVector2(MinLeft, destinationPreEnd.Y));

            // end stub
            result.Add(destinationPreEnd);
            result.Add(destination);

            return result;
        }

        private void DetourColide(ref List<RawVector2> currentPath, RawVector2 source, RawVector2 destination, int index, ref int depth)
        {
            if (depth > 7)
                return;

            for (int i = index; i < GBlocks.Count - 1; i++)
            {
                // TODO: watch out for negative values?

                // calculate collision
                var colider = GBlocks[i].CheckCollision(source, destination);

                if (colider == BlockColider.Top)
                {
                    // add current dot to list and use as start point
                    RawVector2 lineInterruptStart = new RawVector2(source.X, GBlocks[i].BoundryBox.Top - 10f);
                    // left or right?
                    RawVector2 lineInterruptEnd;
                    lineInterruptEnd = new RawVector2(source.X - (GBlocks[i].GetBlockWidth() / 2) - 10f, GBlocks[i].BoundryBox.Top - 10f); // left!

                    currentPath.Add(lineInterruptStart);
                    currentPath.Add(lineInterruptEnd);
                    depth++;
                    DetourColide(ref currentPath, lineInterruptEnd, destination, i + 1, ref depth);
                    return;
                }
                // Y collides from below
                else if (colider == BlockColider.Bottom)
                {
                    RawVector2 lineInterruptStart = new RawVector2(source.X, GBlocks[i].BoundryBox.Bottom + 10f);
                    RawVector2 lineInterruptEnd = new RawVector2(source.X - (GBlocks[i].GetBlockWidth() / 2) - 10f, GBlocks[i].BoundryBox.Bottom + 10f); // left!
                    currentPath.Add(lineInterruptStart);
                    currentPath.Add(lineInterruptEnd);
                    depth++;
                    DetourColide(ref currentPath, lineInterruptEnd, destination, i + 1, ref depth);
                    return;
                }
                // X collides from Left
                else if (colider == BlockColider.Left)
                {
                    RawVector2 lineInterruptStart = new RawVector2(GBlocks[i].BoundryBox.Left - 10f, source.Y);
                    RawVector2 lineInterruptEnd;
                    // up or down?
                    if (source.Y > destination.Y)
                        lineInterruptEnd = new RawVector2(GBlocks[i].BoundryBox.Left - 10f, source.Y + (GBlocks[i].GetBlockHeight()) + 10f); // top
                    else
                        lineInterruptEnd = new RawVector2(GBlocks[i].BoundryBox.Left - 10f, source.Y - (GBlocks[i].GetBlockHeight()) - 10f); // bottom

                    currentPath.Add(lineInterruptStart);
                    currentPath.Add(lineInterruptEnd);
                    depth++;
                    DetourColide(ref currentPath, lineInterruptEnd, destination, i + 1, ref depth);
                    return;
                }
                // X collides from Right
                else if (colider == BlockColider.Right)
                {
                    RawVector2 lineInterruptStart = new RawVector2(GBlocks[i].BoundryBox.Right + 10f, source.Y);
                    RawVector2 lineInterruptEnd;
                    // up or down?
                    if (source.Y > destination.Y)
                        lineInterruptEnd = new RawVector2(GBlocks[i].BoundryBox.Right + 10f, source.Y + (GBlocks[i].GetBlockHeight()) + 10f); // top
                    else
                        lineInterruptEnd = new RawVector2(GBlocks[i].BoundryBox.Right + 10f, source.Y - (GBlocks[i].GetBlockHeight()) - 10f); // bottom

                    currentPath.Add(lineInterruptStart);
                    currentPath.Add(lineInterruptEnd);
                    depth++;
                    DetourColide(ref currentPath, lineInterruptEnd, destination, i + 1, ref depth);
                    return;
                }
            }
            currentPath.Add(new RawVector2(destination.X, destination.Y)); // add destination
        }
    }
}
