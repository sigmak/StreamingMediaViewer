using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Threading.Tasks;
using AxWMPLib;
using System.Threading;
using System.Net;
using System.Drawing;
using System.Linq;

namespace StreamingMediaViewer
{
    public partial class StreamingMediaViewer : Form
    {
        private readonly string encryptionKey = "YourSecretKey123";
        private readonly string encryptedFilesPath;//private readonly string encryptedFilesPath = @"D:\EncryptedFiles\";
        private  string tempFile; ///
        private readonly int bufferSize = 81920; // 80KB 버퍼
        private PictureBox pictureBox;
        private AxWindowsMediaPlayer mediaPlayer; // 참조에 AxInterop.WMPLib.dll 추가
        public StreamingMediaViewer()
        {

            
            // 실행 파일 위치를 기준으로 하위 폴더 경로 설정
            string exePath = Path.GetDirectoryName(Application.ExecutablePath);
            encryptedFilesPath = Path.Combine(exePath, "EncryptedFiles");

            // 폴더가 없으면 생성
            if (!Directory.Exists(encryptedFilesPath))
            {
                Directory.CreateDirectory(encryptedFilesPath);
            }

            InitializeComponent();

            this.Text = "암복Viewer";

            try
            {
                InitializeComponents();
                InitializeMediaPlayer();
                InitializeDataGridView();
                LoadEncryptedFiles();

            } catch(Exception ex)
            {
                MessageBox.Show(ex.Message);  
            }


        }
        private void InitializeMediaPlayer()
        {
            // Windows Media Player ActiveX 컨트롤 동적 생성
            mediaPlayer = new AxWMPLib.AxWindowsMediaPlayer();
            ((System.ComponentModel.ISupportInitialize)(mediaPlayer)).BeginInit();

            //mediaPlayer.Dock = DockStyle.Fill;
            //mediaPlayer.Location = new Point(0, 0);
            mediaPlayer.Name = "mediaPlayer";
            //mediaPlayer.Size = new Size(640, 480);
            mediaPlayer.Size = panel1.Size;
           
            mediaPlayer.Visible = false;

            panel1.Controls.Add (mediaPlayer);
            mediaPlayer.Dock = DockStyle.Fill;
            mediaPlayer.Location = new Point(0, 0);

            panel1.Padding = new Padding(0, 0, 0, 200);
            mediaPlayer.Padding = new Padding(0, 0, 0, 200);
            //this.Controls.Add(mediaPlayer);
            ((System.ComponentModel.ISupportInitialize)(mediaPlayer)).EndInit();

            
        }


        private void InitializeDataGridView()
        {
            dataGridViewMedia.Columns.Add("FileName", "파일명");
            dataGridViewMedia.Columns.Add("FileType", "파일 유형");
            dataGridViewMedia.Columns.Add("FileSize", "파일 크기");
            dataGridViewMedia.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewMedia.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewMedia.MultiSelect = false;
            dataGridViewMedia.ReadOnly = true;
            dataGridViewMedia.CellDoubleClick += dataGridViewMedia_CellDoubleClick;

            
        }

        private void LoadEncryptedFiles()
        {
            dataGridViewMedia.Rows.Clear();
            string[] files = Directory.GetFiles(encryptedFilesPath, "*.enc.*"); //mediaDirectory

            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                string fileName = Path.GetFileName(file);
                string fileType = DetermineFileType(fileName);

                dataGridViewMedia.Rows.Add(fileName, fileType, $"{fileInfo.Length / 1024} KB");
            }
        }

