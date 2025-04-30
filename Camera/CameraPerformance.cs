using MvCamCtrl.NET;
using MvCameraControl;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
        private int nRet;
        private Thread m_hReceiveThread = null;
        
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
        public bool IsCreate { get; set; }
        public bool IsGrab { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public CameraPerformance(MyCamera.MV_CC_DEVICE_INFO cameraInfo)
        {
            this.cameraInfo = cameraInfo;
            MyCamera.MV_GIGE_DEVICE_INFO_EX gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO_EX)MyCamera.ByteToStruct(cameraInfo.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO_EX));
            SerialNumber = gigeInfo.chSerialNumber;
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

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}