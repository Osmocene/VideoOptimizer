using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.CognitiveServices.Speech;

namespace VideoOptimizer
{
    //The video analysis form.
    public partial class Form1 : Form
    {
        DataGridViewTextBoxColumn dataGridViewTextBoxColumn;
        DataGridViewButtonColumn dataGridViewButtonColumn;
        DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        DataGridViewButtonColumn dataGridViewButtonColumn2;
        DataGridViewButtonColumn dataGridViewButtonColumn3;
        DataTable table = new DataTable();
        voPrintable nextPrintable;
        List<voPipeline> list = new List<voPipeline>();
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            dataGridViewTextBoxColumn = new DataGridViewTextBoxColumn { HeaderText = "File", Width = 200, ReadOnly = true }; //0
            dataGridViewButtonColumn = new DataGridViewDisableButtonColumn { HeaderText = "Process", Text = "Start" }; //1
            dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn { HeaderText = "Get audio", ReadOnly = true }; //2
            dataGridViewTextBoxColumn3 = new DataGridViewTextBoxColumn { HeaderText = "Transcribe", ReadOnly = true }; //3
            dataGridViewTextBoxColumn4 = new DataGridViewTextBoxColumn { HeaderText = "Image analysis", ReadOnly = true }; //4
            dataGridViewButtonColumn2 = new DataGridViewDisableButtonColumn { HeaderText = "Image selection", ReadOnly = true }; //5
            dataGridViewButtonColumn3 = new DataGridViewDisableButtonColumn { HeaderText = "Export", ReadOnly = true }; //6

            dataGridView1.Columns.Add(dataGridViewTextBoxColumn);
            dataGridView1.Columns.Add(dataGridViewButtonColumn);
            dataGridView1.Columns.Add(dataGridViewTextBoxColumn2);
            dataGridView1.Columns.Add(dataGridViewTextBoxColumn3);
            dataGridView1.Columns.Add(dataGridViewTextBoxColumn4);
            dataGridView1.Columns.Add(dataGridViewButtonColumn2);
            dataGridView1.Columns.Add(dataGridViewButtonColumn3);
            dataGridView1.DataSource = table;
            timer1.Start();
            saveFileDialog1.Filter = "Word documents|*.docx|PDF|*.pdf";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            foreach (var f in openFileDialog1.FileNames)
            {
                if (!list.Select(x => x.Filename).Contains(f))
                {
                    table.Rows.Add();
                    dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[0].Value = System.IO.Path.GetFileName(f);
                    dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[1].Value = "Analyze";
                    dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[5].Value = "Edit";
                    ((DataGridViewDisableButtonCell)(dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[5])).Enabled = false;
                    dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[6].Value = "Save";
                    ((DataGridViewDisableButtonCell)(dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[6])).Enabled = false;
                    list.Add(new voPipeline(f, textBox1.Text, textBox2.Text));
                }
            }
        }

        private async void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var senderGrid = (DataGridView)sender;

