using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
namespace VideoOptimizer
{
    using ImagestatsDict = Dictionary<float, float[]>;
    /// <summary>
    /// Dependencies: NReco (ffmpeg wrapper)
    /// </summary>
    public class voVideo
    {
        private string directory;
        //private VideoCapture videocapture;
        private float length;
        protected double sensitivity = 0.25;
        private NReco.VideoInfo.FFProbe ffProbeWrapper = new NReco.VideoInfo.FFProbe();
        /// <summary>
        /// Create a new LVideo object that can extract and analyze video frames. Needs the file path.
        /// </summary>
        /// <param name="path"></param>
        public voVideo(string path)
        {
            directory = path;
            var fmi = ffProbeWrapper.GetMediaInfo(path);
            length = (float)fmi.Duration.TotalSeconds;
        }
        /// <summary>
        /// Length in seconds
        /// </summary>
        public double Length
        {
            get { return length; }
        }
        public Image VideoImage(float time)
        {
            time = time > length ? length : time;
            var ffMpeg = new NReco.VideoConverter.FFMpegConverter();
            var imageStream = new MemoryStream();
            ffMpeg.GetVideoThumbnail(directory, imageStream, time);
            try
            {
                return Image.FromStream(imageStream);
            }
            catch (ArgumentException)
            {
                return VideoImage(time - 1);
            }
        }
        /// <summary>
        ///Shrinks a bitmap down to 200x200 and then loops through pixels. 
        ///Computes mean red, green, blue, standard deviation in red, green and blue in a float[] of length 6. 
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        private float[] ImageAnalysis(Bitmap bitmap)
        {
            Bitmap newImage = new Bitmap(200, 200);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                gr.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                gr.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                gr.DrawImage(bitmap, new Rectangle(0, 0, 200, 200));
            }
            int n = (newImage.Width * newImage.Height);
            float rs = 0, gs = 0, bs = 0;
            for (int i = 0; i < newImage.Width; i++)
            {
                for (int j = 0; j < newImage.Height; j++)
                {
                    Color C = newImage.GetPixel(i, j);
                    rs += C.R;
                    gs += C.G;
                    bs += C.B;
                }
            }
            rs /= n;
            gs /= n;
            bs /= n;
            float rt = 0, gt = 0, bt = 0;
            for (int i = 0; i < newImage.Width; i++)
            {
                for (int j = 0; j < newImage.Height; j++)
                {
                    Color C = newImage.GetPixel(i, j);
                    rt += (C.R - rs) * (C.R - rs);
                    gt += (C.G - gs) * (C.G - gs);
                    bt += (C.B - bs) * (C.B - bs);
                }
            }
            rt /= n;
            gt /= n;
            bt /= n;
            rt = (float)Math.Sqrt(rt);
            gt = (float)Math.Sqrt(gt);
            bt = (float)Math.Sqrt(bt);
            return new float[6] { rs, gs, bs, rt, gt, bt };
        }
        /// <summary>
        ///Builds an Imagestatsdict, or resumes building an incomplete imagestatsDict parameter. 
        ///Assume that the video will not return to the exact frame after 25.6s.
        /// </summary>
        /// <param name="imagestatsDict"></param>
        /// <returns></returns>
        protected ImagestatsDict getAnalysis(ImagestatsDict imagestatsDict)
        {
            imagestatsDict = imagestatsDict ?? new ImagestatsDict();
            SegmentAnalysis(imagestatsDict, 0, length, (float)25.6);
            Action<float> scanFrames;
            scanFrames = delegate (float f) {
                var orderedimagestatsDict = imagestatsDict.OrderBy(item => item.Key);
                KeyValuePair<float, float[]> oldPair = orderedimagestatsDict.First();
                foreach (var item in orderedimagestatsDict.Skip(1))
                {
                    if (Different(imagestatsDict, item.Key, oldPair.Key, (float)sensitivity)) //Sensitivity is 0.25
                    {
                        SegmentAnalysis(imagestatsDict, oldPair.Key, item.Key, f);
                    }
                    oldPair = item;
                }
            };
            scanFrames((float)12.8);
            scanFrames((float)6.4);
            scanFrames((float)3.2);
            scanFrames((float)1.6);
            scanFrames((float)0.8);
            scanFrames((float)0.4);
            scanFrames((float)0.2);
            return imagestatsDict;
        }
        /// <summary>
        /// Modifies the incompleteDict by filling in ImageAnalysis outputs from start to end per interval.
        /// </summary>
        /// <param name="incompleteDict"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="interval"></param>
        private void SegmentAnalysis(ImagestatsDict incompleteDict, float start, float end, float interval)
        {
            //incompleteDict may already contain information
            for (float time = (float)Math.Round(start, 1); time < end + 0.1; time = (float)Math.Round(time + interval, 1))
            {
                if (incompleteDict.ContainsKey((float)Math.Round(time, 1)))
                { }
                else
                {
                    float[] iavp = ImageAnalysis(new Bitmap(VideoImage(time)));
                    incompleteDict.Add(time, iavp);
                    Save(incompleteDict);
                    if (Stop())
                    {
                        return;
                    }
                }
            }
        }
        /// <summary>
        /// Save runs every time a frame is analyzed in getAnalysis. 
        /// Ex. you can inherit and overide to serialize imgd to a file every 50 executions of save
        /// </summary>
        protected virtual void Save(ImagestatsDict imgd)
        {
        }
        protected virtual bool Stop()
        {
            return false;
        }
        /// <summary>
        /// Examines the image statistics dictionary at time index1 and index2 and returns whether the frames are different enough. 
        /// Based on distance between standard-deviation-of-R,G,B vectors
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="index1"></param>
        /// <param name="index2"></param>
        /// <param name="fsensitivity"></param>
        /// <returns></returns>
        private Boolean Different(ImagestatsDict dictionary, float index1, float index2, float fsensitivity)
        {
            float e = (float)Math.Round(index1, 1);
            Boolean a = dictionary.TryGetValue(e, out float[] float1);
            float f = (float)Math.Round(index2, 1);
            Boolean b = dictionary.TryGetValue(f, out float[] float2);
            return a && b && Math.Sqrt(((float1[3] - float2[3]) * (float1[3] - float2[3])) + ((float1[4] - float2[4]) * (float1[4] - float2[4])) + ((float1[5] - float2[5]) * (float1[5] - float2[5]))) > fsensitivity;
        }
        /// <summary>
        /// Returns a list of floats representing times when the video has a new key frame.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="fsensitivity"></param>
        /// <param name="videolength"></param>
        /// <returns></returns>
        protected List<float> KeyFramesIndex(ImagestatsDict dictionary, float fsensitivity, float videolength = 4)
        {
            List<float> list = new List<float>();
            Boolean b;
            float i = 0;
            var orderedDictionary = dictionary.OrderBy(y => y.Key);
            KeyValuePair<float, float[]> x_1 = orderedDictionary.First();
            foreach (KeyValuePair<float, float[]> x in orderedDictionary.Skip(1))
            {
                b = Different(dictionary, x_1.Key, x.Key, i > 0 ? fsensitivity / 2 : fsensitivity);
                if (b)
                {
                    if (i <= 0)
                    {
                        list.Add((float)Math.Round(x.Key, 1));
                    }
                    i = videolength;
                }
                else
                {
                    i -= x.Key - x_1.Key;
                }
                x_1 = x;
            }
            return list;
        }
        /// <summary>
        /// Returns a list of floats representing times when the video has a new key frame. Ignores animations (when there is frequent change in a small interval of time). 
        /// Overridable!
        /// </summary>
        /// <returns></returns>
        public virtual List<float> KeyFramesIndex()
        {
            return KeyFramesIndex(getAnalysis(new ImagestatsDict()), (float)0.5);
        }
        protected void ConvertToMp3(string outdirectory)
        {
            var ffMpeg = new NReco.VideoConverter.FFMpegConverter();
            ffMpeg.ConvertMedia(directory, outdirectory, "mp3");
        }
    }
}
