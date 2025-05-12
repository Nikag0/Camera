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
        private CameraPerformance cameraCreate;
        private ObservableCollection<string> nameFindCamers = new ObservableCollection<string>();
        private ObservableCollection<string> nameCreateCamers = new ObservableCollection<string>();
        private ObservableCollection<CameraPerformance> creatingCamersCollection = new ObservableCollection<CameraPerformance>();

        public ObservableCollection<string> NameFindCamers { get => nameFindCamers; private set { nameFindCamers = value; } }
        public ObservableCollection<string> NameCreateCamers { get => nameCreateCamers; private set { nameCreateCamers = value; } }
        public ObservableCollection<CameraPerformance> CreatingCamersCollection { get => creatingCamersCollection; private set { creatingCamersCollection = value; } }

        public bool SearchСamera()
        {
            NameFindCamers.Clear();

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
                        NameFindCamers.Add("GEV: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")");
                    }
                    else
                    {
                        NameFindCamers.Add("GEV: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")");
                    }
                }
            }

            if (NameFindCamers.Count == 0)
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
                MyCamera.MV_CC_DEVICE_INFO cameraInfo = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(findDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));

                if (cameraInfo.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    MyCamera.MV_GIGE_DEVICE_INFO_EX gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO_EX)MyCamera.ByteToStruct(cameraInfo.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO_EX));

                    if (gigeInfo.chSerialNumber == serialNumber)
                    {
                        CreatingCamersCollection.Add(cameraCreate = new CameraPerformance(cameraInfo));

                        if (!cameraCreate.Create())
                        {
                            return false;
                        }

                        if (!NameCreateCamers.Contains("GEV: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")"))
                        {
                            NameCreateCamers.Add("GEV: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")");
                        }
                        return true;
                    }
                }
            }

            return false;
        }

        public bool DestroyCamera(string serialNumber)
        {
            CameraPerformance cameraToDestroy = CreatingCamersCollection.FirstOrDefault(camera => camera.SerialNumber == serialNumber);

            if (cameraToDestroy.IsGrab == true)
            {
                cameraToDestroy.StopGrab();
            }

            if (!cameraToDestroy.Destroy())
            {
                return false;
            }

            NameCreateCamers.Remove(cameraToDestroy.Name);
            CreatingCamersCollection.Remove(cameraToDestroy);
            return true;
        }
    }
}
