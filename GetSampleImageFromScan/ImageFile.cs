using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

namespace GetSampleImageFromScan
{
	public class ImageFile : IDisposable
	{
		public string Path { get; set; }
		public Bitmap Bitmap { get; private set; }  //hoặc Direct Bitmap
		public DirectBitmap BigDrBmp { get; private set; }
		public int Height { get; private set; }
		public int Width { get; private set; }
		public Int32[] Bits { get; private set; }
		public bool Disposed { get; private set; }
		protected GCHandle BitsHandle { get; private set; }

        public ImageFile(string Path)
        {
			Bitmap = (Bitmap)Image.FromFile(Path);
            Width = Bitmap.Width;
            Height = Bitmap.Height;
            Bits = new Int32[Width * Height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
			// ảnh Direct Bitmap đã convert và chứa dạng trong suốt của Bitmap gốc
			BigDrBmp = DirectBitmap.MakeDrbmpFromBmp(DirectBitmap.MakeBmpTrans(Bitmap));

		}
        public void Dispose()
		{
			if (Disposed) return;
			Disposed = true;
			Bitmap.Dispose();
			BigDrBmp.Dispose();
			BitsHandle.Free();
		}

		

		// biến ảnh panel thành list các array số nguyên của ma trận ảnh 28x28
		internal List<byte[]> GetNumberBytes()
		{
			if (BigDrBmp == null)
				return null;
			// list tokens gồm các token, mỗi token gồm list tọa độ XY pixel và thông số màu
			var tokens = new List<List<(int X, int Y, Color Color)>>();     
			var visited = new bool[BigDrBmp.Width, BigDrBmp.Height];
			//var token = new List<(int X, int Y, Color Color)>();
			for (int x = 0; x < BigDrBmp.Width; x++)
			//for (int x = Image.Width; x > 0 ; x--)
			{

				for (int y = 0; y < BigDrBmp.Height; y++)
				{
					var color = BigDrBmp.GetPixel(x, y);
					// duyệt pixel đến khi phát hiện có pixel trống thì ngắt, thêm token vào tokens
					if (!visited[x, y] && color.A != 0)
					{
						tokens.Add(FindTokenFrom(x, y, color, visited));
						y = 0;
						x = 0;
					}
					visited[x, y] = true;   // pixel được duyệt
				}
			}
			return PreprocessTokens(tokens);


		}



		private List<byte[]> PreprocessTokens(List<List<(int X, int Y, Color Color)>> tokens)
		{
			var number = new List<byte[]>();
			foreach (var toke in tokens)
			{
				var xs = toke.Select(t => t.X);
				var ys = toke.Select(t => t.Y);
				var minX = xs.Min();
				var minY = ys.Min();

				//chiều dài rộng của ảnh được khoanh, nhưng tại sao +1
				var w = xs.Max() - xs.Min() + 1;
				var h = ys.Max() - ys.Min() + 1;
				var bmp = new DirectBitmap(w, h);
				foreach (var p in toke)
				{
					// vẽ lại 1 ảnh dựa theo chuỗi giá trị byte trong token
					bmp.SetPixel(p.X - minX, p.Y - minY, p.Color);
				}
				// PadAndCenterImage về cỡ đã định
				// bao gồm cả các khâu DrawSquare, CropToSize, GetMassCenterOffset
				var bytes = PreprocessImage(bmp, new Size(28, 28), false);

				// chuỗi các ảnh 28x28 ngay ngắn xếp cạnh nhau, giá trị dạng list các array 1 chiều
				number.Add(bytes);
			}
			return number;
		}

		internal byte[] PreprocessImage(DirectBitmap bitmap, Size resizeTo, bool showPreProccessed)
		{
			if (bitmap == null)
				return null;
			var img = PadAndCenterImage(bitmap);
			if (img == null || img.Width < 0 || img.Height < 0)
				return null;

			if (showPreProccessed)
			{
				BigDrBmp = img;
                //Invalidate();
            }
            byte[] imgToByteArray = img.ToByteArray();
			return imgToByteArray;
		}

		// hàm này nên nằm bên class Direct bitmap?
		protected DirectBitmap PadAndCenterImage(DirectBitmap bitmap)
		{
			var drawnRect = bitmap.DrawnSquare();
			if (drawnRect == Rectangle.Empty)
				return null;
			//Graphics.DrawRectangle(Pens.Red, drawnRect);
			var bmp2020 = bitmap.CropToSize(drawnRect, 20, 20);

			//Make image larger and center on center of mass
			var off = bmp2020.GetMassCenterOffset();
			var bmp2828 = new DirectBitmap(28, 28);
			var gfx2828 = Graphics.FromImage(bmp2828.Bitmap);
			// vẽ 1 ảnh tại điểm đã chọn
			gfx2828.DrawImage(bmp2020.Bitmap, 4 - off.X, 4 - off.Y);

			bmp2020.Dispose();
			return bmp2828;
		}
		private Color EmptyPixel = Color.FromArgb(0, 0, 0, 0);

		private List<(int, int, Color)> FindTokenFrom(int startX, int startY, Color color, bool[,] visited)
		{
			var token = new List<(int, int, Color)>();
			token.Add((startX, startY, BigDrBmp.GetPixel(startX, startY)));
			var stack = new Stack<(int X, int Y, Color Color)>();
			stack.Push((startX, startY, color));
			while (stack.Count > 0)
			{
				var pixel = stack.Pop();
				for (int dx = -1; dx < 2; dx++)
				{
					for (int dy = -1; dy < 2; dy++)
					{
						var x = pixel.X + dx;
						var y = pixel.Y + dy;
						if (y < 0 || x < 0 || y > BigDrBmp.Height - 1 || x > BigDrBmp.Width - 1)
							continue;

						var c = BigDrBmp.GetPixel(x, y);
						if (!visited[x, y] && c != EmptyPixel)
						{
							stack.Push((x, y, c));
							token.Add((x, y, c));
						}
						visited[x, y] = true;
					}
				}
			}

			return token;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public Bitmap ArrayToBitmap(byte[] bytes)
		{
			Bitmap bmp = new Bitmap(28, 28);

			for (int i = 0; i < 764; i++)
			{
				int x = i / 28;
				int y = i % 28;
				// index y,x vì mảng nhận được được duyệt bằng vòng lặp y rồi x
				bmp.SetPixel(y, x, Color.FromArgb(bytes[i], 0, 0, 255));

			}
			//bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
			return bmp;
		}

		//public void Save1DigitImage(byte[] byts, string folderName)
		//{
		//	var bmp2828 = ArrayToBitmap(byts);
		//	var folderPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
		//	folderPath = folderPath.Substring(6) + @"\Characters\TestData\" + folderName;
		//	if (!Directory.Exists(folderPath))
		//	{
		//		Directory.CreateDirectory(folderPath);
		//	}
		//	string shortDate = DateTime.Now.Year + "." + DateTime.Now.Month + "." + DateTime.Now.Day;
		//	string shortTime = DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second;
		//	string fileName = folderName + "_" + shortDate + "_" + shortTime;
		//	string fullPath = folderPath + @"\" + fileName + ".png";
		//	// đổi tên file bị trùng
		//	int isDup = 0;
		//	while (File.Exists(fullPath))
		//	{
		//		isDup++;
		//		string stt = Convert.ToString(isDup);
		//		fullPath = folderPath + @"\" + fileName + "_(" + stt + ").png";
		//	}
		//	Bitmap newbmp = new Bitmap(bmp2828);
		//	bmp2828.Dispose();
		//	newbmp.Save(fullPath);

		//}
	}
		
}
