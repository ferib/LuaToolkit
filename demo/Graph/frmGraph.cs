using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX;
using SharpDX.Mathematics.Interop;
using System.Threading;
using LuaSharpVM.Disassembler;
using LuaSharpVM.Models;
using LuaSharpVM.Core;
using LuaSharpVM.Decompiler;

namespace Graph
{
    public partial class frmGraph : Form
    {
        private WindowRenderTarget DrawTarget;
        private SharpDX.Direct2D1.Factory factory = new SharpDX.Direct2D1.Factory();
        private SharpDX.DirectWrite.Factory factoryWrite = new SharpDX.DirectWrite.Factory();
        private Brush redBrush;
        private Brush greenBrush;
        private Brush yellowBrush;
        private Brush whiteBrush;
        private Brush purpleBrush;
        private Brush blueBrush;
        private Brush blue2Brush;
        private Brush blackBrush;
        private Brush backgroundBrush;
        private TextFormat fontFat;
        private TextFormat fontBig;
        private TextFormat font;
        private TextFormat fontSmall;
        private TextFormat fontMini;

        //public static U8Flow flow;

        private int graphX;
        private int graphY;
        private bool mouseDown = false;
        private int LastDownX;
        private int LastDownY;
        private int DownClickX;
        private int DownClickY;

        public bool del = false;

        private List<GraphArrow> GArrows;
        private List<GraphBlock> GBlocks;

        public LuaWriter Writer;

        private int targetFunc = 0;
        public int TargetFunc
        {
            get { return targetFunc; }
            set { targetFunc = value;
                ReInitialise(); // re-initialise
            }
        }

        public frmGraph(LuaWriter writter)
        {
            InitializeComponent();

            //Init Direct Draw
            //Set Rendering properties
            RenderTargetProperties renderProp = new RenderTargetProperties()
            {
                PixelFormat = new PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
            };

            //set hwnd target properties (permit to attach Direct2D to window)
            HwndRenderTargetProperties winProp = new HwndRenderTargetProperties()
            {
                Hwnd = this.Handle,
                PixelSize = new Size2(this.ClientSize.Width, this.ClientSize.Height),
                PresentOptions = PresentOptions.None
            };

            //target creation
            DrawTarget = new WindowRenderTarget(factory, renderProp, winProp);

            //redBrush = new SharpDX.Direct2D1.SolidColorBrush(DrawTarget, new RawColor4(0xFE, 0x6B, 0x64, 0xFF));
            redBrush = new SharpDX.Direct2D1.SolidColorBrush(DrawTarget, new RawColor4(255, 0, 0, 255));
            yellowBrush = new SharpDX.Direct2D1.SolidColorBrush(DrawTarget, new RawColor4(0xFB, 0xFD, 0x98, 0xFF));
            greenBrush = new SharpDX.Direct2D1.SolidColorBrush(DrawTarget, new RawColor4(0, 255, 0, 0xFF));
            //greenBrush = new SharpDX.Direct2D1.SolidColorBrush(DrawTarget, new RawColor4(0x77, 0xDD, 0x77, 0xFF));
            whiteBrush = new SharpDX.Direct2D1.SolidColorBrush(DrawTarget, new RawColor4(0xFF, 0xFF, 0xFF, 0xFF));
            purpleBrush = new SharpDX.Direct2D1.SolidColorBrush(DrawTarget, new RawColor4(0xB2, 0x9D, 0xD9, 0xFF));
            blueBrush = new SharpDX.Direct2D1.SolidColorBrush(DrawTarget, new RawColor4(0x77, 0x9E, 0xCB, 0xFF));
            blue2Brush = new SharpDX.Direct2D1.SolidColorBrush(DrawTarget, new RawColor4(0, 0, 255, 255));
            blackBrush = new SharpDX.Direct2D1.SolidColorBrush(DrawTarget, new RawColor4(0x00, 0x00, 0x00, 0xFF));

            backgroundBrush = new SharpDX.Direct2D1.LinearGradientBrush(DrawTarget, new LinearGradientBrushProperties()
            {
                StartPoint = new RawVector2(0, (int)(this.Height * 0.2)),
                EndPoint = new RawVector2(0, this.Height),
            },
            new SharpDX.Direct2D1.GradientStopCollection(DrawTarget, new SharpDX.Direct2D1.GradientStop[]
            {
                new GradientStop()
                {
                    Color = new RawColor4(0xFF, 0xFF, 0xFF, 0xFF),
                    Position = 0,
                },
                new GradientStop()
                {
                    Color = new RawColor4(140, 200, 00, 10), //E0F8FF
                    Position = 1,
                }
            }));

            //create textformat
            fontFat = new SharpDX.DirectWrite.TextFormat(factoryWrite, "Consolas", 36);
            fontBig = new SharpDX.DirectWrite.TextFormat(factoryWrite, "Consolas", 24);
            font = new SharpDX.DirectWrite.TextFormat(factoryWrite, "Consolas", 16);
            fontSmall = new SharpDX.DirectWrite.TextFormat(factoryWrite, "Consolas", 12);
            fontMini = new SharpDX.DirectWrite.TextFormat(factoryWrite, "Consolas", 8);

            //avoid artifacts
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);

