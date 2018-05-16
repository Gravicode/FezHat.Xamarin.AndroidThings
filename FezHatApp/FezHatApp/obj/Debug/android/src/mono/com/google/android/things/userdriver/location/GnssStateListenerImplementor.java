package mono.com.google.android.things.userdriver.location;


public class GnssStateListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.google.android.things.userdriver.location.GnssStateListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onSetEnabled:(Z)V:GetOnSetEnabled_ZHandler:Android.Things.UserDriver.Location.IGnssStateListenerInvoker, Xamarin.Android.Things\n" +
			"";
		mono.android.Runtime.register ("Android.Things.UserDriver.Location.IGnssStateListenerImplementor, Xamarin.Android.Things, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", GnssStateListenerImplementor.class, __md_methods);
	}


	public GnssStateListenerImplementor ()
	{
		super ();
		if (getClass () == GnssStateListenerImplementor.class)
			mono.android.TypeManager.Activate ("Android.Things.UserDriver.Location.IGnssStateListenerImplementor, Xamarin.Android.Things, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}


	public void onSetEnabled (boolean p0)
	{
		n_onSetEnabled (p0);
	}

	private native void n_onSetEnabled (boolean p0);

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
