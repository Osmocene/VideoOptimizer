using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoOptimizer
{
    public class voPipeline
    {
        public struct Progress
        {
            public P ToMp3;
            public P ToMp3Segments;
            public P ToTranscription;
            public P ToKeyFrames;
            public P ToUserKeyFrames;
            public Progress(P mp3, P mp3segments, P transcription, P keyframes, P userframes)
            {
                ToMp3 = mp3;
                ToMp3Segments = mp3segments;
                ToTranscription = transcription;
                ToKeyFrames = keyframes;
                ToUserKeyFrames = userframes;
            }
        }
        public enum P { Yes, No, Working };
        private voVideoSaveable lVideoSaveable;
        private string filename;
        private string apikey;
        private string region;
        private Progress progress;

        public voPipeline(string f, string a, string r)
        {
            filename = f;
            apikey = a;
            region = r;
            lVideoSaveable = new voVideoSaveable(filename);
            progress = new Progress(P.No, P.No, P.No, P.No, P.No);
            UpdateProgress();
        }
        public string Filename { get { return filename; } set { filename = value; } }
        public Progress returnprogress { get { return progress; } set { progress = value; } }
        public void UpdateProgress()
        {
            progress.ToMp3 = System.IO.File.Exists(lVideoSaveable.Mp3File) ? P.Yes : P.No;
            progress.ToMp3Segments = System.IO.File.Exists(voSave.ConventionalFilepath(filename, "Minutes.p")) ? P.Yes : P.No;
            progress.ToTranscription = System.IO.File.Exists(voSave.ConventionalFilepath(filename, "Transcription.p")) && !System.IO.File.Exists(voSave.ConventionalFilepath(filename, "Temp.wav")) ? P.Yes : P.No;
            progress.ToUserKeyFrames = System.IO.File.Exists(voSave.ConventionalFilepath(filename, "Userstats.p")) ? P.Yes : P.No;
            try
            {
                List<float> kfi = voSave.DeserializeObject<List<float>>(lVideoSaveable.PFile);
                progress.ToKeyFrames = P.Yes;
            }
            catch { progress.ToKeyFrames = P.No; }
            progress.ToUserKeyFrames = System.IO.File.Exists(lVideoSaveable.UFile) ? P.Yes : P.No;
            if (progress.ToUserKeyFrames == P.Yes) { progress.ToKeyFrames = P.Yes; }
            if (progress.ToTranscription == P.Yes) { progress.ToMp3Segments = P.Yes; }
            if (progress.ToMp3Segments == P.Yes) { progress.ToMp3 = P.Yes; }
        }
        public void GetMp3()
        {
            lVideoSaveable.SaveToMp3();
        }
        public void GetFrames()
        {
            lVideoSaveable.KeyFramesIndex();
        }
        public void GetMp3Segments()
        {
            voSpeech vP = new voSpeech(lVideoSaveable.Mp3File);
            vP.GetSegments();
        }
        public async Task GetTranscription()
        {
            voSpeech vP = new voSpeech(lVideoSaveable.Mp3File);
            vP.Listminuteintervals = voSave.DeserializeObject<List<voSpeech.MinuteInterval>>(voSave.FileInSameFolder(vP.Filename, "Minutes.p"));
            Microsoft.CognitiveServices.Speech.SpeechConfig localspeechconfig = Microsoft.CognitiveServices.Speech.SpeechConfig.FromSubscription(apikey, region);
            await vP.TranscribeMinutes(localspeechconfig);
        }
        public void CleanTranscription()
        {
            voSpeech vP = new voSpeech(lVideoSaveable.Mp3File);
            vP.Listminuteintervals = voSave.DeserializeObject<List<voSpeech.MinuteInterval>>(voSave.FileInSameFolder(vP.Filename, "Minutes.p"));
            vP.CleanSegments();

            try { System.IO.File.Delete(voSave.FileInSameFolder(vP.Filename, "Minutes.p")); }
            catch { }
            try { System.IO.File.Delete(vP.Filename); }
            catch { }
        }
        public async Task Pipeline()
        {
            if (progress.ToMp3 == P.No)
            {
                progress.ToMp3 = P.Working;
                GetMp3();
                progress.ToMp3 = P.Yes;
            }
            if (progress.ToMp3 == P.Yes && progress.ToMp3Segments == P.No)
            {
                progress.ToMp3Segments = P.Working;
                GetMp3Segments();
                progress.ToMp3Segments = P.Yes;
            }
            if (progress.ToMp3Segments == P.Yes && progress.ToTranscription == P.No)
            {
                progress.ToTranscription = P.Working;
                await GetTranscription();
                progress.ToTranscription = P.Yes;
                //CleanTranscription();
            }
            if (progress.ToKeyFrames == P.No)
            {
                progress.ToKeyFrames = P.Working;
                await Task.Run(() => GetFrames());
                progress.ToKeyFrames = P.Yes;
            }
        }
        public void GetUserFrames()
        {
            Form2 form2 = new Form2(filename, this);
            form2.Show();
        }
    }
}
