using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Common;
using Prism.Events;
using Serilog;
using St.Common;
using St.Common.Commands;

namespace St.CommandListener
{
    public class ServerSocket : ICommandServer
    {
        private Socket _server;
        private readonly IEventAggregator _eventAggregator;

        public IPAddress Address { get; set; }
        public int Port { get; set; }
        public List<Socket> Clients { get; set; }

        public ServerSocket()
        {
            Clients = new List<Socket>();
            _eventAggregator = IoC.Get<IEventAggregator>();

            _eventAggregator.GetEvent<CommandUpdatedEvent>().Subscribe(PushCommandsToClients);
        }

        public async Task RunServer()
        {
            //Address = IPAddress.Parse("172.16.128.74");
            Address = IPAddress.Any;
            Port = GlobalData.Instance.AggregatedConfig.CommandServerPort;

            IPEndPoint ipEndPoint = new IPEndPoint(Address, Port);
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            await Task.Run(() =>
            {
                try
                {
                    _server.Bind(ipEndPoint);
                    _server.Listen(10);

                    ReceiveCommand();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);

                    Log.Logger.Error($"RunServer => {ex}");
                    MessageQueueManager.Instance.AddInfo("启动命令监听服务器失败！");
                }
            });
        }

        private void ReceiveCommand()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        Socket newSocket = _server.Accept();

                        PushCommandToClient(newSocket);

                        Clients.Add(newSocket);

                        ProcessNewSocket(newSocket);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        Log.Logger.Error($"ReadCommand => {ex}");
                    }
                }
            });
        }

        private void ProcessNewSocket(Socket newSocket)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    byte[] dataBytes = new byte[1024];

                    try
                    {
                        var length = newSocket.Receive(dataBytes);

                        if (length == 0)
                        {
                            continue;
                        }

                        string command = Encoding.UTF8.GetString(dataBytes, 0, length);

                        CommandRepeater.Instance.TransmitCommand(int.Parse(command));
                    }
                    catch (Exception ex)
                    {
                        Clients.Remove(newSocket);
                        newSocket.Close();
                        Console.WriteLine(ex);
                        Log.Logger.Error($"ProcessNewSocket => {ex}");
                        break;
                    }
                }
            });
        }

        public void StopServer()
        {
            try
            {
                _server.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Log.Logger.Error($"StopServer => {ex}");
                MessageQueueManager.Instance.AddInfo("停止命令监听服务器失败！");
            }
        }

        private void PushCommandToClient(Socket newSocket)
        {
            string latestCommands = GlobalCommands.Instance.GetCurrentCommands();

            byte[] bytes = Encoding.UTF8.GetBytes(latestCommands);

            try
            {
                newSocket.Send(bytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Log.Logger.Error($"PushCommandToClient => {ex}");
            }
        }

        private void PushCommandsToClients()
        {
            if (Clients.Count <= 0)
            {
                return;
            }

            string latestCommands = GlobalCommands.Instance.GetCurrentCommands();

            Console.WriteLine($"AppActivated：{GlobalCommands.Instance.GetAppActivatedState()}");
            Console.WriteLine($"latestCommands：{latestCommands}");
            Console.WriteLine();

            byte[] bytes = Encoding.UTF8.GetBytes(latestCommands);

            try
            {
                foreach (var client in Clients)
                {
                    client.Send(bytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Log.Logger.Error($"PushCommandsToClients => {ex}");
            }
        }
    }
}