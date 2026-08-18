using BarRaider.SdTools;
using BarRaider.SdTools.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace streamdeck_totalmix
{
    [PluginActionId("de.shells.totalmix.oscchanneldial.action")]
    public class OscChannelDial : EncoderBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    Name = "/1/volume1",
                    SelectedAction = "1",
                    Bus = "Input",
                    SelectedValue = String.Empty,
                    ChannelCount = Globals.channelCount
                };
                return instance;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "Name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "SelectedAction")]
            public string SelectedAction { get; set; }

            [JsonProperty(PropertyName = "Bus")]
            public string Bus { get; set; }

            [JsonProperty(PropertyName = "SelectedValue")]
            public string SelectedValue { get; set; }

            [JsonProperty(PropertyName = "ChannelCount")]
            public Int32 ChannelCount { get; set; }
        }

        #region Private Members

        // ミラーは TotalMix が返した Single の ToString で、こちらは Decimal を Single へ丸めて送っている。
        // 厳密一致は往復で成立しないので、1 tick (0.02) の 1/4 までを同じ値とみなす。
        private const Decimal MirrorTolerance = 0.005M;

        // 不一致がこの回数続いたらキャッシュを捨てて次回転でミラーを基準に取り直す。
        // OnTick は約 1 秒周期でミラーの 1 周回 (約 357 ms) より粗いため、2 回でミラー 2 周回ぶんを超える。
        private const Int32 MirrorMismatchLimit = 2;

        private PluginSettings settings;

        // 最後に自分が送った値。ミラーの周回待ちで基準が巻き戻らないよう回転の基準に優先して使う。
        private Decimal? lastSent;

        private Int32 mirrorMismatchTicks;

        #endregion

        public OscChannelDial(ISDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
                Connection.SetSettingsAsync(JObject.FromObject(settings));

                Logger.Instance.LogMessage(TracingLevel.INFO, $"OscChannelDial: Settings initially set: {this.settings}");
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
                if (!payload.Settings.ContainsKey("ChannelCount") || this.settings.ChannelCount != Globals.channelCount)
                {
                    this.settings.ChannelCount = Globals.channelCount;
                    Connection.SetSettingsAsync(JObject.FromObject(settings));

                    Logger.Instance.LogMessage(TracingLevel.INFO, $"OscChannelDial: Channel Count set to: {Globals.channelCount}");
                }
            }
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "OscChannelDial: Destructor called");
        }

        public override void DialRotate(DialRotatePayload payload)
        {
            try
            {
                if (!TryGetBank(out Dictionary<String, String> bank) || !TryReadVolume(bank, out Decimal mirrorVolume))
                {
                    ShowSyncing();
                    return;
                }

                Decimal next = VolumeMath.NextValue(lastSent ?? mirrorVolume, payload.Ticks, Multiplier());

                Sender.Send($"/1/bus{this.settings.Bus}", 1, Globals.interfaceIp, Globals.interfacePort);
                Sender.Send(this.settings.Name, (Single)next, Globals.interfaceIp, Globals.interfacePort);

                lastSent = next;
                mirrorMismatchTicks = 0;
                ShowChannel(bank, next);

                Logger.Instance.LogMessage(TracingLevel.INFO, $"OscChannelDial: Set Volume: {this.settings.Name} {(Single)next}");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"OscChannelDial: DialRotate => {ex.Message}");
            }
        }

        public override void DialDown(DialPayload payload)
        {
            try
            {
                String muteAddress = $"/1/mute/1/{ChannelNumber()}";
                if (!TryGetBank(out Dictionary<String, String> bank) || !bank.TryGetValue(muteAddress, out String muteValue))
                {
                    ShowSyncing();
                    return;
                }

                Single target = muteValue == "1" ? 0 : 1;

                Sender.Send($"/1/bus{this.settings.Bus}", 1, Globals.interfaceIp, Globals.interfacePort);
                Sender.Send(muteAddress, target, Globals.interfaceIp, Globals.interfacePort);

                Logger.Instance.LogMessage(TracingLevel.INFO, $"OscChannelDial: Set Mute: {muteAddress} {target}");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"OscChannelDial: DialDown => {ex.Message}");
            }
        }

        public override void DialUp(DialPayload payload) { }

        public override void TouchPress(TouchpadPressPayload payload) { }

        public override void OnTick()
        {
            try
            {
                if (!TryGetBank(out Dictionary<String, String> bank) || !TryReadVolume(bank, out Decimal mirrorVolume))
                {
                    ShowSyncing();
                    return;
                }

                if (lastSent.HasValue)
                {
                    if (Math.Abs(mirrorVolume - lastSent.Value) > MirrorTolerance)
                    {
                        mirrorMismatchTicks++;
                        if (mirrorMismatchTicks >= MirrorMismatchLimit)
                        {
                            lastSent = null;
                            mirrorMismatchTicks = 0;
                        }
                    }
                    else
                    {
                        mirrorMismatchTicks = 0;
                    }
                }

                ShowChannel(bank, lastSent ?? mirrorVolume);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"OscChannelDial: OnTick => {ex.Message}");
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);

            // キャッシュは特定のアドレスに紐付くので、チャンネルやバスが変わったら捨てる。
            lastSent = null;
            mirrorMismatchTicks = 0;

            Logger.Instance.LogMessage(TracingLevel.INFO, $"OscChannelDial: Settings loaded: {payload.Settings}");
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        private Boolean TryGetBank(out Dictionary<String, String> bank)
        {
            bank = null;
            if (!Globals.mirroringRequested || !Globals.backgroundConnection)
            {
                return false;
            }
            if (this.settings.Bus == null || this.settings.Name == null)
            {
                return false;
            }
            if (!Globals.bankSettings.TryGetValue(this.settings.Bus, out bank))
            {
                return false;
            }
            // ミラーがそのバスを採れているかの鮮度チェック。前送信とは役割が違う。
            return bank.TryGetValue($"/1/bus{this.settings.Bus}", out String busValue) && busValue == "1";
        }

        private Boolean TryReadVolume(Dictionary<String, String> bank, out Decimal value)
        {
            value = 0M;
            if (!bank.TryGetValue(this.settings.Name, out String raw))
            {
                return false;
            }
            // ミラーの値は同じプロセスの Single.ToString() 由来なので、Invariant ではなく現在のカルチャで読む。
            return Decimal.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private Int32 Multiplier()
        {
            // 空欄や非数は 0 として渡す。VolumeMath が 0 を 1 倍として扱う。
            return Int32.TryParse(this.settings.SelectedValue, out Int32 value) ? value : 0;
        }

        private Int32 ChannelNumber()
        {
            if (!Int32.TryParse(this.settings.SelectedAction, out Int32 selected))
            {
                return 0;
            }
            if (selected >= 1 && selected <= Globals.channelCount)
            {
                return selected;
            }
            if (selected > Globals.channelCount && selected <= Globals.channelCount * 2)
            {
                return selected - Globals.channelCount;
            }
            if (selected > Globals.channelCount * 2 && selected <= Globals.channelCount * 3)
            {
                return selected - Globals.channelCount * 2;
            }
            return 0;
        }

        private void ShowChannel(Dictionary<String, String> bank, Decimal value)
        {
            Int32 channel = ChannelNumber();
            bank.TryGetValue($"/1/trackname{channel}", out String trackname);
            Boolean muted = bank.TryGetValue($"/1/mute/1/{channel}", out String muteValue) && muteValue == "1";
            String display = VolumeMath.ToDisplay(value).ToString(CultureInfo.InvariantCulture);

            Connection.SetFeedbackAsync(new Dictionary<String, String>
            {
                { "title", trackname ?? String.Empty },
                { "value", muted ? "MUTE" : display },
                { "indicator", display }
            });
        }

        private void ShowSyncing()
        {
            Connection.SetFeedbackAsync(new Dictionary<String, String>
            {
                { "title", String.Empty },
                { "value", "syncing..." },
                { "indicator", "0" }
            });
        }

        #endregion
    }
}
