using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

namespace XY_LPWM_COM
{

    public partial class MainWindow : Window
    {
        private WindowsHandler handler = new WindowsHandler();

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = handler;
            this.Closing += quit;
        }
        private void quit(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show("Quit the program?", "Exit", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                handler.UartClose();
                Environment.Exit(0);
            }
            else
            {
                e.Cancel = true;
            }
        }
        private void FreqAddBtn(object sender, RoutedEventArgs e)
        {
            handler.FreqAddBtn(sender, e);
            this.SetFreq.Text = "";
        }

        private void FreqMinBtn(object sender, RoutedEventArgs e)
        {
            handler.FreqMinBtn(sender, e);
            this.SetFreq.Text = "";
        }

        private void FreqSet(object sender, RoutedEventArgs e)
        {
            handler.FreqSet(sender, e, this.SetFreq.Text);
        }

        private void DutyAdd(object sender, RoutedEventArgs e)
        {
            handler.DutyAdd(sender, e);
        }

        private void DutyMin(object sender, RoutedEventArgs e)
        {
            handler.DutyMin(sender, e);
        }

        private void DutySet(object sender, RoutedEventArgs e)
        {
            handler.DutySet(sender, e, this.SetDuty.Text);
        }

        private void UartOpen(object sender, RoutedEventArgs e)
        {
            handler.UartOpen(sender, e);
        }