            if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn &&
                e.RowIndex >= 0)
            {
                if (e.ColumnIndex == 1 && ((DataGridViewDisableButtonCell)(dataGridView1.Rows[e.RowIndex].Cells[1])).Enabled) //Start
                {
                    ((DataGridViewDisableButtonCell)(dataGridView1.Rows[e.RowIndex].Cells[1])).Enabled = false;
                    //await Task.Run(() => list[e.RowIndex].Pipeline());
                    await list[e.RowIndex].Pipeline();
                }
                if (e.ColumnIndex == 5 && ((DataGridViewDisableButtonCell)(dataGridView1.Rows[e.RowIndex].Cells[5])).Enabled) //Edit
                {
                    list[e.RowIndex].GetUserFrames();
                }
                if (e.ColumnIndex == 6 && ((DataGridViewDisableButtonCell)(dataGridView1.Rows[e.RowIndex].Cells[6])).Enabled) //Print
                {
                    nextPrintable = new voPrintable(list[e.RowIndex].Filename);
                    saveFileDialog1.ShowDialog();
                }
            }
        }
        private string PtoString(voPipeline.P p)
        {
            if (p == voPipeline.P.Working)
            {
                return "Working";
            }
            else
            { return p == voPipeline.P.Yes ? "Done" : "Not done"; }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {

        }
        private void saveFileDialog1_FileOk_1(object sender, CancelEventArgs e)
        {
            nextPrintable.Start(saveFileDialog1.FileName);
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    voPipeline.Progress Pr = list[i].returnprogress;
                    DataGridViewRow Row = dataGridView1.Rows[i];
                    Row.Cells[2].Value =
                        Pr.ToMp3 == voPipeline.P.Working || Pr.ToMp3Segments == voPipeline.P.Working
                        ? "Working" : PtoString(Pr.ToMp3Segments);
                    Row.Cells[3].Value = PtoString(Pr.ToTranscription);
                    Row.Cells[4].Value = PtoString(Pr.ToKeyFrames);
                    ((DataGridViewDisableButtonCell)(Row.Cells[5])).Enabled = Pr.ToKeyFrames == voPipeline.P.Yes;
                    if (Pr.ToKeyFrames == voPipeline.P.Yes && Pr.ToTranscription == voPipeline.P.Yes)
                    {
                        ((DataGridViewDisableButtonCell)(Row.Cells[1])).Enabled = false;
                    }
                    ((DataGridViewDisableButtonCell)(Row.Cells[6])).Enabled = Pr.ToUserKeyFrames == voPipeline.P.Yes;
                    dataGridView1.Update();
                    dataGridView1.Refresh();
                }
            }
        }
    }
    //DataGridViewDisableButtonColumn is a DataGridViewButtonColumn with buttons that can be disabled.
    //Source:
    //https://docs.microsoft.com/en-us/dotnet/framework/winforms/controls/disable-buttons-in-a-button-column-in-the-datagrid
    public class DataGridViewDisableButtonColumn : DataGridViewButtonColumn
    {
        public DataGridViewDisableButtonColumn()
        {
            this.CellTemplate = new DataGridViewDisableButtonCell();
        }
    }

    public class DataGridViewDisableButtonCell : DataGridViewButtonCell
    {
        private bool enabledValue;
        public bool Enabled
        {
            get
            {
                return enabledValue;
            }
            set
            {
                enabledValue = value;
            }
        }

        // Override the Clone method so that the Enabled property is copied.
        public override object Clone()
        {
            DataGridViewDisableButtonCell cell =
                (DataGridViewDisableButtonCell)base.Clone();
            cell.Enabled = this.Enabled;
            return cell;
        }

        // By default, enable the button cell.
        public DataGridViewDisableButtonCell()
        {
            this.enabledValue = true;
        }

        protected override void Paint(Graphics graphics,
            Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
            DataGridViewElementStates elementState, object value,
            object formattedValue, string errorText,
            DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            // The button cell is disabled, so paint the border,  
            // background, and disabled button for the cell.
            if (!this.enabledValue)
            {
                // Draw the cell background, if specified.
                if ((paintParts & DataGridViewPaintParts.Background) ==
                    DataGridViewPaintParts.Background)
                {
                    SolidBrush cellBackground =
                        new SolidBrush(cellStyle.BackColor);
                    graphics.FillRectangle(cellBackground, cellBounds);
                    cellBackground.Dispose();
                }

                // Draw the cell borders, if specified.
                if ((paintParts & DataGridViewPaintParts.Border) ==
                    DataGridViewPaintParts.Border)
                {
                    PaintBorder(graphics, clipBounds, cellBounds, cellStyle,
                        advancedBorderStyle);
                }

                // Calculate the area in which to draw the button.
                Rectangle buttonArea = cellBounds;
                Rectangle buttonAdjustment =
                    this.BorderWidths(advancedBorderStyle);
                buttonArea.X += buttonAdjustment.X;
                buttonArea.Y += buttonAdjustment.Y;
                buttonArea.Height -= buttonAdjustment.Height;
                buttonArea.Width -= buttonAdjustment.Width;

                // Draw the disabled button.                
                ButtonRenderer.DrawButton(graphics, buttonArea,
                    System.Windows.Forms.VisualStyles.PushButtonState.Disabled);

                // Draw the disabled button text. 
                if (this.FormattedValue is String)
                {
                    TextRenderer.DrawText(graphics,
                        (string)this.FormattedValue,
                        this.DataGridView.Font,
                        buttonArea, SystemColors.GrayText);
                }
            }
            else
            {
                // The button cell is enabled, so let the base class 
                // handle the painting.
                base.Paint(graphics, clipBounds, cellBounds, rowIndex,
                    elementState, value, formattedValue, errorText,
                    cellStyle, advancedBorderStyle, paintParts);
            }
        }
    }
}
