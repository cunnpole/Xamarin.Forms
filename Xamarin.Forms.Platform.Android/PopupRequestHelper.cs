using System;
using System.Linq;
using Android.App;
using Android.Content;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms.Platform.Android
{
	internal sealed class PopupRequestHelper : IDisposable
	{
		readonly Context _context;
		int _busyCount;

		public PopupRequestHelper(Context context)
		{
			_context = context;
			MessagingCenter.Subscribe<Page, bool>(_context, Page.BusySetSignalName, OnPageBusy);
			MessagingCenter.Subscribe<Page, AlertArguments>(_context, Page.AlertSignalName, OnAlertRequested);
			MessagingCenter.Subscribe<Page, ActionSheetArguments>(_context, Page.ActionSheetSignalName, OnActionSheetRequested);
		}

		public void Dispose()
		{
			MessagingCenter.Unsubscribe<Page, AlertArguments>(_context, Page.AlertSignalName);
			MessagingCenter.Unsubscribe<Page, bool>(_context, Page.BusySetSignalName);
			MessagingCenter.Unsubscribe<Page, ActionSheetArguments>(_context, Page.ActionSheetSignalName);
		}

		public void ResetBusyCount()
		{
			_busyCount = 0;
		}

		void OnPageBusy(Page sender, bool enabled)
		{
			_busyCount = Math.Max(0, enabled ? _busyCount + 1 : _busyCount - 1);

			UpdateProgressBarVisibility(_busyCount > 0);
		}

		void OnActionSheetRequested(Page sender, ActionSheetArguments arguments)
		{
			var builder = new AlertDialog.Builder(_context);
			builder.SetTitle(arguments.Title);
			string[] items = arguments.Buttons.ToArray();
			builder.SetItems(items, (o, args) => arguments.Result.TrySetResult(items[args.Which]));

			if (arguments.Cancel != null)
				builder.SetPositiveButton(arguments.Cancel, (o, args) => arguments.Result.TrySetResult(arguments.Cancel));

			if (arguments.Destruction != null)
				builder.SetNegativeButton(arguments.Destruction, (o, args) => arguments.Result.TrySetResult(arguments.Destruction));

			AlertDialog dialog = builder.Create();
			builder.Dispose();
			//to match current functionality of renderer we set cancelable on outside
			//and return null
			dialog.SetCanceledOnTouchOutside(true);
			dialog.CancelEvent += (o, e) => arguments.SetResult(null);
			dialog.Show();
		}

		void OnAlertRequested(Page sender, AlertArguments arguments)
		{
			AlertDialog alert = new AlertDialog.Builder(_context).Create();
			alert.SetTitle(arguments.Title);
			alert.SetMessage(arguments.Message);
			if (arguments.Accept != null)
				alert.SetButton((int)DialogButtonType.Positive, arguments.Accept, (o, args) => arguments.SetResult(true));
			alert.SetButton((int)DialogButtonType.Negative, arguments.Cancel, (o, args) => arguments.SetResult(false));
			alert.CancelEvent += (o, args) => { arguments.SetResult(false); };
			alert.Show();
		}

		void UpdateProgressBarVisibility(bool isBusy)
		{
			if (!Forms.SupportsProgress)
				return;
#pragma warning disable 612, 618

			// TODO hartez 2017/08/17 15:51:59 I don't feel great about this cast here;
			// perhaps this should be in a separate helper class
			var activity = _context as Activity;

			if (activity == null)
			{
				return;
			}

			activity.SetProgressBarIndeterminate(true);
			activity.SetProgressBarIndeterminateVisibility(isBusy);
#pragma warning restore 612, 618
		}
	}
}