            Writer = writter;

            Thread render = new Thread(new ThreadStart(drawloop));
            render.Priority = ThreadPriority.BelowNormal;
            render.Start();


            void drawloop()
            {
                Initialise(); // create graph blocks & arrows

                if (GBlocks.Count > 0)
                    graphY = (int)GBlocks[TargetFunc].BoundryBox.Top; // jump to first block TODO: fix bug that causes this!

                while (true)
                {
                    DrawData();
                    Thread.Sleep(50);
                }

            }
        }

        private void Initialise()
        {
            // Create & Init GBlock List if needed
            ReInitialise();
        }

        private void ReInitialise()
        {
            CreateBlocks(this.Writer.LuaFunctions[TargetFunc].Blocks);
            foreach (var gb in GBlocks)
            {
                // remap jumps from address to block
                for (int i = 0; i < this.GBlocks.Count; i++)
                {
                    if (gb.Block.JumpsTo != -1 && this.GBlocks[i].Block.HasLineNumber(gb.Block.JumpsTo))
                        gb.JumpsToBlock = i;
                    if (gb.Block.JumpsNext != -1 && this.GBlocks[i].Block.HasLineNumber(gb.Block.JumpsNext))
                        gb.JumpsNextBlock = i;
                }
            }

            // Merge blocks so that multi/merged IF's are in one block
            List<GraphBlock> cisBlocks = GBlocks.FindAll(x => x.Block.IfChainIndex == 0); // chained if start Blocks
            foreach(GraphBlock cisb in cisBlocks)
            {
                int startIndex = GBlocks.IndexOf(cisb);
                var startBlock = GBlocks[startIndex].Block;
                int index = startIndex+1;
                while (GBlocks[index].Block.IfChainIndex > 0 && index < GBlocks.Count-1) 
                {
                    var cLine = GBlocks[index].Block.GetConditionLine();
                    if(cLine != null)
                        startBlock.GetConditionLine().Text = startBlock.GetConditionLine().Text.Replace("\r\n", "").Replace("\t","") + cLine.Text;
                    GBlocks[index].Block.Lines.Clear(); // empty

                    // TODO: jumpTo = (ifchain < 1)+1; jumpNext = (ifchain < 1)

                    if (GBlocks[index + 1].Block.IfChainIndex < 1)
                    {
                        cisb.Block.JumpsNext = GBlocks.Single(x => x.Block.StartAddress == GBlocks[index + 1].Block.StartAddress).Block.StartAddress; // NOTE: hacky fix
                        //cisb.Block.JumpsTo = GBlocks.Single(x => x.Block.StartAddress == GBlocks[index + 2].Block.StartAddress).Block.StartAddress; // NOTE: hacky fix
                        cisb.Block.JumpsTo = GBlocks[index].Block.JumpsTo; // NOTE: this is always right? cuz it calculated before?
                        // NOTE: index+2 is not always right, block can spawn in the middle? (use JumpTo from last block?)
                    }

                    index++;
                } 
            }
            GBlocks.RemoveAll(x => x.Block.Lines.Count == 0); // remove emptied blocks

            CreateArrows();
        }

        private void DrawData()
        {
            //Start to draw
            DrawTarget.BeginDraw();
            DrawTarget.Clear(new RawColor4(0x70, 0x70, 0x70, 255));
            DrawTarget.FillRectangle(new RawRectangleF(0, 0, this.Width, this.Height), backgroundBrush);
            DrawTarget.DrawText($"{this.Writer.LuaFunctions[targetFunc].ToString()} ({graphX},{graphY})", fontSmall, new RawRectangleF(1, 1, this.Width, 20), blackBrush);
            DrawTarget.DrawLine(new RawVector2(1, 18), new RawVector2(250, 18), blackBrush);

            // draw mouse snapline
            if (mouseDown)
                DrawTarget.DrawLine(new RawVector2(DownClickX, DownClickY), new RawVector2(LastDownX, LastDownY), redBrush);

            if (GBlocks != null)
            {
                // start of each rendering loop
                GraphBlock.BlocksHeightOffset = 0;
                if (!del) // debugging stuffs
                {
                    //GBlocks.RemoveRange(GBlocks.Count - (GBlocks.Count - 22), GBlocks.Count - 22);
                    del = true;
                }
                DrawTarget.DrawText($"[{GBlocks.Count}]", fontSmall, new RawRectangleF(1, 21, 150, 20), blackBrush);

                // draw GBlocks
                foreach (var gb in GBlocks)
                    DrawBlock(gb);

                // draw GArrows
                DrawArrows();
            }

            DrawTarget.EndDraw();
        }

