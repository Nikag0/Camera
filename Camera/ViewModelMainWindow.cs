using MvCameraControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Drawing.Configuration;
using System.Threading;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Data;
using System.Drawing;
using MvCamCtrl.NET;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace Camera
{
    public class ViewModelMainWindow : INotifyPropertyChanged
    {
        private ObservableCollection<string> deviceCollection;
        private MyCamera myCamera = new MyCamera();
        private MyCamera.MV_CC_DEVICE_INFO_LIST m_stDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
        private Thread m_hReceiveThread = null;
        private readonly Dispatcher dispatcher;
        private string strUserDefinedName = "";
        private string feedback;
        private int deviceIndex = 0;
        private int nRet;
        private ConnectionStatus сonnectionStatus = ConnectionStatus.Unknown;

        private bool isGrabbing = false;        // Whether it is grabbing image
        private bool isRecord = false;          // Whether it is recording

        public ConnectionStatus ConnectionStatus { get => сonnectionStatus; }
        public int DeviceIndex
        {
            get => deviceIndex;
            set
            {
                deviceIndex = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<string> DeviceCollection
        {
            get => deviceCollection;
            set
            {
                deviceCollection = value;
                OnPropertyChanged();
            }
        }
        public ICommand SearchDeviceCommand { get; }
        public ICommand OpenDeviceCommand { get; }
        public ICommand CloseDeviceCommand { get; }
        public ICommand StartGrabCommand { get; }
        public ICommand StopGrabCommand { get; }
        public string Feedback
        {
            get => feedback;
            set
            {
                feedback = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        public ViewModelMainWindow()
        {
            DeviceCollection = new ObservableCollection<string>();
            SearchDeviceCommand = new DelegateCommands(SearchDevice);
            OpenDeviceCommand = new DelegateCommands(OpenDevice);
            CloseDeviceCommand = new DelegateCommands(ClosenDevice);
            StartGrabCommand = new DelegateCommands(StartGrab);
            StopGrabCommand = new DelegateCommands(StopGrab);
        }

        private void SearchDevice(object parameter)
        {
            deviceCollection.Clear();
            nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref m_stDeviceList);

            // Проверка на успешное выполнение операции. Константа MV_OK равна 0.
            if (nRet != MvError.MV_OK)
            {
                Feedback =$"Enumerate devices fail!{nRet}";
                return;
            }

            for (int i = 0; i < m_stDeviceList.nDeviceNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_stDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));

                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    MyCamera.MV_GIGE_DEVICE_INFO_EX gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO_EX)MyCamera.ByteToStruct(device.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO_EX));

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

                        deviceCollection.Add("GEV: " + strUserDefinedName + " (" + gigeInfo.chSerialNumber + ")");
                    }
                    else
                    {
                        deviceCollection.Add("GEV: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")");
                    }
                }
            }
        }

        private void OpenDevice(object parameter)
        {
            if (m_stDeviceList.nDeviceNum == 0 || deviceCollection.Count == 0)
            {
                Feedback = "No device";
                return;
            }

            if (deviceIndex < 0)
            {
                Feedback = "The device number cannot be less than 0";
                return;
            }

            MyCamera.MV_CC_DEVICE_INFO device =
            (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_stDeviceList.pDeviceInfo[deviceIndex], typeof(MyCamera.MV_CC_DEVICE_INFO));


            nRet = myCamera.MV_CC_CreateDevice_NET(ref device);
            if (MyCamera.MV_OK != nRet)
            {
                Feedback = $"Create Device fail! {nRet}";
                return;
            }

            nRet = myCamera.MV_CC_OpenDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                myCamera.MV_CC_DestroyDevice_NET();
                Feedback = $"Device open fail! {nRet}";
                return;
            }


            if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                int nPacketSize = myCamera.MV_CC_GetOptimalPacketSize_NET();
                if (nPacketSize > 0)
                {
                    nRet = myCamera.MV_CC_SetIntValueEx_NET("GevSCPSPacketSize", nPacketSize);
                    if (nRet != MyCamera.MV_OK)
                    {
                        Feedback = $"Set Packet Size failed! {nRet}";
                    }
                }
                else
                {
                    Feedback = $"Get Packet Size failed! {nPacketSize}";
                }
            }

            myCamera.MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
            myCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);

            SwitchConnectionStatus(ConnectionStatus.Connect);
        }

        private void ClosenDevice(object parameter)
        {
            if (isGrabbing == true)
            {
                //StopGrab();
            }

            myCamera.MV_CC_CloseDevice_NET();
            myCamera.MV_CC_DestroyDevice_NET();

            SwitchConnectionStatus(ConnectionStatus.Disconnect);
        }

        public void ReceiveThreadProcess()
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
                    IntPtr hWnd = IntPtr.Zero;
                    Dispatcher.Invoke(new Action(() =>
                    {
                        hWnd 
                    }));

                    stDisplayInfo.hWnd = hWnd;
                    stDisplayInfo.pData = stFrameInfo.pBufAddr;
                    stDisplayInfo.nDataLen = stFrameInfo.stFrameInfo.nFrameLen;
                    stDisplayInfo.nWidth = stFrameInfo.stFrameInfo.nWidth;
                    stDisplayInfo.nHeight = stFrameInfo.stFrameInfo.nHeight;
                    stDisplayInfo.enPixelType = stFrameInfo.stFrameInfo.enPixelType;

                    myCamera.MV_CC_DisplayOneFrame_NET(ref stDisplayInfo);
                    myCamera.MV_CC_FreeImageBuffer_NET(ref stFrameInfo);
                }
            }
            }

        private void StartGrab(object parameter)
            {
            // Set flag to false
            isGrabbing = true;

            m_hReceiveThread = new Thread(ReceiveThreadProcess);
            m_hReceiveThread.Start();

            // ch:开始采集 | en:Start Grabbing
            nRet = myCamera.MV_CC_StartGrabbing_NET();
            if (MyCamera.MV_OK != nRet)
            {
                isGrabbing = false;
                feedback = $"Start Grabbing Fail! {nRet}";
                return;
            }
        }

        private void StopGrab(object parameter)
        {
            // Set flag to false
            isGrabbing = false;
 
            int nRet = myCamera.MV_CC_StopGrabbing_NET();
            if (nRet != MyCamera.MV_OK)
            {
                Feedback = $"Stop Grabbing Fail! {nRet}";
            }
        }

        public void SwitchConnectionStatus(ConnectionStatus connectionStatus)
        {
            switch (connectionStatus)
            {
                case ConnectionStatus.Unknown:
                    Feedback = "Статус неизвестен";
                    break;
                case ConnectionStatus.Connect:
                    Feedback = "Устройство подключено";
                    break;
                case ConnectionStatus.Disconnect:
                    Feedback = "Устройство отключено";
                    break;
            }
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}