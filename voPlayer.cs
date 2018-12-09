using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows;


namespace VideoOptimizer
{
    public partial class voPlayer : Form
    {
        int currentsegmentindex = 0;
        string Ffilename;
        List<voSpeech.RecognizedText> Listvcrt;
        List<float> pfile;
        Boolean transcriptionexists = false;
        Boolean pexists = false;
        Boolean spoil = false;
        double lastprevposition;
        double lastnextposition;
        public voPlayer()
        {
            InitializeComponent();
        }

        private void wmvE_Load(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void wmvE_KeyPress(object sender, KeyPressEventArgs e)
        {
        }
        private void HandleKeyPress()
        {

        }
        private void button3_Click(object sender, EventArgs e)
        {
            Switch();
        }
        private void Switch()
        {
            if (axWindowsMediaPlayer1.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                axWindowsMediaPlayer1.Ctlcontrols.pause();
            }
            else
            {
                axWindowsMediaPlayer1.Ctlcontrols.play();
            }
        }
        [STAThread]
        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            LoadTranscription(openFileDialog1.FileName);
        }
        private void LoadTranscription(string filename)
        {
            Ffilename = filename;
            axWindowsMediaPlayer1.URL = filename;
            axWindowsMediaPlayer1.Ctlcontrols.play();
            if (System.IO.File.Exists(voSave.ConventionalFilepath(filename, "Transcription.p")) && !System.IO.File.Exists(voSave.ConventionalFilepath(filename, "Temp.wav")))
            {
                transcriptionexists = true;
                Listvcrt = voSave.DeserializeObject<List<voSpeech.RecognizedText>>(voSave.ConventionalFilepath(filename, "Transcription.p"));            
            }
            else
            {
                transcriptionexists = false;
                Listvcrt = new List<voSpeech.RecognizedText>();
            }
            if (System.IO.File.Exists(voSave.ConventionalFilepath(filename, "stats.p")))
            {
                pexists = true;
                pfile = voSave.DeserializeObject<List<float>>(voSave.ConventionalFilepath(filename, "stats.p"));
                if (pfile.Count <= 1)
                { pexists = false; }
            }
            else
            {
                pexists = false;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (transcriptionexists == true)
            {
                double currenttime = axWindowsMediaPlayer1.Ctlcontrols.currentPosition;
                int nextsegmentindex = 0;
                string temp = "";
                for (int i = 1; i < Listvcrt.Count(); i++ )
                {
                    if (Listvcrt[i].seconds > currenttime && Listvcrt[i-1].seconds <= currenttime)
                    {
                        nextsegmentindex = i - 1;
                        temp = Listvcrt[nextsegmentindex].text;
                        break;
                    }
                    else
                    {
                        nextsegmentindex = i;
                    }
                }
                if (nextsegmentindex != currentsegmentindex)
                {
                    currentsegmentindex = nextsegmentindex;
                    if (spoil)
                    { textBox1.Text = Listvcrt[currentsegmentindex].text; }
                }
            }
        }
        private void button5_Click(object sender, EventArgs e)
        {
            NextSection();
        }
        private void button6_Click(object sender, EventArgs e)
        {
            PrevSection();
        }
        private void NextSection()
        {
            if (Listvcrt == null || Listvcrt.Count <= 1)
            {
                textBox1.Text = "Empty";
                return;
            }
            if (currentsegmentindex + 1 >= Listvcrt.Count)
            {
                axWindowsMediaPlayer1.Ctlcontrols.currentPosition = Listvcrt[0].seconds;
            }
            else
            {
                axWindowsMediaPlayer1.Ctlcontrols.currentPosition = Listvcrt[currentsegmentindex + 1].seconds;
            }
            Switch();
            Switch();
        }
        private void PrevSection()
        {
            if (currentsegmentindex - 1 < 0)
            {
                axWindowsMediaPlayer1.Ctlcontrols.currentPosition = 0;
            }
            else
            {
                axWindowsMediaPlayer1.Ctlcontrols.currentPosition = Listvcrt[currentsegmentindex - 1].seconds;
            }
            Switch();
            Switch();
        }
        private void Next10()
        {
            lastnextposition = axWindowsMediaPlayer1.Ctlcontrols.currentPosition;
            if (lastprevposition - lastnextposition> 1 && lastprevposition - lastprevposition < 10)
            {
                axWindowsMediaPlayer1.Ctlcontrols.currentPosition = lastprevposition;
            }
            else
            {
                axWindowsMediaPlayer1.Ctlcontrols.currentPosition = Math.Min(axWindowsMediaPlayer1.currentMedia.duration, axWindowsMediaPlayer1.Ctlcontrols.currentPosition + 10);
            }
            Switch();
            Switch();
        }
        private void Prev10()
        {
            lastprevposition = axWindowsMediaPlayer1.Ctlcontrols.currentPosition;
            if (lastprevposition - lastnextposition > 1 && lastprevposition - lastnextposition < 25)
            {
                axWindowsMediaPlayer1.Ctlcontrols.currentPosition = lastnextposition;
            }
            else
            {
                axWindowsMediaPlayer1.Ctlcontrols.currentPosition = Math.Max(0, axWindowsMediaPlayer1.Ctlcontrols.currentPosition - 10);
            }
            Switch();
            Switch();
        }
        private void NextP()
        {
            if (!pexists)
            { return; }
            double currenttime = axWindowsMediaPlayer1.Ctlcontrols.currentPosition;
            for(int i = 0; i < pfile.Count ;i++ )
            {
                if (pfile[i] > currenttime + 2)
                {
                    axWindowsMediaPlayer1.Ctlcontrols.currentPosition = pfile[i];
                    Switch();
                    Switch();
                    return;
                }
            }
            axWindowsMediaPlayer1.Ctlcontrols.currentPosition = 0;
        }
        private void PrevP()
        {
            if (!pexists)
            { return; }
            double currenttime = axWindowsMediaPlayer1.Ctlcontrols.currentPosition;
            for (int i = pfile.Count - 1; i >= 0; i--)
            {
                if (pfile[i] < currenttime - 2)
                {
                    axWindowsMediaPlayer1.Ctlcontrols.currentPosition = pfile[i];
                    Switch();
                    Switch();
                    return;
                }
            }
            axWindowsMediaPlayer1.Ctlcontrols.currentPosition = pfile[pfile.Count - 1];
        }
        private void button7_Click(object sender, EventArgs e)
        {
            Next10();
        }
        private void button8_Click(object sender, EventArgs e)
        {
            Prev10();
        }
        private void wmvE_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    {
                        Prev10();
                        break;
                    }
                case Keys.Right:
                    {
                        Next10();
                        break;
                    }
                case Keys.Up:
                    {
                        PrevSection();
                        break;
                    }
                case Keys.Down:
                    {
                        NextSection();
                        break;
                    }
                case Keys.Space:
                    {
                        Switch();
                        break;
                    }
                case Keys.Enter:
                    {
                        openFileDialog1.ShowDialog();
                        break;
                    }
                case Keys.D:
                    {
                        NextP();
                        break;
                    }
                case Keys.A:
                    {
                        PrevP();
                        break;
                    }
            }
        }
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            wmvE_KeyDown(sender, e);
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            spoil = checkBox1.Checked;
            if (!spoil)
            {
                textBox1.Text = "";
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            NextP();
        }
        private void button4_Click(object sender, EventArgs e)
        {
            PrevP();
        }
    }
}
