using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using Color = System.Drawing.Color;
using Path = System.IO.Path;

namespace ImageCodec
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private const int ENCODED_DATA_LENGTH = 512 * 512 * 3;
		private const string ENCODED_IMAGE_NAME = "EncodedImg";
		private const string ENCODED_IMAGES_DIRECTORY_NAME = "EncodedImages";

		private List<byte> EndPattern = new List<byte>() { 35, 56, 75 };

		private string m_Massage = "";

		private string m_TempDirectory = Path.Combine(Path.GetTempPath(), ENCODED_IMAGES_DIRECTORY_NAME);

		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;
			Directory.CreateDirectory(m_TempDirectory);
		}
		public event PropertyChangedEventHandler PropertyChanged;

		public int MaxMessageLength
		{
			get { return 512 * 512; }
		}

		public string Message
		{
			get { return m_Massage; }
			set
			{
				m_Massage = value;
				OnPropertyChanged(nameof(Message));
			}
		}

		public string MessageLengthStatus
		{
			get { return (MaxMessageLength - Message.Length) + " / " + MaxMessageLength; }
		}

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

			if (propertyName.Equals(nameof(Message)))
			{
				OnPropertyChanged(nameof(MessageLengthStatus));
			}
		}
		private void ButtonClick_DecodeImage(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog
			{
				InitialDirectory = m_TempDirectory,
				Title = "Browse Image Files",

				CheckFileExists = true,
				CheckPathExists = true,

				RestoreDirectory = true,

				ReadOnlyChecked = true,
				ShowReadOnly = true
			};

			if (dlg.ShowDialog() == true)
			{
				var data = DecodeImage(dlg.FileName);
				ShowDecodedData(data);
			}
		}

		private void ButtonClick_EncodeMessage(object sender, RoutedEventArgs e)
		{
			var encodedBytes = EncodeData();
			var bmp = CreateBitmap(encodedBytes);
			SaveEncodedImage(bmp, ImageFormat.Png);

			Process.Start("explorer.exe", m_TempDirectory);

			Message = "";
		}

		private Bitmap CreateBitmap(List<byte> bytes)
		{
			int width = 512;
			int height = 512;
			int bpp = 24;

			Bitmap bmp = new Bitmap(width, height);

			for (int y = 0; y < width; y++)
			{
				for (int x = 0; x < height; x++)
				{
					int i = ((y * width) + x) * (bpp / 8);
					if (bpp == 24) // in this case you have 3 color values (red, green, blue)
					{
						// first byte will be red, because you are writing it as first value
						byte r = bytes[i];
						byte g = bytes[i + 1];
						byte b = bytes[i + 2];
						Color color = Color.FromArgb(r, g, b);
						bmp.SetPixel(y, x, color);
					}
				}
			}

			return bmp;
		}

		private List<byte> DecodeImage(string imagePath)
		{
			List<byte> decodedData = new List<byte>();

			Bitmap img = new Bitmap(imagePath);
			for (int x = 0; x < img.Width; x++)
			{
				for (int y = 0; y < img.Height; y++)
				{
					Color pixel = img.GetPixel(x, y);

					decodedData.Add(pixel.R);
					decodedData.Add(pixel.G);
					decodedData.Add(pixel.B);
				}
			}

			// decode the message
			for (int i = 0; i < decodedData.Count; i++)
				decodedData[i] = (byte)(decodedData[i] >> 1);

			int patternIndex = 0;
			bool foundPattern = false;
			for (patternIndex = 0; patternIndex < (decodedData.Count - EndPattern.Count); patternIndex++)
			{
				for (int i = 0; i < EndPattern.Count; i++)
				{
					if (decodedData[patternIndex + i] == EndPattern[i] && i == (EndPattern.Count - 1))
					{
						foundPattern = true;
						break;
					}
				}

				if (foundPattern)
					break;
			}

			if (!foundPattern)
				throw new InvalidOperationException("End pattern not foound");

			return decodedData.GetRange(0, patternIndex);
		}

		private List<byte> EncodeData()
		{
			var encodedData = new List<byte>();

			foreach (char c in Message)
				encodedData.Add((byte)c);

			encodedData.AddRange(EndPattern);

			for (int i = 0; i < encodedData.Count; i++)
				if (encodedData[i] > 128)
					throw new InvalidDataException();

			// encrypt data
			for (int i = 0; i < encodedData.Count; i++)
				encodedData[i] = (byte)(encodedData[i] << 1);

			// pad the data with random bytes
			Random rand = new Random();
			while (encodedData.Count < ENCODED_DATA_LENGTH)
				encodedData.Add((byte)rand.Next(33, 254));

			return encodedData;
		}

		private void SaveEncodedImage(Bitmap bmp, ImageFormat fmt)
		{
			string filePath = Path.Combine(m_TempDirectory, ENCODED_IMAGE_NAME);

			int i = 0;
			while (true)
			{
				string t = filePath + "_" + i.ToString().PadLeft(3, '0') + "." + fmt.ToString().ToLower();
				if (!File.Exists(t))
				{
					filePath = t;
					break;
				}

				i++;
			}

			bmp.Save(filePath, fmt);
		}

		private void ShowDecodedData(List<byte> data)
		{
			string msg = "";

			foreach (byte b in data)
				msg += (char)b;

			Message = msg;
		}
	}
}