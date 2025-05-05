using MvCamCtrl.NET;
using MvCameraControl;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Camera
{
    public class CameraPerformance : INotifyPropertyChanged
    {
        private BitmapSource inImg;
        private MyCamera myCamera = new MyCamera();
        private MyCamera.MV_CC_DEVICE_INFO cameraInfo = new MyCamera.MV_CC_DEVICE_INFO();
        private IPAddress ipAddress;
        private byte[] ipBytes;
        private int nRet;
        private Thread m_hReceiveThread = null;
        private string exposureTime;
        private string gain;
        private string frameRate;
        private bool isCreate = false;
        private bool isGrab = false;

        public string Ip { get; set; }
        public string Name { get; set; }
        public MyCamera MyCamera { get => myCamera; private set { myCamera = value; } }
        public BitmapSource InImg 
        { 
            get => inImg; 

            set 
            { 
                inImg = value;
                OnPropertyChanged();
            }
        }
        public string SerialNumber { get; private set; }
        public string ExposureTime
        {
            get => exposureTime;

            set
            {
                exposureTime = value;
                OnPropertyChanged();
            }
        }
        public string Gain
        {
            get => gain;

            set
            {
                gain = value;
                OnPropertyChanged();
            }
        }
        public string FrameRate
        {
            get => frameRate;

            set
            {
                frameRate = value;
                OnPropertyChanged();
            }
        }
        public bool IsCreate 
        {
            get => isCreate;

            set
            {
                isCreate = value;
                OnPropertyChanged();
            }
        }
        public bool IsGrab  
        {
            get => isGrab;

            set
            {
                isGrab = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        public CameraPerformance(MyCamera.MV_CC_DEVICE_INFO cameraInfo)
        {
            this.cameraInfo = cameraInfo;
            MyCamera.MV_GIGE_DEVICE_INFO_EX gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO_EX)MyCamera.ByteToStruct(cameraInfo.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO_EX));
            SerialNumber = gigeInfo.chSerialNumber;
            Name = "GEV: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")";
            ipBytes = BitConverter.GetBytes(gigeInfo.nCurrentIp);
            ipAddress = new IPAddress(ipBytes);
            Ip = ipAddress.ToString();
        }

        private void ReceiveThreadProcess()
        {

            MyCamera.MV_FRAME_OUT stFrameInfo = new MyCamera.MV_FRAME_OUT();
            MyCamera.MV_DISPLAY_FRAME_INFO stDisplayInfo = new MyCamera.MV_DISPLAY_FRAME_INFO();
            MyCamera.MV_DISPLAY_FRAME_INFO_EX stDisplayInfoEx = new MyCamera.MV_DISPLAY_FRAME_INFO_EX();
            int nRet = MyCamera.MV_OK;

            while (IsGrab)
            {
                nRet = myCamera.MV_CC_GetImageBuffer_NET(ref stFrameInfo, 1000);
                if (nRet == MyCamera.MV_OK)
                {
                    int width = stFrameInfo.stFrameInfo.nWidth;
                    int height = stFrameInfo.stFrameInfo.nHeight;

                    BitmapSource bitmapSource = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, stFrameInfo.pBufAddr, (int)stFrameInfo.stFrameInfo.nFrameLen, width * 1);
                    bitmapSource.Freeze();

                    InImg = bitmapSource;

                    myCamera.MV_CC_DisplayOneFrame_NET(ref stDisplayInfo);
                    myCamera.MV_CC_FreeImageBuffer_NET(ref stFrameInfo);
                }
            }
        }

        public bool StartGrab()
        {
            IsGrab = true;
            m_hReceiveThread = new Thread(ReceiveThreadProcess);
            m_hReceiveThread.Start();

            nRet = myCamera.MV_CC_StartGrabbing_NET();
            if (MyCamera.MV_OK != nRet)
            {
                IsGrab = false;
                return false;
            }

            return true;
        }

        public bool StopGrab()
        {
            IsGrab = false;

            int nRet = myCamera.MV_CC_StopGrabbing_NET();
            if (nRet != MyCamera.MV_OK)
            {
                IsGrab = true;
                return false;
            }

            return true;
        }

        public bool Connect()
        {
            nRet = myCamera.MV_CC_OpenDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }
            return true;
        }
        public bool Disconnect()
        {
            nRet = myCamera.MV_CC_CloseDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }
            return true;
        }

        public bool Create()
        {

        }

        public bool Disconnect()
        {

        }


        public void GetParam()
        {
            MyCamera.MVCC_FLOATVALUE stParam = new MyCamera.MVCC_FLOATVALUE();
            nRet = myCamera.MV_CC_GetFloatValue_NET("ExposureTime", ref stParam);
            if (MyCamera.MV_OK == nRet)
            {
                ExposureTime = stParam.fCurValue.ToString("F1");
            }

            nRet = myCamera.MV_CC_GetFloatValue_NET("Gain", ref stParam);
            if (MyCamera.MV_OK == nRet)
            {
                Gain = stParam.fCurValue.ToString("F1");
            }

            nRet = myCamera.MV_CC_GetFloatValue_NET("ResultingFrameRate", ref stParam);
            if (MyCamera.MV_OK == nRet)
            {
                FrameRate = stParam.fCurValue.ToString("F1");
            }
        }

        public string SetParam()
        
        {
            try
            {
                float.Parse(ExposureTime);
                float.Parse(Gain);
                float.Parse(FrameRate);
            }
            catch
            {
                return "Please enter correct type!";
            }

            myCamera.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
            int nRet = myCamera.MV_CC_SetFloatValue_NET("ExposureTime", float.Parse(ExposureTime));
            if (nRet != MyCamera.MV_OK)
            {
                return "Set Exposure Time Fail!";
            }

            myCamera.MV_CC_SetEnumValue_NET("GainAuto", 0);
            nRet = myCamera.MV_CC_SetFloatValue_NET("Gain", float.Parse(Gain));
            if (nRet != MyCamera.MV_OK)
            {
                return "Set Gain Fail!";
            }

            nRet = myCamera.MV_CC_SetFloatValue_NET("AcquisitionFrameRate", float.Parse(FrameRate));
            if (nRet != MyCamera.MV_OK)
            {
                return "Set Frame Rate Fail!";
            }

            return "Parametrs Setter";
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}