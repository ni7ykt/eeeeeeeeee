using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using NAudio.Wave;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace 進階炸麥;

public class Form1 : Form
{
	private readonly object _audioLock = new object();

	private WaveInEvent _waveIn;

	private WaveOutEvent _waveOut;

	private BufferedWaveProvider _waveProvider;

	private readonly List<int> _deviceIndices = new List<int>();

	private readonly Random _random = new Random();

	private readonly YoutubeClient _ytClient = new YoutubeClient();

	private MediaFoundationReader _ytAudioReader;

	private string _cachedStreamUrl = null;

	private float _explosionGain = 1f;

	private float _noiseLevel = 0f;

	private float _musicExplosionMultiplier = 1f;

	private IContainer components = null;

	private Guna2TextBox guna2TextBox2;

	private Label label1;

	private Label label2;

	private Guna2TrackBar guna2TrackBar1;

	private Guna2Button guna2Button1;

	private Guna2ComboBox guna2ComboBox1;

	private Guna2TrackBar guna2TrackBar2;

	private Guna2ToggleSwitch guna2ToggleSwitch1;

	private Label label3;

	private Label label4;

	private Guna2TrackBar guna2TrackBar3;

	private Label label5;

	private Label label6;

	private Guna2ToggleSwitch guna2ToggleSwitch2;

	private Label label7;

	private Label label8;

	private Guna2ToggleSwitch guna2ToggleSwitch3;

	private Label label9;

	private Label label10;

	private Guna2ToggleSwitch guna2ToggleSwitch4;

	private Guna2ToggleSwitch guna2ToggleSwitch5;

	private Label label11;

