namespace DecodeTool {
    partial class DecodeTool {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent() {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.definitionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.moreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.more1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.hexView = new System.Windows.Forms.RichTextBox();
            this.panel3 = new System.Windows.Forms.Panel();
            this.goProblem = new System.Windows.Forms.Button();
            this.goStart = new System.Windows.Forms.Button();
            this.forward = new System.Windows.Forms.Button();
            this.back = new System.Windows.Forms.Button();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.typeList = new System.Windows.Forms.ListBox();
            this.valueList = new System.Windows.Forms.ListBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.boolLabel = new System.Windows.Forms.Label();
            this.intLabel = new System.Windows.Forms.Label();
            this.stringLabel = new System.Windows.Forms.Label();
            this.setButton = new System.Windows.Forms.Button();
            this.showTypes = new System.Windows.Forms.Button();
            this.optStringType = new TypeSelection();
            this.singleType = new TypeSelection();
            this.boolType = new TypeSelection();
            this.button1 = new System.Windows.Forms.Button();
            this.intType = new TypeSelection();
            this.stringType = new TypeSelection();
            this.typeNameLabel = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.setHeader = new System.Windows.Forms.Button();
            this.headerLength = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.repeatInfo = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            //((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel3.SuspendLayout();
            //((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.definitionsToolStripMenuItem,
            this.moreToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(587, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // definitionsToolStripMenuItem
            // 
            this.definitionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadToolStripMenuItem,
            this.saveToolStripMenuItem});
            this.definitionsToolStripMenuItem.Name = "definitionsToolStripMenuItem";
            this.definitionsToolStripMenuItem.Size = new System.Drawing.Size(76, 20);
            this.definitionsToolStripMenuItem.Text = "Definitions";
            // 
            // loadToolStripMenuItem
            // 
            this.loadToolStripMenuItem.Name = "loadToolStripMenuItem";
            this.loadToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.loadToolStripMenuItem.Text = "Load";
            this.loadToolStripMenuItem.Click += new System.EventHandler(this.loadToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // moreToolStripMenuItem
            // 
            this.moreToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.more1ToolStripMenuItem});
            this.moreToolStripMenuItem.Name = "moreToolStripMenuItem";
            this.moreToolStripMenuItem.Size = new System.Drawing.Size(47, 20);
            this.moreToolStripMenuItem.Text = "More";
            // 
            // more1ToolStripMenuItem
            // 
            this.more1ToolStripMenuItem.Name = "more1ToolStripMenuItem";
            this.more1ToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            this.more1ToolStripMenuItem.Text = "Bool <-> OptString";
            this.more1ToolStripMenuItem.Click += new System.EventHandler(this.more1ToolStripMenuItem_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.hexView);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.panel3);
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Panel2.Controls.Add(this.panel2);
            this.splitContainer1.Panel2.Controls.Add(this.typeNameLabel);
            this.splitContainer1.Panel2.Controls.Add(this.panel1);
            this.splitContainer1.Panel2.Controls.Add(this.repeatInfo);
            this.splitContainer1.Size = new System.Drawing.Size(587, 548);
            this.splitContainer1.SplitterDistance = 195;
            this.splitContainer1.TabIndex = 1;
            // 
            // hexView
            // 
            this.hexView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.hexView.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.hexView.Location = new System.Drawing.Point(0, 0);
            this.hexView.Name = "hexView";
            this.hexView.Size = new System.Drawing.Size(195, 548);
            this.hexView.TabIndex = 0;
            this.hexView.Text = "";
            // 
            // panel3
            // 
            this.panel3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel3.Controls.Add(this.goProblem);
            this.panel3.Controls.Add(this.goStart);
            this.panel3.Controls.Add(this.forward);
            this.panel3.Controls.Add(this.back);
            this.panel3.Location = new System.Drawing.Point(2, 212);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(383, 26);
            this.panel3.TabIndex = 16;
            // 
            // goProblem
            // 
            this.goProblem.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.goProblem.Location = new System.Drawing.Point(246, 3);
            this.goProblem.Name = "goProblem";
            this.goProblem.Size = new System.Drawing.Size(75, 23);
            this.goProblem.TabIndex = 23;
            this.goProblem.Text = "problem";
            this.goProblem.UseVisualStyleBackColor = true;
            // 
            // goStart
            // 
            this.goStart.Location = new System.Drawing.Point(3, 3);
            this.goStart.Name = "goStart";
            this.goStart.Size = new System.Drawing.Size(75, 23);
            this.goStart.TabIndex = 22;
            this.goStart.Text = "<<";
            this.goStart.UseVisualStyleBackColor = true;
            // 
            // forward
            // 
            this.forward.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.forward.Location = new System.Drawing.Point(165, 3);
            this.forward.Name = "forward";
            this.forward.Size = new System.Drawing.Size(75, 23);
            this.forward.TabIndex = 21;
            this.forward.Text = ">";
            this.forward.UseVisualStyleBackColor = true;
            // 
            // back
            // 
            this.back.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.back.Location = new System.Drawing.Point(84, 3);
            this.back.Name = "back";
            this.back.Size = new System.Drawing.Size(75, 23);
            this.back.TabIndex = 20;
            this.back.Text = "<";
            this.back.UseVisualStyleBackColor = true;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer2.Location = new System.Drawing.Point(7, 4);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.typeList);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.valueList);
            this.splitContainer2.Size = new System.Drawing.Size(381, 202);
            this.splitContainer2.SplitterDistance = 124;
            this.splitContainer2.TabIndex = 15;
            // 
            // typeList
            // 
            this.typeList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.typeList.FormattingEnabled = true;
            this.typeList.Location = new System.Drawing.Point(0, 0);
            this.typeList.Name = "typeList";
            this.typeList.Size = new System.Drawing.Size(124, 202);
            this.typeList.TabIndex = 14;
            // 
            // valueList
            // 
            this.valueList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.valueList.FormattingEnabled = true;
            this.valueList.Location = new System.Drawing.Point(0, 0);
            this.valueList.Name = "valueList";
            this.valueList.Size = new System.Drawing.Size(253, 202);
            this.valueList.TabIndex = 13;
            this.valueList.SelectedIndexChanged += new System.EventHandler(this.valueList_SelectedIndexChanged);
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.Controls.Add(this.label5);
            this.panel2.Controls.Add(this.label4);
            this.panel2.Controls.Add(this.boolLabel);
            this.panel2.Controls.Add(this.intLabel);
            this.panel2.Controls.Add(this.stringLabel);
            this.panel2.Controls.Add(this.setButton);
            this.panel2.Controls.Add(this.showTypes);
            this.panel2.Controls.Add(this.optStringType);
            this.panel2.Controls.Add(this.singleType);
            this.panel2.Controls.Add(this.boolType);
            this.panel2.Controls.Add(this.button1);
            this.panel2.Controls.Add(this.intType);
            this.panel2.Controls.Add(this.stringType);
            this.panel2.Location = new System.Drawing.Point(7, 244);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(383, 215);
            this.panel2.TabIndex = 14;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(2, 159);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(47, 13);
            this.label5.TabIndex = 23;
            this.label5.Text = "optstring";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(2, 127);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(27, 13);
            this.label4.TabIndex = 22;
            this.label4.Text = "float";
            // 
            // boolLabel
            // 
            this.boolLabel.AutoSize = true;
            this.boolLabel.Location = new System.Drawing.Point(2, 89);
            this.boolLabel.Name = "boolLabel";
            this.boolLabel.Size = new System.Drawing.Size(27, 13);
            this.boolLabel.TabIndex = 21;
            this.boolLabel.Text = "bool";
            // 
            // intLabel
            // 
            this.intLabel.AutoSize = true;
            this.intLabel.Location = new System.Drawing.Point(2, 55);
            this.intLabel.Name = "intLabel";
            this.intLabel.Size = new System.Drawing.Size(18, 13);
            this.intLabel.TabIndex = 20;
            this.intLabel.Text = "int";
            // 
            // stringLabel
            // 
            this.stringLabel.AutoSize = true;
            this.stringLabel.Location = new System.Drawing.Point(2, 16);
            this.stringLabel.Name = "stringLabel";
            this.stringLabel.Size = new System.Drawing.Size(32, 13);
            this.stringLabel.TabIndex = 19;
            this.stringLabel.Text = "string";
            // 
            // setButton
            // 
            this.setButton.Location = new System.Drawing.Point(277, 187);
            this.setButton.Name = "setButton";
            this.setButton.Size = new System.Drawing.Size(75, 23);
            this.setButton.TabIndex = 18;
            this.setButton.Text = "Set";
            this.setButton.UseVisualStyleBackColor = true;
            this.setButton.Click += new System.EventHandler(this.setButton_Click);
            // 
            // showTypes
            // 
            this.showTypes.Location = new System.Drawing.Point(196, 187);
            this.showTypes.Name = "showTypes";
            this.showTypes.Size = new System.Drawing.Size(75, 23);
            this.showTypes.TabIndex = 17;
            this.showTypes.Text = "Show";
            this.showTypes.UseVisualStyleBackColor = true;
            this.showTypes.Click += new System.EventHandler(this.showTypes_Click);
            // 
            // optStringType
            // 
            this.optStringType.Location = new System.Drawing.Point(55, 150);
            this.optStringType.Name = "optStringType";
            this.optStringType.Size = new System.Drawing.Size(302, 31);
            this.optStringType.TabIndex = 16;
            this.optStringType.Type = null;
            // 
            // singleType
            // 
            this.singleType.Location = new System.Drawing.Point(55, 118);
            this.singleType.Name = "singleType";
            this.singleType.Size = new System.Drawing.Size(302, 31);
            this.singleType.TabIndex = 15;
            this.singleType.Type = null;
            // 
            // boolType
            // 
            this.boolType.Location = new System.Drawing.Point(55, 84);
            this.boolType.Name = "boolType";
            this.boolType.Size = new System.Drawing.Size(302, 28);
            this.boolType.TabIndex = 14;
            this.boolType.Type = null;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(5, 187);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 13;
            this.button1.Text = "Delete";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.delete_Click);
            // 
            // intType
            // 
            this.intType.Location = new System.Drawing.Point(55, 46);
            this.intType.Name = "intType";
            this.intType.Size = new System.Drawing.Size(302, 33);
            this.intType.TabIndex = 12;
            this.intType.Type = null;
            // 
            // stringType
            // 
            this.stringType.Location = new System.Drawing.Point(55, 9);
            this.stringType.Name = "stringType";
            this.stringType.Size = new System.Drawing.Size(302, 31);
            this.stringType.TabIndex = 11;
            this.stringType.Type = null;
            // 
            // typeNameLabel
            // 
            this.typeNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.typeNameLabel.AutoSize = true;
            this.typeNameLabel.Location = new System.Drawing.Point(7, 462);
            this.typeNameLabel.Name = "typeNameLabel";
            this.typeNameLabel.Size = new System.Drawing.Size(60, 13);
            this.typeNameLabel.TabIndex = 13;
            this.typeNameLabel.Text = "Typename:";
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.setHeader);
            this.panel1.Controls.Add(this.headerLength);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(3, 503);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(382, 42);
            this.panel1.TabIndex = 12;
            // 
            // setHeader
            // 
            this.setHeader.Location = new System.Drawing.Point(191, 10);
            this.setHeader.Name = "setHeader";
            this.setHeader.Size = new System.Drawing.Size(162, 23);
            this.setHeader.TabIndex = 8;
            this.setHeader.Text = "Set";
            this.setHeader.UseVisualStyleBackColor = true;
            this.setHeader.Click += new System.EventHandler(this.setHeaderLength_Click);
            // 
            // headerLength
            // 
            this.headerLength.Location = new System.Drawing.Point(85, 12);
            this.headerLength.Name = "headerLength";
            this.headerLength.Size = new System.Drawing.Size(100, 20);
            this.headerLength.TabIndex = 7;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "header length";
            // 
            // repeatInfo
            // 
            this.repeatInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.repeatInfo.AutoSize = true;
            this.repeatInfo.Location = new System.Drawing.Point(4, 487);
            this.repeatInfo.Name = "repeatInfo";
            this.repeatInfo.Size = new System.Drawing.Size(68, 13);
            this.repeatInfo.TabIndex = 9;
            this.repeatInfo.Text = "select data...";
            // 
            // DecodeTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(587, 572);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "DecodeTool";
            this.Text = "HexView";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            //((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            //((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.RichTextBox hexView;
        private System.Windows.Forms.Label repeatInfo;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button setHeader;
        private System.Windows.Forms.TextBox headerLength;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolStripMenuItem definitionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadToolStripMenuItem;
        private System.Windows.Forms.Label typeNameLabel;
        private System.Windows.Forms.Panel panel2;
        private TypeSelection optStringType;
        private TypeSelection singleType;
        private TypeSelection boolType;
        private System.Windows.Forms.Button button1;
        private TypeSelection intType;
        private TypeSelection stringType;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ListBox typeList;
        private System.Windows.Forms.ListBox valueList;
        private System.Windows.Forms.Button showTypes;
        private System.Windows.Forms.ToolStripMenuItem moreToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem more1ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.Button setButton;
        private System.Windows.Forms.Label stringLabel;
        private System.Windows.Forms.Label intLabel;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label boolLabel;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button goProblem;
        private System.Windows.Forms.Button goStart;
        private System.Windows.Forms.Button forward;
        private System.Windows.Forms.Button back;

    }
}

