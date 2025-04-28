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
using System.Windows.Media.Media3D;

namespace Camera
{
    public class CameraManager
    {
        private MyCamera.MV_CC_DEVICE_INFO_LIST findDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
        private List<string> nameCamers = new List<string>();
        private ObservableCollection<CameraPerformance> creatingCamersCollection = new ObservableCollection<CameraPerformance>();
        private CameraPerformance cameraCreate;

        public ObservableCollection<CameraPerformance> CreatingCamersCollection { get => creatingCamersCollection; private set { creatingCamersCollection = value; } }
        public MyCamera.MV_CC_DEVICE_INFO_LIST FindDeviceList { get => findDeviceList; private set { findDeviceList = value; } }
        public List<string> NameCamers { get => nameCamers; private set { nameCamers = value; } }

        public bool SearchСamera()
        {
            nameCamers.Clear();

            int nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE, ref findDeviceList);

            if (nRet != 0)
            {
                return false;
            }

            for (int i = 0; i < findDeviceList.nDeviceNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO) Marshal.PtrToStructure(findDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));

                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    MyCamera.MV_GIGE_DEVICE_INFO_EX gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO_EX) MyCamera.ByteToStruct(device.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO_EX));

                    if ((gigeInfo.chUserDefinedName.Length > 0) && (gigeInfo.chUserDefinedName[0] != '\0'))
                    {
                        nameCamers.Add("GEV: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")");
                    }
                    else
                    {
                        nameCamers.Add("GEV: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")");
                    }
                }
            }

            if (nameCamers.Count == 0)
            {
                return false;
            }

            return true;
        }

        public bool CameraCreate(string serialNumber)
        {
            if (findDeviceList.nDeviceNum == 0)
            {
                return false;
            }

            for (int i = 0; i < findDeviceList.nDeviceNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO deviceInfo = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(findDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));

                if (deviceInfo.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    MyCamera.MV_GIGE_DEVICE_INFO_EX gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO_EX)MyCamera.ByteToStruct(deviceInfo.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO_EX));

                    if (gigeInfo.chSerialNumber == serialNumber)
                    {
                        creatingCamersCollection.Add(cameraCreate = new CameraPerformance(deviceInfo, gigeInfo.chSerialNumber));

                        int nRet = cameraCreate.MyCamera.MV_CC_CreateDevice_NET(ref deviceInfo);
                        if (MyCamera.MV_OK != nRet)
                        {
                            return false;
                        }

                        nRet = cameraCreate.MyCamera.MV_CC_OpenDevice_NET();
                        if (MyCamera.MV_OK != nRet)
                        {
                            cameraCreate.MyCamera.MV_CC_DestroyDevice_NET();
                            return false;
                        }

                        cameraCreate.IsCreate = true;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool DestroyCamera(string serialNumber)
        {
            CameraPerformance cameraToDestroy = creatingCamersCollection.FirstOrDefault(camera => camera.SerialNumber == serialNumber);

            if (cameraToDestroy.IsGrab == true)
            {
                cameraToDestroy.StopGrab();
            }

            cameraToDestroy.MyCamera.MV_CC_CloseDevice_NET();
            cameraToDestroy.MyCamera.MV_CC_DestroyDevice_NET();
            creatingCamersCollection.Remove(cameraToDestroy);
            cameraToDestroy.IsCreate = false;
            return true;
        }
    }
}
