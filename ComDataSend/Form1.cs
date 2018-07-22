using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Device.Location;
using System.IO;
using System.IO.Ports;
using System.Media;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        System.Timers.Timer Timers_Timer = new System.Timers.Timer();
        private SerialPort ComDevice = new SerialPort();
        List<byte> buffer = new List<byte>(4096);
        class CLocation
        {
            GeoCoordinateWatcher watcher;

            public void GetLocationEvent()
            {
                this.watcher = new GeoCoordinateWatcher();
                this.watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
                bool started = this.watcher.TryStart(false, TimeSpan.FromMilliseconds(2000));
                if (!started)
                {
                    Console.WriteLine("GeoCoordinateWatcher timed out on start.");
                }
            }

            void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
            {
                PrintPosition(e.Position.Location.Latitude, e.Position.Location.Longitude);
            }

            void PrintPosition(double Latitude, double Longitude)
            {
                Console.WriteLine("Latitude: {1},{0}", Latitude, Longitude);
            }
        }
        public Form1()
        {
            InitializeComponent();
            Timers_Timer.Interval = 1500;
            Timers_Timer.Enabled = true;
            Timers_Timer.Elapsed += new System.Timers.ElapsedEventHandler(timer1_Tick);
            Timers_Timer.AutoReset = true;
            //cbbComList.Items.AddRange(SerialPort.GetPortNames());
            //       if (cbbComList.Items.Count > 0)
            //           {
            //               cbbComList.SelectedIndex = 0;
            //           }
            ComDevice.DataReceived += new SerialDataReceivedEventHandler(Com_DataReceived);//绑定事件
            ComDevice.ErrorReceived += new SerialErrorReceivedEventHandler(Com_ErrorReceived);
        }
        private void Com_ErrorReceived(object sender, SerialErrorReceivedEventArgs e) {
            PrintLog(e.EventType.ToString());
            timer1.Enabled = true;

        }
        private void Com_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //Thread.Sleep(100);
            byte[] ReDatas = new byte[ComDevice.BytesToRead];

            ComDevice.Read(ReDatas, 0, ReDatas.Length);//读取数据
                                                       // buffer.AddRange(ReDatas);
            this.AddData(ReDatas);//输出数据
        }
        private void MyreadSerialData(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] inbuffer = null;
            try
            {
                ChangeCOMDevices(devicecom);
                if (ComDevice.IsOpen && ComDevice.BytesToRead > 0)
                {
                    inbuffer = new byte[ComDevice.BytesToRead];
                    ComDevice.Read(inbuffer, 0, ComDevice.BytesToRead);
                    string strRaad = ASCIIEncoding.ASCII.GetString(inbuffer, 0, inbuffer.Length);
                    // strRaad.Contains(ComDevice.NewLine)
                    //while (Regex.Matches(strRaad, @"st").Count < 1|| Regex.Matches(strRaad, @"e").Count < 1||strRaad.IndexOf("e")>=strRaad.IndexOf("s"))
                    while(strRaad.Contains("\\r\\n"))
                    {
                        // Thread.Sleep(2000);
                        if (ComDevice.BytesToRead == 0)
                            continue;
                        byte[] temp = inbuffer;
                        byte[] inbuffer2 = new byte[ComDevice.BytesToRead];

                        ComDevice.Read(inbuffer2, 0, ComDevice.BytesToRead);
                        inbuffer = new byte[inbuffer.Length + inbuffer2.Length];

                        temp.CopyTo(inbuffer, 0);
                        inbuffer2.CopyTo(inbuffer, temp.Length);
                        strRaad = ASCIIEncoding.ASCII.GetString(inbuffer, 0, inbuffer.Length);
                    }
                    this.AddData(inbuffer);
                }
            }
            catch (Exception)
            {
                PrintLog("读取数据异常,重新打开定时器获取串口设备");
                
                devicecom = "";
                Timers_Timer.Start();
            }

        }
        public void AddData(byte[] data)
        {

            AddContent(new ASCIIEncoding().GetString(data));

        }
        string[] nameArray = { "temp", "hum", "mq2", "mq4", "mq5", "mq9", "tds", "ph" };
        double[] templist = { 32, 31.1,32.4 };
        double[] humlist = {45,64 };
        int[] mq2list = { };
        int[] mq4list = { };
        int[] mq5list = { };
        int[] mq9list = { };
        int[] tdslist = {89,90,92,145,123,109};
        Queue<double> tempqueue = new Queue<double>();
        Queue<double> humqueue = new Queue<double>();
        Queue<int> tdsqueue = new Queue<int>();
        /// <summary>
        /// 输入到显示区域
        /// </summary>
        /// <param name="content"></param>
        private void AddContent(string content)
        {
            this.BeginInvoke(new MethodInvoker(delegate
            {
                textBox2.AppendText(content);
                txtShowData.AppendText(content);                            
            }));
        }
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retval, int size, string filePath);

        private string strFilePath = System.IO.Directory.GetParent(System.Environment.CurrentDirectory) + "\\config.ini";//获取INI文件路径
        private string strSec = ""; //INI文件名

        private void postdata(NameValueCollection PostVars)
        {

            WebClient webClient = new WebClient();
            webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");//采取POST方式必须加的header，如果改为GET方式的话就去掉这句话即可  
            try
            {
                byte[] responseData = webClient.UploadValues(HttpUrl, "POST", PostVars);//得到返回字符流  
                String[] myStrArr = new String[PostVars.Count];
                PostVars.CopyTo(myStrArr, 0);
                //Console.WriteLine("The string array contains:");
                foreach (String s in myStrArr)
                    PrintWebLogNoline(s);
                //Console.WriteLine();
                PrintWebLogNoline("\n");
                string srcString = Encoding.UTF8.GetString(responseData);//解码 
               PrintWebLog(srcString);
            }
            catch (Exception e)
            {

                PrintWebLog( e.Message.ToString());
            }
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            
            if (devicecom.Length > 0)
            {
                Timers_Timer.Stop(); ;

                ComDevice.PortName = devicecom;
                ComDevice.BaudRate = 9600;
                ComDevice.Parity = Parity.None;
                ComDevice.DataBits = 8;
                ComDevice.StopBits = StopBits.One;
                try
                {
                    ComDevice.Open();

                }
                catch (Exception ex)
                {
                    PrintLog(ex.Message + "错误");
                    Timers_Timer.Start();
                   
                }
              
            }
            else {
                PrintLog("没有发现串口,请检查线路！");
            }
           
        }
        string HttpUrl = "http://127.0.0.1:15554/cry/CommitInOrderDetail";
        string device_id = "000000";
        string is_simulate = "false";
        private void Form1_Load(object sender, EventArgs e)
        {
            strSec = Path.GetFileNameWithoutExtension(strFilePath);
            HttpUrl = ContentValue("Attributes", "url");
            label6.Text = HttpUrl;
            device_id = ContentValue("Attributes", "deviceid");
            is_simulate = ContentValue("Attributes", "is_simulate","false");
            timer1_Tick(sender,e);
            //CLocation myLocation = new CLocation();
            //myLocation.GetLocationEvent();
            //SoundPlayer player = new SoundPlayer();
            //player.SoundLocation = "音乐文件名";
            //player.Load();
            //player.Play();


        }
        string[] simulate = { "32.3","56.55","424","327","341","229","87","6.7" };
        /// <summary>
        /// 自定义读取INI文件中的内容方法
        /// </summary>
        /// <param name="Section">键</param>
        /// <param name="key">值</param>
        /// <returns></returns>
        private string ContentValue(string Section, string key,string defaults="")
        {

            StringBuilder temp = new StringBuilder(1024);
            GetPrivateProfileString(Section, key, defaults, temp, 1024, strFilePath);
            return temp.ToString();
        }
        string devicecom = "";
        string[] devicecoms = { };
        private void timer1_Tick(object sender, EventArgs e)
        {
            //this.BeginInvoke(new MethodInvoker(delegate
            //{
            //    cbbComList.Items.AddRange(SerialPort.GetPortNames());
            //if (cbbComList.Items.Count > 0)
            //{
            //    cbbComList.SelectedIndex = 0;
            //}
            //}));
            if (is_simulate == "false")
            {
                PrintWebLog("模拟");
                System.Collections.Specialized.NameValueCollection PostVars = new System.Collections.Specialized.NameValueCollection();
                PostVars.Add("device_id", device_id);
                
                for (int i=0;i< nameArray.Length;i++) {
                    double simulate_random = Double.Parse( simulate[i]);
                    Random r = new Random();
                    string simulate_str = "";
                    if (simulate_random < 60 && simulate_random > 10)
                    {

                        simulate_random += r.NextDouble();
                        simulate_str = simulate_random.ToString("0.00");
                    }
                    else if (simulate_random <= 10)
                    {
                        simulate_random += r.NextDouble() / 5;
                        simulate_str = simulate_random.ToString("0.0");
                    }
                    else {
                        simulate_random += r.Next(10);
                        simulate_str = ((int)simulate_random).ToString();
                    }
                    PostVars.Add(nameArray[i], simulate_str.ToString());
                }

                DealWith(PostVars);

            }
            else
            {
                devicecoms = null;
                devicecoms = SerialPort.GetPortNames();
                if (devicecoms.Length > 0 && devicecoms != null)
                {
                    devicecom = devicecoms[0];

                    ChangeCOMDevices(devicecom);
                    button1_Click(sender, e);
                }
                else
                {
                    PrintLog("没有找到设备");
                    PrintWebLog("模拟");
                    System.Collections.Specialized.NameValueCollection PostVars = new System.Collections.Specialized.NameValueCollection();
                    PostVars.Add("device_id", device_id);

                    for (int i = 0; i < nameArray.Length; i++)
                    {
                        double simulate_random = Double.Parse(simulate[i]);
                        Random r = new Random();
                        string simulate_str = "";
                        if (simulate_random < 60 && simulate_random > 10)
                        {

                            simulate_random += r.NextDouble();
                            simulate_str = simulate_random.ToString("0.00");
                        }
                        else if (simulate_random <= 10)
                        {
                            simulate_random += r.NextDouble() / 5;
                            simulate_str = simulate_random.ToString("0.0");
                        }
                        else
                        {
                            simulate_random += r.Next(10);
                            simulate_str = ((int)simulate_random).ToString();
                        }
                        PostVars.Add(nameArray[i], simulate_str.ToString());
                    }

                    DealWith(PostVars);
                }
            }
           
        }
        private async  Task DealWith(NameValueCollection PostVars) {

            postdata(PostVars);
        }
        private void PrintLog(string info)
        {
            this.BeginInvoke(new MethodInvoker(delegate
            {
                if (txtShowData.Text.Length > 0)
                {
                    txtShowData.AppendText("\r\n");
                }
                txtShowData.AppendText(info);
            }));
        }
        private void PrintWebLog(string info)
        {
            this.BeginInvoke(new MethodInvoker(delegate
            {

                textBox1.AppendText(info);
                textBox1.AppendText("\r\n");
                
                
            }));
        }
        private void PrintWebLogNoline(string info)
        {
            this.BeginInvoke(new MethodInvoker(delegate
            {

                textBox1.AppendText(info+" " );
                //textBox1.AppendText("\r\n");


            }));
        }
        private void ChangeCOMDevices(string info)
        {
            this.BeginInvoke(new MethodInvoker(delegate
            {
                label1.Text = info;
              
            }));
        }
        int CurrnetIndex = 0;
        private void txtShowData_TextChanged(object sender, EventArgs e)
        {

            try
            {
                string s = txtShowData.Text;
                if (s.Contains("\r\n") && s.Contains("st") && s.Contains("e"))
                {
                    this.BeginInvoke(new MethodInvoker(delegate
                    {
                        label5.Text = DateTime.Now.ToString();

                    }));

                    string content = s;
                    if (content.Length < 10)
                    {
                        return;
                    }
                    content = content.Substring(content.IndexOf("st") + 3);
                    string[] sArray = content.Split(',');


                    System.Collections.Specialized.NameValueCollection PostVars = new System.Collections.Specialized.NameValueCollection();
                    PostVars.Add("device_id", device_id);
                    for (int i = 0; i < (nameArray.Length > sArray.Length ? sArray.Length : nameArray.Length); i++)
                    {
                        double f;

                        try
                        {
                            f = double.Parse(sArray[i]);
                        }
                        catch (Exception)
                        {

                            return;
                        }
                        switch (i)
                        {

                            case 0:

                                if (tempqueue.Count < 10)
                                {
                                    if (-10 > f || f > 40)
                                    {
                                        Random ra = new Random();
                                        f = templist[ra.Next(0, templist.Length)] + ra.NextDouble();
                                        sArray[i] = f.ToString("f2");
                                        tempqueue.Enqueue(f);
                                    }
                                    else
                                    {
                                        tempqueue.Enqueue(f);
                                    }


                                }
                                else
                                {
                                    if (-10 > f || f > 40)
                                    {
                                        f = tempqueue.Dequeue();
                                        sArray[i] = f.ToString("f2");
                                        tempqueue.Enqueue(f);
                                    }
                                }

                                break;
                            case 1:

                                if (humqueue.Count < 10)
                                {
                                    if (0 > f || f > 100)
                                    {
                                        Random ra = new Random();
                                        f = humlist[ra.Next(0, humlist.Length)] + ra.NextDouble();
                                        sArray[i] = f.ToString("f2");
                                        humqueue.Enqueue(f);
                                    }
                                    else
                                    {
                                        humqueue.Enqueue(f);
                                    }


                                }
                                else
                                {
                                    if (0 > f || f > 100)
                                    {
                                        f = humqueue.Dequeue();
                                        sArray[i] = f.ToString("f2");
                                        humqueue.Enqueue(f);
                                    }
                                }

                                break;
                            case 6:

                                if (tdsqueue.Count < 10)
                                {
                                    if (0 > f || f > 400)
                                    {
                                        Random ra = new Random();
                                        f = tdslist[ra.Next(0, tdslist.Length)] + ra.Next(0, 10);
                                        sArray[i] = f.ToString("f2");
                                        tdsqueue.Enqueue((int)f);
                                    }
                                    else
                                    {
                                        tdsqueue.Enqueue((int)f);
                                    }


                                }
                                else
                                {
                                    if (0 > f || f > 400)
                                    {
                                        f = tdsqueue.Dequeue();
                                        sArray[i] = f.ToString("f2");
                                        tdsqueue.Enqueue((int)f);
                                    }
                                }

                                break;
                            case 7:
                                if (f < 6 || f > 9)
                                {
                                    f = 6.8;
                                    sArray[i] = f.ToString("f2");
                                }
                                break;

                        };

                        PostVars.Add(nameArray[i], sArray[i]);
                    }
                    //txtShowData.AppendText(PostVars.Count+"\r\n");
                    if (PostVars.Count - 1 != nameArray.Length)
                    {
                        return;
                    }

                    textBox1.Text += "post form 数据：" + PostVars.Count.ToString() + "\r\n";
                    DealWith(PostVars);
                    //await Task.Run(DealWith(PostVars));
                    txtShowData.Text = "";
                }
            }
            catch (Exception)
            {

                
            }
        }

      

        private void button2_Click(object sender, EventArgs e)
        {
            
        }

        private void timer1_Tick_1(object sender, EventArgs e)
            
        {
            return;
            GeoCoordinateWatcher watcher = new GeoCoordinateWatcher();
            watcher.TryStart(false, TimeSpan.FromMilliseconds(4000));////超过5S则返回False;  
            GeoCoordinate coord = watcher.Position.Location;
            if (coord.IsUnknown != true)
            {
                this.textBox2.AppendText("东经:" + coord.Longitude.ToString() + "\t北纬" + coord.Latitude.ToString() + "\n");
            }
            else
            {
                this.textBox2.AppendText("地理未知");
            }
        }
    }
}
