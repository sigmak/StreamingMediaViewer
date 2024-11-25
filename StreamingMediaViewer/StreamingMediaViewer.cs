using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Threading.Tasks;
using WMPLib;
using AxWMPLib;
using System.Threading;
using System.Net;

namespace StreamingMediaViewer
{
    public partial class StreamingMediaViewer : Form
    {
        private readonly string encryptionKey = "YourSecretKey123";
        private readonly string encryptedFilesPath;//private readonly string encryptedFilesPath = @"D:\EncryptedFiles\";
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

            try
            {
                InitializeComponents();
            } catch(Exception ex)
            {
                MessageBox.Show(ex.Message);  
            }
            
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
            this.Controls.Add(pictureBox);
            this.Controls.Add(mediaPlayer);
           // this.Controls.Add(wPlayer);
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
                    string encryptedFile = Path.Combine(encryptedFilesPath, fileName + ".encrypted");

                    using (var progress = new ProgressForm())
                    {
                        progress.Show(this);
                        await EncryptFileAsync(sourceFile, encryptedFile, progress.ReportProgress);
                    }

                    await DisplayMediaAsync(encryptedFile);
                }
            }
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
            mediaPlayer.Visible = true;

            // 비디오를 위한 암호화된 스트리밍 프록시 생성
            string proxyUrl = await CreateStreamingProxyAsync(encryptedFile);
            mediaPlayer.URL = proxyUrl;
        }

        private async Task<string> CreateStreamingProxyAsync(string encryptedFile)
        {
            // 로컬 HTTP 서버를 생성하여 암호화된 비디오 스트리밍
            var proxyServer = new StreamingProxyServer(encryptedFile, encryptionKey);
            await proxyServer.StartAsync();
            return proxyServer.ProxyUrl;
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
