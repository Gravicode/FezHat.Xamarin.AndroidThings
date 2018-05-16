package mono.com.google.android.things.update;


public class StatusListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.google.android.things.update.StatusListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onStatusUpdate:(Lcom/google/android/things/update/UpdateManagerStatus;)V:GetOnStatusUpdate_Lcom_google_android_things_update_UpdateManagerStatus_Handler:Android.Things.Update.IStatusListenerInvoker, Xamarin.Android.Things\n" +
			"";
		mono.android.Runtime.register ("Android.Things.Update.IStatusListenerImplementor, Xamarin.Android.Things, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", StatusListenerImplementor.class, __md_methods);
	}


	public StatusListenerImplementor ()
	{
		super ();
		if (getClass () == StatusListenerImplementor.class)
			mono.android.TypeManager.Activate ("Android.Things.Update.IStatusListenerImplementor, Xamarin.Android.Things, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}


	public void onStatusUpdate (com.google.android.things.update.UpdateManagerStatus p0)
	{
		n_onStatusUpdate (p0);
	}

	private native void n_onStatusUpdate (com.google.android.things.update.UpdateManagerStatus p0);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
