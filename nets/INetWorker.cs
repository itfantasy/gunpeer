using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace itfantasy.nodepeer.nets
{
    public interface INetWorker
    {
	    void Connect(string url, string tag);
	    void Send(byte[] msg);
	    void SendAsync(byte[] msg, Action<bool> callback=null);
	    error BindEventListener(INetEventListener eventListener);
        void Close();
    }
}
