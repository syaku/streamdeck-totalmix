// OSC Sender

using BarRaider.SdTools;
using Rug.Osc.Core;
using System;
using System.Net;
using System.Threading.Tasks;

namespace streamdeck_totalmix
{
    internal class Sender
    {
        public static Task Send(String name, Single value, IPAddress ip, Int32 port)
        {

            // 送信ごとに UDP ソケットを 1 個だけ開いて確実に閉じる。
            // 例外は呼び出し元 (300ms 周期のポーリング) を止めないよう、従来どおり握り潰してログに出す。
            try
            {
                using (OscSender sender = CreateSender(ip, port))
                {
                    sender.Connect();
                    sender.Send(new OscMessage(name, value));
                    sender.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, "Sender: Send: " + ex.Message);
            }

            return Task.CompletedTask;
        }

        // Rug.Osc.Core は Rug.Osc 1.2.8 にあった 4 引数のコンストラクタを持たず、
        // timeToLive / messageBufferSize / maxPacketSize まで明示を要求する。
        private static OscSender CreateSender(IPAddress ip, Int32 port)
        {
            return new OscSender(
                local: IPAddress.Any,
                localPort: 0,
                remote: ip,
                remotePort: port,
                timeToLive: OscSocket.DefaultMulticastTimeToLive,
                messageBufferSize: OscSender.DefaultMessageBufferSize,
                maxPacketSize: OscSocket.DefaultPacketSize);
        }
    }
}