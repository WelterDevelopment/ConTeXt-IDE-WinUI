using Microsoft.UI.Xaml;
using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ConTeXt_IDE.Shared.Helpers
{
	public static class TaskbarUtility
	{
		private static ITaskbarList4 _taskbarList;

		static TaskbarUtility()
		{
			if (!IsSupported())
				throw new Exception("Taskbar functions not available");

			_taskbarList = (ITaskbarList4)new CTaskbarList();
			_taskbarList.HrInit();
		}

		private static bool IsSupported()
		{
			return Environment.OSVersion.Platform == PlatformID.Win32NT &&
							Environment.OSVersion.Version.CompareTo(new Version(6, 1)) >= 0;
		}

		public static void SetProgressState(TaskbarProgressBarStatus state)
		{
			Task.Run(() =>
			{
				_taskbarList.SetProgressState(App.MainWindow.hWnd, state);
			});
		}

		public static void SetProgressValue(int currentValue, int maximumValue)
		{
			Task.Run(() =>
			{
				_taskbarList.SetProgressValue(App.MainWindow.hWnd,
								Convert.ToUInt64(currentValue),
								Convert.ToUInt64(maximumValue));
			});
		}
	}
	internal enum HResult
	{
		Ok = 0x0000

		// Add more constants here, if necessary
	}

	public enum TaskbarProgressBarStatus
	{
		NoProgress = 0,
		Indeterminate = 0x1,
		Normal = 0x2,
		Error = 0x4,
		Paused = 0x8
	}

	internal enum ThumbButtonMask
	{
		Bitmap = 0x1,
		Icon = 0x2,
		Tooltip = 0x4,
		THB_FLAGS = 0x8
	}

	[Flags]
	internal enum ThumbButtonOptions
	{
		Enabled = 0x00000000,
		Disabled = 0x00000001,
		DismissOnClick = 0x00000002,
		NoBackground = 0x00000004,
		Hidden = 0x00000008,
		NonInteractive = 0x00000010
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	internal struct ThumbButton
	{
		///
		/// WPARAM value for a THUMBBUTTON being clicked.
		///
		internal const int Clicked = 0x1800;

		[MarshalAs(UnmanagedType.U4)]
		internal ThumbButtonMask Mask;
		internal uint Id;
		internal uint Bitmap;
		internal IntPtr Icon;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		internal string Tip;
		[MarshalAs(UnmanagedType.U4)]
		internal ThumbButtonOptions Flags;
	}

	internal enum SetTabPropertiesOption
	{
		None = 0x0,
		UseAppThumbnailAlways = 0x1,
		UseAppThumbnailWhenActive = 0x2,
		UseAppPeekAlways = 0x4,
		UseAppPeekWhenActive = 0x8
	}

	// using System.Runtime.InteropServices
	[ComImportAttribute()]
	[GuidAttribute("c43dc798-95d1-4bea-9030-bb99e2983a1a")]
	[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface ITaskbarList4
	{
		// ITaskbarList
		[PreserveSig]
		void HrInit();
		[PreserveSig]
		void AddTab(IntPtr hwnd);
		[PreserveSig]
		void DeleteTab(IntPtr hwnd);
		[PreserveSig]
		void ActivateTab(IntPtr hwnd);
		[PreserveSig]
		void SetActiveAlt(IntPtr hwnd);

		// ITaskbarList2
		[PreserveSig]
		void MarkFullscreenWindow(
						IntPtr hwnd,
						[MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

		// ITaskbarList3
		[PreserveSig]
		void SetProgressValue(IntPtr hwnd, UInt64 ullCompleted, UInt64 ullTotal);
		[PreserveSig]
		void SetProgressState(IntPtr hwnd, TaskbarProgressBarStatus tbpFlags);
		[PreserveSig]
		void RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);
		[PreserveSig]
		void UnregisterTab(IntPtr hwndTab);
		[PreserveSig]
		void SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
		[PreserveSig]
		void SetTabActive(IntPtr hwndTab, IntPtr hwndInsertBefore, uint dwReserved);
		[PreserveSig]
		HResult ThumbBarAddButtons(
						IntPtr hwnd,
						uint cButtons,
						[MarshalAs(UnmanagedType.LPArray)] ThumbButton[] pButtons);
		[PreserveSig]
		HResult ThumbBarUpdateButtons(
						IntPtr hwnd,
						uint cButtons,
						[MarshalAs(UnmanagedType.LPArray)] ThumbButton[] pButtons);
		[PreserveSig]
		void ThumbBarSetImageList(IntPtr hwnd, IntPtr himl);
		[PreserveSig]
		void SetOverlayIcon(
				IntPtr hwnd,
				IntPtr hIcon,
				[MarshalAs(UnmanagedType.LPWStr)] string pszDescription);
		[PreserveSig]
		void SetThumbnailTooltip(
						IntPtr hwnd,
						[MarshalAs(UnmanagedType.LPWStr)] string pszTip);
		[PreserveSig]
		void SetThumbnailClip(
						IntPtr hwnd,
						IntPtr prcClip);

		// ITaskbarList4
		void SetTabProperties(IntPtr hwndTab, SetTabPropertiesOption stpFlags);
	}

	[GuidAttribute("56FDF344-FD6D-11d0-958A-006097C9A090")]
	[ClassInterfaceAttribute(ClassInterfaceType.None)]
	[ComImportAttribute()]
	internal class CTaskbarList { }


	public static class WindowInfo
	{
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsIconic(ref IntPtr hWnd);

		public static bool IsMinimized(IntPtr handle)
		{
			return IsIconic(ref handle);
		}
	}

		public static class FlashWindow
	{
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

		[StructLayout(LayoutKind.Sequential)]
		private struct FLASHWINFO
		{
			/// <summary>
			/// The size of the structure in bytes.
			/// </summary>
			public uint cbSize;
			/// <summary>
			/// A Handle to the Window to be Flashed. The window can be either opened or minimized.
			/// </summary>
			public IntPtr hwnd;
			/// <summary>
			/// The Flash Status.
			/// </summary>
			public uint dwFlags;
			/// <summary>
			/// The number of times to Flash the window.
			/// </summary>
			public uint uCount;
			/// <summary>
			/// The rate at which the Window is to be flashed, in milliseconds. If Zero, the function uses the default cursor blink rate.
			/// </summary>
			public uint dwTimeout;
		}

		/// <summary>
		/// Stop flashing. The system restores the window to its original stae.
		/// </summary>
		public const uint FLASHW_STOP = 0;

		/// <summary>
		/// Flash the window caption.
		/// </summary>
		public const uint FLASHW_CAPTION = 1;

		/// <summary>
		/// Flash the taskbar button.
		/// </summary>
		public const uint FLASHW_TRAY = 2;

		/// <summary>
		/// Flash both the window caption and taskbar button.
		/// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags.
		/// </summary>
		public const uint FLASHW_ALL = 3;

		/// <summary>
		/// Flash continuously, until the FLASHW_STOP flag is set.
		/// </summary>
		public const uint FLASHW_TIMER = 4;

		/// <summary>
		/// Flash continuously until the window comes to the foreground.
		/// </summary>
		public const uint FLASHW_TIMERNOFG = 12;


		/// <summary>
		/// Flash the spacified Window (Form) until it recieves focus.
		/// </summary>
		/// <param name="form">The Form (Window) to Flash.</param>
		/// <returns></returns>
		public static bool Flash(IntPtr handle)
		{
			FLASHWINFO fi = Create_FLASHWINFO(handle, FLASHW_ALL | FLASHW_TIMERNOFG, uint.MaxValue, 0);
			return FlashWindowEx(ref fi);
		}

		private static FLASHWINFO Create_FLASHWINFO(IntPtr handle, uint flags, uint count, uint timeout)
		{
			FLASHWINFO fi = new FLASHWINFO();
			fi.cbSize = Convert.ToUInt32(Marshal.SizeOf(fi));
			fi.hwnd = handle;
			fi.dwFlags = flags;
			fi.uCount = count;
			fi.dwTimeout = timeout;
			return fi;
		}

		/// <summary>
		/// Flash the specified Window (form) for the specified number of times
		/// </summary>
		/// <param name="form">The Form (Window) to Flash.</param>
		/// <param name="count">The number of times to Flash.</param>
		/// <returns></returns>
		public static bool Flash(IntPtr handle, uint flags, uint count)
		{
			FLASHWINFO fi = Create_FLASHWINFO(handle, flags, count, 0);
			return FlashWindowEx(ref fi);
		}
	}
}