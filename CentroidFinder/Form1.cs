using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Accord.Imaging;

namespace CentroidFinder
{
    public partial class Form1 : Form
    {
		private string FilePath = string.Empty;
		protected bool ValidData;

		public Form1()
        {
            InitializeComponent();
			panel.DragEnter += OnDragEnter;
			panel.DragDrop += OnDragDrop;
        }

        private Bitmap Score()
        {
			Bitmap bmp = (Bitmap)pb.BackgroundImage.Clone();
            HorizontalIntensityStatistics his = new HorizontalIntensityStatistics(bmp);
			VerticalIntensityStatistics vis = new VerticalIntensityStatistics(bmp);
			int[] hisArr = his.Red.ToArray();
			int[] visArr = vis.Red.ToArray();
			int[,] checkCollision = new int[bmp.Width + 1, bmp.Height + 1];
			using (Graphics  g = Graphics.FromImage(bmp))
            {
                for (int i = 0; i < bmp.Width; i++)
                {
					int j = bmp.Height / 2;
					int k = ConvertRange(hisArr.Max(), hisArr.Min(), (int)(-j * 0.5), (int)(j * 0.5), hisArr[i]);
					checkCollision[i, j + k] += 1;
					checkCollision[Math.Abs(i - bmp.Width), j + k] += 1;
					//g.DrawEllipse(Pens.Red, new Rectangle(i, j + k, 3, 3));
					//g.DrawEllipse(Pens.Green, new Rectangle(Math.Abs(i-bmp.Width), j + k, 3, 3));
				}
				for (int j = 0; j < bmp.Height; j++)
				{
					int i = bmp.Width / 2;
					int l = ConvertRange(visArr.Max(), visArr.Min(), (int)(-i * 0.5), (int)(i * 0.5), visArr[j]);
					//g.DrawEllipse(Pens.Blue, new Rectangle(i + l, j, 3, 3));
					//g.DrawEllipse(Pens.Yellow, new Rectangle(i + l, Math.Abs(j-bmp.Height), 3, 3));
					checkCollision[i + l, j] += 1;
					checkCollision[i + l, Math.Abs(j - bmp.Height)] += 1;
				}
				List<PointF> collisions = new List<PointF>();
				for (int i = 0; i < bmp.Width; i++)
				{
					for (int j = 0; j < bmp.Height; j++)
					{
						if (checkCollision[i,j] >= 2)
                        {
							collisions.Add(new PointF(i,j));
							g.FillEllipse(Brushes.Red, new Rectangle(i - 3, j - 3, 6, 6));
						}
					}
				}
				PointF centroid = GetCentroid(collisions);
				g.FillEllipse(Brushes.Gold, new Rectangle((int)(centroid.X - 10), (int)(centroid.Y - 10), 20, 20));
				g.FillEllipse(Brushes.Green, new Rectangle((int)(centroid.X - 2), (int)(centroid.Y - 2), 4, 4));
				Text = centroid.ToString();
			}
			return bmp;
		}

		public static int ConvertRange(
			int originalStart, int originalEnd, // original range
			int newStart, int newEnd, // desired range
			int value) // value to convert
		{
			double scale = (double)(newEnd - newStart) / (originalEnd - originalStart);
			return (int)(newStart + ((value - originalStart) * scale));
		}

		/// <summary>
		/// Method to compute the centroid of a polygon. This does NOT work for a complex polygon.
		/// </summary>
		/// <param name="poly">points that define the polygon</param>
		/// <returns>centroid point, or PointF.Empty if something wrong</returns>
		public static PointF GetCentroid(List<PointF> poly)
		{
			float accumulatedArea = 0.0f;
			float centerX = 0.0f;
			float centerY = 0.0f;

			for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
			{
				float temp = poly[i].X * poly[j].Y - poly[j].X * poly[i].Y;
				accumulatedArea += temp;
				centerX += (poly[i].X + poly[j].X) * temp;
				centerY += (poly[i].Y + poly[j].Y) * temp;
			}

			if (Math.Abs(accumulatedArea) < 1E-7f)
				return PointF.Empty;  // Avoid division by zero

			accumulatedArea *= 3f;
			return new PointF(centerX / accumulatedArea, centerY / accumulatedArea);
		}

		private void OnDragDrop(object sender, DragEventArgs e)
		{
			if (ValidData)
            {
				pb.BackgroundImage = new Bitmap(FilePath);
				pb.Image = Score();
			}				
		}

		private void OnDragEnter(object sender, DragEventArgs e)
		{
            ValidData = GetFilename(out FilePath, e);
			if (ValidData)
				e.Effect = DragDropEffects.Copy;
			else
				e.Effect = DragDropEffects.None;
		}

		protected bool GetFilename(out string filename, DragEventArgs e)
		{
			bool ret = false;
			filename = string.Empty;

			if ((e.AllowedEffect & DragDropEffects.Copy) == DragDropEffects.Copy)
			{
                if (((IDataObject)e.Data).GetData("FileDrop") is Array data)
                {
                    if ((data.Length == 1) && (data.GetValue(0) is String))
                    {
                        filename = ((string[])data)[0];
                        string ext = Path.GetExtension(filename).ToLower();
                        if ((ext == ".jpg") || (ext == ".png") || (ext == ".bmp"))
                            ret = true;
                    }
                }
            }
			return ret;
		}
	}
}
