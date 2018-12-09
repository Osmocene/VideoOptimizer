using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VideoOptimizer
{
    public partial class Form2 : Form
    {
        private voPipeline vp;
        private string Filename;
        DataGridViewTextBoxColumn dataGridViewTextBoxColumn;
        DataGridViewCheckBoxColumn dataGridViewCheckBoxColumn;
        DataGridViewImageColumn dataGridViewImageColumn;
        List<float> kfi;
        List<float> ukfi;
        public Form2(string filename, voPipeline vPPipeline)
        {
            InitializeComponent();
            Filename = filename;
            vp = vPPipeline;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn { HeaderText = "Time", ReadOnly = true };
            dataGridViewCheckBoxColumn = new DataGridViewCheckBoxColumn { HeaderText = "Include?", Width = 80 };
            dataGridViewImageColumn = new DataGridViewImageColumn { HeaderText = "Image", Width = 200, ImageLayout = DataGridViewImageCellLayout.Zoom };
            dataGridView1.Columns.Add(dataGridViewTextBoxColumn);
            dataGridView1.Columns.Add(dataGridViewCheckBoxColumn);
            dataGridView1.Columns.Add(dataGridViewImageColumn);
            voVideoSaveable vPVideoSaveable = new voVideoSaveable(Filename);
            kfi = vPVideoSaveable.KeyFramesIndex();
            dataGridView1.RowTemplate.Height = 100;
            if (System.IO.File.Exists(vPVideoSaveable.UFile))
            {
                ukfi = voSave.DeserializeObject<List<float>>(vPVideoSaveable.UFile);
                foreach (float f in kfi)
                {
                    dataGridView1.Rows.Add(f.ToString(), ukfi.Contains(f), vPVideoSaveable.VideoImage(f));
                }
            }
            else
            {
                foreach (float f in kfi)
                {
                    dataGridView1.Rows.Add(f.ToString(), true, vPVideoSaveable.VideoImage(f));
                }
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<float> outkf = new List<float>();
            for (int i = 0; i < dataGridView1.RowCount; i++)
            {
                if (Convert.ToBoolean(dataGridView1.Rows[i].Cells[1].Value))
                {
                    outkf.Add(kfi[i]);
                }
            }
            voVideoSaveable vPVideoSaveable = new voVideoSaveable(Filename);
            voSave.SerializableObject(outkf, vPVideoSaveable.UFile);
            vp.UpdateProgress();
            Close();
        }
    }
}
