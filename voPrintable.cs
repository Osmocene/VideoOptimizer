using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Office.Interop.Word;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace VideoOptimizer
{

    class voPrintable
    {
        //Of the video.
        public struct PictureOrText
        {
            public string text;
            public Boolean isText;
            public double time;
            public PictureOrText(string Text, Boolean Istext, double Time)
            {
                text = Text;
                isText = Istext;
                time = Time;
            }
        }
        private string filename;
        private bool PDF;
        public voPrintable(string f)
        {
            filename = f;
        }
        public void Start(string sout)
        {
            PDF = Path.GetExtension(sout) == ".pdf";
            voVideoSaveable voVideoSaveable = new voVideoSaveable(filename);
            List<float> listfloat = voSave.DeserializeObject<List<float>>(voVideoSaveable.UFile);
            List<voSpeech.RecognizedText> listvospeech =
                voSave.DeserializeObject<List<voSpeech.RecognizedText>>(voSave.ConventionalFilepath(filename, "Transcription.p"));
            List<PictureOrText> listcontent = new List<PictureOrText>();
            List<PictureOrText> listparagraph = new List<PictureOrText>();
            foreach (float f in listfloat)
            {
                listcontent.Add(new PictureOrText("", false, f));
            }
            foreach (voSpeech.RecognizedText vsrt in listvospeech)
            {
                listcontent.Add(new PictureOrText(vsrt.text, true, vsrt.seconds));
            }
            listcontent = listcontent.OrderBy(x => x.time).ToList();
            foreach (PictureOrText pot in listcontent)
            {
                if (listparagraph == null || listparagraph.Count == 0)
                {
                    listparagraph.Add(pot);
                }
                else if (pot.isText && listparagraph.Last().isText)
                {
                    listparagraph[listparagraph.Count - 1] = new PictureOrText(listparagraph.Last().text + " " + pot.text, true, listparagraph.Last().time);
                }
                else
                {
                    listparagraph.Add(pot);
                }
            }
            Microsoft.Office.Interop.Word.Application winword = new Microsoft.Office.Interop.Word.Application();
            winword.ShowAnimation = false;
            winword.Visible = false;
            object missing = System.Reflection.Missing.Value;
            Document document = winword.Documents.Add(ref missing, ref missing, ref missing, ref missing);
            Microsoft.Office.Interop.Word.Paragraph para2 = document.Content.Paragraphs.Add(ref missing);
            object styleHeading2 = "Heading 2";
            para2.Range.set_Style(ref styleHeading2);
            para2.Range.Text = "Transcription: " + System.IO.Path.GetFileName(filename) + ". " + DateTime.Now.ToString("d MMM yyyy");
            para2.Range.InsertParagraphAfter();
            for (int i = 0; i < listparagraph.Count; i++)
            {
                if (listparagraph[i].isText)
                {
                    Microsoft.Office.Interop.Word.Paragraph para = document.Content.Paragraphs.Add(ref missing);
                    para.Range.Text = listparagraph[i].text;
                }
                else
                {
                    System.Drawing.Image image = voVideoSaveable.VideoImage((float)listparagraph[i].time);
                    image = ResizeImage(image, Convert.ToInt32(image.Size.Width / 1.5), Convert.ToInt32(image.Size.Height / 1.5));
                    string pictureFile = voSave.ConventionalFilepath(filename, "Temp.bmp");
                    image.Save(pictureFile);
                    Microsoft.Office.Interop.Word.Paragraph para = document.Content.Paragraphs.Add(ref missing);
                    Object oMissed = document.Paragraphs[document.Paragraphs.Count].Range;
                    Object oLinkToFile = false;
                    Object oSaveWithDocument = true;
                    para.Range.InlineShapes.AddPicture(pictureFile, ref oLinkToFile, ref oSaveWithDocument, ref missing);
                }
            }
            object osout = sout;
            if (PDF)
            {
                document.SaveAs2(ref osout, WdSaveFormat.wdFormatPDF);
            }
            else
            {
                document.SaveAs2(ref osout);
            }
            object saveChanges = WdSaveOptions.wdDoNotSaveChanges;
            document.Close(ref saveChanges, ref missing, ref missing);
            document = null;
            winword.Quit(ref missing, ref missing, ref missing);
            winword = null;
        }
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new System.Drawing.Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}
