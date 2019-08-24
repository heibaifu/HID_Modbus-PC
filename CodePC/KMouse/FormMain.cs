﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;//使用串口
using System.Runtime.InteropServices;//隐藏光标的
using System.Collections.Concurrent;    //使用ConcurrentQueue
using System.Threading;

namespace KMouse
{
	public partial class FormMain : Form
	{
        public Queue<string> queue_message = new Queue<string>();

        keyQ kq = new keyQ();
        COM com = new COM();

		//常量
		private const byte _VersionGit = 25;

        public enum eFunc_OP : int    //设定enum的数据类型
        {
            NULL = 0x00,
            EKEY = 0x01,
            CMDLIST = 0x02,
        }

        private eFunc_OP func_op;

        Modbus mdbs = new Modbus();
        Cmdlist cmd_list = new Cmdlist();

		public FormMain()
		{
			InitializeComponent();
		}

		private void FormMain_Load(object sender, EventArgs e)
		{
            this.Text = "KMouse Git" + _VersionGit.ToString();

            textBox_Cycle.Text = Properties.Settings.Default._cmdlist_cycle;
            textBox_EKey.Text = Properties.Settings.Default._eKey_string;
            textBox_Cmdlist.Text = Properties.Settings.Default._cmdlist_string;
            func_op = (eFunc_OP)Properties.Settings.Default._func_op;

            com.ControlModule_Init(comboBox_COMNumber, comboBox_COMBaudrate,
                comboBox_COMCheckBit, comboBox_COMDataBit, comboBox_COMStopBit);
            com.Init(mdbs);
            bool res = com.Open();
            if(res == true)
            {
                button_COMOpen.Text = "COM is opened";
                button_COMOpen.ForeColor = System.Drawing.Color.Green;

                //comboBox_COMBaudrate.Enabled = false;
                comboBox_COMCheckBit.Enabled = false;
                comboBox_COMDataBit.Enabled = false;
                comboBox_COMNumber.Enabled = false;
                comboBox_COMStopBit.Enabled = false;
            }

            kq.Init(queue_message);

            mdbs.Init(kq, com.serialport, queue_message,
                Delegate_ModbusCallBack_Identify,
                Delegate_ModbusCallBack_Click,
                Delegate_ModbusCallBack_Speed);
            mdbs.echo_en = checkBox_ShowUart.Checked;
            
            Update_Func_OP_State();
            button_Space.Focus();
                  
            cmd_list.Init(button_Run, textBox_Cmdlist, textBox_Point, mdbs, this);
            cmd_list.BatCall(textBox_ComRec, com.serialport);
            cmd_list.cycle_total = int.Parse(textBox_Cycle.Text);
        }

        private void timer_CloseForm_Tick(object sender, EventArgs e)
        {
            if(com.serialport.IsOpen == true)
            {
                bool res = com.Close();
                if(res == false)
                {
                    return;
                }     
            }

            kq.Close();
            mdbs.Close();

            Func_PropertiesSettingsSave();  //关闭的时候保存参数

			notifyIcon.Dispose();
            this.Close();
            System.Environment.Exit(0);     //把netcom线程也结束了
        }

		private void FormMain_FormClosing(object sender, FormClosingEventArgs e)   //窗体关闭函数
		{
            e.Cancel = true;//取消窗体的关闭
            timer_CloseForm.Enabled = true;
		}

        private void Func_PropertiesSettingsSave()
        {
            Properties.Settings.Default._cmdlist_cycle = textBox_Cycle.Text;
            Properties.Settings.Default._eKey_string = textBox_EKey.Text;
            Properties.Settings.Default._cmdlist_string = textBox_Cmdlist.Text;
            Properties.Settings.Default._func_op = (int)func_op;

            Properties.Settings.Default._baudrate_select_index = comboBox_COMBaudrate.SelectedIndex;
            Properties.Settings.Default._com_num_select_index = comboBox_COMNumber.SelectedIndex;

            Properties.Settings.Default.Save();       
        }

		private void KMouse_SizeChanged(object sender, EventArgs e)
		{
			if(this.WindowState == FormWindowState.Minimized)  //判断是否最小化
			{
				this.ShowInTaskbar = false; //不显示在系统任务栏
				notifyIcon.Visible = true;  //托盘图标可见
			}
		}

