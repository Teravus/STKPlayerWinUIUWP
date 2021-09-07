using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using LibVLCSharp.Shared;
using LibVLCSharp.Platforms.UWP;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Core;
using Windows.UI;
using System.Threading;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace STKPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private LibVLC _libVLCMain;
        private LibVLC _libVLCInfo;

        private MediaPlayer _mediaPlayerMain;
      
        private Point _lastClickPoint = new Point(0d, 0d);
        

        private static double _OriginalMainVideoHeight = 800d;
        private static double _OriginalMainVideoWidth = 1290d;

        private double _OriginalAspectRatio = _OriginalMainVideoWidth / _OriginalMainVideoHeight;

        private List<SceneDefinition> _scenes = new List<SceneDefinition>();
        private List<SceneDefinition> _infoScenes = new List<SceneDefinition>();
        private List<SceneDefinition> _computerScenes = new List<SceneDefinition>();
        private List<SceneDefinition> _holodeckScenes = new List<SceneDefinition>();

        private List<HotspotDefinition> _hotspots = new List<HotspotDefinition>();

        private long _maxVideoMS = 0;

        private readonly DispatcherTimer _clickTimer = new DispatcherTimer();
        

        private bool _MainVideoLoaded = false;
        private bool _mcurVisible = false;
        private ScenePlayer _mainScenePlayer = null;
        private SupportingPlayer _supportingPlayer = null;
        private bool _actionTime = false;
        private const Windows.System.VirtualKey AccentTilde = (Windows.System.VirtualKey)223;

        BitmapImage HolodeckCursor = null;  //new BitmapImage(new Uri(Path.Combine("Assets", "KlingonHolodeckCur.gif"), UriKind.Relative));
        BitmapImage dktahgCursor = null; // new BitmapImage(new Uri(Path.Combine("Assets", "dktahg.gif"), UriKind.Relative));
        

        public MainPage()
        {
            
            this.InitializeComponent();
            Window.Current.CoreWindow.KeyDown +=  (ob, ea) =>
            {
                if (_MainVideoLoaded)
                {
                    switch (ea.VirtualKey)
                    {
                        case Windows.System.VirtualKey.Q:
                            Quit();
                            return;
                        case Windows.System.VirtualKey.S:
                            if ( SaveDialog.Visibility == Visibility.Collapsed)
                            {
                               
                                if (VideoView.MediaPlayer.IsPlaying)
                                    VideoView.MediaPlayer.Pause();
                                SaveDialog.Visibility = Visibility.Visible;
                                var pointerPosition = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;
                                var pos = Window.Current.CoreWindow.PointerPosition;
                                CurEmulator.Margin = new Thickness(pos.X - Window.Current.Bounds.X, pos.Y - Window.Current.Bounds.Y + 1, 0, 0);
                                _mcurVisible = true;
                                CurEmulator.Source = dktahgCursor;
                                CurEmulator.Visibility = Visibility.Visible;
                            }
                            return;
                        case Windows.System.VirtualKey.LeftButton:
                            if (txtMS.Visibility == Visibility.Visible && txtOffsetMs.Visibility == Visibility.Visible) // Only if debug is visible
                            {
                                VideoView.MediaPlayer.Time -= 15000;
                            }
                            break;
                        case Windows.System.VirtualKey.RightButton:
                            if (txtMS.Visibility == Visibility.Visible && txtOffsetMs.Visibility == Visibility.Visible) // Only if debug is visible
                            {
                                VideoView.MediaPlayer.Time += 15000;
                            }
                            break;
                        
                        case Windows.System.VirtualKey.C:
                            if (txtMS.Visibility == Visibility.Visible && txtOffsetMs.Visibility == Visibility.Visible) // Only if debug is visible
                            {
                                if (_mainScenePlayer != null)
                                {
                                    _mainScenePlayer.JumpToChallenge();
                                }
                            }
                            break;
                        case Windows.System.VirtualKey.Enter:
                            if (txtMS.Visibility == Visibility.Visible && txtOffsetMs.Visibility == Visibility.Visible) // Only if debug is visible
                            {
                                float offsetint = 0;
                                if (txtOffsetMs.Text.Length > 0)
                                    offsetint = Convert.ToSingle(txtOffsetMs.Text);
                                if (txtMS.Text.Length > 0)
                                {
                                    //VideoView.MediaPlayer.Time = (long)(((Convert.ToSingle(txtMS.Text)-2)* 98.14f) + (offsetint));
                                    VideoView.MediaPlayer.Time = (long)(Utilities.Frames15fpsToMS(Convert.ToInt32(txtMS.Text) - 2) + (offsetint * 100));

                                }
                            }
                            break;
                        case Windows.System.VirtualKey.Tab:
                            if (txtMS.Visibility == Visibility.Visible && txtOffsetMs.Visibility == Visibility.Visible)
                            {
                                txtMS.Visibility = Visibility.Collapsed;
                                txtOffsetMs.Visibility = Visibility.Collapsed;
                                lstScene.Visibility = Visibility.Collapsed;
                                lstComputer.Visibility = Visibility.Collapsed;
                                CurEmulator.Visibility = Visibility.Collapsed;
                                _mainScenePlayer.VisualizeRemoveHotspots();
                                _mcurVisible = false;
                            }
                            else
                            {
                                txtMS.Visibility = Visibility.Visible;
                                txtOffsetMs.Visibility = Visibility.Visible;
                                lstScene.Visibility = Visibility.Visible;
                                lstComputer.Visibility = Visibility.Visible;
                                CurEmulator.Visibility = Visibility.Visible;
                                var pointerPosition = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;

                                var pos = Window.Current.CoreWindow.PointerPosition;
                                CurEmulator.Margin = new Thickness(pos.X - Window.Current.Bounds.X, pos.Y - Window.Current.Bounds.Y + 1, 0, 0);
                                
                                _mainScenePlayer.VisualizeHotspots(VideoViewGrid);
                                _mcurVisible = true;
                            }
                            break;
                    }
                }
                
            };

            _clickTimer.Interval = TimeSpan.FromSeconds(0.2); //wait for the other click for 200ms
            _clickTimer.Tick += (o1,em) =>
            {
                lock (_clickTimer)
                    _clickTimer.Stop();
                // Fire Single Click.

                var aspectquery = Utilities.GetMax(VideoViewGrid.ActualWidth, VideoViewGrid.ActualHeight, _OriginalAspectRatio);
                var clickareawidth = VideoViewGrid.ActualWidth;
                var clickareaheight = VideoViewGrid.ActualHeight;
                var clickOffsetL = 0d;
                var clickOffsetT = 0d;

                switch (aspectquery.Direction)
                {
                    case "W":
                        clickareawidth = aspectquery.Length;
                        clickOffsetL = (VideoViewGrid.ActualWidth - clickareawidth) * 0.376d; // Why are we chopping it into almost thirds instead of half 0.376d?  This should be the black bar width for top and bottom.
                        break;
                    case "H":
                        clickareaheight = aspectquery.Length;
                        clickOffsetT = (VideoViewGrid.ActualHeight - clickareaheight) * 0.376d;
                        break;
                }


                var relclickX = (int)(((_lastClickPoint.X / clickareawidth) * _OriginalMainVideoWidth) - clickOffsetL);
                var relclickY = (int)(((_lastClickPoint.Y / clickareaheight) * _OriginalMainVideoHeight) - clickOffsetT);
                long time = 0;
                TimeSpan ts = TimeSpan.Zero;
                if (_MainVideoLoaded)
                {
                    time = VideoView.MediaPlayer.Time;
                    ts = TimeSpan.FromMilliseconds(time);
                }
                System.Diagnostics.Debug.WriteLine("{0},{1}({2},{3})[{4}] - t{5} - f{6}", _lastClickPoint.X, _lastClickPoint.Y, relclickX, relclickY, ts.ToString(@"hh\:mm\:ss"), (long)((float)time), Utilities.MsTo15fpsFrames(time));
                if (_MainVideoLoaded && _mainScenePlayer != null && !string.IsNullOrEmpty(_mainScenePlayer.ScenePlaying))
                {
                    _mainScenePlayer.MouseClick(relclickX, relclickY, VideoView.MediaPlayer.IsPlaying);
                }
                //VideoView.MediaPlayer.Time = 269200;
               


            };
            

            Loaded += (s, e) =>
            {
                ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
                formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
                CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                coreTitleBar.ExtendViewIntoTitleBar = true;
                
                VideoView.Loaded += (s1, e1) =>
                {
                    List<string> options = new List<string>();
                    options.AddRange(VideoView.SwapChainOptions.ToList());
                    //options.Add("--verbose=2");

                    _libVLCMain = new LibVLC(options.ToArray());
                    _mediaPlayerMain = new MediaPlayer(_libVLCMain);
                    VideoView.MediaPlayer = _mediaPlayerMain;

                    _libVLCMain.Log += Log_Fired;

                    
                    


                    VideoView.DoubleTapped += (s2, e2) =>
                                {
                                    if (!_actionTime) // No pausing during active time.  It is too difficult to separate single and double clicks during some scenes that you need to rapid click.
                                    {

                                        SwitchGameModeActiveInfo();

                                        e2.Handled = true;

                                        lock (_clickTimer)
                                            _clickTimer.Stop();
                                    }
                                };


                    VideoView.Tapped += (s3, s4) =>
                    {
                        var tappedspot = s4.GetPosition(s3 as UIElement);


                        _lastClickPoint = tappedspot;

                        lock (_clickTimer)
                        {
                            _clickTimer.Stop();
                            _clickTimer.Start();
                        }
                    };
                    this.KeyDown +=  (o4, s5) =>
                    {
                        if (s5.Key == Windows.System.VirtualKey.Q)
                        {
                            Quit();
                        }
                    };
                    VideoViewGrid.KeyDown +=  (o4, s5) =>
                    {
                        if (s5.Key == Windows.System.VirtualKey.Q)
                        {
                            Quit();
                        }
                    };
                    this.KeyDown += (o4, s5) =>
                    {
                        if (s5.Key == Windows.System.VirtualKey.Space)
                            SwitchGameModeActiveInfo();
                    };
                    VideoView.KeyDown += (o4, s5) =>
                    {
                        if (s5.Key == Windows.System.VirtualKey.Space)
                            SwitchGameModeActiveInfo();
                    };
                    VideoView.KeyDown +=  (o4, s5) =>
                    {
                        if (s5.Key == Windows.System.VirtualKey.Q)
                        {
                            Quit();
                        }
                    };
                    VideoInfo.Loaded += (s2, e2) =>
                    {
                        List<string> options2 = new List<string>();
                        options.AddRange(VideoInfo.SwapChainOptions.ToList());
                        //options.Add("--verbose=2");

                        _libVLCInfo = new LibVLC(options.ToArray());
                        
                        VideoInfo.MediaPlayer = new MediaPlayer(_libVLCInfo);
                        
                        _libVLCInfo.Log += Log_Fired;

                        var InfoScenes = _scenes.Where(xy => xy.SceneType == SceneType.Info).ToList();
                        _infoScenes = InfoScenes;

                        var ComputerScenes = SceneLoader.LoadSupportingScenesFromAsset("computerscenes.txt");
                        _computerScenes = ComputerScenes;
                        var HolodeckScenes = SceneLoader.LoadSupportingScenesFromAsset("holodeckscenes.txt");
                        _holodeckScenes = HolodeckScenes;
                        _supportingPlayer = new SupportingPlayer(VideoInfo, InfoScenes, ComputerScenes, HolodeckScenes, _libVLCInfo);
                        Load_Computer_list(_computerScenes);

                    };
                };

            };

            Unloaded += (s, e) =>
            {
                VideoView.MediaPlayer = null;
                _supportingPlayer.Dispose();
                VideoInfo.MediaPlayer = null;

                _mediaPlayerMain.Dispose();
                

                this._libVLCMain.Dispose();
                this._libVLCInfo.Dispose();
            };
            SizeChanged += (s, e) =>
            {
                Size NewSize = e.NewSize;
                Size OldSize = e.PreviousSize;
                VideoViewGrid.Width = this.ActualWidth;
                VideoViewGrid.Height = this.ActualHeight;
                VideoView.Width = this.ActualWidth;
                VideoView.Height = this.ActualHeight;
                ImgStartMain.Height = this.ActualHeight;
                ImgStartMain.Width = this.ActualWidth;
                grdStartControls.Height = this.ActualHeight;
                grdStartControls.Width = this.ActualWidth;
                //VideoView.HorizontalAlignment = HorizontalAlignment.Left;
                //VideoView.VerticalAlignment = VerticalAlignment.Top;

            };
            btnNewGame.Click += (s, e) =>
            {
                PrepPlayer();
                _mainScenePlayer.TheSupportingPlayer = _supportingPlayer;
                _mainScenePlayer.PlayScene(_scenes[0]);
                //CurEmulator.Visibility = Visibility.Visible;


            };
            lstScene.SelectionChanged += (s, e) =>
            {
                var ClickedItems = e.AddedItems;
                foreach (var item in ClickedItems)
                {
                    ComboBoxItem citem = item as ComboBoxItem;
                    string scenename = citem.Content.ToString();
                    SceneDefinition founddef = null;
                    foreach (var scenedef in _scenes)
                    {
                        if (scenedef.Name == scenename)
                        {
                            founddef = scenedef;
                            break;
                        }
                    }
                    if (founddef != null && _MainVideoLoaded)
                    {
                        _mainScenePlayer.PlayScene(founddef);
                    }
                }
            };
            lstComputer.SelectionChanged += (s, e) =>
            {

                var ClickedItems = e.AddedItems;
                foreach (var item in ClickedItems)
                {
                    ComboBoxItem citem = item as ComboBoxItem;
                    string scenename = citem.Content.ToString();
                    SceneDefinition founddef = null;
                    foreach (var scenedef in _computerScenes)
                    {
                        if (scenedef.Name == scenename)
                        {
                            founddef = scenedef;
                            break;
                        }
                    }
                    if (founddef != null && _MainVideoLoaded)
                    {
                        _supportingPlayer.DebugSetEvents(false);
                        _supportingPlayer.QueueScene(founddef, "computer"); // I'm treating these like info regardless of the actual type so it doesn't affect the main video when testing.
                    }
                }
            };
            PointerMoved += (s, e) =>
            {
                if (!_mcurVisible)
                    return;
                UIElement sui = s as UIElement;
                var point = e.GetCurrentPoint(sui);
                CurEmulator.Margin = new Thickness(point.Position.X, point.Position.Y + 1,0,0);
                // I would rather this be a windows mouse cursor..  but this will have to do with UWP for now.
                // Also the + 1 offset is so you don't register mouse clicks on the image element.  We need those clicks to hit the grid.
                
            };
            btnLoadGame.Click += async (s, e) =>
            {

               var saves = await SaveLoader.LoadSavesFromAsset("RIVER.TXT");
               lstRiver.Items.Clear();
               foreach (var save in saves)
                   lstRiver.Items.Add(new ComboBoxItem() { Content = save.SaveName });

               LoadDialog.Visibility = Visibility.Visible;
               //
            };

            lstRiver.SelectionChanged += (s, e) =>
            {
                if (lstRiver.SelectedIndex >=0)
                {
                    btnLoad.IsEnabled = true;
                }
                else
                {
                    btnLoad.IsEnabled = false;
                }
            };

            btnLoad.Click += async (s, e) =>
            {
                if (lstRiver.SelectedIndex >= 0)
                {
                    SaveDefinition savedef = null;
                    var saves = await SaveLoader.LoadSavesFromAsset("RIVER.TXT");
                    foreach (var save in saves)
                    {
                        ComboBoxItem citem = lstRiver.SelectedValue as ComboBoxItem;
                        if (save.SaveName == citem.Content.ToString())
                        {
                            savedef = save;
                        }
                    }
                    if (savedef != null)
                    {
                        LoadDialog.Visibility = Visibility.Collapsed;
                        PrepPlayer();
                        _mainScenePlayer.TheSupportingPlayer = _supportingPlayer;
                        _mainScenePlayer.LoadSave(savedef);

                    }
                }

            };
            btnLoadCancel.Click += (s, e) =>
            {
                LoadDialog.Visibility = Visibility.Collapsed;
            };
            btnQuitGame.Click +=  (s, e) =>
            {
                Quit();
            };
            HolodeckCursor = new BitmapImage(new Uri(Path.Combine(System.IO.Directory.GetCurrentDirectory(),"Assets", "KlingonHolodeckCur.gif")));
            dktahgCursor =  new BitmapImage(new Uri(Path.Combine(System.IO.Directory.GetCurrentDirectory(),"Assets", "dktahg.gif")));
            btnSaveCancel.Click += (s, e) =>
            {
                if (!VideoView.MediaPlayer.IsPlaying)
                    VideoView.MediaPlayer.Play();
                SaveDialog.Visibility = Visibility.Collapsed;
               
                _mcurVisible = false;
                CurEmulator.Source = dktahgCursor;
                CurEmulator.Visibility = Visibility.Collapsed;
            };
            btnSave.Click += async(s, e) =>
            {
                string SaveName = txtSaveName.Text;

                if (string.IsNullOrEmpty(SaveName))
                {
                    txtSaveErrorText.Text = "Please type a Name in the box.";
                    txtSaveErrorText.Visibility = Visibility.Visible;
                    return;
                }

                if (Utilities.ValidateSaveGameName(SaveName))
                {
                    SaveDefinition info = _mainScenePlayer.GetSaveInfo();

                    info.SaveName = SaveName;
                    
                    List<SaveDefinition> saves = await SaveLoader.LoadSavesFromAsset("RIVER.TXT");
                    saves.Add(info);
                    await SaveLoader.SaveSavesToAsset(saves, "RIVER.TXT");

                    if (!VideoView.MediaPlayer.IsPlaying)
                        VideoView.MediaPlayer.Play();
                    SaveDialog.Visibility = Visibility.Collapsed;

                    _mcurVisible = false;
                    CurEmulator.Source = dktahgCursor;
                    CurEmulator.Visibility = Visibility.Collapsed;
                    txtSaveErrorText.Visibility = Visibility.Collapsed;
                    txtSaveErrorText.Text = "";
                    return;

                }
                txtSaveErrorText.Text = "Please remove this dishonorable text you Ferengi Ha'DIbaH!";
                txtSaveErrorText.Visibility = Visibility.Visible;
                

            };
        }
        private void SwitchGameModeActiveInfo()
        {
            if (!_actionTime) // No pausing during active time.  It is too difficult to separate single and double clicks during some scenes that you need to rapid click.
            {
                if (_MainVideoLoaded)
                {
                    // If the Save or load dialog is visible, we want them resuming the video with double click
                    if (SaveDialog.Visibility == Visibility.Collapsed && LoadDialog.Visibility == Visibility.Collapsed)
                    {
                        if (VideoView.MediaPlayer.IsPlaying)
                        {
                            VideoView.MediaPlayer.Pause();
                            CurEmulator.Visibility = Visibility.Visible;

                            CurEmulator.Source = HolodeckCursor;
                            HolodeckCursor.Play();
                            var beep = _holodeckScenes[0];
                            var hum = _holodeckScenes[1];
                            _supportingPlayer.QueueScene(beep, "holodeck");
                            _supportingPlayer.QueueScene(hum, "holodeck", 0, true);
                            //CurEmulator.SetAnimatedSource(img, image);
                            //CurEmulator.SetRepeatBehavior(img, new RepeatBehavior(0));
                            //CurEmulator.SetRepeatBehavior(img, RepeatBehavior.Forever);


                            var pointerPosition = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;
                            var pos = Window.Current.CoreWindow.PointerPosition;
                            CurEmulator.Margin = new Thickness(pos.X - Window.Current.Bounds.X, pos.Y - Window.Current.Bounds.Y + 1, 0, 0);
                            _mcurVisible = true;
                        }
                        else
                        {
                            _supportingPlayer.Pause();
                            _supportingPlayer.ClearQueue();

                            VideoView.MediaPlayer.Play();
                            CurEmulator.Source = dktahgCursor;
                            dktahgCursor.Play();
                            CurEmulator.Visibility = Visibility.Collapsed;
                            _mcurVisible = false;
                        }
                    }
                }
            }
        }
        private void Quit()
        {
            SceneDefinition scene = null;
            if (_scenes != null && _scenes.Count == 0)
            {
                PrepPlayer();
            }
            if (_scenes == null)
            {
                PrepPlayer();
            }
            if (_scenes.Count == 0)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                ApplicationView.GetForCurrentView().TryConsolidateAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            else
            {
                scene = _scenes[0];
                if (scene != null)
                {
                    _mainScenePlayer.PlayScene(scene);
                }
                scene = _scenes.Where(xy => xy.Name == "LOGO1").FirstOrDefault();
                if (scene != null)
                {
                    _mainScenePlayer.PlayScene(scene);
                }
            }
        }
        private void PrepPlayer()
        {
            btnNewGame.IsEnabled = false;
            grdStartControls.Visibility = Visibility.Collapsed;

            _scenes = SceneLoader.LoadScenesFromAsset("scenes.txt");
            _hotspots = HotspotLoader.LoadHotspotsFromAsset("hotspots.txt");
            _infoScenes = _scenes.Where(xy => xy.SceneType == SceneType.Info).ToList();

            for (int i = 0; i < _hotspots.Count; i++)
            {
                var hotspot = _hotspots[i];
                foreach (var scene in _scenes)
                {
                    if (hotspot.RelativeVideoName.ToLowerInvariant() == scene.Name.ToLowerInvariant())
                    {
                        if (hotspot.ActionVideo.ToLowerInvariant().StartsWith("ip"))
                        {
                            scene.PausedHotspots.Add(hotspot);
                        }
                        else
                        {
                            scene.PlayingHotspots.Add(hotspot);
                        }

                    }
                }
            }

            Load_Scene_List(_scenes);
            Load_Main_Video();
            //Load_Info_Video();
            ImgStartMain.Visibility = Visibility.Collapsed;
            Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = null;
            _mainScenePlayer = new ScenePlayer(VideoView, _scenes);
            _mainScenePlayer.ActionOn += () =>
            {
                _actionTime = true;
                _clickTimer.Interval = TimeSpan.FromSeconds(0.05);
                ShowCursor();
            };
            _mainScenePlayer.ActionOff += () =>
            {
                _actionTime = false;
                _clickTimer.Interval = TimeSpan.FromSeconds(0.2);
                HideCursor();
            };
            _mainScenePlayer.QuitGame += async () =>
            {
                await ApplicationView.GetForCurrentView().TryConsolidateAsync();
            };
            _mainScenePlayer.InfoVideoTrigger += (start, end) =>
            {
                SceneDefinition InfoSceneToPlay = _infoScenes.Where(xy => xy.StartMS >= start && xy.EndMS <= end).FirstOrDefault();
                // todo write a way to find the scene by start and end.
                
                if (InfoSceneToPlay != null)
                    _supportingPlayer.QueueScene(InfoSceneToPlay, "info", 0);

            };
        }
        private void ShowCursor()
        {
            CurEmulator.Visibility = Visibility.Visible;
            var pointerPosition = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;

            var pos = Window.Current.CoreWindow.PointerPosition;
            CurEmulator.Margin = new Thickness(pos.X - Window.Current.Bounds.X, pos.Y - Window.Current.Bounds.Y + 1, 0, 0);
            _mcurVisible = true;
        }
        private void HideCursor()
        {
            CurEmulator.Visibility = Visibility.Collapsed;
           
            _mcurVisible = false;
        }

        private void Load_Main_Video()
        {
            string videopath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Main2_59_orig_aspect.mp4");
            using (var media = new Media(_libVLCMain, videopath, FromType.FromPath))
            {
                //media.AddOption("start-time=120.0");
                //media.AddOption("stop-time=180.0");
                _mediaPlayerMain.Play(media);
                _maxVideoMS = media.Duration;
                _MainVideoLoaded = true;
                this._mediaPlayerMain.Pause();
                //_mainVideoMedia = media;   // This media shouldn't ever be disposed.

                foreach (var track in media.Tracks)
                {
                    if (track.TrackType == TrackType.Video)
                    {
                        _OriginalMainVideoHeight = (int)media.Tracks[0].Data.Video.Height;
                        _OriginalMainVideoWidth = (int)media.Tracks[0].Data.Video.Width;
                    }
                }
            }
            
        }
        private void Load_Info_Video()
        {
            
            
        }
        
        private void Load_Scene_List(List<SceneDefinition> defs)
        {
            lstScene.Items.Clear();
            
            foreach (var def in defs)
            {
                if (def.SceneType == SceneType.Main || def.SceneType == SceneType.Inaction || def.SceneType == SceneType.Bad)
                    lstScene.Items.Add(new ComboBoxItem() { Content = def.Name });
            }
        }
        private void Load_Computer_list(List<SceneDefinition> defs)
        {
            lstComputer.Items.Clear();

            foreach (var def in defs)
            {
               lstComputer.Items.Add(new ComboBoxItem() { Content = def.Name });
            }
        }
        private void Log_Fired(object sender, LogEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine(e.FormattedLog);
        }
    }
}
