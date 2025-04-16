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


namespace Camera
{
    public class ViewModelMainWindow : INotifyPropertyChanged
    {
        private ObservableCollection<string> deviceCollection;
        private List<IDeviceInfo> deviceInfoList = new List<IDeviceInfo>();
        IDevice device = null;
        readonly DeviceTLayerType enumTLayerType = DeviceTLayerType.MvGigEDevice | DeviceTLayerType.MvUsbDevice
            | DeviceTLayerType.MvGenTLGigEDevice | DeviceTLayerType.MvGenTLCXPDevice | DeviceTLayerType.MvGenTLCameraLinkDevice | DeviceTLayerType.MvGenTLXoFDevice;

        private int connectedDevices;
        private string feedback;
        private int deviceIndex = 0;
        private ConnectionStatus сonnectionStatus = ConnectionStatus.Unknown;
        private readonly object saveImageLock = new object();

        private bool isGrabbing = false;        // Whether it is grabbing image
        private bool isRecord = false;          // Whether it is recording
        private Thread receiveThread = null;    // Receive image thread
        private IFrameOut frameForSave;
        private IFrameOut frameOut;
        private IntPtr pictureBox1;




        public ConnectionStatus ConnectionStatus { get => сonnectionStatus; }
        public IFrameOut FrameOut
        {
            get => frameOut;
            set
            {
                frameOut = value;
                OnPropertyChanged();
            }
        }
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
        }

        private void SearchDevice(object parameter)
        {
            deviceCollection.Clear();
            int nRet = DeviceEnumerator.EnumDevices(enumTLayerType, out deviceInfoList);

            if (nRet != MvError.MV_OK)
            {
                Feedback =$"Enumerate devices fail!{nRet}";
                return;
            }

            for (int i = 0; i < deviceInfoList.Count; i++)
            {
                IDeviceInfo deviceInfo = deviceInfoList[i];
                if (deviceInfo.UserDefinedName != "")
                {
                    deviceCollection.Add(deviceInfo.TLayerType.ToString() + ": " + deviceInfo.UserDefinedName + " (" + deviceInfo.SerialNumber + ")");
                }
                else
                {
                    deviceCollection.Add(deviceInfo.TLayerType.ToString() + ": " + deviceInfo.ManufacturerName + " " + deviceInfo.ModelName + " (" + deviceInfo.SerialNumber + ")");
                }
            }
        }

        private void OpenDevice(object parameter)
        {
            if (deviceInfoList.Count == 0 || deviceCollection.Count == 0)
            {
                Feedback = "No device";
                return;
            }

            //// Get selected device information
            IDeviceInfo deviceInfo = deviceInfoList[deviceIndex];

            if (deviceIndex < 0)
            {
                Feedback = "The device number cannot be less than 0";
                return;
            }

            try
            {
                // Open device
                device = DeviceFactory.CreateDevice(deviceInfo);
            }
            catch (Exception ex)
            {
                Feedback = $"Create Device fail! {ex.Message}";
                return;
            }

            int result = device.Open();
            if (result != MvError.MV_OK)
            {
                Feedback = $"Open Device fail! {result}";
                return;
            }

            //Whether it is GigE device
            if (device is IGigEDevice)
            {
                //Switch to GigE device
                IGigEDevice gigEDevice = device as IGigEDevice;

                // Optimal package size for network detection (valid for GigE camera only)
                int optionPacketSize;
                result = gigEDevice.GetOptimalPacketSize(out optionPacketSize);
                if (result != MvError.MV_OK)
                {
                    Feedback = $"Warning: Get Packet Size failed! {result}";
                }
                else
                {
                    result = device.Parameters.SetIntValue("GevSCPSPacketSize", (long)optionPacketSize);
                    if (result != MvError.MV_OK)
                    {
                        Feedback = $"Warning: Set Packet Size failed! {result}";
                    }
                }
            }

            // Set continuous acquisition mode
            device.Parameters.SetEnumValueByString("AcquisitionMode", "Continuous");
            device.Parameters.SetEnumValueByString("TriggerMode", "Off");

            // Control operation
            SwitchConnectionStatus(ConnectionStatus.Connect);
        }

        private void ClosenDevice(object parameter)
        {
            // Reset streaming flag
            if (isGrabbing == true)
            {
                bnStopGrab_Click();
            }

            // Close device
            if (device != null)
            {
                device.Close();
                device.Dispose();
            }

            // Control operation
            SwitchConnectionStatus(ConnectionStatus.Disconnect);
        }

        public void ReceiveThreadProcess()
        {
            int nRet;

            Graphics graphics;   // Draw image with GDI on pictureBox

            while (isGrabbing)
            {
                nRet = device.StreamGrabber.GetImageBuffer(1000, out frameOut);
                if (MvError.MV_OK == nRet)
                {
                    if (isRecord)
                    {
                        device.VideoRecorder.InputOneFrame(frameOut.Image);
                    }

                    lock (saveImageLock)
                    {
                        frameForSave = frameOut.Clone() as IFrameOut;
                    }
#if !GDI_RENDER
                    device.ImageRender.DisplayOneFrame(pictureBox1, frameOut.Image);
#else
                    // Draw image with GDI
                    try
                    {
                        using (Bitmap bitmap = frameOut.Image.ToBitmap())
                        {
                            if (graphics == null)
                            {
                                graphics = pictureBox1.CreateGraphics();
                            }

                            Rectangle srcRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                            Rectangle dstRect = new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height);
                            graphics.DrawImage(bitmap, dstRect, srcRect, GraphicsUnit.Pixel);
                        }
                    }
                    catch (Exception e)
                    {
                        device.StreamGrabber.FreeImageBuffer(frameOut);
                        MessageBox.Show(e.Message);
                        return;
                    }
#endif
                    device.StreamGrabber.FreeImageBuffer(frameOut);
                }
            }
        }

        private void StartGrab(object parameter)
        {
            try
            {
                // Set flag bit
                isGrabbing = true;

                receiveThread = new Thread(ReceiveThreadProcess);
                receiveThread.Start();
            }
            catch (Exception ex)
            {
                Feedback = $"Start thread failed! {ex.Message}";
                throw;
            }

            // Start grabbing
            int result = device.StreamGrabber.StartGrabbing();
            if (result != MvError.MV_OK)
            {
                isGrabbing = false;
                receiveThread.Join();
                Feedback = $"Start Grabbing Fail! {result}";
                return;
            }
        }

        private void bnStopGrab_Click()
        {
            // Set flag to false
            isGrabbing = false;
            receiveThread.Join();

            // Stop grabbing
            int result = device.StreamGrabber.StopGrabbing();
            if (result != MvError.MV_OK)
            {
                Feedback = $"Stop Grabbing Fail! {result}";
            }

            // Control operation
            bnStopGrab_Click();
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