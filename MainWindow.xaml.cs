using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace Client
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //номер порта для обмена сообщениями
        int port = 8888;
        //ip адрес сервера
        string address = "127.0.0.1";
        //объявление TCP клиента
        TcpClient client = null;
        //объявление канала соединения с сервером
        NetworkStream stream = null;
        //имя пользователя
        string userName = "";

        public MainWindow()
        {
            InitializeComponent();
            this.Closed += MainWindow_Closed;
        }
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            DisconnectClient();
        }

        private void ToConnect(object sender, RoutedEventArgs e)
        {
            //получение имени пользователя
            userName = name.Text;
            try //если возникнет ошибка - переход в catch
            {
                //создание клиента
                client = new TcpClient(address, port);
                //получение канала для обмена сообщениями
                stream = client.GetStream();
                //создание нового потока для ожидания сообщения от сервера
                Thread listenThread = new Thread(() => listen());
                listenThread.Start();
                Dispatcher.BeginInvoke(new Action(() => log.Items.Add("Вы подключены к серверу!")));
                ToSendButton.IsEnabled = true;
                DiscToServ.IsEnabled = true;
                ConnToServ.IsEnabled = false;
            }
            catch (Exception ex)
            {
                log.Items.Add(ex.Message);
            }
        }

        private void ToSend(object sender, RoutedEventArgs e)
        {
            try
            {
                if (stream != null)
                {
                    //получение сообщения
                    string message = msg.Text;
                    //добавление имени пользователя к сообщению
                    message = String.Format("{0}: {1}", userName, message);
                    //преобразование сообщение в массив байтов
                    byte[] data = Encoding.Unicode.GetBytes(message);
                    //отправка сообщения
                    stream.Write(data, 0, data.Length);
                    msg.Text = "";
                }
                else
                {
                    MessageBox.Show("Вы не подключены к серверу!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                DisconnectClient();
            }

        }

        private void DisconnectClient()
        {
            ConnToServ.IsEnabled = true;
            DiscToServ.IsEnabled = false;
            stream?.Close();
            stream = null;
            client?.Close();
            client = null;
        }

        //функция ожидания сообщений от сервера
        void listen()
        {
            try //в случае возникновения ошибки - переход к catch
            {
                //цикл ожидания сообщениями
                while (true)
                {
                    //буфер для получаемых данных
                    byte[] data = new byte[64];
                    //объект для построения смтрок
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    //до тех пор, пока есть данные в потоке
                    do
                    {
                        //получение 64 байт
                        bytes = stream.Read(data, 0, data.Length);
                        //формирование строки
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (stream.DataAvailable);
                    if (bytes == 0)
                    {
                        MessageBox.Show("Сервер разорвал подключение!");
                        Dispatcher.BeginInvoke(new Action(() => DisconnectClient()));
                        return;
                    }
                    //получить строку
                    string message = builder.ToString();
                    //вывод сообщения в лог клиента
                    Dispatcher.BeginInvoke(new Action(() => log.Items.Add("Сервер: " + message)));
                }
            }
            catch (Exception ex)
            {
                //вывести сообщение об ошибке
                MessageBox.Show(ex.Message);
                Dispatcher.BeginInvoke(new Action(() => DisconnectClient()));
            }
        }

        private void ToDisconnect(object sender, RoutedEventArgs e)
        {
            DisconnectClient();
            Dispatcher.BeginInvoke(new Action(() => log.Items.Add("Вы покинули сервер!")));
            ToSendButton.IsEnabled = false;
            DiscToServ.IsEnabled = false;
            ConnToServ.IsEnabled = true;
        }
    }
}
