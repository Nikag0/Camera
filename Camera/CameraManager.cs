using MvCamCtrl.NET;
using MvCameraControl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Camera
{
    public class CameraManager
    {
        private CameraCommands cameraCommands;
        private PerfomanceCamera perfomanceCamera; 
        private MyCamera myCamera = new MyCamera();
        private MyCamera.MV_CC_DEVICE_INFO_LIST findDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
        private ObservableCollection<PerfomanceCamera> collectionCamera = new ObservableCollection<PerfomanceCamera>();
        private int nRet;
        private string strUserDefinedName = "";
        private BitmapSource inImg1;

        public ObservableCollection<PerfomanceCamera> CollectionCamera { get => collectionCamera; private set { collectionCamera = value; } }
        public CameraManager()
        {
            CameraCommands cameraCommands = new CameraCommands(myCamera);
            this.cameraCommands = cameraCommands;
        }

        public bool SearchDevice()
        {
            nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref findDeviceList);

            if (nRet != 0)
            {
                return false;
            }

            for (int i = 0; i < findDeviceList.nDeviceNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(findDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));

                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    MyCamera.MV_GIGE_DEVICE_INFO_EX gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO_EX)MyCamera.ByteToStruct(device.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO_EX));

                    if (collectionCamera.Any(camera => camera.SerialNumer == gigeInfo.chSerialNumber))
                    {
                        return true;
                    }

                    if ((gigeInfo.chUserDefinedName.Length > 0) && (gigeInfo.chUserDefinedName[0] != '\0'))
                    {
                        if (MyCamera.IsTextUTF8(gigeInfo.chUserDefinedName))
                        {
                            strUserDefinedName = Encoding.UTF8.GetString(gigeInfo.chUserDefinedName).TrimEnd('\0');
                        }
                        else
                        {
                            strUserDefinedName = Encoding.Default.GetString(gigeInfo.chUserDefinedName).TrimEnd('\0');
                        }

                        collectionCamera.Add(perfomanceCamera = new PerfomanceCamera
                        {
                            Name = "GEV: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")",
                            SerialNumer = gigeInfo.chSerialNumber,
                            NubmerInDeviceLict = i
                        });
                    }
                    else
                    {
                        collectionCamera.Add(perfomanceCamera = new PerfomanceCamera
                        {
                            Name = "GEV: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")",
                            SerialNumer = gigeInfo.chSerialNumber,
                            NubmerInDeviceLict = i
                        });
                    }
                }
            }

            if (collectionCamera.Count == 0)
            {
                return false;
            }
            return true;
        }

        public string OpenDevice(string serialNumberDevice, int indexSelectDevice)
        {

            if (collectionCamera.Any(camera => camera.SerialNumer == serialNumberDevice && camera.IsOpen))
            {
                return "Device already open";
            }

            PerfomanceCamera cameraToUpdate = collectionCamera.FirstOrDefault(camera => camera.SerialNumer == serialNumberDevice);

            if (cameraToUpdate != null)
            {
                cameraToUpdate.IsOpen = true;
            }

            return cameraCommands.OpenDevice(findDeviceList, indexSelectDevice);
        }

        public BitmapSource StartGrab()
        {
            cameraCommands.StartGrab();
            return cameraCommands.InImg;
        }
    }
}
