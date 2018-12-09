using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VideoOptimizer
{
    using ImagestatsDict = Dictionary<float, float[]>;
    public class voVideoSaveable : voVideo
    {
        //Will save to this
        private string pfile;
        private string ufile;
        private string mp3file;
        Random rnd = new Random();
        private int i = 0;
        public string PFile { get { return pfile; } }
        public string UFile { get { return ufile; } }
        public string Mp3File { get { return mp3file; } }
        public voVideoSaveable(string path) : base(path)
        {
            pfile = voSave.ConventionalFilepath(path, "stats.p");
            ufile = voSave.ConventionalFilepath(path, "Userstats.p");
            mp3file = voSave.ConventionalFilepath(path, "audio.mp3");
            voSave.CreateOrganizingFolder(path);
        }

        //Path = object to be worked on ex. @C:\Users\YHM\Desktop\Videos\sample.mp4
        //Filename = ex. stats.p
        protected override void Save(ImagestatsDict imgd)
        {
            if (rnd.Next(1, 40) == 10)
            {
                voSave.SerializableObject(imgd, pfile);
            }
        }
        protected override bool Stop()
        {
            if (i < 100)
            {
                i++;
                return false;
            }
            else
            {
                //return true;
                return false;
            }
        }
        public void Refresh()
        {
            i = 0;
        }
        /// <summary>
        /// Will try to get a float[] from pfile. 
        /// If unsuccessful, try to get an ImagestatsDict from pfile, and getAnalysis to complete. Save float[] to pfile. <br/>
        /// </summary>
        /// <returns></returns>
        public override List<float> KeyFramesIndex()
        {
            //
            try
            {
                List<float> kfi = voSave.DeserializeObject<List<float>>(pfile);
                return kfi;
            }
            catch { }
            ImagestatsDict dictionary = new ImagestatsDict();
            try
            {
                dictionary = voSave.DeserializeObject<ImagestatsDict>(pfile);
            }
            catch { }
            List<float> outkfi = KeyFramesIndex(getAnalysis(dictionary), (float)0.5);
            voSave.SerializableObject(outkfi, pfile);
            return outkfi;
        }
        public void SaveToMp3()
        {
            ConvertToMp3(mp3file);
        }
    }
}
