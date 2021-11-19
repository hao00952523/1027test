using MathWorks.MATLAB.NET.Arrays;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ST1Yolov4_DLL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace _1027test.Controllers
{
    [Route("api/[controller]")]
    //[ApiController]
    public class FileUploadController : ControllerBase
    {
        //!!static 靜態初始化宣告(使值在未明確指定前不改變原本內容)
        public static ST1Yolov4 ST1yolov4; // MATLAB DLL class
        public static MWArray YoloModel;
        public IActionResult Get()
        {
            ST1yolov4 = new ST1Yolov4();
            MWArray modelpath = @"model/YOLO.mat";
            YoloModel = ST1yolov4.ReadYoloModel(modelpath);
            
            return Ok("file upload api running..");
        }//可成功讀入初始化就執行YOLO讀入
        public static IWebHostEnvironment _enviroment;
        public FileUploadController(IWebHostEnvironment environment)//執行在web環境中的動作
        {
            _enviroment = environment;
            
        }
        public IConfiguration Configuration { get; }
        //IConfiguration介面 會需要KEY值files ，第一個-H裡的files為KEY的值
        //上傳功能curl
        //curl -X POST "https://localhost:44356/api/FileUpload"-H  "accept: files/plain" -H  "Content-Type: multipart/form-data" -F "files=@C:\Users\user\Desktop\ng.bmp;type=image/.bmp"
        //files後放要上傳的檔案位置，複製貼上的@C:要重新打過才能正常跑

        public class FileUPloadAPI
        {
            public IFormFile files { get; set; }
            //IFormFile介面 宣告files從內容配置標頭取得檔案名
        }


        [HttpPost]//上傳照片功能
        public async Task<string> Post(FileUPloadAPI objFile)//Task類別為非同步作業讓post方法等待照片傳入
        {
            try
            {
                if (objFile.files.Length > 0)//判斷檔案存在
                {
                    string path = _enviroment.WebRootPath + "\\Upload\\";
                    //WebRootPath會連向wwwroot這個資料夾，wwwroot資料夾裡沒有"Upload"這個放置檔案資料夾，在有檔案傳入時會新增一個Upload資料夾
                    if (!Directory.Exists(path))//判斷存取檔案位置存在
                    {
                        Directory.CreateDirectory(path);
                        //"Directory.CreateDirectory(string)方法"
                        //如不存在，在指定的路徑建立所有目錄和子目錄
                    }
                    using (FileStream fileStream = System.IO.File.Create(path + objFile.files.FileName))//參考FileStream方法
                    {
                        objFile.files.CopyTo(fileStream);//將檔案
                        fileStream.Flush();//FileStream.Flush()方法，清除這個資料流的緩衝區，讓所有緩衝資料全部寫入檔案。
                        return "Upload";// 回傳Upload
                    }
                    
                }
                else
                {
                    return "Failed";
                    //檔案不存在回傳"Failed"
                }
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }

            
            

            
        }

        [HttpGet("{fileName}")]//判斷功能  在網址後加"/(fileName)圖片檔名(含副檔名)" 可判斷圖片是否有瑕疵
        //判斷功能curl=>  curl https://localhost:44356/api/FileUpload/要測試圖片的檔名
        public async Task<IActionResult> Garning([FromRoute] string fileName)
        //使用[HttpGet]方法宣告方法名稱"Garning"
        //[[HttpGet("{fileName}")]]的fileName與90行fileName相通透過FormRoute這個方式擷取路由資料("檔案名稱")
        {
            string path = _enviroment.WebRootPath + "\\Upload\\";
            var filePath = path + fileName+ ".bmp";
            var filePath2 = path + fileName + ".jpg";
            var filePath3 = path + fileName + ".png";
            var filePath4 = path + fileName + ".jpeg";
            var filePath5 = path + fileName + ".gif";
            if (System.IO.File.Exists(filePath))
            {
                filePath = filePath;
            }
            else if (System.IO.File.Exists(filePath2))
            {
                filePath = filePath2;
            }
            else if (System.IO.File.Exists(filePath3))
            {
                filePath = filePath3;
            }
            else if (System.IO.File.Exists(filePath4))
            {
                filePath = filePath4;
            }
            else if (System.IO.File.Exists(filePath5))
            {
                filePath = filePath5;
            }
            else
            {
                return Ok("file not in folder");
            }

            //95、96帶入檔案位置
            if (System.IO.File.Exists(filePath))//"File.Exists(string)"方法判斷檔案是否存在
            {
                Bitmap image1 = new Bitmap(filePath);
                byte[] image1_1d = BmpToByteArray(image1);

                MWArray mwImage1_1d = new MWNumericArray(image1.Height * image1.Width, 1, image1_1d);
                MWNumericArray imageSize = new MWNumericArray(new double[] { image1.Height, image1.Width });

                MWLogicalArray useGPU = false;
                MWNumericArray imageResize = new MWNumericArray(new double[] { 608, 608 });
                MWArray thConfidence = 0.5; // 判定為瑕疵框的confidence值
                MWArray thDefectArea = 50; // 判定為瑕疵框的面積

                MWArray[] result = ST1yolov4.PredictYoloModel(2, YoloModel, mwImage1_1d,
                    imageSize, imageResize,
                    useGPU, thConfidence, thDefectArea);

                string classVar = result[0].ToString(); // 系統判定為良品("OK")or瑕疵("NG")
                                                        // boxInfo: N個(瑕疵框)以六個值為一個瑕疵框資訊的字串，
                                                        // 分別是: 框左上角X(水平)座標, 框左上角Y(垂直)座標, 
                                                        //      框寬度(水平), 框高度(垂直), 瑕疵框類別, 該框confidence
                                                        // [重要]瑕疵框資訊以左上角為原點，並且MATLAB原點為(1,1)
                string boxInfo = result[1].ToString();
                if (classVar.Equals("OK"))
                {
                    Console.WriteLine("OK");
                    // 該樣本是良品 
                    return Ok("OK");
                }
                else
                {
                    Console.WriteLine("failed");
                    // 該樣本是瑕疵
                    return Ok("failed");
                }
            }
            else
            {
                return Ok("not exist");
                //上傳的檔案非bmp檔，需傳入.bmp檔才可判斷
            }

        }
        private byte[] BmpToByteArray(Bitmap bmp)
        {
            // Lock the bitmap's bits. 
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
             bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
             bmp.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = bmpData.Stride * bmp.Height;
            byte[] pixelByte = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, pixelByte, 0, bytes); bmp.UnlockBits(bmpData);

            return pixelByte;
        }

    }


}