	public Form1()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		InitializeComponent();
		((Form)this).Load += Form1_Load;
	}

	private void Form1_Load(object sender, EventArgs e)
	{
		RefreshMicrophoneList();
		UpdateExplosionGain();
		UpdateNoiseGain();
		UpdateMusicExplosionGain();
	}

	private async void guna2Button1_Click(object sender, EventArgs e)
	{
		string url = ((Control)guna2TextBox2).Text.Trim();
		if (string.IsNullOrEmpty(url))
		{
			return;
		}
		try
		{
			((Control)guna2Button1).Enabled = false;
			((Control)label11).Text = "連線YT中...";
			Video video = await _ytClient.Videos.GetAsync(VideoId.op_Implicit(url), default(CancellationToken));
			IStreamInfo streamInfo = StreamInfoExtensions.GetWithHighestBitrate((IEnumerable<IStreamInfo>)(await _ytClient.Videos.Streams.GetManifestAsync(VideoId.op_Implicit(url), default(CancellationToken))).GetAudioOnlyStreams());
			_cachedStreamUrl = streamInfo.Url;
			((Control)label11).Text = video.Title;
			if (guna2ToggleSwitch3.Checked)
			{
				RestartAudio();
			}
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			((Control)label11).Text = "連接失敗";
			MessageBox.Show("連結解析錯誤: " + ex.Message, "錯誤", (MessageBoxButtons)0, (MessageBoxIcon)16);
		}
		finally
		{
			if (!((Control)this).IsDisposed)
			{
				((Control)guna2Button1).Enabled = true;
			}
		}
	}

	private void SyncSwitches()
	{
		if (guna2ToggleSwitch1.Checked || guna2ToggleSwitch2.Checked || guna2ToggleSwitch3.Checked)
		{
			StartAudio();
		}
		else
		{
			StopAudio();
		}
	}

	private void UpdateMusicExplosionGain()
	{
		_musicExplosionMultiplier = (guna2ToggleSwitch5.Checked ? ((float)guna2TrackBar1.Value) : 1f);
	}

	private void guna2ToggleSwitch1_CheckedChanged(object sender, EventArgs e)
	{
		SyncSwitches();
	}

	private void guna2ToggleSwitch2_CheckedChanged(object sender, EventArgs e)
	{
		SyncSwitches();
	}

	private void guna2ToggleSwitch3_CheckedChanged(object sender, EventArgs e)
	{
		SyncSwitches();
	}

	private void guna2ToggleSwitch5_CheckedChanged(object sender, EventArgs e)
	{
		UpdateMusicExplosionGain();
	}

	private void guna2TrackBar1_Scroll(object sender, ScrollEventArgs e)
	{
		UpdateMusicExplosionGain();
	}

	private void guna2TrackBar2_Scroll(object sender, ScrollEventArgs e)
	{
		UpdateNoiseGain();
	}

	private void guna2TrackBar3_Scroll(object sender, ScrollEventArgs e)
	{
		UpdateExplosionGain();
	}

	private void StartAudio()
	{
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Expected O, but got Unknown
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Expected O, but got Unknown
		lock (_audioLock)
		{
			if (((ListControl)guna2ComboBox1).SelectedIndex == -1)
			{
				return;
			}
			StopAudio();
			try
			{
				int deviceNumber = _deviceIndices[((ListControl)guna2ComboBox1).SelectedIndex];
				WaveFormat val = new WaveFormat(44100, 1);
				_waveIn = new WaveInEvent
				{
					DeviceNumber = deviceNumber,
					WaveFormat = val,
					BufferMilliseconds = 50
				};
				_waveProvider = new BufferedWaveProvider(val)
				{
					DiscardOnBufferOverflow = true
				};
				_waveOut = new WaveOutEvent
				{
					DeviceNumber = FindVirtualCable()
				};
				_waveOut.Init((IWaveProvider)(object)_waveProvider);
				_waveOut.Play();
				if (guna2ToggleSwitch3.Checked && !string.IsNullOrEmpty(_cachedStreamUrl))
				{
					_ytAudioReader = new MediaFoundationReader(_cachedStreamUrl);
				}
				_waveIn.DataAvailable += delegate(object s, WaveInEventArgs args)
				{
					lock (_audioLock)
					{
						if (_waveProvider != null)
						{
							byte[] buffer = args.Buffer;
							for (int i = 0; i < args.BytesRecorded; i += 2)
							{
								short num = BitConverter.ToInt16(buffer, i);
								float num2 = num;
								if (guna2ToggleSwitch2.Checked)
								{
									num2 *= _explosionGain * 900000f;
								}
								if (guna2ToggleSwitch1.Checked)
								{
									num2 += (float)(_random.NextDouble() * 2.0 - 1.0) * _noiseLevel * 1500f;
								}
								if (guna2ToggleSwitch3.Checked && _ytAudioReader != null)
								{
									byte[] array = new byte[2];
									try
									{
										if (((Stream)(object)_ytAudioReader).Read(array, 0, 2) > 0)
										{
											float num3 = BitConverter.ToInt16(array, 0);
											num2 += num3 * _musicExplosionMultiplier;
										}
									}
									catch
									{
									}
								}
								num2 = Math.Max(-32768f, Math.Min(32767f, num2));
								byte[] bytes = BitConverter.GetBytes((short)num2);
								buffer[i] = bytes[0];
								buffer[i + 1] = bytes[1];
							}
							_waveProvider.AddSamples(buffer, 0, args.BytesRecorded);
						}
					}
				};
				_waveIn.StartRecording();
			}
			catch (Exception ex)
			{
				MessageBox.Show("啟動失敗: " + ex.Message);
			}
		}
	}

	private void StopAudio()
	{
		lock (_audioLock)
		{
			try
			{
				WaveInEvent waveIn = _waveIn;
				if (waveIn != null)
				{
					waveIn.StopRecording();
				}
				WaveInEvent waveIn2 = _waveIn;
				if (waveIn2 != null)
				{
					waveIn2.Dispose();
				}
				_waveIn = null;
				WaveOutEvent waveOut = _waveOut;
				if (waveOut != null)
				{
					waveOut.Stop();
				}
				WaveOutEvent waveOut2 = _waveOut;
				if (waveOut2 != null)
				{
					waveOut2.Dispose();
				}
				_waveOut = null;
				((Stream)(object)_ytAudioReader)?.Dispose();
				_ytAudioReader = null;
				_waveProvider = null;
			}
			catch
			{
			}
		}
	}

	private void UpdateExplosionGain()
	{
		_explosionGain = (float)Math.Pow(guna2TrackBar3.Value, 2.0);
	}

	private void UpdateNoiseGain()
	{
		_noiseLevel = (float)Math.Pow(guna2TrackBar2.Value, 1.5) / 50f;
	}

	private int FindVirtualCable()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < WaveOut.DeviceCount; i++)
		{
			WaveOutCapabilities capabilities = WaveOut.GetCapabilities(i);
			string text = ((WaveOutCapabilities)(ref capabilities)).ProductName.ToUpper();
			if (text.Contains("CABLE") || text.Contains("VIRTUAL"))
			{
				return i;
			}
		}
		return 0;
	}

	private void RefreshMicrophoneList()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		((ComboBox)guna2ComboBox1).Items.Clear();
		_deviceIndices.Clear();
		for (int i = 0; i < WaveIn.DeviceCount; i++)
		{
			ObjectCollection items = ((ComboBox)guna2ComboBox1).Items;
			WaveInCapabilities capabilities = WaveIn.GetCapabilities(i);
			items.Add((object)((WaveInCapabilities)(ref capabilities)).ProductName);
			_deviceIndices.Add(i);
		}
		if (((ComboBox)guna2ComboBox1).Items.Count > 0)
		{
			((ListControl)guna2ComboBox1).SelectedIndex = 0;
		}
	}

	private void RestartAudio()
	{
		StopAudio();
		StartAudio();
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		StopAudio();
		((Form)this).OnFormClosing(e);
	}

	private void guna2ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
	{
		RestartAudio();
	}

	private void label11_Click(object sender, EventArgs e)
	{
	}

	private void guna2TextBox2_TextChanged(object sender, EventArgs e)
	{
	}

	private void guna2ToggleSwitch4_CheckedChanged(object sender, EventArgs e)
	{
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		((Form)this).Dispose(disposing);
	}

	private void InitializeComponent()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Expected O, but got Unknown
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected O, but got Unknown
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected O, but got Unknown
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Expected O, but got Unknown
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Expected O, but got Unknown
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Expected O, but got Unknown
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Expected O, but got Unknown
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Expected O, but got Unknown
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Expected O, but got Unknown
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Expected O, but got Unknown
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Expected O, but got Unknown
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Expected O, but got Unknown
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_0371: Expected O, but got Unknown
		//IL_0457: Unknown result type (might be due to invalid IL or missing references)
		//IL_0461: Expected O, but got Unknown
		//IL_0502: Unknown result type (might be due to invalid IL or missing references)
		//IL_050c: Expected O, but got Unknown
		//IL_060c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0616: Expected O, but got Unknown
		//IL_0719: Unknown result type (might be due to invalid IL or missing references)
		//IL_0723: Expected O, but got Unknown
		//IL_088f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0899: Expected O, but got Unknown
		//IL_092c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0936: Expected O, but got Unknown
		//IL_0a1e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a28: Expected O, but got Unknown
		//IL_0a4d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a57: Expected O, but got Unknown
		//IL_0aea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0af4: Expected O, but got Unknown
		//IL_0cce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cd8: Expected O, but got Unknown
		//IL_0d6e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d78: Expected O, but got Unknown
		//IL_0f55: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f5f: Expected O, but got Unknown
		//IL_0ff5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fff: Expected O, but got Unknown
		//IL_1323: Unknown result type (might be due to invalid IL or missing references)
		//IL_132d: Expected O, but got Unknown
		//IL_13ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_13f4: Expected O, but got Unknown
		ComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof(Form1));
		guna2TextBox2 = new Guna2TextBox();
		label1 = new Label();
		label2 = new Label();
		guna2TrackBar1 = new Guna2TrackBar();
		guna2Button1 = new Guna2Button();
		guna2ComboBox1 = new Guna2ComboBox();
		guna2TrackBar2 = new Guna2TrackBar();
		guna2ToggleSwitch1 = new Guna2ToggleSwitch();
		label3 = new Label();
		label4 = new Label();
		guna2TrackBar3 = new Guna2TrackBar();
		label5 = new Label();
		label6 = new Label();
		guna2ToggleSwitch2 = new Guna2ToggleSwitch();
		label7 = new Label();
		label8 = new Label();
		guna2ToggleSwitch3 = new Guna2ToggleSwitch();
		label9 = new Label();
		label10 = new Label();
		guna2ToggleSwitch4 = new Guna2ToggleSwitch();
		guna2ToggleSwitch5 = new Guna2ToggleSwitch();
		label11 = new Label();
		((Control)this).SuspendLayout();
		((Control)guna2TextBox2).Cursor = Cursors.IBeam;
		guna2TextBox2.DefaultText = "";
		guna2TextBox2.DisabledState.BorderColor = Color.FromArgb(208, 208, 208);
		guna2TextBox2.DisabledState.FillColor = Color.FromArgb(226, 226, 226);
		guna2TextBox2.DisabledState.ForeColor = Color.FromArgb(138, 138, 138);
		guna2TextBox2.DisabledState.PlaceholderForeColor = Color.FromArgb(138, 138, 138);
		guna2TextBox2.FocusedState.BorderColor = Color.FromArgb(94, 148, 255);
		((Control)guna2TextBox2).Font = new Font("Segoe UI", 9f);
		guna2TextBox2.HoverState.BorderColor = Color.FromArgb(94, 148, 255);
		((Control)guna2TextBox2).Location = new Point(422, 66);
		((Control)guna2TextBox2).Name = "guna2TextBox2";
		guna2TextBox2.PlaceholderText = "";
		guna2TextBox2.SelectedText = "";
		((Control)guna2TextBox2).Size = new Size(200, 36);
		((Control)guna2TextBox2).TabIndex = 1;
		guna2TextBox2.TextChanged += guna2TextBox2_TextChanged;
		((Control)label1).AutoSize = true;
		((Control)label1).Font = new Font("新細明體", 20.25f, (FontStyle)1, (GraphicsUnit)3, (byte)136);
		((Control)label1).ForeColor = SystemColors.ButtonHighlight;
		((Control)label1).Location = new Point(312, 69);
		((Control)label1).Name = "label1";
		((Control)label1).Size = new Size(104, 27);
		((Control)label1).TabIndex = 3;
		((Control)label1).Text = "YT連結";
		((Control)label2).AutoSize = true;
		((Control)label2).Font = new Font("新細明體", 21.75f, (FontStyle)1, (GraphicsUnit)3, (byte)136);
		((Control)label2).ForeColor = SystemColors.ButtonHighlight;
		((Control)label2).Location = new Point(21, 68);
		((Control)label2).Name = "label2";
		((Control)label2).Size = new Size(73, 29);
		((Control)label2).TabIndex = 4;
		((Control)label2).Text = "設備";
		((Control)guna2TrackBar1).Location = new Point(367, 340);
		((Control)guna2TrackBar1).Name = "guna2TrackBar1";
		((Control)guna2TrackBar1).Size = new Size(300, 23);
		((Control)guna2TrackBar1).TabIndex = 5;
		guna2TrackBar1.ThumbColor = Color.FromArgb(160, 113, 255);
		guna2TrackBar1.Scroll += new ScrollEventHandler(guna2TrackBar1_Scroll);
		guna2Button1.BorderRadius = 3;
		guna2Button1.BorderThickness = 2;
		guna2Button1.DisabledState.BorderColor = Color.DarkGray;
		guna2Button1.DisabledState.CustomBorderColor = Color.DarkGray;
		guna2Button1.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
		guna2Button1.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
		((Control)guna2Button1).Font = new Font("Segoe UI", 9f);
		((Control)guna2Button1).ForeColor = Color.White;
		((Control)guna2Button1).Location = new Point(626, 66);
		((Control)guna2Button1).Name = "guna2Button1";
		((Control)guna2Button1).Size = new Size(61, 36);
		((Control)guna2Button1).TabIndex = 7;
		((Control)guna2Button1).Text = "搜尋";
		((Control)guna2Button1).Click += guna2Button1_Click;
		((Control)guna2ComboBox1).BackColor = Color.Transparent;
		guna2ComboBox1.DrawMode = (DrawMode)1;
		guna2ComboBox1.DropDownStyle = (ComboBoxStyle)2;
		guna2ComboBox1.FocusedColor = Color.FromArgb(94, 148, 255);
		guna2ComboBox1.FocusedState.BorderColor = Color.FromArgb(94, 148, 255);
		((Control)guna2ComboBox1).Font = new Font("Segoe UI", 10f);
		((Control)guna2ComboBox1).ForeColor = Color.FromArgb(68, 88, 112);
		((ComboBox)guna2ComboBox1).ItemHeight = 30;
		((Control)guna2ComboBox1).Location = new Point(98, 64);
		((Control)guna2ComboBox1).Name = "guna2ComboBox1";
		((Control)guna2ComboBox1).Size = new Size(196, 36);
		((Control)guna2ComboBox1).TabIndex = 9;
		((ComboBox)guna2ComboBox1).SelectedIndexChanged += guna2ComboBox1_SelectedIndexChanged;
		((Control)guna2TrackBar2).Location = new Point(26, 340);
		((Control)guna2TrackBar2).Name = "guna2TrackBar2";
		((Control)guna2TrackBar2).Size = new Size(300, 23);
		((Control)guna2TrackBar2).TabIndex = 10;
		guna2TrackBar2.ThumbColor = Color.FromArgb(160, 113, 255);
		guna2TrackBar2.Scroll += new ScrollEventHandler(guna2TrackBar2_Scroll);
		guna2ToggleSwitch1.CheckedState.BorderColor = Color.FromArgb(94, 148, 255);
		guna2ToggleSwitch1.CheckedState.FillColor = Color.FromArgb(94, 148, 255);
		guna2ToggleSwitch1.CheckedState.InnerBorderColor = Color.White;
		guna2ToggleSwitch1.CheckedState.InnerColor = Color.White;
		((Control)guna2ToggleSwitch1).Location = new Point(151, 271);
		((Control)guna2ToggleSwitch1).Name = "guna2ToggleSwitch1";
		((Control)guna2ToggleSwitch1).Size = new Size(41, 27);
		((Control)guna2ToggleSwitch1).TabIndex = 11;
		guna2ToggleSwitch1.UncheckedState.BorderColor = Color.FromArgb(125, 137, 149);
		guna2ToggleSwitch1.UncheckedState.FillColor = Color.FromArgb(125, 137, 149);
		guna2ToggleSwitch1.UncheckedState.InnerBorderColor = Color.White;
		guna2ToggleSwitch1.UncheckedState.InnerColor = Color.White;
		guna2ToggleSwitch1.CheckedChanged += guna2ToggleSwitch1_CheckedChanged;
		((Control)label3).AutoSize = true;
		((Control)label3).Font = new Font("新細明體", 20.25f, (FontStyle)1, (GraphicsUnit)3, (byte)136);
		((Control)label3).ForeColor = SystemColors.ButtonHighlight;
		((Control)label3).Location = new Point(21, 310);
		((Control)label3).Name = "label3";
		((Control)label3).Size = new Size(124, 27);
		((Control)label3).TabIndex = 12;
		((Control)label3).Text = "雜訊調整";
		((Control)label4).AutoSize = true;
		((Control)label4).Font = new Font("新細明體", 20.25f, (FontStyle)1, (GraphicsUnit)3, (byte)136);
		((Control)label4).ForeColor = SystemColors.ButtonHighlight;
		((Control)label4).Location = new Point(21, 271);
		((Control)label4).Name = "label4";
		((Control)label4).Size = new Size(124, 27);
		((Control)label4).TabIndex = 13;
		((Control)label4).Text = "雜訊開關";
		((Control)guna2TrackBar3).Location = new Point(26, 233);
		((Control)guna2TrackBar3).Name = "guna2TrackBar3";
		((Control)guna2TrackBar3).Size = new Size(300, 23);
		((Control)guna2TrackBar3).TabIndex = 14;
		guna2TrackBar3.ThumbColor = Color.FromArgb(160, 113, 255);
		guna2TrackBar3.Scroll += new ScrollEventHandler(guna2TrackBar3_Scroll);
		((Control)label5).AutoSize = true;
		((Control)label5).Font = new Font("新細明體", 20.25f, (FontStyle)1, (GraphicsUnit)3, (byte)136);
		((Control)label5).ForeColor = SystemColors.ButtonHighlight;
		((Control)label5).Location = new Point(21, 203);
		((Control)label5).Name = "label5";
		((Control)label5).Size = new Size(124, 27);
		((Control)label5).TabIndex = 15;
		((Control)label5).Text = "爆麥調整";
		((Control)label6).AutoSize = true;
		((Control)label6).Font = new Font("新細明體", 20.25f, (FontStyle)1, (GraphicsUnit)3, (byte)136);
		((Control)label6).ForeColor = SystemColors.ButtonHighlight;
		((Control)label6).Location = new Point(21, 164);
		((Control)label6).Name = "label6";
		((Control)label6).Size = new Size(124, 27);
		((Control)label6).TabIndex = 16;
		((Control)label6).Text = "爆麥開關";
		guna2ToggleSwitch2.CheckedState.BorderColor = Color.FromArgb(94, 148, 255);
		guna2ToggleSwitch2.CheckedState.FillColor = Color.FromArgb(94, 148, 255);
		guna2ToggleSwitch2.CheckedState.InnerBorderColor = Color.White;
		guna2ToggleSwitch2.CheckedState.InnerColor = Color.White;
		((Control)guna2ToggleSwitch2).Location = new Point(151, 164);
		((Control)guna2ToggleSwitch2).Name = "guna2ToggleSwitch2";
		((Control)guna2ToggleSwitch2).Size = new Size(41, 27);
		((Control)guna2ToggleSwitch2).TabIndex = 17;
		guna2ToggleSwitch2.UncheckedState.BorderColor = Color.FromArgb(125, 137, 149);
		guna2ToggleSwitch2.UncheckedState.FillColor = Color.FromArgb(125, 137, 149);
		guna2ToggleSwitch2.UncheckedState.InnerBorderColor = Color.White;
		guna2ToggleSwitch2.UncheckedState.InnerColor = Color.White;
		guna2ToggleSwitch2.CheckedChanged += guna2ToggleSwitch2_CheckedChanged;
		((Control)label7).AutoSize = true;
		((Control)label7).Font = new Font("新細明體", 20.25f, (FontStyle)1, (GraphicsUnit)3, (byte)136);
		((Control)label7).ForeColor = SystemColors.ButtonHighlight;
		((Control)label7).Location = new Point(362, 310);
		((Control)label7).Name = "label7";
		((Control)label7).Size = new Size(124, 27);
		((Control)label7).TabIndex = 18;
		((Control)label7).Text = "音樂調整";
		((Control)label8).AutoSize = true;
		((Control)label8).Font = new Font("新細明體", 20.25f, (FontStyle)1, (GraphicsUnit)3, (byte)136);
		((Control)label8).ForeColor = SystemColors.ButtonHighlight;
		((Control)label8).Location = new Point(362, 271);
		((Control)label8).Name = "label8";
		((Control)label8).Size = new Size(124, 27);
		((Control)label8).TabIndex = 19;
		((Control)label8).Text = "播放開關";
		guna2ToggleSwitch3.CheckedState.BorderColor = Color.FromArgb(94, 148, 255);
		guna2ToggleSwitch3.CheckedState.FillColor = Color.FromArgb(94, 148, 255);
		guna2ToggleSwitch3.CheckedState.InnerBorderColor = Color.White;
		guna2ToggleSwitch3.CheckedState.InnerColor = Color.White;
		((Control)guna2ToggleSwitch3).Location = new Point(492, 271);
		((Control)guna2ToggleSwitch3).Name = "guna2ToggleSwitch3";
		((Control)guna2ToggleSwitch3).Size = new Size(41, 27);
		((Control)guna2ToggleSwitch3).TabIndex = 20;
		guna2ToggleSwitch3.UncheckedState.BorderColor = Color.FromArgb(125, 137, 149);
		guna2ToggleSwitch3.UncheckedState.FillColor = Color.FromArgb(125, 137, 149);
		guna2ToggleSwitch3.UncheckedState.InnerBorderColor = Color.White;
		guna2ToggleSwitch3.UncheckedState.InnerColor = Color.White;
		guna2ToggleSwitch3.CheckedChanged += guna2ToggleSwitch3_CheckedChanged;
		((Control)label9).AutoSize = true;
		((Control)label9).Font = new Font("新細明體", 20.25f, (FontStyle)1, (GraphicsUnit)3, (byte)136);
		((Control)label9).ForeColor = SystemColors.ButtonHighlight;
		((Control)label9).Location = new Point(543, 310);
		((Control)label9).Name = "label9";
		((Control)label9).Size = new Size(68, 27);
		((Control)label9).TabIndex = 21;
		((Control)label9).Text = "回音";
		((Control)label10).AutoSize = true;
		((Control)label10).Font = new Font("新細明體", 20.25f, (FontStyle)1, (GraphicsUnit)3, (byte)136);
		((Control)label10).ForeColor = SystemColors.ButtonHighlight;
		((Control)label10).Location = new Point(362, 233);
		((Control)label10).Name = "label10";
		((Control)label10).Size = new Size(124, 27);
		((Control)label10).TabIndex = 22;
		((Control)label10).Text = "八環奏樂";
		guna2ToggleSwitch4.CheckedState.BorderColor = Color.FromArgb(94, 148, 255);
		guna2ToggleSwitch4.CheckedState.FillColor = Color.FromArgb(94, 148, 255);
		guna2ToggleSwitch4.CheckedState.InnerBorderColor = Color.White;
		guna2ToggleSwitch4.CheckedState.InnerColor = Color.White;
		((Control)guna2ToggleSwitch4).Location = new Point(608, 310);
		((Control)guna2ToggleSwitch4).Name = "guna2ToggleSwitch4";
		((Control)guna2ToggleSwitch4).Size = new Size(41, 27);
		((Control)guna2ToggleSwitch4).TabIndex = 23;
		guna2ToggleSwitch4.UncheckedState.BorderColor = Color.FromArgb(125, 137, 149);
		guna2ToggleSwitch4.UncheckedState.FillColor = Color.FromArgb(125, 137, 149);
		guna2ToggleSwitch4.UncheckedState.InnerBorderColor = Color.White;
		guna2ToggleSwitch4.UncheckedState.InnerColor = Color.White;
		guna2ToggleSwitch4.CheckedChanged += guna2ToggleSwitch4_CheckedChanged;
		guna2ToggleSwitch5.CheckedState.BorderColor = Color.FromArgb(94, 148, 255);
		guna2ToggleSwitch5.CheckedState.FillColor = Color.FromArgb(94, 148, 255);
		guna2ToggleSwitch5.CheckedState.InnerBorderColor = Color.White;
		guna2ToggleSwitch5.CheckedState.InnerColor = Color.White;
		((Control)guna2ToggleSwitch5).Location = new Point(492, 233);
		((Control)guna2ToggleSwitch5).Name = "guna2ToggleSwitch5";
		((Control)guna2ToggleSwitch5).Size = new Size(41, 27);
		((Control)guna2ToggleSwitch5).TabIndex = 24;
		guna2ToggleSwitch5.UncheckedState.BorderColor = Color.FromArgb(125, 137, 149);
		guna2ToggleSwitch5.UncheckedState.FillColor = Color.FromArgb(125, 137, 149);
		guna2ToggleSwitch5.UncheckedState.InnerBorderColor = Color.White;
		guna2ToggleSwitch5.UncheckedState.InnerColor = Color.White;
		guna2ToggleSwitch5.CheckedChanged += guna2ToggleSwitch5_CheckedChanged;
		((Control)label11).AutoSize = true;
		((Control)label11).Font = new Font("新細明體", 18f, (FontStyle)1, (GraphicsUnit)3, (byte)136);
		((Control)label11).ForeColor = SystemColors.ButtonHighlight;
		((Control)label11).Location = new Point(313, 114);
		((Control)label11).Name = "label11";
		((Control)label11).Size = new Size(110, 24);
		((Control)label11).TabIndex = 25;
		((Control)label11).Text = "尚未連接";
		((Control)label11).Click += label11_Click;
		((ContainerControl)this).AutoScaleDimensions = new SizeF(6f, 12f);
		((ContainerControl)this).AutoScaleMode = (AutoScaleMode)1;
		((Control)this).BackColor = SystemColors.ActiveCaptionText;
		((Control)this).BackgroundImage = (Image)componentResourceManager.GetObject("$this.BackgroundImage");
		((Form)this).ClientSize = new Size(699, 439);
		((Control)this).Controls.Add((Control)(object)label11);
		((Control)this).Controls.Add((Control)(object)guna2ToggleSwitch5);
		((Control)this).Controls.Add((Control)(object)guna2ToggleSwitch4);
		((Control)this).Controls.Add((Control)(object)label10);
		((Control)this).Controls.Add((Control)(object)label9);
		((Control)this).Controls.Add((Control)(object)guna2ToggleSwitch3);
		((Control)this).Controls.Add((Control)(object)label8);
		((Control)this).Controls.Add((Control)(object)label7);
		((Control)this).Controls.Add((Control)(object)guna2ToggleSwitch2);
		((Control)this).Controls.Add((Control)(object)label6);
		((Control)this).Controls.Add((Control)(object)label5);
		((Control)this).Controls.Add((Control)(object)guna2TrackBar3);
		((Control)this).Controls.Add((Control)(object)label4);
		((Control)this).Controls.Add((Control)(object)label3);
		((Control)this).Controls.Add((Control)(object)guna2ToggleSwitch1);
		((Control)this).Controls.Add((Control)(object)guna2TrackBar2);
		((Control)this).Controls.Add((Control)(object)guna2ComboBox1);
		((Control)this).Controls.Add((Control)(object)guna2Button1);
		((Control)this).Controls.Add((Control)(object)guna2TrackBar1);
		((Control)this).Controls.Add((Control)(object)label2);
		((Control)this).Controls.Add((Control)(object)label1);
		((Control)this).Controls.Add((Control)(object)guna2TextBox2);
		((Form)this).FormBorderStyle = (FormBorderStyle)3;
		((Control)this).Name = "Form1";
		((Control)this).Text = "Form1";
		((Control)this).ResumeLayout(false);
		((Control)this).PerformLayout();
	}
}