        private void CreateBlocks(List<LuaScriptBlock> blocks)
        {
            GBlocks = new List<GraphBlock>();
            int count = 0;
            GraphBlock.BlocksHeightOffset = 0;
            foreach (var b in blocks)
            {
                //if (b.Lines.Count == 1 && b.Lines[0].Instr.OpCode == LuaOpcode.FORLOOP)
                //    b.Text = "FORLOOP";

                GBlocks.Add(new GraphBlock(b, this.Width, this.Height));
            }
            //GraphBlock.BlocksHeightOffset /= 2; // TODO: find out why its multiplied by two 
        }

        private void CreateArrows()
        {
            GArrows = new List<GraphArrow>();
            // TODO: dont render all blocks

            // TODO: add thicc-nes
            // TODO: add 'points' so arrow end/start dont share start/destination
            // add arrow dict with block offsets if needed
            foreach (var GB in GBlocks)
            {
                // TODO: use both Block Index or Address Number for lines (merged block use block!)
                var thicc = GB.GetBlockWidth();
                if (GB.Block.JumpsTo == -1 && GB.Block.JumpsNext != -1)
                {
                    GArrows.Add(new GraphArrow(ref GBlocks)
                    {
                        Brush = blue2Brush,
                        Start = GB.Block.StartAddress,
                        End = GB.Block.JumpsNext
                    });
                }
                else if (GB.Block.JumpsNext != -1)
                {
                    //GB.BoundryBox.Left += thicc;
                    //GB.BoundryBox.Right += thicc;
                    GArrows.Add(new GraphArrow(ref GBlocks)
                    {
                        //Brush = redBrush,
                        Brush = greenBrush, // colors are reverse due to jump skip logic
                        Start = GB.Block.StartAddress,
                        End = GB.Block.JumpsNext
                    });
                }
                if (GB.Block.JumpsTo != -1)
                {
                    //GArrows.Add(block.JumpsToBlock, new RawVector2(((box.Left + box.Right) / 2), box.Bottom)); // green, jump OK
                    GArrows.Add(new GraphArrow(ref GBlocks)
                    {
                        //Brush = greenBrush,
                        Brush = redBrush, // colors are reverse due to jump skip logic
                        Start = GB.Block.StartAddress,
                        End = GB.Block.JumpsTo
                    });
                }
            }
        }

        private void DrawBlock(GraphBlock block)
        {
            // update box with mouse positions
            block.Update(graphX, graphY);

            if (!block.IsVisible(this.Width, this.Height))
                return;

            // draw
            DrawTarget.FillRectangle(block.BoundryBox, whiteBrush);
            DrawTarget.DrawLine(new RawVector2(block.BoundryBox.Left - 2, block.BoundryBox.Top), new RawVector2(block.BoundryBox.Right + 1, block.BoundryBox.Top), blackBrush);         // ----
            DrawTarget.DrawLine(new RawVector2(block.BoundryBox.Left - 2, block.BoundryBox.Top), new RawVector2(block.BoundryBox.Left - 2, block.BoundryBox.Bottom), blackBrush);       // |
            DrawTarget.DrawLine(new RawVector2(block.BoundryBox.Right + 1, block.BoundryBox.Top), new RawVector2(block.BoundryBox.Right + 1, block.BoundryBox.Bottom), blackBrush);     //      |
            DrawTarget.DrawLine(new RawVector2(block.BoundryBox.Left - 2, block.BoundryBox.Bottom), new RawVector2(block.BoundryBox.Right + 1, block.BoundryBox.Bottom), blackBrush);   // _____
            DrawTarget.DrawText(block.BlockInnerString, fontSmall, block.BoundryBox, blackBrush);
        }

        private void DrawArrows()
        {
            if (GArrows == null)
                return;

            foreach (var ar in GArrows)
            {
                var path = ar.CalculatePaths();
                RawVector2? lastPoint = null;
                foreach (var p in path)
                {
                    if (lastPoint.HasValue)
                        DrawTarget.DrawLine(lastPoint.Value, p, ar.Brush);
                    lastPoint = p;
                }
            }
        }

        private void frmGraph_MouseMove(object sender, MouseEventArgs e)
        {
            if (!mouseDown)
                return;

            graphX += (int)((LastDownX - e.X) * 1.25);
            graphY += (int)((LastDownY - e.Y) * 1.25);

            // do math and save
            LastDownX = e.X;
            LastDownY = e.Y;
        }

        private void frmGraph_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }

        private void frmGraph_MouseDown(object sender, MouseEventArgs e)
        {
            LastDownX = e.X; // reset
            LastDownY = e.Y;
            DownClickX = e.X;
            DownClickY = e.Y;
            mouseDown = true;
        }

        private void frmGraph_Scroll(object sender, ScrollEventArgs e)
        {
            // this is not accepting?
            if (e.OldValue > e.NewValue)
            {
                // zoom out
                this.Width = (int)(this.Width * 1.05);
                this.Height = (int)(this.Height * 1.05);
            }
            else
            {
                this.Width = (int)(this.Width * 0.95);
                this.Height = (int)(this.Height * 0.95);
            }

        }

    }
}
