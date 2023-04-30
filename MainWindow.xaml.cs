using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Net;
using System.Windows;

namespace HoN_Launcher
{
    enum LaucherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const bool V = true;
        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;

        private LaucherStatus _status;

        internal LaucherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status) 
                {
                    case LaucherStatus.ready:
                        PlayButton.Content = "Launch Game";
                        break;
                    case LaucherStatus.failed:
                        PlayButton.Content = "Update Failed - Retry";
                        break;
                    case LaucherStatus.downloadingGame:
                        PlayButton.Content = "Downloading Game";
                        break;
                    case LaucherStatus.downloadingUpdate:
                        PlayButton.Content = "Downloading Update";
                        break;
                    default:
                        break;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, "Build.zip");
            gameExe = Path.Combine(rootPath, "Build", "testApp.exe");
        }

        private void CheckForUpdates()
        {
            if(File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text  = localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?id=1eTrK-nWdsoE5LW3vq-ffZ5KVSJAOon2U&export=download"));

                    if (onlineVersion.IsDifferentThan(localVersion)) 
                    {
                        InstallGameFiles(false, onlineVersion);
                    }
                    else
                    {
                        Status = LaucherStatus.ready;
                    }
                }
                catch (Exception ex)
                {
                    Status = LaucherStatus.failed;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                }
            } 
            else
            {
                InstallGameFiles(false, Version.zero);
            }
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion) 
        {
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LaucherStatus.downloadingUpdate;
                } 
                else
                {
                    Status = LaucherStatus.downloadingGame;
                    _onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?id=1eTrK-nWdsoE5LW3vq-ffZ5KVSJAOon2U&export=download"));
                }

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallBack);
                webClient.DownloadFileAsync(new Uri("https://www.dropbox.com/s/34jjyctywi3jnwd/testApp.zip?dl=1"), gameZip, _onlineVersion);
            }
            catch (Exception ex)
            {
                Status = LaucherStatus.failed;
                MessageBox.Show($"Error checking for game files: {ex}");
            }
        }

        private void DownloadGameCompletedCallBack(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((Version)e.UserState).ToString();
                ZipFile.ExtractToDirectory(gameZip, Path.Combine(rootPath, "Build"));
                File.Delete(gameZip);

                File.WriteAllText(versionFile, onlineVersion);

                VersionText.Text = "Version:  " + onlineVersion;
                Status = LaucherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LaucherStatus.failed;
                MessageBox.Show($"Error finishing download: {ex}");
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private void PlayButton_Click(object sender, EventArgs e) 
        {
            if(File.Exists(gameExe) && Status == LaucherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, "Build");
                Process.Start(startInfo);
                //Close launcher window after starting game - disabled when testing
                //Close(); 
            }
            else if(Status == LaucherStatus.failed)
            {
                CheckForUpdates();
            }
        }
    }

    struct Version
    {
        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;   
        }

        internal Version(string _version)
        {
            string[] _versionStrings = _version.Split('.');
            if(_versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(_versionStrings[0]);
            minor = short.Parse(_versionStrings[1]);
            subMinor = short.Parse(_versionStrings[2]);
        }

        internal bool IsDifferentThan(Version _otherVersion)
        {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != _otherVersion.subMinor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
