namespace ArcadeShooter.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        // Форма не использует конструктор дизайнера — всё содержимое
        // рисуется вручную в MainForm.cs (метод MainForm_Paint),
        // поэтому здесь только обязательный минимум.
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            SuspendLayout();
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Name = "MainForm";
            ResumeLayout(false);
        }
    }
}
