using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StreamingMediaViewer
{
    //진행율 표시를 위한 폼
    public partial class ProgressForm : Form
    {
        private ProgressBar progressBar;
        public ProgressForm()
        {
            //InitializeComponent();

            this.Size = new System.Drawing.Size(300, 100);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Text = "처리 중...";
            this.StartPosition = FormStartPosition.CenterParent;

            progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Location = new System.Drawing.Point(10, 20),
                Size = new System.Drawing.Size(260, 20)
            };

            this.Controls.Add(progressBar);
        }
        public void ReportProgress(int percentage)
        {
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke(new Action(() => progressBar.Value = percentage));
            }
            else
            {
                progressBar.Value = percentage;
            }
        }
    }
}
