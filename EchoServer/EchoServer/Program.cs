using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EchoServer
{
    //MARKER:记得配合Unity的AboutSocket食用，包含了同步和异步的方法，目前所用的全部方法都为异步
    class Program
    {
        //监听Socket
        static Socket listenfd;
        //客户端Socket以及状态信息
        static Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();
        static void Main(string[] args)
        {
            
            Console.WriteLine("Hello World");
            //MARKER:Socket,创建套接字的过程
            //第一个参数是获得IP地址的地址族(IPv4返回InterNetwork，对于IPv6返回InterNetworkV6)
            //第二个参数指定传递的Socket类型，Stream为字节流类型（TCP），而Dgram为数据报类型（UDP）。
            listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //Bind (将字符串转化为IPAddress实例)
            IPAddress ipAdr = IPAddress.Parse("127.0.0.1");
            //将网络端终结点表示为IP地址以及端口号
            IPEndPoint ipEp = new IPEndPoint(ipAdr, 8888);
            //将套接字绑定到对应的网络端终结点上
            listenfd.Bind(ipEp);
            //Listen(参数为挂起的连接队列的最大长度),设置listenfd为监听状态
            listenfd.Listen(0);
            Console.WriteLine("[服务器]启动成功");
            //Accept(异步状态)
            listenfd.BeginAccept(AcceptCallback, listenfd);
            //等待
            Console.ReadLine();
            //while (true)
            //{
            //    //Accpet(阻塞状态)
            //    Socket connfd = listenfd.Accept();
            //    Console.WriteLine("[服务器]Accept");
            //    //Receive
            //    //接收的缓冲字节数组（大小为1024）
            //    byte[] readBuff = new byte[1024];
            //    //其返回值为获取得到的字节
            //    int count = connfd.Receive(readBuff);
            //    //注意这个GetString和后续的GetBytes方法对于位数组和字符串之间的转换
            //    string readStr = System.Text.Encoding.Default.GetString(readBuff,0,count);
            //    Console.WriteLine("[服务器接收]" + readStr);
            //    //send

            //    byte[] sendBytes = System.Text.Encoding.Default.GetBytes(DateTime.Now.ToString()+readStr);
            //    connfd.Send(sendBytes);
            //}

        }
        public static void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Console.WriteLine("[服务器]Accept");
                //在回调函数中获取传入的Socket信息
                Socket socket= (Socket)ar.AsyncState;
                //结束异步接收，并创建一个新的Socket用于处理交流的信息
                Socket clientfd = listenfd.EndAccept(ar);
                //clients
                ClientState state = new ClientState();
                state.socket = clientfd;
                //将ClientState加入到clients字典中
                clients.Add(clientfd, state);
                //接收数据
                clientfd.BeginReceive(state.readBuff, 0, 1024, 0, ReceiveCallback, state);
                //继续Accept
                socket.BeginAccept(AcceptCallback, socket);
                Console.WriteLine(clientfd.RemoteEndPoint + " connect");
            }catch(SocketException ex)
            {
                Console.WriteLine("Socket Accept fail" + ex.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                ClientState state = (ClientState)ar.AsyncState;
                Socket clientfd = state.socket;
                //count记录下获取的消息的字节数
                int count = state.socket.EndReceive(ar);
                //客户端关闭
                if (count == 0)
                {
                    clientfd.Close();
                    clients.Remove(clientfd);
                    Console.WriteLine($"{clientfd}客户端的Socket关闭");
                    return;
                }
                string recvStr = System.Text.Encoding.Default.GetString(state.readBuff, 0, count);
                Console.WriteLine("收到消息" + recvStr);
                byte[] sendBytes = System.Text.Encoding.Default.GetBytes($"{clientfd.RemoteEndPoint}: {recvStr}" );
                //MARKER:如果是想广播的话这里要设置对所有添加入clients的ClientState都发送信息
                foreach(ClientState cs in clients.Values)
                {
                    //注意这里的长度要根据sendBytes的实际长度（如果直接写1024的话会产生越界）
                    cs.socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, SendCallback, cs);
                }
                //用得到的消息沟通的Socket继续进行后续的消息获取
                clientfd.BeginReceive(state.readBuff, 0, 1024, 0, ReceiveCallback, state);
            }catch(SocketException ex)
            {
                Console.WriteLine("Socket Receive fail" + ex.ToString());
            }
            
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                ClientState state = (ClientState)ar.AsyncState;
                Socket clientfd = state.socket;
                int count = clientfd.EndSend(ar);
                Console.WriteLine("server send succ:" + count);
            }catch(SocketException ex)
            {
                Console.WriteLine("Socket Send fail" + ex.ToString());
            }
        }
    }
}
