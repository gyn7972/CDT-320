using QMC.RemoteViewer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SP_RemoteViewer
{
    public partial class Form1 : Form
    {
        private ScreenCaptureServer _server;
        
        public Form1()
        {
            
            InitializeComponent();
            LoadScreenList();
        }

        /// <summary>
        /// 모니터 목록 로드
        /// </summary>
        private void LoadScreenList()
        {
            cmbScreenSelect.Items.Clear();
            
            var screens = ScreenCaptureServer.GetAllScreens();
            for (int i = 0; i < screens.Length; i++)
            {
                var screen = screens[i];
                string displayText = $"모니터 {i + 1} ({screen.Bounds.Width}x{screen.Bounds.Height})";
                if (screen.Primary)
                {
                    displayText += " [주 모니터]";
                }
                cmbScreenSelect.Items.Add(displayText);
            }

            // 기본 선택: 주 모니터
            cmbScreenSelect.SelectedIndex = 0;
        }

        #region 서버 기능

        private void btnServerStart_Click(object sender, EventArgs e)
        {
            try
            {
                int port = (int)numServerPort.Value;
                _server = new ScreenCaptureServer(port);
                _server.Quality = (long)numServerQuality.Value;
                _server.FPS = (int)numServerFPS.Value;
                _server.ScreenIndex = cmbScreenSelect.SelectedIndex;

                // 이벤트 연결
                _server.StatusChanged += Server_StatusChanged;
                _server.ClientConnected += Server_ClientConnected;
                _server.ClientDisconnected += Server_ClientDisconnected;

                _server.Start();

                
                // UI 상태 변경
                btnServerStart.Enabled = false;
                btnServerStop.Enabled = true;
                numServerPort.Enabled = false;
                numServerQuality.Enabled = false;
                numServerFPS.Enabled = false;
                cmbScreenSelect.Enabled = false;
                lblServerStatus.Text = "상태: 실행 중";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"서버 시작 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnServerStop_Click(object sender, EventArgs e)
        {
            if (_server != null)
            {
                _server.Stop();
                _server = null;

                // UI 상태 변경
                btnServerStart.Enabled = true;
                btnServerStop.Enabled = false;
                numServerPort.Enabled = true;
                numServerQuality.Enabled = true;
                numServerFPS.Enabled = true;
                cmbScreenSelect.Enabled = true;
                lblServerStatus.Text = "상태: 중지됨";
            }
        }

        private void Server_StatusChanged(object sender, string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Server_StatusChanged(sender, status)));
                return;
            }

            AppendServerLog(status);
        }

        private void Server_ClientConnected(object sender, string clientInfo)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Server_ClientConnected(sender, clientInfo)));
                return;
            }

            AppendServerLog($">>> 클라이언트 연결: {clientInfo}");
        }

        private void Server_ClientDisconnected(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Server_ClientDisconnected(sender, e)));
                return;
            }

            AppendServerLog("<<< 클라이언트 연결 해제");
        }

        private void AppendServerLog(string message)
        {
            string timeStamp = DateTime.Now.ToString("HH:mm:ss");
            txtServerLog.AppendText($"[{timeStamp}] {message}\r\n");
        }

        #endregion

        #region 클라이언트 기능

        private void btnClientConnect_Click(object sender, EventArgs e)
        {
            try
            {
                string serverAddress = txtServerAddress.Text;
                int port = (int)numClientPort.Value;

                remoteViewer.Connect(serverAddress, port);

                // UI 상태 변경
                btnClientConnect.Enabled = false;
                btnClientDisconnect.Enabled = true;
                txtServerAddress.Enabled = false;
                numClientPort.Enabled = false;
                lblClientStatus.Text = "상태: 연결됨";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"서버 연결 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClientDisconnect_Click(object sender, EventArgs e)
        {
            remoteViewer.Disconnect();

            // UI 상태 변경
            btnClientConnect.Enabled = true;
            btnClientDisconnect.Enabled = false;
            txtServerAddress.Enabled = true;
            numClientPort.Enabled = true;
            lblClientStatus.Text = "상태: 연결 안됨";
        }

        private void remoteViewer_StatusChanged(object sender, string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => remoteViewer_StatusChanged(sender, status)));
                return;
            }

            // 상태 메시지를 레이블에 표시할 수도 있음
            // lblClientStatus.Text = $"상태: {status}";
        }

        private void chkStretchImage_CheckedChanged(object sender, EventArgs e)
        {
            remoteViewer.StretchImage = chkStretchImage.Checked;
            remoteViewer.Invalidate();
        }

        #endregion

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 서버 종료
            if (_server != null && _server.IsRunning)
            {
                _server.Stop();
            }

            // 클라이언트 연결 해제
            if (remoteViewer.IsConnected)
            {
                remoteViewer.Disconnect();
            }
        }
    }
}
