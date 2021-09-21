using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
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
			for (int soHL = 0; soHL < 10; soHL++)
            {
				string childFolder = soHL.ToString();
				string ChildOrigFolder = OrigDirectoryFP + childFolder;
				var files = Directory.GetFiles(ChildOrigFolder);

				int sttFile = 0;
				foreach (var file in files)
				{
					ImageFile imgFile = new ImageFile(file);

					// lấy ảnh Direct Bitmap trong class ImageFile
					DirectBitmap newBigDrBmp = imgFile.BigDrBmp;
					Console.WriteLine("");
					Console.WriteLine("File thứ {0} của tập Huấn Luyện cho số {1}: ", sttFile+1, soHL);
                    //checkBmpNull(bmp);
                    //checkDrBmpNull(drbmp);

                    // lưu ảnh lớn: dùng để test ảnh có được nhận dạng không
                    string workingDirectory = Environment.CurrentDirectory;
                    workingDirectory = Directory.GetParent(workingDirectory).Parent.FullName;
                    string childDestFolderPath = workingDirectory + @"\DestinationFolder";
                    string filenametest = "test_" + soHL.ToString() + "_" + sttFile.ToString() + ".png";
                    if (!Directory.Exists(childDestFolderPath))
                    {
                        Directory.CreateDirectory(childDestFolderPath);
                    }
                    string fullpathtest = childDestFolderPath + @"\" + filenametest;
                    newBigDrBmp.Bitmap.Save(fullpathtest);
                    Console.WriteLine("Lưu ảnh lớn thành công, filename = {0}", filenametest);

                    /// tách 1 bitmap thành nhiều bitmap nhỏ hơn, kq dạng list của list (tuple)

                    List<byte[]> ImageS_Bytes = imgFile.GetNumberBytes();
					Console.WriteLine("Đã chuyển DrBmp thành ImageBytes thành công.");

					// test đếm số phần tử
					int soAnhCon = 0;
					foreach (var image_bytes in ImageS_Bytes)
                    {
						if (image_bytes == null) 
							continue;
						else
							soAnhCon++;
                    }						
					Console.WriteLine("Số lượng ảnh con trong ảnh lớn thứ {0} là: {1}.", sttFile+1, soAnhCon);
					
					// duyệt các mảng ảnh con
					int sttDrbmp = 0;
					foreach (var image_bytes in ImageS_Bytes)
					{
						if (image_bytes == null)
							continue;
						else
						{
							DirectBitmap childDrbmp = DirectBitmap.MakeDrbmpFromBmp(DirectBitmap.ArrayToBitmap(image_bytes));
							// lưu ảnh con
							DirectBitmap.save1BitmapToDest(childDrbmp, soHL.ToString());
							sttDrbmp++; 
						}							

					}
					Console.WriteLine("Đã lưu {0} ảnh con trong ảnh lớn thứ {1} tập HL cho số {2} thành công.", soAnhCon, sttFile + 1, soHL);
					Console.WriteLine("");

					//break;	// tạm thời chỉ duyệt 1 file
					sttFile++;
                    if (sttFile == files.Count())
						continue;
				};

				// Console.WriteLine("Để tiết kiệm thời gian, tạm thời không chạy tập HL cho số tiếp theo");
                break;  //tạm thời không chạy tập HL cho số tiếp theo
            }

			//Process.Start("explorer.exe", @"D:\4_Code_no_cloud\GetSampleImageFromScan\GetSampleImageFromScan\DestinationFolder");
			string destPath = workingDirectoryFP + @"\DestinationFolder";
			Console.WriteLine("------------------------\n");
			Console.WriteLine("Lưu tập ảnh tại thư mục {0}.", destPath);
			Console.Write("Ấn phím 'Y' để mở thư mục kiểm tra HOẶC ấn phím bất kỳ để tiếp tục: ");
			if (Console.ReadLine() == "y")
			{
				Process.Start(destPath);
				//Process.Start("explorer.exe", @"D:\4_Code_no_cloud\GetSampleImageFromScan\GetSampleImageFromScan\DestinationFolder");
				Console.WriteLine("--- Ấn phím bất kỳ để tiếp tục ----\n");
				Console.ReadKey();
			}
			Console.WriteLine("Chương trình sẽ chạy ngầm chương trình tạo data Mnist bằng file python.");
			//chạy Python từ CMD
			// https://stackoverflow.com/questions/1469764/run-command-prompt-commands
			//string strCmdText;
			//strCmdText = "/C copy /b Image1.jpg + Archive.rar Image2.jpg";
			//System.Diagnostics.Process.Start("CMD.exe", strCmdText);

			//System.Diagnostics.Process process = new System.Diagnostics.Process();
			//System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
			//startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			//startInfo.FileName = "cmd.exe";
			//startInfo.Arguments = "/C copy /b Image1.jpg + Archive.rar Image2.jpg";
			//process.StartInfo = startInfo;
			//process.Start();

			var process = new System.Diagnostics.Process();
			string PythonPath = Directory.GetParent(workingDirectoryFP).FullName + @"\ConvertToMnistPy";
			var startInfo = new System.Diagnostics.ProcessStartInfo
			{
				WorkingDirectory = PythonPath,
				WindowStyle = ProcessWindowStyle.Normal,
				FileName = "cmd.exe",
				RedirectStandardInput = true,
				UseShellExecute = false
			};

			process.StartInfo = startInfo;
			process.Start();
			string my_python_runner = "python convert_to_mnist_format.py DestinationFolderFromCSharp 20 10";
			process.StandardInput.WriteLine(my_python_runner);
			Process.Start(PythonPath + @"\converted_to_MNIST");
			Console.WriteLine("Chương trình đã tạo xong tập train và test định dạng idx từ dữ liệu ảnh mới thu thập được. " +
				"\n Quay trở lại môi trường C# để huấn luyện tập dữ liệu mới.");



			;

			


		}
    }
}
