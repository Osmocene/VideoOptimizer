using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.IO;
using NAudio.Wave;
namespace VideoOptimizer
{
    class voSpeech
    {
        public struct MinuteInterval
        {
            public TimeSpan begin;
            public TimeSpan end;
            public string structfilename;
            public MinuteInterval(TimeSpan b, TimeSpan e, string s)
            {
                begin = b; end = e; structfilename = s;
            }
        }
        public struct RecognizedText
        {
            public double seconds;
            public string text;
            public RecognizedText(double s, string t)
            {
                seconds = s; text = t;
            }
        }
        private string filename;
        private List<MinuteInterval> listminuteintervals;
        private TimeSpan totallength;
        private List<RecognizedText> listtranscription;
        public string Filename { get { return filename; } set { filename = value; } }
        public List<MinuteInterval> Listminuteintervals { get { return listminuteintervals; } set { listminuteintervals = value; } }
        public TimeSpan Totallength { get { return totallength; } set { totallength = value; } }
        public List<RecognizedText> Listtranscription { get { return listtranscription; } set { listtranscription = value; } }
        public voSpeech(string path)
        {
            filename = path;
            using (Mp3FileReader reader = new Mp3FileReader(filename))
            {
                totallength = reader.TotalTime;
            }
        }
        public void GetSegments()
        {
            List<MinuteInterval> outlist = new List<MinuteInterval>();
            TimeSpan begin = new TimeSpan(0, 0, 0);
            for (int i = 1; ; i++)
            {
                if (begin.Add(TimeSpan.FromSeconds(60)) < totallength)
                {
                    outlist.Add(new MinuteInterval(begin, begin.Add(TimeSpan.FromSeconds(60)), "Minute " + i + ".mp3"));
                    begin = begin.Add(TimeSpan.FromSeconds(60));
                }
                else
                {
                    outlist.Add(new MinuteInterval(begin, totallength, "Minute " + i + ".mp3"));
                    listminuteintervals = outlist;
                    voSave.SerializableObject(listminuteintervals, voSave.FileInSameFolder(filename, "Minutes.p"));
                    foreach (MinuteInterval mi in listminuteintervals)
                    {
                        WriteASegment(voSave.FileInSameFolder(filename, mi.structfilename), mi.begin, mi.end);
                    }
                    return;
                }
            }
        }
        /// <summary>
        /// Requires listminuteintervals to be built.
        /// </summary>
        public void CleanSegments()
        {
            foreach (MinuteInterval mi in listminuteintervals)
            {
                File.Delete(voSave.FileInSameFolder(filename, mi.structfilename));
            }
        }
        private void WriteASegment(string outfile, TimeSpan begin, TimeSpan end)
        {
            using (Mp3FileReader reader = new Mp3FileReader(filename))
            using (FileStream writer = File.Create(outfile))
            {
                Mp3Frame frame;
                reader.Skip((int)Math.Floor(begin.TotalSeconds) - 1);
                while ((frame = reader.ReadNextFrame()) != null)
                {
                    if (reader.CurrentTime >= begin && reader.CurrentTime <= end)
                    {
                        writer.Write(frame.RawData, 0, frame.RawData.Length);
                    }
                    else
                    {
                        if (reader.CurrentTime > end)
                            return;
                    }
                }
            }
        }
        public static void ConvertToWav(string infile, string outfile)
        {
            var ffMpeg = new NReco.VideoConverter.FFMpegConverter();
            ffMpeg.ConvertMedia(infile, outfile, "wav");
        }
        public async Task TranscribeMinutes(SpeechConfig speechConfig)
        {
            List<RecognizedText> outtranscription = new List<RecognizedText>();
            foreach (var mi in listminuteintervals)
            {
                ConvertToWav(voSave.FileInSameFolder(filename, mi.structfilename), voSave.FileInSameFolder(filename, "Temp.wav"));
                var localsrresult = await SR(voSave.FileInSameFolder(filename, "Temp.wav"), speechConfig);
                foreach (var r in localsrresult)
                {
                    if (r.Reason == ResultReason.RecognizedSpeech)
                    {
                        outtranscription.Add(new RecognizedText(mi.begin.TotalSeconds + (r.OffsetInTicks / 10000000), r.Text));
                    }
                }
                outtranscription = outtranscription.OrderBy(x => x.seconds).ToList();
                voSave.SerializableObject(outtranscription, voSave.FileInSameFolder(filename, "Transcription.p"));
                File.Delete(voSave.FileInSameFolder(filename, "Temp.wav"));
            }
            listtranscription = outtranscription;
        }
        public void DebugTM(SpeechConfig speechConfig)
        {
            List<RecognizedText> outtranscription = new List<RecognizedText>();
            foreach (var mi in listminuteintervals)
            {
                ConvertToWav(voSave.FileInSameFolder(filename, mi.structfilename), voSave.FileInSameFolder(filename, "Temp.wav"));
            }
        }
        private async Task<List<SpeechRecognitionResult>> SR(string filename, SpeechConfig config)
        {
            var stopRecognition = new TaskCompletionSource<bool>();
            List<SpeechRecognitionResult> listspeechrecognitionresult = new List<SpeechRecognitionResult>();
            using (var audioInput = AudioConfig.FromWavFileInput(filename))
            using (var recognizer = new SpeechRecognizer(config, audioInput))
            {
                recognizer.Recognized += (s, e) => listspeechrecognitionresult.Add(e.Result);
                recognizer.Canceled += (s, e) =>
                {
                    listspeechrecognitionresult.Add(e.Result);
                    stopRecognition.TrySetResult(false);
                };
                recognizer.SessionStopped += (s, e) => stopRecognition.TrySetResult(false);
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
                await stopRecognition.Task;
                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                var x = CancellationDetails.FromResult(listspeechrecognitionresult[0]);
                if (x.Reason == CancellationReason.Error)
                {
                    System.Windows.Forms.MessageBox.Show("Transcription error: " + x.ErrorDetails);
                }
                return listspeechrecognitionresult;
            }
        }
    }
}
