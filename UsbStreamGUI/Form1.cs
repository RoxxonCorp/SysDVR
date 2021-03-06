﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UsbStreamGUI
{
	public partial class Form1 : Form
	{
		StreamTarget CurTarget = StreamTarget.File;
		StreamKind CurKind = StreamKind.Audio;

		Dictionary<StreamTarget, IStreamTargetControl> StreamControls = new Dictionary<StreamTarget, IStreamTargetControl>
		{
		  { StreamTarget.Mpv , new MpvStreamControl() { Dock = DockStyle.Fill} },
		  { StreamTarget.File , new FileStreamControl() { Dock = DockStyle.Fill} },
		  { StreamTarget.Tcp , new TCPStreamControl(){ Dock = DockStyle.Fill} },
		};

		public Form1()
		{
			InitializeComponent();
		}

		void SetDefaultText() => this.Text = "UsbStreamGUI 1.0";

		private void Form1_Load(object sender, EventArgs e)
		{
			SetDefaultText();

			if (!File.Exists("UsbStream.exe"))
			{
				MessageBox.Show("UsbStream.exe not found, did you extract all the files in the same folder ?");
				this.Close();
			}

			if (Utils.FindExecutableInPath("dotnet.exe") == null)
				if (MessageBox.Show(".NET core 3.0 doesn't seem to be installed on this pc but it's needed for UsbStream, do you want to open the download page ?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
					System.Diagnostics.Process.Start("https://dotnet.microsoft.com/download");
			
			radioButton6.Checked = true;
			radioButton1.Checked = true;
		}

		private void StreamTargetSelected(object sender, EventArgs e)
		{
			if (!((RadioButton)sender).Checked) return;

			CurTarget = (StreamTarget)Enum.Parse(typeof(StreamTarget), ((RadioButton)sender).Tag as string);
			StreamConfigPanel.Controls.Clear();
			StreamConfigPanel.Controls.Add((Control)StreamControls[CurTarget]);
		}

		private void StreamKindSelected(object sender, EventArgs e)
		{
			if (!((RadioButton)sender).Checked) return;

			CurKind = (StreamKind)Enum.Parse(typeof(StreamKind), ((RadioButton)sender).Text);

			foreach (IStreamTargetControl c in StreamControls.Values)
				c.TargetKind = CurKind;
		}

		string GetExtraArgs() 
		{
			StringBuilder str = new StringBuilder();

			void append(string s) { if (str.Length != 0) str.Append(" "); str.Append(s); }

			if (cbStats.Checked) append("--print-stats");
			if (cbDesync.Checked) append("--desync-fix");
			if (cbUsbLog.Checked) append("--usb-debug");
			if (cbUsbWarn.Checked) append("--usb-warn");
			return str.ToString();
		}

		string GetFinalCommand() 
		{
			try
			{
				return $"UsbStream.exe {StreamControls[CurTarget].GetCommandLine()} {GetExtraArgs()}";
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error: " + ex.Message);
			}
			return null;
		}

		private void Launch(object sender, EventArgs e)
		{
			string cmd = GetFinalCommand();
			if (cmd != null)
			{
				System.Diagnostics.Process.Start("cmd", $"/K {cmd}");
				this.Close();
			}
		}

		private void ExportBatch(object sender, EventArgs e)
		{
			string cmd = GetFinalCommand();
			if (cmd == null) return;
			File.WriteAllText($"Launch_{CurTarget}_{CurKind}.bat", cmd);
			if (MessageBox.Show("Done, launch UsbStream now ?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
				Launch(sender,e);
		}

		private void BatchInfo(object sender, EventArgs e) =>
			MessageBox.Show("This will create a bat file to launch UsbStream with the selected options you will just need to double click it. The file name depends on the configuration, you can rename it later.\r\n");

		private void button4_Click(object sender, EventArgs e) => MessageBox.Show(
				"UsbStream requires .NET core 3.0 (note that it's not the same thing as .NET framework), in case you don't have it yet you can download it from microsoft's website: https://dotnet.microsoft.com/download\r\n\r\n" +
				"Make sure to properly setup the drivers following the github guide before attempting to stream\r\n" +
				"If UsbStream can't connect to SysDVR make sure it's running and that it's in usb mode (you can set that from the settings homebrew)\r\n\r\n" + 
				"If the stream is laggy open the home menu for a few seconds to let UsbStream flush the data\r\n\r\n" +
				"In case of issues check UsbStream output for errors and search in the github issues or on the gbatemp thread, chances are someone else already faced that issue.\r\n\r\n");

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => System.Diagnostics.Process.Start("https://github.com/exelix11/SysDVR/blob/master/readme.md#usage");
	}

	static class Utils 
	{
		public static string FindExecutableInPath(string fileName) =>
			Environment.GetEnvironmentVariable("PATH")
				.Split(Path.PathSeparator)
				.Select(x => Path.Combine(x, fileName))
				.FirstOrDefault(x => File.Exists(x));
	}
}
