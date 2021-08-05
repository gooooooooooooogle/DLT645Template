using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using Utils.Enum;
using Utils.helper;

namespace _645Template
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        bool continueFlag;

        SerialPort sp;

        public void InitSystem()
        {
            // 获取串口
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(SerialPort.GetPortNames());
            comboBox1.SelectedIndex = 1;

            // 设置校验位
            comboBox2.Items.Clear();
            comboBox2.Items.Add("奇");
            comboBox2.Items.Add("偶");
            comboBox2.Items.Add("无");
            comboBox2.SelectedIndex = 1;

            // 设置波特率
            comboBox3.Items.Clear();
            comboBox3.Items.Add("1200");
            comboBox3.Items.Add("2400");
            comboBox3.Items.Add("4800");
            comboBox3.Items.Add("9600");
            comboBox3.Items.Add("19200");
            comboBox3.Items.Add("115200");
            comboBox3.SelectedIndex = 1;

            //设置默认通讯密码
            textpass1.Text = "02";
            textpass2.Text = "123456";

            //默认表号
            textAddr.Text = "AAAAAAAAAAAA";

            //全选所有命令
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, true);
            }

            // 获取串口实例
            sp = new SerialPort();

            // 默认为真
            continueFlag = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitSystem();
        }

        public  string GetSendFrame(string name)
        {
            string sendFrame = string.Empty;
            string meterAddr = textAddr.Text.Trim();
            if (name == "抄流水号")
            {
                sendFrame = "FEFEFEFE68AAAAAAAAAAAA681300DF16";
            }
            else if (name == "XXX")
            {
                sendFrame = CommHelper.Get645Frame(meterAddr, "11", "71040001", "", "", "");
            }
            return sendFrame;
        }

        public void StartTest()
        {
            continueFlag = true;
            Util.SetUIVal(richTextBox1, $"-----------------------{DateTime.Now:yyyy-MM-dd HH:mm:ss}-----------------------\n", ControlType.RichTextBox);
            int cmdListLen = checkedListBox1.Items.Count;
            for (int i = 0; i < cmdListLen; i++)
            {
                if (checkedListBox1.GetItemChecked(i))
                {
                    if (continueFlag)
                    {
                        string cmdName = checkedListBox1.Items[i].ToString();
                        // 获取发送报文
                        string sendFrame = GetSendFrame(cmdName);
                        string comName = Util.GetUIVal(comboBox1, ControlType.ComboBox);
                        int buadRate = Convert.ToInt32(Util.GetUIVal(comboBox3, ControlType.ComboBox));
                        string parity = Util.GetUIVal(comboBox2, ControlType.ComboBox);
                        // 设置串口属性信息
                        sp = CommHelper.SetSerialPortParam(sp, comName, buadRate, parity);
                        Util.SetUIVal(richTextBox1, $"{i + 1}.【{textAddr.Text}】执行【{cmdName}】:", ControlType.RichTextBox, Color.Blue);
                        Util.SetUIVal(richTextBox2, $"【{cmdName}】 发送报文：  {sendFrame.ToUpper()}", ControlType.RichTextBox);
                        // 通过串口发送报文
                        CommHelper.PortSend(sp, sendFrame);

                        // 开始接收返回报文
                        string receiveStr = "";
                        int time = 0;
                        while (true)
                        {
                            time++;
                            receiveStr += CommHelper.PortReceive(sp, "open");
                            if (receiveStr.Length > 0)
                            {
                                if (receiveStr.Substring(receiveStr.Length - 2, 2) == "16")
                                {
                                    break;
                                }
                            }
                            // 超时时间10 * 200 两秒
                            if (time == 10)
                            {
                                break;
                            }
                        }
                        sp.Close();
                        Util.SetUIVal(richTextBox2, $"【{cmdName}】 返回报文：  {receiveStr.ToUpper()}", ControlType.RichTextBox);
                        
                        // 判断返回报文的合法性
                        Dictionary<string, string> dic = CommHelper.Check645Legality(sendFrame, receiveStr);
                        if (dic["result"] == "true")
                        {
                            // 解析报文
                            bool result = AnalyzeFrame(cmdName, receiveStr);
                            if (result)
                            {
                                Util.SetUIVal(richTextBox1, $"【{cmdName}】 成功", ControlType.RichTextBox, Color.Green);
                            }
                            else
                            {
                                Util.SetUIVal(richTextBox1, $"【{cmdName}】 失败", ControlType.RichTextBox, Color.Red);
                                break;
                            }
                        }
                        else
                        {
                            Util.SetUIVal(richTextBox1, $"【{cmdName}】 通讯异常。原因：{dic["message"]}", ControlType.RichTextBox, Color.Red);
                            break;
                        }
                    }
                }
                Application.DoEvents();
            }
            Util.SetUIVal(richTextBox1, $"通讯结束", ControlType.RichTextBox, Color.Blue);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Thread test = new Thread(StartTest);
            test.Start();
        }

        public bool AnalyzeFrame(string cmdName, string receiveStr)
        {
            if (receiveStr == "")
            {
                return false;
            }

            // 获取645报文数据域数据
            string dataArea = CommHelper.Get645DataArea(receiveStr);

            if (cmdName == "抄流水号")
            {
                string meterAddr = Util.ReverseStr(dataArea);
                Util.SetUIVal(richTextBox1, $"当前表号：{meterAddr}", ControlType.RichTextBox);
                Util.SetUIVal(textAddr, meterAddr, ControlType.TextBox);
            }
            else if (cmdName == "XX")
            {

            }
            return true;
        }

        private void 清空ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            richTextBox2.Clear();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            continueFlag = false;
        }

        private void 全选ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int len = checkedListBox1.Items.Count;
            for (int i = 0; i < len; i++)
            {
                checkedListBox1.SetItemChecked(i, true);
            }
        }

        private void 取消ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int len = checkedListBox1.Items.Count;
            for (int i = 0; i < len; i++)
            {
                checkedListBox1.SetItemChecked(i, false);
            }
        }

        private void checkBox1_Click(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                textAddr.Text = "AAAAAAAAAAAA";
            }
            else
            {
                textAddr.Text = "";
            }
        }
    }
}
