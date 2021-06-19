using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EchoServer
{
    //MARKER:用于保存客户端信息
    class ClientState
    {
        public Socket socket;
        public byte[] readBuff = new byte[1024]; //注意这里存储的是从这个socket里获取得到的数据（byte字节数组）
    }
}