		private void notifyIcon_DoubleClick(object sender, EventArgs e)
		{
			//判断是否已经最小化于托盘 
			//if(WindowState == FormWindowState.Minimized)
			{
				WindowState = FormWindowState.Normal;	//还原窗体显示 
				this.Activate();						//激活窗体并给予它焦点 
				this.ShowInTaskbar = true;				//任务栏区显示图标 
				notifyIcon.Visible = false;				//托盘区图标隐藏 
			}
		}       

        private void button_eKeyClear_Click(object sender, EventArgs e)
        {
            textBox_Cmdlist.Text = "";
            mdbs.success_cnt = 0;
        }

        private void button_Func_Click(object sender, EventArgs e)
        {
            if(func_op == eFunc_OP.EKEY)
            {
                func_op = eFunc_OP.CMDLIST;
            }
            else if(func_op == eFunc_OP.CMDLIST)
            {
                func_op = eFunc_OP.NULL;
            }
            else
            {
                func_op = eFunc_OP.EKEY;
            }

            Update_Func_OP_State();
        }

        private void textBox_Cycle_TextChanged(object sender, EventArgs e)
        {
            cmd_list.cycle_total = int.Parse(textBox_Cycle.Text);
        }

        private void Update_Func_OP_State()
        {
            if(func_op == eFunc_OP.EKEY)
            {
                kq.modbus_kb_waiting_max = keyQ.MODBUS_KB_WAITING_EKEY;
                textBox_Cmdlist.Enabled = false;
                textBox_EKey.Enabled = true;
                button_Func.Text = "eKey";

                groupBox_Keyboard.Enabled = true;
                groupBox_COM.Enabled = true;
                groupBox_Ctrl.Enabled = true;
                groupBox_Mouse.Enabled = true;
            }
            else if(func_op == eFunc_OP.CMDLIST)
            {
                kq.modbus_kb_waiting_max = keyQ.MODBUS_KB_WAITING_NORMAL;
                textBox_Cmdlist.Enabled = true;                
                textBox_EKey.Enabled = false;
                button_Func.Text = "CList";

                groupBox_Keyboard.Enabled = false;
                groupBox_COM.Enabled = false;
                groupBox_Ctrl.Enabled = false;
                groupBox_Mouse.Enabled = false;
            }
            else
            {
                textBox_Cmdlist.Enabled = false;
                textBox_EKey.Enabled = false;
                button_Func.Text = "Null";

                groupBox_Keyboard.Enabled = true;
                groupBox_COM.Enabled = true;
                groupBox_Ctrl.Enabled = true;
                groupBox_Mouse.Enabled = true;
            }
        }

        private void button_Modbus_Send_Click(object sender, EventArgs e)
		{
			byte Reg;
			uint Val;

			if(textBox_Modbus_Reg.Text.Length == 0)
			{
				Reg = 0;
			}
			else
			{
				Reg = Convert.ToByte(textBox_Modbus_Reg.Text);
			}
			if(textBox_Modbus_Val.Text.Length == 0)
			{
				Val = 0;
			}
			else
			{
				Val = Convert.ToUInt32(textBox_Modbus_Val.Text);
			}

			mdbs.Send_03((Modbus.REG)Reg, 1, Val);
		}

        private void timer_background_Tick(object sender, EventArgs e)
        {
            this.Invoke((EventHandler)(delegate
            {
                label_SuccessCmdCnt.Text = "Success: " + mdbs.success_cnt.ToString();
                label_FailCmdCnt.Text = "Fail: " + mdbs.fail_cnt.ToString();

                if(textBox_Cmdlist.Enabled == true)
                {
                    label_Cycle.Text = "Cycle: " + cmd_list.cycle_cnt.ToString() + " / ";
                }
                else
                {
                    label_Cycle.Text = "Cycle: " + (cmd_list.cycle_cnt + 1).ToString() + " / ";
                }
            }));

            if(checkBox_ShowUart.Checked == false)
            {
                if(queue_key_str.Count > 0)
                {
                    textBox_ComRec.AppendText(queue_key_str.Dequeue());
                }
            }

            if(queue_message.Count > 0)
            {
                textBox_ComRec.AppendText("\r\n" + queue_message.Dequeue());
            }
            
            label_Rcv.Text = "Received:" + com.recv_cnt.ToString() + "(Bytes)";
        }

