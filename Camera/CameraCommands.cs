using MvCamCtrl.NET;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Camera
{
    public class CameraCommands
    {
        private int nRet;
        private bool isGrabbing;
        private Thread m_hReceiveThread = null;
        private BitmapSource inImg;
        private MyCamera myCamera = new MyCamera();
        public BitmapSource InImg { get => inImg; private set { inImg = value; } }

        public CameraCommands(MyCamera myCamera)
        {
            this.myCamera = myCamera;
        }

        public string OpenDevice(MyCamera.MV_CC_DEVICE_INFO_LIST findDeviceList, int selectDevice)
        {
            if (selectDevice < 0)
            {
                return "No select device";
            }

            MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(findDeviceList.pDeviceInfo[selectDevice], typeof(MyCamera.MV_CC_DEVICE_INFO));


            nRet = myCamera.MV_CC_CreateDevice_NET(ref device);
            if (MyCamera.MV_OK != nRet)
            {
                return $"Create Device fail! {nRet}";
            }

            nRet = myCamera.MV_CC_OpenDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                myCamera.MV_CC_DestroyDevice_NET();
                return $"Device open fail! {nRet}";
            }


            if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                int nPacketSize = myCamera.MV_CC_GetOptimalPacketSize_NET();
                if (nPacketSize > 0)
                {
                    nRet = myCamera.MV_CC_SetIntValueEx_NET("GevSCPSPacketSize", nPacketSize);
                    if (nRet != MyCamera.MV_OK)
                    {
                        return $"Set Packet Size failed! {nRet}";
                    }
                }
                else
                {
                    return $"Get Packet Size failed! {nPacketSize}";
                }
            }

            myCamera.MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
            myCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);

            return "Device Open";
        }

        private void ClosenDevice()
        {
            if (isGrabbing == true)
            {
                StopGrab();
            }

            myCamera.MV_CC_CloseDevice_NET();
            myCamera.MV_CC_DestroyDevice_NET();
        }

        private void ReceiveThreadProcess()
        {

            MyCamera.MV_FRAME_OUT stFrameInfo = new MyCamera.MV_FRAME_OUT();
            MyCamera.MV_DISPLAY_FRAME_INFO stDisplayInfo = new MyCamera.MV_DISPLAY_FRAME_INFO();
            MyCamera.MV_DISPLAY_FRAME_INFO_EX stDisplayInfoEx = new MyCamera.MV_DISPLAY_FRAME_INFO_EX();
            int nRet = MyCamera.MV_OK;

            while (isGrabbing)
            {
                nRet = myCamera.MV_CC_GetImageBuffer_NET(ref stFrameInfo, 1000);
                if (nRet == MyCamera.MV_OK)
                {
                    int width = stFrameInfo.stFrameInfo.nWidth;
                    int height = stFrameInfo.stFrameInfo.nHeight;

                    BitmapSource bitmapSource = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, stFrameInfo.pBufAddr, (int)stFrameInfo.stFrameInfo.nFrameLen, width * 1);
                    bitmapSource.Freeze();

                    inImg = bitmapSource;

                    myCamera.MV_CC_DisplayOneFrame_NET(ref stDisplayInfo);
                    myCamera.MV_CC_FreeImageBuffer_NET(ref stFrameInfo);
                }
            }
        }

        public bool StartGrab()
        {
            // Set flag to false
            isGrabbing = true;

            m_hReceiveThread = new Thread(ReceiveThreadProcess);
            m_hReceiveThread.Start();

            nRet = myCamera.MV_CC_StartGrabbing_NET();
            if (MyCamera.MV_OK != nRet)
            {
                isGrabbing = false;
                return false;
            }
            return true;
        }

        private void StopGrab()
        {
            isGrabbing = false;

            int nRet = myCamera.MV_CC_StopGrabbing_NET();
            if (nRet != MyCamera.MV_OK)
            {
                //Feedback = $"Stop Grabbing Fail! {nRet}";
            }
            //Feedback = "Stop Grab";
        }
    }
}