        private string DetermineFileType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            switch (extension)
            {
                case ".jpg": // ".enc.jpg"
                case ".png": // ".enc.png"
                    return "이미지";
                case ".mp4": //".enc.mp4"
                case ".avi": //".enc.avi"
                    return "동영상";
                default:
                    return "알 수 없음";
            }
        }
        private async  void dataGridViewMedia_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                string fileName = dataGridViewMedia.Rows[e.RowIndex].Cells[0].Value.ToString();
                string fullPath = Path.Combine(encryptedFilesPath, fileName); //mediaDirectory

                try
                {
                    //string decryptedTempFile = DecryptFile(fullPath);
                    //OpenMediaFile(decryptedTempFile);
                    await DisplayMediaAsync(fullPath);//await DisplayMediaAsync(encryptedFile); // await 오류나면 해당 모듈에 async 를 추가해야됨.
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"파일 열기 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private string DecryptFile(string encryptedFilePath)
        {
            string tempFilePath = Path.GetTempFileName();

            using (FileStream fsInput = new FileStream(encryptedFilePath, FileMode.Open, FileAccess.Read))
            using (FileStream fsOutput = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            using (Aes aes = Aes.Create())
            {
                aes.Key = System.Text.Encoding.UTF8.GetBytes(encryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16]; // 초기화 벡터를 0으로 설정

                using (CryptoStream cs = new CryptoStream(fsInput, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    cs.CopyTo(fsOutput);
                }
            }

            return tempFilePath;
        }

        //private void OpenMediaFile(string filePath)
        //{
        //    string extension = Path.GetExtension(filePath).ToLower();

        //    switch (extension)
        //    {
        //        case ".jpg":
        //        case ".png":
        //            using (Form imageForm = new Form())
        //            {
        //                imageForm.StartPosition = FormStartPosition.CenterScreen;
        //                imageForm.Size = new Size(800, 600);

        //                PictureBox pictureBox = new PictureBox
        //                {
        //                    Image = Image.FromFile(filePath),
        //                    SizeMode = PictureBoxSizeMode.Zoom,
        //                    Dock = DockStyle.Fill
        //                };

        //                imageForm.Controls.Add(pictureBox);
        //                imageForm.ShowDialog();
        //            }
        //            break;

        //        case ".mp4":
        //        case ".avi":
        //            // VLC 미디어 플레이어 사용 (추가 설정 필요)
        //            System.Diagnostics.Process.Start(filePath);
        //            break;
        //    }

        //    // 임시 파일 삭제
        //    File.Delete(filePath);
        //}

        private void OpenMediaFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();

            switch (extension)
            {
                case ".jpg":
                case ".png":
                    using (Form imageForm = new Form())
                    {
                        imageForm.StartPosition = FormStartPosition.CenterScreen;
                        imageForm.Size = new Size(800, 600);

                        PictureBox pictureBox = new PictureBox
                        {
                            Image = Image.FromFile(filePath),
                            SizeMode = PictureBoxSizeMode.Zoom,
                            Dock = DockStyle.Fill
                        };

                        imageForm.Controls.Add(pictureBox);
                        imageForm.ShowDialog();
                    }
                    break;

                case ".mp4":
                case ".avi":
                    // Windows Media Player를 사용하여 동영상 재생
                    Form videoForm = new Form();
                    videoForm.StartPosition = FormStartPosition.CenterScreen;
                    videoForm.Size = new Size(800, 600);

                    // 기존 MediaPlayer 컨트롤 설정
                    mediaPlayer.URL = filePath;
                    mediaPlayer.Visible = true;
                    mediaPlayer.Width = videoForm.Width;
                    mediaPlayer.Height = videoForm.Height;

                    videoForm.Controls.Add(mediaPlayer);
                    videoForm.FormClosing += (s, e) =>
                    {
                        mediaPlayer.Visible = false;
                        mediaPlayer.URL = null;
                        File.Delete(filePath); // 임시 파일 삭제
                    };

                    videoForm.ShowDialog();
                    break;
            }
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            LoadEncryptedFiles();
        }
        private void InitializeComponents()
        {
            this.Size = new System.Drawing.Size(800, 600);

            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };


            mediaPlayer = new AxWMPLib.AxWindowsMediaPlayer
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            var menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("파일");
            fileMenu.DropDownItems.Add("파일 업로드", null, async (s, e) => await UploadFile_ClickAsync());
            menuStrip.Items.Add(fileMenu);

            this.Controls.Add(menuStrip);
            panel1.Controls.Add(pictureBox); //this.Controls.Add(pictureBox);
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.Padding = new Padding(0, 0, 0, 200);

        }

        private async Task UploadFile_ClickAsync()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "미디어 파일|*.jpg;*.png;*.mp4|모든 파일|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string sourceFile = openFileDialog.FileName;
                    string fileName = Path.GetFileName(sourceFile);

                    //출처: https://coding-abc.kr/120 [coding-abc.kr:티스토리] 
                    string file = Path.GetFileNameWithoutExtension(fileName);
                    string ext = Path.GetExtension(fileName);

                    //string encryptedFile = Path.Combine(encryptedFilesPath, fileName + ".encrypted");
                    string encryptedFile = Path.Combine(encryptedFilesPath, file + ".enc" + ext);

                    using (var progress = new ProgressForm())
                    {
                        progress.Show(this);
                        await EncryptFileAsync(sourceFile, encryptedFile, progress.ReportProgress);
                    }

                    await DisplayMediaAsync(encryptedFile);
                }
            }
            LoadEncryptedFiles(); //업로드후 파일목록이 담긴 datagridview 갱신
        }

        private async Task EncryptFileAsync(string sourceFile, string destinationFile, Action<int> progressCallback)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = System.Text.Encoding.UTF8.GetBytes(encryptionKey);
                aes.GenerateIV();

                using (FileStream fsInput = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                using (FileStream fsOutput = new FileStream(destinationFile, FileMode.Create))
                {
                    // IV 저장
                    await fsOutput.WriteAsync(aes.IV, 0, aes.IV.Length);

                    using (var encryptor = aes.CreateEncryptor())
                    using (CryptoStream cs = new CryptoStream(fsOutput, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] buffer = new byte[bufferSize];
                        int bytesRead;
                        long totalBytes = fsInput.Length;
                        long currentBytes = 0;

                        while ((bytesRead = await fsInput.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await cs.WriteAsync(buffer, 0, bytesRead);
                            currentBytes += bytesRead;
                            int progressPercentage = (int)((double)currentBytes / totalBytes * 100);
                            progressCallback(progressPercentage);
                        }
                    }
                }
            }
        }

        private class DecryptingStream : Stream 
        {
            private readonly Stream baseStream;
            private readonly CryptoStream cryptoStream;
            private bool disposed = false;

            public DecryptingStream(Stream encryptedStream, byte[] key)
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;

                    // IV 읽기
                    byte[] iv = new byte[aes.IV.Length];
                    encryptedStream.Read(iv, 0, iv.Length);
                    aes.IV = iv;

                    this.baseStream = encryptedStream;
                    this.cryptoStream = new CryptoStream(
                        encryptedStream,
                        aes.CreateDecryptor(),
                        CryptoStreamMode.Read);
                }
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return cryptoStream.Read(buffer, offset, count);
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return await cryptoStream.ReadAsync(buffer, offset, count, cancellationToken);
            }

            protected override void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        cryptoStream.Dispose();
                        baseStream.Dispose();
                    }
                    disposed = true;
                }
                base.Dispose(disposing);
            }

            public override void Flush() => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }


        private async Task DisplayMediaAsync(string encryptedFile)
        {
            string extension = Path.GetExtension(encryptedFile.Replace(".encrypted", "")).ToLower();

            if (extension == ".jpg" || extension == ".png")
            {
                await DisplayImageAsync(encryptedFile);
            }
            else if (extension == ".mp4")
            {
                await DisplayVideoAsync(encryptedFile);
            }
        }

        private async Task DisplayImageAsync(string encryptedFile)
        {
            mediaPlayer.Visible = false;
            pictureBox.Visible = true;

            // 이미지의 경우 메모리에 완전히 로드해야 하지만, 스트리밍으로 복호화
            using (var fsInput = new FileStream(encryptedFile, FileMode.Open, FileAccess.Read))
            using (var decryptStream = new DecryptingStream(fsInput, System.Text.Encoding.UTF8.GetBytes(encryptionKey)))
            using (var msOutput = new MemoryStream())
            {
                await decryptStream.CopyToAsync(msOutput);
                pictureBox.Image = System.Drawing.Image.FromStream(new MemoryStream(msOutput.ToArray()));
            }
        }

        private async Task DisplayVideoAsync(string encryptedFile)
        {
            pictureBox.Visible = false;
            mediaPlayer.Dock = DockStyle.Fill;
            mediaPlayer.Padding = new Padding(0, 0, 0, 200);
            mediaPlayer.Visible = true;
            panel1.Update();


            mediaPlayer.Ctlcontrols.stop(); // 재생 중지
            mediaPlayer.URL = null;         // URL 초기화

            // 복호화된 파일을 임시 디렉터리에 저장
            //string tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(encryptedFile) + ".mp4");
            tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(encryptedFile) + ".mp4");
            try
            {
                // 암호화된 파일 복호화
                using (var fsInput = new FileStream(encryptedFile, FileMode.Open, FileAccess.Read))
                using (var decryptStream = new DecryptingStream(fsInput, System.Text.Encoding.UTF8.GetBytes(encryptionKey)))
                {
                    // 복호화된 파일 쓰기
                    using (var fsOutput = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await decryptStream.CopyToAsync(fsOutput);
                    }

                    // 파일이 생성되었는지 확인
                    if (!File.Exists(tempFile) || new FileInfo(tempFile).Length == 0)
                    {
                        throw new IOException("복호화된 파일 생성에 실패했습니다.");
                    }

                    
                    // 미디어 플레이어에 URL 설정
                    mediaPlayer.URL = tempFile;
                }



                // 재생 완료 후 파일 삭제를 위한 이벤트 핸들러 추가
                mediaPlayer.PlayStateChange -= MediaPlayer_PlayStateChange;
                mediaPlayer.PlayStateChange += MediaPlayer_PlayStateChange;


                // 폼 닫힐 때 파일 삭제 (보호 장치)
                this.FormClosing += (s, e) =>
                {
                    DeleteTempFile(); // 임시 파일 삭제
                };
            }
            catch (IOException ioEx)
            {
                
                // 파일이 생성되었는지 확인
                if (!File.Exists(tempFile))
                {
                    // 파일 IO 문제 처리 (예: 경로, 권한 문제)
                    MessageBox.Show($"파일 처리 중 오류 발생: {ioEx.Message}", "파일 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (UnauthorizedAccessException uaEx)
            {
                // 파일 권한 문제 처리
                MessageBox.Show($"파일 접근 권한 오류: {uaEx.Message}", "권한 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                // 일반적인 예외 처리
                MessageBox.Show($"비디오 재생 준비 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 임시 파일 삭제 메서드
        private void DeleteTempFile()
        {
            try
            {
                if (!string.IsNullOrEmpty(tempFile) && File.Exists(tempFile))
                {
                    mediaPlayer.Ctlcontrols.stop();
                    File.Delete(tempFile);
                }
            }
            catch (Exception ex)
            {
                // 오류 발생 시 메시지 표시를 생략하고 로그 기록
                LogError("임시 파일 삭제 중 오류 발생", ex);
            }
        }
        // 로그 기록 메서드
        private void LogError(string message, Exception ex)
        {
            // 로그 파일 경로 설정 (필요에 따라 수정)
            string logFilePath = Path.Combine(Path.GetTempPath(), "app_errors.log");
            try
            {
                File.AppendAllText(logFilePath, $"{DateTime.Now}: {message} - {ex.Message}{Environment.NewLine}");
            }
            catch
            {
                // 로그 기록 중 실패해도 추가 동작은 생략
            }
        }
        // 핸들러에서 상태 확인 후 파일 삭제
        private void MediaPlayer_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            // 상태 8은 "MediaEnded" 상태
            if (e.newState == 8) // MediaEnded
            {
                try
                {
                    if (File.Exists(tempFile))
                    {
                        mediaPlayer.Ctlcontrols.stop(); // 재생 중지
                        File.Delete(tempFile);          // 파일 삭제
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"파일 삭제 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task<string> CreateStreamingProxyAsync(string encryptedFile)
        {
            // 로컬 HTTP 서버를 생성하여 암호화된 비디오 스트리밍
            var proxyServer = new StreamingProxyServer(encryptedFile, encryptionKey);
            await proxyServer.StartAsync();
            return proxyServer.ProxyUrl;
        }

        private void StreamingMediaViewer_Resize(object sender, EventArgs e)
        {
            if (panel1.Controls.OfType<PictureBox>().Any())
            {
                // pictureBox가 panel1에 존재할 때의 처리
                // MessageBox.Show("PictureBox exists in panel1.");
                pictureBox.Dock = DockStyle.Fill;
                pictureBox.Padding = new Padding(0, 0, 0, 200);

            }

            if (panel1.Controls.OfType<AxWindowsMediaPlayer>().Any())
            {
                // pictureBox가 panel1에 존재할 때의 처리
                // MessageBox.Show("PictureBox exists in panel1.");
                mediaPlayer.Dock = DockStyle.Fill;
                mediaPlayer.Padding = new Padding(0, 0, 0, 200);

            }
        }

        private async void dataGridViewMedia_KeyUp(object sender, KeyEventArgs e)
        {
            // 키가 Up 또는 Down인지 확인
            if (e.KeyData == Keys.Up || e.KeyData == Keys.Down)
            {
                // Null 체크: CurrentRow와 Cells가 유효한지 확인
                if (dataGridViewMedia.CurrentRow != null &&
                    dataGridViewMedia.CurrentRow.Cells.Count > 0 &&
                    dataGridViewMedia.CurrentRow.Cells[0].Value != null)
                {
                    string fileName = dataGridViewMedia.CurrentRow.Cells[0].Value.ToString();

                    // Null 체크: encryptedFilesPath가 유효한지 확인
                    if (!string.IsNullOrEmpty(encryptedFilesPath))
                    {
                        string fullPath = Path.Combine(encryptedFilesPath, fileName);

                        try
                        {
                            // 파일 경로를 비동기적으로 처리
                            await DisplayMediaAsync(fullPath);
                        }
                        catch (Exception ex)
                        {
                            // 오류 메시지 표시
                            MessageBox.Show($"파일 열기 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("파일 경로가 올바르지 않습니다.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("선택된 행이 없거나 유효하지 않습니다.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

    }

    // 스트리밍 프록시 서버 (비디오 스트리밍을 위한 간단한 HTTP 서버)
    public class StreamingProxyServer
    {
        private readonly HttpListener listener;
        private readonly string encryptedFilePath;
        private readonly string encryptionKey;
        private readonly string proxyUrl;
        private bool isRunning;

        public string ProxyUrl => proxyUrl;

        public StreamingProxyServer(string encryptedFilePath, string encryptionKey)
        {
            this.encryptedFilePath = encryptedFilePath;
            this.encryptionKey = encryptionKey;
            this.listener = new HttpListener();

            // 랜덤 포트 할당
            int port = new Random().Next(49152, 65535);
            this.proxyUrl = $"http://localhost:{port}/video";
            this.listener.Prefixes.Add($"http://localhost:{port}/");
        }

        public async Task StartAsync()
        {
            if (isRunning) return;

            isRunning = true;
            listener.Start();

            await Task.Run(async () =>
            {
                while (isRunning)
                {
                    try
                    {
                        var context = await listener.GetContextAsync();
                        ProcessRequestAsync(context);
                    }
                    catch (HttpListenerException)
                    {
                        break;
                    }
                }
            });
        }

        private async void ProcessRequestAsync(HttpListenerContext context)
        {
            var response = context.Response;
            response.ContentType = "video/mp4";
            response.Headers.Add("Accept-Ranges", "bytes");

            try
            {
                using (var fileStream = new FileStream(encryptedFilePath, FileMode.Open, FileAccess.Read))
                using (var decryptStream = new DecryptingStream(fileStream, System.Text.Encoding.UTF8.GetBytes(encryptionKey)))
                {
                    var rangeHeader = context.Request.Headers["Range"];
                    if (!string.IsNullOrEmpty(rangeHeader))
                    {
                        // Range 요청 처리
                        ProcessRangeRequest(response, decryptStream, rangeHeader);
                    }
                    else
                    {
                        // 전체 파일 스트리밍
                        await decryptStream.CopyToAsync(response.OutputStream);
                    }
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                byte[] error = System.Text.Encoding.UTF8.GetBytes(ex.Message);
                await response.OutputStream.WriteAsync(error, 0, error.Length);
            }
            finally
            {
                response.OutputStream.Close();
            }
        }

        private void ProcessRangeRequest(HttpListenerResponse response, Stream decryptStream, string rangeHeader)
        {
            // Range 헤더 파싱 및 처리
            var range = rangeHeader.Replace("bytes=", "").Split('-');
            long start = long.Parse(range[0]);
            long end = range.Length > 1 && !string.IsNullOrEmpty(range[1])
                ? long.Parse(range[1])
                : decryptStream.Length - 1;

            response.StatusCode = 206;
            response.Headers.Add("Content-Range", $"bytes {start}-{end}/{decryptStream.Length}");
            response.ContentLength64 = end - start + 1;

            byte[] buffer = new byte[81920];
            int bytesRead;
            long bytesRemaining = response.ContentLength64;

            while (bytesRemaining > 0 && (bytesRead = decryptStream.Read(buffer, 0, (int)Math.Min(bytesRemaining, buffer.Length))) > 0)
            {
                response.OutputStream.Write(buffer, 0, bytesRead);
                bytesRemaining -= bytesRead;
            }
        }

        public void Stop()
        {
            isRunning = false;
            listener.Stop();
        }

        private class DecryptingStream : Stream
        {
            private readonly Stream baseStream;
            private readonly CryptoStream cryptoStream;
            private bool disposed = false;

            public DecryptingStream(Stream encryptedStream, byte[] key)
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;

                    // IV 읽기
                    byte[] iv = new byte[aes.IV.Length];
                    encryptedStream.Read(iv, 0, iv.Length);
                    aes.IV = iv;

                    this.baseStream = encryptedStream;
                    this.cryptoStream = new CryptoStream(
                        encryptedStream,
                        aes.CreateDecryptor(),
                        CryptoStreamMode.Read);
                }
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return cryptoStream.Read(buffer, offset, count);
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return await cryptoStream.ReadAsync(buffer, offset, count, cancellationToken);
            }

            protected override void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        cryptoStream.Dispose();
                        baseStream.Dispose();
                    }
                    disposed = true;
                }
                base.Dispose(disposing);
            }

            public override void Flush() => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

    }

}
