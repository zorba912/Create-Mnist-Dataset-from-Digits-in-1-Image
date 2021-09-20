using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace GetSampleImageFromScan
{
	public class DirectBitmap : IDisposable
	{
		public Bitmap Bitmap { get; set; }
		public Int32[] Bits { get; private set; }
		public bool Disposed { get; private set; }
		public int Height { get; private set; }
		public int Width { get; private set; }

		protected GCHandle BitsHandle { get; private set; }

		public DirectBitmap(int width, int height)
		{
			Width = width;
			Height = height;
			Bits = new Int32[width * height];
			BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
			Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());
		}

		public void SetPixel(int x, int y, Color colour)
		{
			int index = x + (y * Width);
			int col = colour.ToArgb();

			Bits[index] = col;
		}

		public Color GetPixel(int x, int y)
		{
			int index = x + (y * Width);
			int col = Bits[index];
			Color result = Color.FromArgb(col);

			return result;
		}

		public void Dispose()
		{
			if (Disposed) return;
			Disposed = true;
			Bitmap.Dispose();
			BitsHandle.Free();
		}

		/// <summary>
		/// Finds smallest square around the drawing.
		/// </summary>
		/// <returns></returns>
		public Rectangle DrawnSquare()
		{
			// vì giới hạn trái sẽ trượt dần về bên phải, phải về trái...
			var fromX = int.MaxValue;
			var toX = int.MinValue;
			var fromY = int.MaxValue;
			var toY = int.MinValue;
			var empty = true;
			for (int y = 0; y < Bitmap.Height; y++)
			{
				for (int x = 0; x < Bitmap.Width; x++)
				{
					var pixel = Bitmap.GetPixel(x, y);
					if (pixel.A > 0)
					{
						empty = false;
						// quét từ trái qua phải, từ trên xuống dưới
						// nếu phát hiện có điểm not empty nằm bên trái hơn cả giới hạn trái
						// thì dời giới hạn trái về điểm đó
						if (x < fromX)
							fromX = x;
						if (x > toX)
							toX = x;
						if (y < fromY)
							fromY = y;
						if (y > toY)
							toY = y;
					}
				}
			}
			if (empty)
				return Rectangle.Empty;
			int dx = toX - fromX;
			int dy = toY - fromY;
			var side = Math.Max(dx, dy);
			if (dy > dx)
				fromX -= (side - dx) / 2;
			else
				fromY -= (side - dy)/ 2;
			//g.DrawRectangle(red, minX, minY, maxX - minX, maxY - minY);
			return new Rectangle(fromX, fromY, side, side);
		}

		/// <summary>
		/// Crops a portion of the bitmap and return a new bitmap with a new size.
		/// </summary>
		/// <param name="drawnRect"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <returns></returns>
		public DirectBitmap CropToSize(Rectangle drawnRect, int width, int height)
		{
			var bmp = new DirectBitmap(width, height);
			bmp.Bitmap.SetResolution(Bitmap.HorizontalResolution, Bitmap.VerticalResolution);

			var gfx = Graphics.FromImage(bmp.Bitmap);
			gfx.CompositingQuality = CompositingQuality.HighQuality;
			gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
			gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;
			gfx.SmoothingMode = SmoothingMode.AntiAlias;
			var rect = new Rectangle(0, 0, width, height);
			//using (var wrapMode = new ImageAttributes())
			//{
			//	wrapMode.SetWrapMode(WrapMode.Tile);
			//	gfx.DrawImage(Bitmap, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
			//}
			gfx.DrawImage(Bitmap, rect, drawnRect, GraphicsUnit.Pixel);
			return bmp;
		}
		
		/// <summary>
		/// Returns the offset of mass center of the drawing.
		/// </summary>
		/// <returns></returns>
		public Point GetMassCenterOffset()
		{
			var path = new List<Vector2>();
			for (int y = 0; y < Height; y++)
			{
				for (int x = 0; x < Width; x++)
				{
					var c = GetPixel(x, y);
					if (c.A > 0)
						path.Add(new Vector2(x, y));
				}
			}
			var centroid = path.Aggregate(Vector2.Zero, (current, point) => current + point) / path.Count();
			return new Point((int)centroid.X - Width / 2, (int)centroid.Y - Height / 2);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="image"></param>
		/// <returns></returns>
		public byte[] ToByteArray()
		{
			var bytes = new List<byte>();
			for (int y = 0; y < Bitmap.Height; y++)
			{
				for (int x = 0; x < Bitmap.Width; x++)
				{
					var color = Bitmap.GetPixel(x, y);
					var i = color.A;
					bytes.Add(i);
				}
			}
			return bytes.ToArray();
		}

		public DirectBitmap PadAndCenterImage(DirectBitmap bitmap)
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
			gfx2828.DrawImage(bmp2020.Bitmap, 4 - off.X, 4 - off.Y);

			bmp2020.Dispose();
			return bmp2828;
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
				// Image = img;
				//Invalidate();
			}

			return img.ToByteArray();
		}

		//public void save1BitmapToPng(DirectBitmap bmp, string folderName)
		//{
		//	var folderPath = @"C:\Users\blinc\source\repos\GetSampleImageFromScan\GetSampleImageFromScan\DestinationFolder";
		//	if (!Directory.Exists(folderPath))
		//	{
		//		Directory.CreateDirectory(folderPath);
		//	}
		//	string fileName = folderName;
		//	string fullPath = folderPath + @"\" + folderName + @"\" + fileName ;
		//	int isDup = 0;
		//	while (File.Exists(fullPath))
		//	{
		//		isDup++;
		//		string stt = Convert.ToString(isDup);
		//		fullPath = folderPath + @"\" + fileName + "_(" + stt + ").bmp";
		//	}
		//	DirectBitmap newbmp = bmp;
		//	bmp.Dispose();
		//	newbmp.Bitmap.Save(fullPath);

		//}

		// biến ảnh panel thành list các array số nguyên của ma trận ảnh 28x28
		public List<byte[]> GetNumberBytes()
		{
			//Splits it into tokens

			var tokens = new List<List<(int X, int Y, Color Color)>>();     // list tokens gồm các token, mỗi token gồm list tọa độ XY pixel và thông số màu
			var visited = new bool[Width, Height];
			//var token = new List<(int X, int Y, Color Color)>();
			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					var color = GetPixel(x, y);
					if (!visited[x, y] && color != EmptyPixel)
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
				var w = xs.Max() - xs.Min() + 1;
				var h = ys.Max() - ys.Min() + 1;
				var bmp = new DirectBitmap(w, h);
				foreach (var p in toke)
				{
					bmp.SetPixel(p.X - minX, p.Y - minY, p.Color);
				}
				var bytes = PreprocessImage(bmp, new Size(28, 28), false);
				number.Add(bytes);
			}
			return number;
		}

		private readonly Color EmptyPixel = Color.FromArgb(0, 0, 0, 0);

		private List<(int, int, Color)> FindTokenFrom(int startX, int startY, Color color, bool[,] visited)
		{
			var token = new List<(int, int, Color)>();
			token.Add((startX, startY, GetPixel(startX, startY)));
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
						if (y < 0 || x < 0 || y > Height - 1 || x > Width - 1)
							continue;

						var c = GetPixel(x, y);
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
		/// Biến array các bytes thành bitmap
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static Bitmap ArrayToBitmap(byte[] bytes)
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
		/// <summary>
		/// Biến đổi tạo bitmap trong suốt
		/// </summary>
		/// <param name="scrBitmap"></param>
		/// <returns></returns>
		public static Bitmap MakeBmpTrans(Bitmap scrBitmap)
		{
			Color orginalColor;
			//make an empty bitmap the same size as scrBitmap
			Bitmap newBitmap = new Bitmap(scrBitmap.Width, scrBitmap.Height); //tạo ảnh mới cùng cỡ
			for (int i = 0; i < scrBitmap.Width; i++)
			{
				for (int j = 0; j < scrBitmap.Height; j++)
				{
					//get the pixel from the scrBitmap image

					orginalColor = scrBitmap.GetPixel(10, 10);
					if (scrBitmap.GetPixel(i, j) == orginalColor)
						newBitmap.SetPixel(i, j, Color.Empty);
					else
						newBitmap.SetPixel(i, j, scrBitmap.GetPixel(i, j));
				}
			}
			scrBitmap.Dispose();
			return newBitmap;
		}

		/// <summary>
		/// Tạo Direct bitmap từ bitmap
		/// </summary>
		/// <param name="bmp"></param>
		/// <returns></returns>
		public static DirectBitmap MakeDrbmpFromBmp(Bitmap bmp)
		{
			DirectBitmap drbmp = new DirectBitmap(bmp.Width, bmp.Height);
			for (int x = 0; x < bmp.Width; x++)
				for (int y = 0; y < bmp.Height; y++)
				{
					Color colorNow = bmp.GetPixel(x, y);
					drbmp.SetPixel(x, y, colorNow);
				}
			return drbmp;
		}
		/// <summary>
		/// Hiện danh sách các pixel trống
		/// </summary>
		/// <param name="drbmp"></param>
		/// <returns></returns>
		public static List<(int X, int Y)> ShowEmptyPixel(DirectBitmap drbmp)
        {
			//var tokens = new List<List<(int X, int Y)>>();
			List <(int X, int Y)> locationEmpty = new List<(int X, int Y)>();
			int countEmpty = 0;
			for (int x = 0; x < drbmp.Width; x++)
            {
				for (int y = 0; y < drbmp.Height; y++)
                {
					if (drbmp.GetPixel(x, y).A == 0)
                    {
						locationEmpty.Add((x, y));
						countEmpty++;
					}						
				}					
			}				
			return locationEmpty;

		}

		/// <summary>
		/// Kiểm tra Bmp null
		/// </summary>
		/// <param name="bmp"></param>
		static void checkBmpNull(Bitmap bmp)
		{

			bool isBmpNull = true;
			int bmpWidth = bmp.Size.Width;
			int bmpHeight = bmp.Size.Height;
			int bmpNotNullCount = 0;
			for (int x = 0; x < bmpWidth; x++)
			{
				for (int y = 0; y < bmpHeight; y++)
				{
					if (bmp.GetPixel(x, y).A != 0)
					{
						isBmpNull = false;
						//Console.WriteLine("Bmp is not null at pixel {0},{1}.", x, y);
						bmpNotNullCount++;
					}
				}
			}
			if (isBmpNull == true)
				Console.WriteLine("Bmp is null after {0} check loops.", bmpWidth * bmpHeight);
			else
				Console.WriteLine("Bmp is not null at {0} pixels.", bmpNotNullCount);
		}

		/// <summary>
		/// Kiểm tra Direct Bitmap null
		/// </summary>
		/// <param name="drbmp"></param>
		static void checkDrBmpNull(DirectBitmap drbmp)
		{
			// check if 1 direct bitmap is null or not
			bool isDrBmpNull = true;
			int drbmpWidth = drbmp.Bitmap.Size.Width;
			int drbmpHeight = drbmp.Bitmap.Size.Height;
			int drbmpNotNullCount = 0;
			for (int x = 0; x < drbmpWidth; x++)
			{
				for (int y = 0; y < drbmpHeight; y++)
				{
					if (drbmp.GetPixel(x, y).A != 0)
					{
						isDrBmpNull = false;
						//Console.WriteLine("DrBmp is not null at pixel {0},{1}.", x, y);
						drbmpNotNullCount++;

					}
				}
			}
			if (isDrBmpNull == true)
				Console.WriteLine("DrBmp is null after {0} check loops.", drbmpWidth * drbmpHeight);
			else
				Console.WriteLine("DrBmp is not null at {0} pixels.", drbmpNotNullCount);
		}

		/// <summary>
		/// Lưu 1 ảnh vào thư mục đích
		/// </summary>
		/// <param name="drbmp"></param>
		/// <param name="childDestFolder"></param>
		public static void save1BitmapToDest(DirectBitmap drbmp, string childDestFolder)
		{
			// get the current WORKING directory (i.e. \bin\Debug)
			string workingDirectory = Environment.CurrentDirectory;
			workingDirectory = Directory.GetParent(workingDirectory).Parent.FullName;
			string childDestFolderPath = workingDirectory + @"\DestinationFolder\" + childDestFolder;
			// "C:\Users\blinc\source\repos\GetSampleImageFromScan\GetSampleImageFromScan\DestinationFolder";
			if (!Directory.Exists(childDestFolderPath))
			{
				Directory.CreateDirectory(childDestFolderPath);
			}
			string fileName = childDestFolder;
			string fullPath = childDestFolderPath + @"\" + fileName + ".png";
			int isDup = 0;
			while (File.Exists(fullPath))
			{
				isDup++;
				string stt = Convert.ToString(isDup);
				fullPath = childDestFolderPath + @"\" + fileName + "_(" + stt + ").png";
			}
			//fullPath += ".bmp";
			// thủ tục lưu ảnh qua stream ít mắc lỗi hơn
			DirectBitmap clonedImg = drbmp;
			clonedImg.Bitmap.Save(fullPath);
			//using (MemoryStream memory = new MemoryStream())
			//{
			//    using (FileStream fs = new FileStream(fullPath, FileMode.Create, FileAccess.ReadWrite))
			//    {
			//        clonedImg.Bitmap.Save(memory, ImageFormat.Bmp);
			//        byte[] bytes = memory.ToArray();
			//        fs.Write(bytes, 0, bytes.Length);
			//    }
			//}
			drbmp.Dispose();
		}
	}
}