        void Delegate_ModbusCallBack_Identify(uint value)
        {
            this.Invoke((EventHandler)(delegate
            {
                label_Status.Text = value.ToString();
            }));
        }

        void Delegate_ModbusCallBack_Click(uint value)
        {
            this.Invoke((EventHandler)(delegate
            {
                if(button_ClickLeft.Enabled == false)
                {
                    button_ClickLeft.Enabled = true;
                }
                if(button_ClickRight.Enabled == false)
                {
                    button_ClickRight.Enabled = true;
                }
            }));
        }

        void Delegate_ModbusCallBack_Speed(uint value)
        {
            this.Invoke((EventHandler)(delegate
            {
        	    if(button_SpeedUp.Enabled == false)
			    {
				    button_SpeedUp.Enabled = true;
			    }
			    if(button_SpeedDown.Enabled == false)
			    {
				    button_SpeedDown.Enabled = true;
			    }
			    if(kq.mouse_speed_chk == false)
			    {
				    kq.mouse_speed_chk = true;
			    }
			    label_MouseSpeed.Text = value.ToString();
            }));
        }

        private void checkBox_ShowTxt_CheckedChanged(object sender, EventArgs e)
        {
            mdbs.echo_en = checkBox_ShowUart.Checked;

            textBox_ComRec.Text = "";
        }


        /********************与串口控制相关的 Start***************************/
        private void label_ClearRec_DoubleClick(object sender, EventArgs e)
        {
            textBox_ComRec.Text = "";
            com.recv_cnt = 0;
        }

        private void comboBox_COMNumber_DropDown(object sender, EventArgs e)
        {
            com.comboBox_COMNumber_DropDown(sender, e);
        }

        private void comboBox_COMNumber_SelectedIndexChanged(object sender, EventArgs e)
        {
            com.comboBox_COMNumber_SelectedIndexChanged(sender, e);
        }

        private void comboBox_COMBaudrate_SelectedIndexChanged(object sender, EventArgs e)
        {
            com.comboBox_COMBaudrate_SelectedIndexChanged(sender, e);
        }

        private void comboBox_COMDataBit_SelectedIndexChanged(object sender, EventArgs e)
        {
            com.comboBox_COMDataBit_SelectedIndexChanged(sender, e);
        }

        private void comboBox_COMCheckBit_SelectedIndexChanged(object sender, EventArgs e)
        {
            com.comboBox_COMCheckBit_SelectedIndexChanged(sender, e);
        }

        private void comboBox_COMStopBit_SelectedIndexChanged(object sender, EventArgs e)
        {
            com.comboBox_COMStopBit_SelectedIndexChanged(sender, e);
        }

        public void SetComStatus(bool IsRunning)
        {
            if(IsRunning == true)
            {
                button_COMOpen.Text = "COM is opened";
                button_COMOpen.ForeColor = Color.Green;
                comboBox_COMCheckBit.Enabled = false;
                comboBox_COMDataBit.Enabled = false;
                comboBox_COMNumber.Enabled = false;
                comboBox_COMStopBit.Enabled = false;
            }
            else
            {
                button_COMOpen.Text = "COM is closed";
                button_COMOpen.ForeColor = Color.Red;
                comboBox_COMCheckBit.Enabled = true;
                comboBox_COMDataBit.Enabled = true;
                comboBox_COMNumber.Enabled = true;
                comboBox_COMStopBit.Enabled = true;

                if(button_COMOpen.Enabled == false)
                {
                    button_COMOpen.Enabled = true;
                }
            }
        }

        private void button_ComOpen_Click(object sender, EventArgs e)
        {
            com.button_ComOpen_Click(sender, e, this);
        }

        private void comboBox_COMNumber_DropDownClosed(object sender, EventArgs e)
        {
            com.comboBox_COMNumber_DropDownClosed(sender, e);
        }
        /********************与串口控制相关的 End***************************/
    }
}
