using System.Collections.Generic;

namespace EchoTelegram
{
    public class AppSettings
    {
        public SQLSettings sqlsettings { get; set; }
        public TelegrammSetting telegramsettings { get; set; }
        public SettingsSMSGate settingsSMSGate { get; set; }
        public TimerCallback timerCallback { get; set; }

        /// <summary>
        /// ��������� ����������� � ����������
        /// </summary>
        public class TelegrammSetting
        {
            public string botName { get; set; } = "ulges_echo_bot";
            public string botAdmin { get; set; } = "@UlgesEchoBot";
            public string token { get; set; } = "1452444783:AAE9eb9yYnz0Mdg4E26OfQWNeSHBLOEi4CQ";
            public long botId { get; set; } = 1452444783;
            public long userId { get; set; } = 465686572;
            public long userTelefon { get; set; } = 79279850825;
        }

        /// <summary>
        /// ��������� ����������� � ���� ������
        /// </summary>
        public class SQLSettings
        {

            public string DBName { get; set; } = "";
            public string ServerDB { get; set; } = "ulges-app";
            public string Autentification { get; set; } = "Windows";
            public string UserConnect { get; set; }
            public string PasswConnect { get; set; }
        }
        /// <summary>
        /// ��������� ����������� � ���� ������ SMS- ���������
        /// TelefonNumber SMSGste 9374512684
        /// </summary>
        public class SettingsSMSGate
        {
            public string ServerDB { get; set; } = $"192.168.1.248"; // ulges-app
            public string UserConnect { get; set; } = "Admin"; // ���� SMS
            public string PasswConnect { get; set; }
            //public string connectString { get; set; } = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=\\\\192.168.1.248\\smsgate\\SMSGateServer.mdb";
            public string connectString { get; set; } = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=\\\\192.168.1.248\\smsgate\\SMSGateServer.mdb";
            /// <summary>
            /// ������ ��������� ��� ������ (������� ����� ������������ � ��������)
            /// </summary>
            public string numberSelectTelefones { get; set; } = ""; //"79023572744";
            /// <summary>
            /// ����� �������� ������� SMS Gate
            /// </summary>
            public string numberTelefonSMSGate { get; set; } = "79374512684";
            /// <summary>
            /// �������������� ���������� ��� ������ ������
            /// </summary>
            public direction DIRECT { get; set; } = new direction();
        }
        /// <summary>
        /// ���������� ���������
        /// </summary>
        public class direction
        {
            /// <summary>
            ///  �������� ��������� (0)
            /// </summary>
            public bool InputMessage { get; set; } = false;
            /// <summary>
            /// ���������  ��������� (1)
            /// </summary>
            public bool OutgoingMessage { get; set; } = true;
        }
        public class TimerCallback
        {
            /// <summary>
            /// ������ ������, �� ��������� 2 ���.
            /// </summary>
            public int period { get; set; } = 2000;
        }
    }
}
// Provider=Microsoft.Jet.OLEDB.4.0;Data Source=\\\\Ulges-app\\smsgate\\SMSGateServer.mdb
// Provider=Microsoft.ACE.OLEDB.12.0;Data Source=\\\\192.168.1.248\\smsgate\\SMSGateServer.mdb
// Admin

// Dsn=SMSGATE4;dbq=C:\Program Files (x86)\NEVO-ASC\SMSGATE.4\SMSGateServer.mdb;driverid=25;fil=MS Access;maxbuffersize=2048;pagetimeout=5"
// SMS