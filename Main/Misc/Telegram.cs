using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using VicTool.Controls;

namespace VicTool.Main.Misc
{
    public class TBot
    {
        private TelegramBotClient _botClient;
        private long _chatId = 1227438923;
        public EventHandler<string> RecievedInput;
        public EventHandler<CallbackQuery> OnCallBack;
        public bool Enabled { get; set; } = false;
        public TBot()
        {
            var botClient = new TelegramBotClient("");
            _botClient = botClient;

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
            }; 
            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: new CancellationToken()
            );
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (!Enabled)
                return;
            if (update.Type == UpdateType.CallbackQuery)
            {
                OnCallBack?.Invoke(this,update.CallbackQuery);
            }
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Type != UpdateType.Message)
                return;
            // Only process text messages
            if (update.Message?.Type != MessageType.Text)
                return;

            //var chatId = update.Message.Chat.Id;
            var messageText = update.Message.Text;
            RecievedInput?.Invoke(null,messageText);
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Com.WriteLine(exception.ToString());
            return Task.CompletedTask;
        }
        public void Write(string message, IReplyMarkup replyMarkup = null)
        {
            if (!Enabled)
                return;
            Dispatcher.CurrentDispatcher.InvokeAsync(async () =>
            {
                await _botClient.SendTextMessageAsync(
                    chatId: _chatId,
                    text: message,
                    cancellationToken: new CancellationToken(),
                    replyMarkup: replyMarkup);
            });
        }

        public void SendSnapshot(UserControl visual, string message = null)
        {
            if (!Enabled)
                return;
            Dispatcher.CurrentDispatcher.InvokeAsync(async () =>
            {
                if (message != null)
                    await _botClient.SendTextMessageAsync(
                        chatId: _chatId,
                        text: message,
                        cancellationToken: new CancellationToken());

                //var width = (int)visual.ActualWidth;
                //var height = (int)visual.ActualHeight;
                Rect bounds = VisualTreeHelper.GetDescendantBounds(visual);
                var width = (int)(bounds.Width + bounds.X) * 1;
                var height = (int)(bounds.Height + bounds.Y) * 1;
                RenderTargetBitmap renderTargetBitmap =
                    new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

                DrawingVisual drawingVisual = new DrawingVisual();
                using (DrawingContext dc = drawingVisual.RenderOpen())
                {
                    VisualBrush sourceBrush = new VisualBrush(visual);
                    dc.DrawRectangle(sourceBrush, null, new Rect(new Point(), new Size(width, height)));
                }
                renderTargetBitmap.Render(drawingVisual);

                //renderTargetBitmap.Render(visual);
                PngBitmapEncoder pngImage = new PngBitmapEncoder();
                pngImage.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
                using (var memoryStream = new MemoryStream())
                {
                    pngImage.Save(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    var photo = new InputOnlineFile(memoryStream);
                    await _botClient.SendPhotoAsync(_chatId, photo);
                }
            });

        }
    }
}