        private void UartClose(object sender, RoutedEventArgs e)
        {
            handler.UartClose(sender, e);
        }
        
    }

    public class WindowsHandler : INotifyPropertyChanged
    {
        //UART
        public static string FALL = "FALL  \n";
        public static string DOWN = "DOWN  \n";
        private SerialPort sp = new SerialPort();

        public void UartOpen(object sender, RoutedEventArgs e)
        {
            try
            {
                sp.PortName = _COM;
                sp.BaudRate = int.Parse(_Baudrate);
                sp.Parity = Parity.None;
                sp.DataBits = 8;
                sp.StopBits = StopBits.One;
                sp.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open UART.\nDetail: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                sp.Close();
                return;
            }
            sp.Write("D666");
            Thread.Sleep(30);
            string rtn = sp.ReadExisting();
            if (rtn != FALL)
            {
                MessageBox.Show("Communication failed!\nDevice return: \"" + rtn + "\"", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                sp.Close();
                return;
            }
            UartRead();
            MessageBox.Show("UART Connected!", "Succeeded", MessageBoxButton.OK, MessageBoxImage.Information);
            _ConnectionStatus = "Connected";
            ConnectionStatusTextBlock = _ConnectionStatus;
        }

        public void UartClose(object sender, RoutedEventArgs e)
        {
            try
            {
                sp.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to close UART.\nDetail: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            MessageBox.Show("UART Disconnected!", "Succeeded", MessageBoxButton.OK, MessageBoxImage.Information);
            _ConnectionStatus = "Disonnected";
            ConnectionStatusTextBlock = _ConnectionStatus;
        }

        public void UartClose()
        {
            try
            {
                sp.Close();
                return;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Failed to close UART.\nDetail: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        public void UartSet(string text)
        {
            try
            {
                sp.Write(text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Set failed.\nDetail: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Thread.Sleep(30);
            string rtn = sp.ReadExisting();
            if (rtn != DOWN)
            {
                MessageBox.Show("Set Failed.\n Device return: " + rtn, "Error", MessageBoxButton.OK, MessageBoxImage.Error); ;
            }
        }

        public void UartRead()
        {
            sp.Write("read");
            Thread.Sleep(50);
            string rtn = sp.ReadExisting();
            string freqStr, dutyStr;
            long tmpFreq = 100;
            int tmpDuty = 100;
            try
            {
                freqStr = rtn.Split("    ")[0];
                dutyStr = rtn.Split("    ")[1];
                freqStr = freqStr.Split("=")[1];
            }catch(Exception ex)
            {
                MessageBox.Show("Failed to read status.\nDetail: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            // Freq
            if (freqStr.IndexOf("K")!=-1)
            {
                freqStr=freqStr.Split('K')[0];
                try
                {
                    double temp=double.Parse(freqStr);
                    temp *= 1000;
                    tmpFreq = (long)temp;
                }catch(Exception ex)
                {
                    MessageBox.Show("Failed to read freq.\nDetail: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
            }
            else
            {
                freqStr = freqStr.Split('H')[0];
                try
                {
                    double temp = double.Parse(freqStr);
                    tmpFreq = (long)temp;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to read freq.\nDetail: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Duty
            try
            {
                dutyStr = dutyStr.Split("=")[1];
                dutyStr = dutyStr.Split("%")[0];
                tmpDuty = int.Parse(dutyStr);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Failed to read duty.\nDetail: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Update Freq
            _freq = tmpFreq;
            FreqToSTR();
            FreqToCMD();
            OnPropertyChanged(nameof(FreqSlider));

            // Update Duty
            _duty = tmpDuty;
            DutyParse();
            DutyTextBlock = _duty + "";
            DutyCMDBlock = _dutyCMD;
            OnPropertyChanged(nameof(DutySlider));
        }


        public event PropertyChangedEventHandler PropertyChanged;

        // COM Number
        public string _COM = "COM1";
        public string COMComboBox
        {
            get
            {
                return "System.Windows.Controls.ComboBoxItem: COM1";
            }
            set
            {
                _COM = value;
                int index = _COM.IndexOf(": ") + 2;
                _COM = _COM.Substring(index, _COM.Length - index);
                OnPropertyChanged(nameof(COMComboBox));
                OnPropertyChanged(nameof(COMTextBlock));
            }
        }
        public string COMTextBlock
        {
            get
            {
                return _COM;
            }
            set
            {
                _COM = value;
                OnPropertyChanged(nameof(COMTextBlock));
            }
        }

        //BaudRate
        public string _Baudrate = "9600";
        public string BaudrateCombobox
        {
            get
            {
                return "System.Windows.Controls.ComboBoxItem: 9600";
            }
            set
            {
                _Baudrate = value;
                int index = _Baudrate.IndexOf(": ") + 2;
                _Baudrate = _Baudrate.Substring(index, _Baudrate.Length - index);
                OnPropertyChanged(nameof(BaudrateCombobox));
                OnPropertyChanged(nameof(BaudrateTextBlock));
            }
        }
        public string BaudrateTextBlock
        {
            get
            {
                return _Baudrate;
            }
            set
            {
                _Baudrate = value;
                OnPropertyChanged(nameof(BaudrateTextBlock));
            }
        }

        // Connection status
        public string _ConnectionStatus = "Disconnected";
        public string ConnectionStatusTextBlock
        {
            get
            {
                return _ConnectionStatus;
            }
            set
            {
                _ConnectionStatus = value;
                OnPropertyChanged(nameof(ConnectionStatusTextBlock));
            }
        }

        //Freq

        public long _freq = 100;
        public long _freqTmp = 100;
        public string _freqStr = "Freq:100hz";
        public string _freqCMD = "F100";
        public string FreqTextBlock
        {
            get
            {
                return _freqStr;
            }
            set
            {
                _freqStr = value;
                OnPropertyChanged(nameof(FreqTextBlock));
            }
        }
        public string FreqCMDTextblock
        {
            get { return _freqCMD; }
            set
            {
                _freqCMD = value;
                OnPropertyChanged(nameof(FreqCMDTextblock));
            }
        }

        public string FreqSlider
        {
            get
            {
                return _freq + "";
            }
            set
            {
                int parse = 0, len = 0, pow = 1;
                string parseStr = value;
                if (parseStr.IndexOf(".") != -1)
                {
                    parseStr = parseStr.Substring(0, parseStr.IndexOf("."));
                }
                parse = int.Parse(parseStr);
                if (parse >= 0 && parse <= 150000)
                {
                    if (parse <= 999)
                    {
                        _freq = parse;
                    }
                    else
                    {
                        parseStr = parse + "";
                        len = parseStr.Length;
                        parseStr = parseStr.Substring(0, 3);
                        parse = int.Parse(parseStr);
                        len -= parseStr.Length;
                        for (int i = 0; i < len; i++)
                        {
                            pow *= 10;
                        }
                        parse *= pow;
                        _freq = parse;
                    }
                }
                _freq = parse;
                FreqToSTR();
                FreqToCMD();
                UartSet(_freqCMD);
                OnPropertyChanged(nameof(FreqSlider));
            }
        }


        public void FreqAddBtn(object sender, RoutedEventArgs e)
        {
            if (000 <= _freq && _freq <= 999)
            {
                _freq++;
            }
            else if (_freq > 999 && _freq <= 9990)
            {
                _freq += 10;
            }
            else if (_freq > 9990 && _freq <= 99900)
            {
                _freq += 100;
            }
            else if (_freq > 99900 && _freq < 150000)
            {
                _freq += 1000;
            }
            else
            {
                ;
            }

            FreqToSTR();
            FreqToCMD();
            UartSet(_freqCMD);
        }

        public void FreqMinBtn(object sender, RoutedEventArgs e)
        {
            if (000 < _freq && _freq <= 1000)
            {
                _freq--;
            }
            else if (_freq > 1000 && _freq <= 10000)
            {
                _freq -= 10;
            }
            else if (_freq > 10000 && _freq <= 100000)
            {
                _freq -= 100;
            }
            else if (_freq > 100000 && _freq <= 150000)
            {
                _freq -= 1000;
            }
            else
            {
                ;
            }
            FreqToSTR();
            FreqToCMD();
            UartSet(_freqCMD);
        }

        public void FreqToSTR()
        {
            double freq = _freq;
            if (_freq < 1000)
            {
                _freqStr = "Freq:" + _freq + "hz";
            }
            else
            {
                _freqStr = "Freq:" + freq / 1000 + "khz";
            }
            FreqTextBlock = _freqStr;
            // FreqSlider = _freq+"";
            OnPropertyChanged(nameof(FreqSlider));  //This one instead
        }

        public void FreqToCMD()
        {
            string cvt = "";
            if (0 <= _freq && _freq <= 9)
            {
                _freqCMD = "F00" + _freq;
            }
            else if (10 <= _freq && _freq <= 99)
            {
                _freqCMD = "F0" + _freq;
            }
            else if (100 <= _freq && _freq <= 999)
            {
                _freqCMD = "F" + _freq;
            }
            else if (_freq > 999 && _freq <= 9990)
            {
                cvt += _freq;
                cvt = cvt.Substring(0, 3);
                cvt = cvt.Insert(1, ".");
                _freqCMD = "F" + cvt;
            }
            else if (_freq > 9990 && _freq <= 99900)
            {
                cvt += _freq;
                cvt = cvt.Substring(0, 3);
                cvt = cvt.Insert(2, ".");
                _freqCMD = "F" + cvt;
            }
            else if (_freq > 99900 && _freq <= 150000)
            {
                cvt += _freq;
                cvt = cvt.Substring(0, 3);
                cvt = cvt.Insert(1, ".");
                cvt = cvt.Insert(3, ".");
                _freqCMD = "F" + cvt;
            }
            else
            {
                _freqCMD = "ERR";
            }
            FreqCMDTextblock = _freqCMD;
        }

        public void FreqSet(object sender, RoutedEventArgs e, string text)
        {
            int parse = 0, len = 0, pow = 1;
            string parseStr;
            if (text == "")
            {
                UartSet(_freqCMD);
                return;
            }
            try
            {
                parse = int.Parse(text);
            }
            catch (FormatException ex)
            {
                MessageBox.Show("Not a number.\nDetail: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (parse >= 0 && parse <= 150000)
            {
                if (parse <= 999)
                {
                    _freq = parse;
                }
                else
                {
                    parseStr = parse + "";
                    len = parseStr.Length;
                    parseStr = parseStr.Substring(0, 3);
                    parse = int.Parse(parseStr);
                    len -= parseStr.Length;
                    for (int i = 0; i < len; i++)
                    {
                        pow *= 10;
                    }
                    parse *= pow;
                    _freq = parse;
                }
            }
            else
            {
                MessageBox.Show("Out of range.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            parseStr = parse + "";
            FreqToSTR();
            FreqToCMD();
            UartSet(_freqCMD);
        }

        //Duty

        public int _duty = 100;
        public string _dutyCMD = "D100";

        public string DutySlider
        {
            get
            {
                return _duty + "";
            }
            set
            {
                string parseStr = value;
                if (parseStr.IndexOf(".") != -1)
                {
                    parseStr = parseStr.Substring(0, parseStr.IndexOf("."));
                }
                _duty = int.Parse(parseStr);
                DutyParse();
                DutyTextBlock = _duty + "";
                DutyCMDBlock = _dutyCMD;
                UartSet(_dutyCMD);
            }
        }

        public string DutyTextBlock
        {
            get
            {
                return "Duty: " + _duty + "%";
            }
            set
            {
                OnPropertyChanged(nameof(DutyTextBlock));
            }
        }

        public string DutyCMDBlock
        {
            get
            {
                return _dutyCMD;
            }
            set
            {
                OnPropertyChanged(nameof(DutyCMDBlock));
            }
        }

        public void DutyParse()
        {
            if (_duty == 100)
            {
                _dutyCMD = "D100";
            }
            else if (_duty > 9)
            {
                _dutyCMD = "D0" + _duty;
            }
            else
            {
                _dutyCMD = "D00" + _duty;
            }
        }

        public void DutyAdd(object sender, RoutedEventArgs e)
        {
            if (_duty < 100)
            {
                _duty++;
            }
            DutyParse();
            DutyTextBlock = _duty + "";
            DutyCMDBlock = _dutyCMD;
            OnPropertyChanged(nameof(DutySlider));
            UartSet(_dutyCMD);
        }

        public void DutyMin(object sender, RoutedEventArgs e)
        {
            if (_duty > 0)
            {
                _duty--;
            }
            DutyParse();
            DutyTextBlock = _duty + "";
            DutyCMDBlock = _dutyCMD;
            OnPropertyChanged(nameof(DutySlider));
            UartSet(_dutyCMD);
        }

        public void DutySet(object sender, RoutedEventArgs e, string text)
        {
            int test;
            if (text == "")
            {
                UartSet(_dutyCMD);
                return;
            }
            try
            {
                test = int.Parse(text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Not a number.\nDetail: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (test <= 100 && test >= 0)
            {
                _duty = test;
            }
            else
            {
                MessageBox.Show("Out of range.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            DutyParse();
            DutyTextBlock = _duty + "";
            DutyCMDBlock = _dutyCMD;
            OnPropertyChanged(nameof(DutySlider));
            UartSet(_dutyCMD);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
