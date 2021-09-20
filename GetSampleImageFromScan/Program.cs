using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace GetSampleImageFromScan
{
    class Program
    {
		static void Main(string[] args)
        {
			Console.OutputEncoding = Encoding.UTF8;
			string workingDirectoryFP = Environment.CurrentDirectory;
			workingDirectoryFP = Directory.GetParent(workingDirectoryFP).Parent.FullName;
			string OrigDirectoryFP = workingDirectoryFP + @"\OriginalFolder\";

			//Duyệt tập huấn luyện từ 0 đến 9
			for (int soHL = 0; soHL <= 9; soHL++)
            {
				string childFolder = soHL.ToString();
				string ChildOrigFolder = OrigDirectoryFP + childFolder;
				var files = Directory.GetFiles(ChildOrigFolder);
				//foreach (var file in files)
				//{
				//	Console.WriteLine(file);
				//}

				int sttFile = 0;
				foreach (var file in files)
				{
					ImageFile imgFile = new ImageFile(file);

					//// khởi tạo 1 Direct bitmap từ ảnh lớn
					//Bitmap bmp0 = imgFile.Bitmap;
					//// chuyển thành ảnh trong suốt
					//Bitmap bmp = DirectBitmap.MakeBmpTrans(bmp0);

					// lấy ảnh Direct Bitmap trong class ImageFile
					DirectBitmap newBigDrBmp = imgFile.BigDrBmp;
					Console.WriteLine("");
					Console.WriteLine("File thứ {0} của tập Huấn Luyện cho số {1}: ", sttFile+1, soHL);
                    //checkBmpNull(bmp);
                    //checkDrBmpNull(drbmp);
                    //Console.ReadKey();

                    // lưu ảnh lớn
                    string workingDirectory = Environment.CurrentDirectory;
                    workingDirectory = Directory.GetParent(workingDirectory).Parent.FullName;
                    string childDestFolderPath = workingDirectory + @"\DestinationFolder";
                    string filenametest = "test_" + soHL.ToString() +"_"+  sttFile.ToString() + ".png";
                    if (!Directory.Exists(childDestFolderPath))
                    {
                        Directory.CreateDirectory(childDestFolderPath);
                    }
                    string fullpathtest = childDestFolderPath + @"\" + filenametest;
					newBigDrBmp.Bitmap.Save(fullpathtest);
                    Console.WriteLine("Lưu ảnh lớn thành công, filename = {0}", filenametest);

                    /// tách 1 bitmap thành nhiều bitmap nhỏ hơn, kq dạng list của list (tuple)
                    // XEM LẠI CÁCH CALL HÀM NÀY
                    // LẤY GetNumberBytes ra Programs??
                    List<byte[]> ImageS_Bytes = imgFile.GetNumberBytes();
					Console.WriteLine("Đã chuyển DrBmp thành ImageBytes thành công.");

					// test đếm số phần tử
					int soAnhCon = 0;
					foreach (var image_bytes in ImageS_Bytes)
                    {
						soAnhCon++;
                    }						
					Console.WriteLine("Số lượng ảnh con trong ảnh lớn thứ {0} là: {1}.", sttFile+1, soAnhCon);
					
					// duyệt các mảng ảnh con
					int sttDrbmp = 0;
					foreach (var image_bytes in ImageS_Bytes)
					{
						DirectBitmap childDrbmp = DirectBitmap.MakeDrbmpFromBmp(DirectBitmap.ArrayToBitmap(image_bytes));
						// lưu ảnh con
						DirectBitmap.save1BitmapToDest(childDrbmp, soHL.ToString());
						sttDrbmp++;
					}
					Console.WriteLine("Đã lưu {0} ảnh con trong ảnh lớn thứ {1} tập HL cho số {2} thành công.", soAnhCon, sttFile + 1, soHL + 1);
					Console.WriteLine("");

					//break;	// tạm thời chỉ duyệt 1 file
					sttFile++;
                    if (sttFile == files.Count())
						continue;
				};

				Console.WriteLine("Để tiết kiệm thời gian, tạm thời không chạy tập HL cho số tiếp theo");
                //break;  //tạm thời không chạy tập HL cho số tiếp theo
            }

			//Process.Start("explorer.exe", @"D:\4_Code_no_cloud\GetSampleImageFromScan\GetSampleImageFromScan\DestinationFolder");
			string destPath = workingDirectoryFP + @"\DestinationFolder";
			Console.WriteLine("------------------------\n");
			Console.WriteLine("Lưu tập ảnh tại thư mục {0}.", destPath);
			Console.Write("Ấn phím 'Y' để mở thư mục kiểm tra HOẶC ấn phím bất kỳ để đóng console: ");
			if (Console.ReadLine() == "y")
			{
				Process.Start(destPath);
				//Process.Start("explorer.exe", @"D:\4_Code_no_cloud\GetSampleImageFromScan\GetSampleImageFromScan\DestinationFolder");
				Console.WriteLine("--- Ấn phím bất kỳ để đóng chương trình ----\n");
				Console.ReadKey();
			}				
			else
			{ };

			


		}
    }
}
