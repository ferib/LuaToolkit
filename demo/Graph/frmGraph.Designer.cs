namespace Graph
{
    partial class frmGraph
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmGraph));
            this.SuspendLayout();
            // 
            // frmGraph
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(785, 688);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmGraph";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "Graph View";
            this.Scroll += new System.Windows.Forms.ScrollEventHandler(this.frmGraph_Scroll);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.frmGraph_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.frmGraph_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.frmGraph_MouseUp);
            this.ResumeLayout(false);

        }

        #endregion
    }
}

