namespace PackFileManager {
    partial class PackedEsfEditor {
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

        #region Vom Komponenten-Designer generierter Code

        /// <summary> 
        /// Erforderliche Methode für die Designerunterstützung. 
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent() {
            this.esfComponent = new EsfControl.EditEsfComponent();
            this.SuspendLayout();
            // 
            // editEsfComponent1
            // 
            this.esfComponent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.esfComponent.Location = new System.Drawing.Point(0, 0);
            this.esfComponent.Name = "editEsfComponent1";
            this.esfComponent.RootNode = null;
            this.esfComponent.ShowCode = false;
            this.esfComponent.Size = new System.Drawing.Size(150, 150);
            this.esfComponent.TabIndex = 0;
            // 
            // PackedEsfEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.esfComponent);
            this.Name = "PackedEsfEditor";
            this.ResumeLayout(false);

        }

        #endregion

        private EsfControl.EditEsfComponent esfComponent;
    }
}
