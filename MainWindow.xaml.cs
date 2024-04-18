using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace TextSync
{
	public partial class MainWindow : Window
	{
		// These have to be getters/setters
		class DataModel : INotifyPropertyChanged
		{
			private string _data;
			public string Data
			{
				get => _data;
				set
				{
					_data = value;
					OnPropertyChanged("Data");
				}
			}

			private string _filename;
			public string Filename
			{
				get => _filename;
				set
				{
					_filename = value;
					OnPropertyChanged("Filename");
				}
			}

			// One way to source, no changes on our end
			public string TargetHWND { get; set; }
			public string ControlID { get; set; }

			//https://stackoverflow.com/questions/11274402/binding-in-textblock-doesnt-work-in-wpf
			public event PropertyChangedEventHandler PropertyChanged;
			public void OnPropertyChanged(string PropertyName)
			{
				if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
			}
		}

		DataModel dataModel = new DataModel() { Data = ":^)", Filename = "Select a file to monitor..." };
		public MainWindow()
		{
			InitializeComponent();
			DataContext = dataModel;
		}

		// Select a new file to monitor
		private void button_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog pickFile = new();
			pickFile.Multiselect = false;
			pickFile.RestoreDirectory = true;
			pickFile.Title = "Pick file to watch...";
			pickFile.CheckFileExists = true;
			pickFile.CheckPathExists = true;

			if (pickFile.ShowDialog() == true) {
				DisarmWatcher();
				dataModel.Filename = pickFile.FileName;
				ArmWatcher(dataModel.Filename);
			}
		}


		//private static System.IO.File syncFile;
		// Sets up watcher
		private FileSystemWatcher watcher = new();
		private void ArmWatcher(string filename)
		{
			// Trigger by ourselves
			updateSyncTextBox(filename);
			// https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=net-8.0
			watcher = new FileSystemWatcher();
			watcher.Path = Path.GetDirectoryName(filename);
			// The filter is very weird and just plain doesn't work 
			watcher.Filter = Path.GetFileName(filename);
			watcher.Changed += new FileSystemEventHandler(onChanged);
			watcher.Renamed += onChanged;
			watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
			watcher.EnableRaisingEvents = true; // why?
												//watcher.IncludeSubdirectories = true;

		}

		// Reset state for new file selected
		private void DisarmWatcher()
		{
			watcher.Dispose();
		}

		// SOLUTION: test for file availability
		private static bool IsFileReady(string filename)
		{
			// If the file can be opened for exclusive access it means that the file
			// is no longer locked by another process.
			try {
				using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
					return inputStream.Length >= 0;
				}
			}
			catch (IOException) {
				return false;
			}
		}

		// Watcher on changed
		private void onChanged(object sender, FileSystemEventArgs e)
		{
			if (e.FullPath != dataModel.Filename) return;
			// if anything happens to our file just reread that
			// fun fact this tryc
			try {
				updateSyncTextBox(dataModel.Filename);
			}
			catch (IOException ex) {
				System.Diagnostics.Debug.WriteLine("Error updating sync:");
				System.Diagnostics.Debug.WriteLine(e.ChangeType.ToString());
				System.Diagnostics.Debug.WriteLine(ex.Message);
				MessageBox.Show("Error, file in use...");
			}
		}

		// Sync data to local textarea and external textarea
		private void updateSyncTextBox(string path)
		{
			// WARNING: AWFUL HACK
			// we wait a couple seconds
			// till file becomes available again
			// https://stackoverflow.com/questions/6629219/net-construct-for-while-loop-with-timeout
			int timeout = 1000; // One second
			System.Threading.SpinWait.SpinUntil(() => IsFileReady(path), timeout);

			using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
				using (StreamReader reader = new StreamReader(stream)) {
					while (!reader.EndOfStream) {
						// Update data model
						// For some reason I can't access Text property directly
						// WPF is meh
						string data = reader.ReadToEnd();
						dataModel.Data = data;
						// Then update remote HWND
						UploadToTextControl();
					}
				}
			}
		}

		// Interpret main window HWND, usually a big hex number
		private IntPtr InterpretHandle()
		{
			IntPtr HWND;
			bool success = IntPtr.TryParse(dataModel.TargetHWND, NumberStyles.HexNumber,
				CultureInfo.CurrentCulture, out HWND);
			if (!success) {
				MessageBox.Show("Handle " + dataModel.TargetHWND + " invalid");
			}
			return HWND;
		}

		// Interpret ControlID, hex number. For notepad.exe it's always 0xF, usually constants
		private int InterpretControlID()
		{
			int.TryParse(dataModel.ControlID, NumberStyles.HexNumber,
				CultureInfo.CurrentCulture, out int ID); // Inline declaration?
			return ID;
		}

		[DllImport("user32.dll", SetLastError = false)]
		public static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, IntPtr wParam, string lParam);
		public const uint WM_SETTEXT = 0x000C;

		private void UploadToTextControl()
		{
			IntPtr windowHWND = InterpretHandle();
			int ControlID = InterpretControlID();

			IntPtr iptrHWndControl = GetDlgItem(windowHWND, ControlID);
			HandleRef hrefHWndTarget = new HandleRef(null, iptrHWndControl);
			SendMessage(hrefHWndTarget, WM_SETTEXT, IntPtr.Zero, dataModel.Data);
		}

		private void TestHWND(object sender, RoutedEventArgs e)
		{
			// TODO: https://stackoverflow.com/questions/20021256/enable-user-to-select-control-window-from-another-process
			// Do this next
			UploadToTextControl();
		}
	}
}
