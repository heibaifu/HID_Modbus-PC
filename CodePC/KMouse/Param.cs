﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace KMouse
{
    class Param
    {
        //常量
        public const double _VersionGit = 28;	//Git版本号

        [Serializable]
        public struct tParam
        {
            public const string COM_IS_OPEN = "COM_IS_OPEN";
            public const string COM_SELECT = "COM_SELECT";
            public const string COM_BAUDRATE = "COM_BAUDRATE";            
            public const string EKEY_STRING = "EKEY_STRING";
            public const string FUNC_OP = "FUNC_OP";
            public const string CMDLIST_STRING = "CMDLIST_STRING";
            public const string CMDLIST_CYCLE = "CMDLIST_CYCLE";
            public const string BAT_PATH = "BAT_PATH";

            public bool com_is_open;
            public int com_select;
            public int com_baudrate;            
            public string eKey_String;
            public int func_op;
            public string cmdlist_string;
            public int cmdlist_cycle;

            public string bat_path_string;
        }
        static public tParam ini;

        static public string path_ini_file = ".\\KMouse.ini";

        static void CreateIniFile()
        {
            StreamWriter sw = File.CreateText(path_ini_file);

            string str = "";

            str += tParam.COM_IS_OPEN + "=" + ini.com_is_open + ";\r\n";
            str += tParam.COM_SELECT + "=" + ini.com_select + ";\r\n";
            str += tParam.COM_BAUDRATE + "=" + ini.com_baudrate + ";\r\n";

            str += tParam.EKEY_STRING + "=" + ini.eKey_String + ";\r\n";
            str += tParam.FUNC_OP + "=" + ini.func_op + ";\r\n";

            str += tParam.CMDLIST_STRING + "=" + ini.cmdlist_string + ";\r\n";
            str += tParam.CMDLIST_CYCLE + "=" + ini.cmdlist_cycle + ";\r\n";

            str += tParam.BAT_PATH + "=" + ini.bat_path_string + ";\r\n";

            sw.WriteLine(str);

            sw.Close();
        }

        static public void SaveIniParam()
        {
            CreateIniFile();
        }

        static public void LoadIniParameter()
        {
        tag_open:
            if(File.Exists(path_ini_file) == false) //第一次创建ini文件，填写默认值
            {
                ini.com_is_open = false;
                ini.com_select = -1;
                ini.com_baudrate = -1;
                
                ini.eKey_String = "Hello world";
                ini.func_op = 0;

                ini.cmdlist_string = "Identify()";
                ini.cmdlist_cycle = 0;

                ini.bat_path_string = "";

                CreateIniFile();

                goto tag_open;
            }

            StreamReader sr = new StreamReader(path_ini_file);
            
            int i = 0;
            string string_line;
            int line_number = 0;

            int index_equal = -1;
            int line_equal = 0;

            int index_end = -1;

            string param_name = "NULL";
            string param_value = "NULL";

            while((string_line = sr.ReadLine()) != null)
            {
                if(string_line == "")
                {
                    break;
                }

                if(index_equal == -1)                                       //还没找到参数名的开头
                {
                    index_equal = string_line.IndexOf("=");
                    if(index_equal != -1)                                   //找到了参数名了
                    {
                        line_equal = line_number;
                        param_name = string_line.Substring(0, index_equal);
                        param_value = "";
                    }
                }
                
                if(index_end == -1)                                         //还没找到参数值的开头
                {
                    index_end = string_line.IndexOf(";");
                    if(index_end != -1)                                     //找到了分号
                    {
                        if(line_number == line_equal)                       //等号和分号同一行
                        {
                            param_value = string_line.Substring(index_equal + 1, index_end - index_equal - 1);
                        }
                        else
                        {
                            param_value += string_line.Substring(0, index_end);
                        }
                    }
                    else                                                    //本行没有分号，就把整行的字符追加
                    {
                        if(param_value == "")                               //找到了开头，还没找到结尾
                        {
                            Dbg.WriteLine("A:% B:%", index_equal + 1, string_line.Length - index_equal);
                            param_value += string_line.Substring(index_equal + 1, string_line.Length - index_equal - 1) + "\r\n";
                        }
                        else
                        {
                            param_value += string_line + "\r\n";;
                        }
                    }
                }

                if((index_equal != -1) && (index_end != -1))                //开头结尾都搜到了
                {                    
                    Dbg.WriteLine("Param[%] line:% index:%|% name:% value:%", i++, line_number, index_equal, index_end, param_name, param_value);

                    //ui->plainTextEdit_ScanCMD->appendPlainText(str);
                    if(param_name == tParam.COM_IS_OPEN)
                    {
                        ini.com_is_open = bool.Parse(param_value);
                        Dbg.WriteLine("get COM_IS_OPEN. value:%", ini.com_is_open);
                    }
                    else if(param_name == tParam.COM_SELECT)
                    {
                        ini.com_select = int.Parse(param_value);
                        Dbg.WriteLine("get COM_SELECT. value:%", ini.com_baudrate);
                    }
                    else if(param_name == tParam.COM_BAUDRATE)
                    {
                        ini.com_baudrate = int.Parse(param_value);
                        Dbg.WriteLine("get COM_BAUDRATE. value:%", ini.com_baudrate);
                    }
                    else if(param_name == tParam.EKEY_STRING)
                    {
                        ini.eKey_String = param_value;
                        Dbg.WriteLine("get EKEY_STRING. value:%", ini.eKey_String);
                    }
                    else if(param_name == tParam.FUNC_OP)
                    {
                        ini.func_op = int.Parse(param_value);
                        Dbg.WriteLine("get FUNC_OP. value:%x", ini.func_op);
                    }
                    else if(param_name == tParam.CMDLIST_STRING)
                    {
                        ini.cmdlist_string = param_value;
                        Dbg.WriteLine("get CMDLIST_STRING. value:%", ini.cmdlist_string);
                    }
                    else if(param_name == tParam.CMDLIST_CYCLE)
                    {
                        ini.cmdlist_cycle = int.Parse(param_value);
                        Dbg.WriteLine("get CMDLIST_CYCLE. value:%", ini.cmdlist_cycle);
                    }
                    else if(param_name == tParam.BAT_PATH)
                    {
                        ini.bat_path_string = param_value;
                        Dbg.WriteLine("get BAT_PATH. value:%", ini.bat_path_string);
                    }
                    else
                    {
                        Dbg.WriteLine(false, "???what param" + param_name + "??? ");
                    }

                    param_name = "NULL";
                    param_value = "NULL";

                    index_equal = -1;
                    index_end = -1;
                }

                line_number++;
            }

            sr.Close();
        }

        public static bool GetBoolFromParameter(int parameter, int shiftbit)
        {
            bool res;
            
            if((parameter & (1 << shiftbit)) != 0)
            {
                res = true;
            }
            else
            {
                res = false;
            }

            //Console.WriteLine("Get:{0:X} {1:X} res:{2:X}", parameter, shiftbit, res);

            return res;
        }

        public static void SetBoolToParameter(ref int parameter, bool val, int shiftbit)
        {
            int res;

            if (val == true)
            {
                parameter |= 1 << shiftbit;
            }
            else
            {
                parameter &= ~(1 << shiftbit);
            }

            res = parameter;
        }
    }
}
